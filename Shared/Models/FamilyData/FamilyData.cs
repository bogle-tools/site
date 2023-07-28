using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Serialization;

public class FamilyData
{
    public FamilyData(IAppData appData) {
        AppData = appData;
        Year = DateTime.Now.Year;

        Debts = new();
        Accounts = new();
        People = new();
        Questions = new();
        RetirementData = new();
        
        People.Add(new Person() { Identifier = "person 1", FamilyData = this, PersonIndex = 0 });
        People.Add(new Person() { Identifier = "person 2", FamilyData = this, PersonIndex = 1 });

        SetBackPointers();
    }

    // set back pointers in familydata
    public void SetBackPointers()
    {
        for (int personIndex = 0; personIndex < this.People.Count; personIndex++) {
            var person = this.People[personIndex];
            person.PersonIndex = personIndex;
            person.SetBackPointers(this);
        }
    }

    [JsonIgnore]
    public double PercentTotal { get; set; }
    
    [JsonIgnore]
    public IAppData AppData { get; set;}    

    public string Title { get; set; }
    public RetirementData RetirementData { get; set; }
    public EmergencyFund EmergencyFund { get; set; } = new();


    public bool DebtsComplete {
        get {
            return ((Debts.Count == 0 && DebtFree == TriState.True) || Debts.Count > 0);
        }
    }
    
    public string RetirementIncomeNeeded {
        get {
            string outStr = "";
            double incomeNeeded = 0.0;
            double inflationAffectedIncome = 0.0;
            double portfolioRunningBalance = Value + (EmergencyFund.CurrentBalance ?? 0.0);
            int yearIndex = 0;
            bool done = false;
            bool?[] retired = { null, null };

            for (var i = 0; i < PersonCount; i++) {
                var ageThisYear = People[i].Age + yearIndex;
                if (People[i].Retirement.RetirementAge < ageThisYear) {
                    retired[i] = true;
                    incomeNeeded = RetirementData.AnnualExpenses;
                } else {
                    retired[i] = false;
                }

                if (People[i].Retirement.SSAnnual.HasValue && (People[i].Retirement.SSAge < ageThisYear)) {
                    incomeNeeded -= People[i].Retirement.SSAnnual.Value;
                }

                foreach (var pension in People[i].Retirement.Pensions) {
                    if (pension.BeginningAge < ageThisYear) {
                        if (!pension.OneTime) {
                            if (pension.HasCola) {
                                incomeNeeded -= pension.Income;
                            } else {
                                inflationAffectedIncome -= pension.Income;
                            }
                        }                        
                    }
                }
            }
            
            bool?[] forecastDone = { null, null };
            while (!done) {
                double adjustBack = 0.0;
                string significantYear = null;
                string yearNote = null;
                for (var i = 0; i < PersonCount; i++) {
                    var ageThisYear = People[i].Age + yearIndex;
                    forecastDone[i] = People[i].Retirement.ForecastEndAge <= ageThisYear;
                    if (People[i].Retirement.RetirementAge == ageThisYear) {
                        // TODO: what happens when ages don't both retire in same year? (1/2 income for both now)
                        retired[i] = true;
                        incomeNeeded += RetirementData.AnnualExpenses / PersonCount;
                        significantYear += (significantYear != null ? ", " : "") + "retirement (" + People[i].Identifier + ")";
                    }
                    if (People[i].Retirement.RetirementAge > ageThisYear) {
                        incomeNeeded -= (this.PlannedSavings ?? 0.0) / PersonCount;
                        adjustBack += (this.PlannedSavings ?? 0.0) / PersonCount;
                    }

                    if (People[i].Retirement.SSAnnual.HasValue && (People[i].Retirement.SSAge == ageThisYear)) {
                        incomeNeeded -= People[i].Retirement.SSAnnual.Value;
                        significantYear += (significantYear != null ? ", " : "") + "social security ("+People[i].Identifier+")";
                    }

                    foreach (var pension in People[i].Retirement.Pensions)
                    {
                        if (pension.BeginningAge == ageThisYear) {
                            if (pension.OneTime) {
                                portfolioRunningBalance += pension.Income;
                                yearNote += " "+(pension.Title != null?pension.Title:"adj.")+" (1 time)";
                            }
                            else
                            {
                                if (pension.HasCola) {
                                    incomeNeeded -= pension.Income;
                                } else {
                                    inflationAffectedIncome -= pension.Income;
                                }
                            
                                significantYear += (significantYear != null ? "; " : "") + (pension.Title != null?pension.Title:"adj.");
                            }                        
                        }
                    }
                }
                
                int year = yearIndex + DateTime.Now.Year;
                foreach (var incomeExpense in this.RetirementData.IncomeExpenses) {
                    if (incomeExpense.BeginningYear == year) {
                        if (incomeExpense.OneTime) {
                            portfolioRunningBalance += incomeExpense.Income;
                            yearNote += " "+(incomeExpense.Title != null?incomeExpense.Title:"adj.")+" (1 time)";
                        }
                        else {
                            if (incomeExpense.HasCola) {
                                incomeNeeded -= incomeExpense.Income;
                            } else {
                                inflationAffectedIncome -= incomeExpense.Income;
                            }

                            significantYear += (significantYear != null ? "; " : "") + (incomeExpense.Title != null?incomeExpense.Title:"adj.");
                        }
                    }
                }

                done = forecastDone[0].Value && (forecastDone[1] == null || forecastDone[1].Value);

                if (retired[0].Value || (retired[1] == null || retired[1].Value)) {
                    if (significantYear != null) {
                        outStr += "<b>========= "+significantYear+"</b><br/>";
                    }

                    outStr += (yearIndex+DateTime.Now.Year) + " " + FormatUtilities.formatMoneyThousands(+incomeNeeded+inflationAffectedIncome) + " " + FormatUtilities.formatPercent((incomeNeeded+inflationAffectedIncome)/portfolioRunningBalance*100.0) +"<b>" + (yearNote!=null?" &lt;== ":"") + yearNote + "</b><br/>";
                }
                
                inflationAffectedIncome *= .97;

                yearIndex++;
                incomeNeeded += adjustBack;
            }

            return outStr;
        }
    }

    public TriState DebtFree { get; set; }
    public List<Debt> Debts { get; set; }
    
    private TaxFilingStatus _taxFilingStatus = TaxFilingStatus.Single;
    [Required]
    public TaxFilingStatus TaxFilingStatus { 
        get {
            return _taxFilingStatus;
        }
        set {
            _taxFilingStatus = value;
        }
    }

    public string TaxFilingString {
        get {
            return TaxFilingStatus switch
            {
                TaxFilingStatus.Single => "Single",
                TaxFilingStatus.MarriedFilingJointly => "Married filing jointly",
                TaxFilingStatus.MarriedFilingSeperately => "Married filing separately",
                TaxFilingStatus.MarriedFilingSeperatelyAndLivingApart => "Married filing separately (and living apart)",
                _ => "Choice needed",
            };
        }
    }

    public int? AdjustedGrossIncome { get; set; }
    public int? IncomeTaxPaid { get; set; }
    public int? TaxableToInvest { get; set; }
    public int? PlannedSavings { 
        get {
            int? annualExpenses = EmergencyFund.MonthlyExpenses * 12;
            int investFromTaxable = TaxableToInvest ?? 0;
            int totalSalaries = 0;
            for (int i = 0; i < PersonCount; i++)
            {
                var person = People[i];
                if (person.EmployerPlan.AnnualSalary != null)
                {
                    totalSalaries += person.EmployerPlan.AnnualSalary.Value;
                }
            }
            
            return totalSalaries - (IncomeTaxPaid ?? 0) - annualExpenses + investFromTaxable;
        }
    }
    public string? FederalMarginalTaxBracket { get; set; }
    private int _Year;
    public int Year { 
        get {
            return _Year;
        }
        set {
            _Year = value;
        }
    }
    public string? StateMarginalTaxBracket { get; set; }
    public string? StateOfResidence { get; set; }

    public double? Stocks { get; set; }
    public double? Bonds { get; set; }
    public int? International { get; set; }

    public double? StockBalance { get; set; }
    public double? InternationalStockBalance { get; set; }
    public double? BondBalance { get; set; }
    public double? CashBalance { get; set; }
    public double? OtherBalance { get; set; }

    public double ActualStockAllocation {
        get {
            var overallTotal = (StockBalance ?? 0.0) + (InternationalStockBalance ?? 0.0) + (BondBalance ?? 0.0) + (OtherBalance ?? 0.0) + (CashBalance ?? 0.0);
            if (overallTotal > 0.0)
            {
                return ( (StockBalance ?? 0.0) + (InternationalStockBalance ?? 0.0) ) / overallTotal;
            }
            else
            {
                return double.NaN;
            }
        }
    }

    public double ActualInternationalStockAllocation {
        get {
            var stockTotal = (StockBalance ?? 0.0) + (InternationalStockBalance ?? 0.0);
            if (stockTotal > 0.0)
            {
                return (InternationalStockBalance ?? 0.0) / stockTotal;
            }
            else
            {
                return double.NaN;
            }
        }
    }

    public double OverallER { get; set; }
    public int InvestmentsMissingER { get; set; }
    public double ExpensesTotal { get; set; }
    
    public double ActualBondAllocation {
        get {
            var overallTotal = (StockBalance ?? 0.0) + (InternationalStockBalance ?? 0.0) + (BondBalance ?? 0.0) + (OtherBalance ?? 0.0) + (CashBalance ?? 0.0);

            if (overallTotal > 0.0)
            {
                return (BondBalance ?? 0.0) / overallTotal;
            }
            else
            {
                return double.NaN;
            }
        }
    }

    public double ActualCashAllocation {
        get {
            var overallTotal = (StockBalance ?? 0.0) + (InternationalStockBalance ?? 0.0) + (BondBalance ?? 0.0) + (OtherBalance ?? 0.0) + (CashBalance ?? 0.0);

            if (overallTotal > 0.0)
            {
                return (CashBalance ?? 0.0) / overallTotal;
            }
            else
            {
                return double.NaN;
            }
        }
    }

    public double ActualOtherAllocation {
        get {
            var overallTotal = (StockBalance ?? 0.0) + (InternationalStockBalance ?? 0.0) + (BondBalance ?? 0.0) + (OtherBalance ?? 0.0) + (CashBalance ?? 0.0);

            if (overallTotal > 0.0)
            {
                return (OtherBalance ?? 0.0) / overallTotal;
            }
            else
            {
                return double.NaN;
            }
        }
    }
    public List<Person> People { get; set; }

    public double Value { 
        get {
            double newValue = 0;
            foreach (var account in Accounts) 
            {
                newValue += account.Value;
            }
            
            return newValue;
        }
    }

    public int ValueStyle { get; set; } = 0;

    public List<Account> Accounts { get; set; }

    private int? _PIN;
    [JsonIgnore]
    public int? PIN { 
        get { return _PIN; }
        set {
            _PIN = value; 
            foreach (var account in Accounts) {
                account.PIN = _PIN;
            }
        }    
    }

    public bool PINProtected { get; set; }

    [JsonIgnore]
    public List<Investment> GroupedInvestments { 
        get {
            Dictionary<string,Investment> _GroupedInvestments = new();
            foreach (var account in Accounts) 
            {
                foreach (var investment in account.Investments)
                {
                    var key = string.IsNullOrEmpty(investment.Ticker) ? investment.Name ?? Random.Shared.ToString(): investment.Ticker;
                    Investment? matchingInvestment;
                    if (!_GroupedInvestments.ContainsKey(key))
                    {
                        matchingInvestment = new Investment(PIN) { Name = investment.Name, AssetType = investment.AssetType, Ticker = investment.Ticker, PercentChange = investment.PercentChange, LastUpdated = investment.LastUpdated, SharesPIN = null, Price = investment.Price, PreviousClose = investment.PreviousClose, ValuePIN = 0.0 };
                        _GroupedInvestments.Add(key, matchingInvestment);
                    }
                    else
                    {
                        matchingInvestment = _GroupedInvestments[key];
                    }

                    if (investment.SharesPIN != null) {
                        if (matchingInvestment.SharesPIN == null)
                        {
                            matchingInvestment.SharesPIN = investment.SharesPIN;
                        }
                        else
                        {
                            matchingInvestment.SharesPIN += investment.SharesPIN;
                        }
                    } 
                    
                    if (investment.ValuePIN != null) {
                        matchingInvestment.ValuePIN += investment.ValuePIN;
                    }
                }
            }

            var listInvestments = new List<Investment>();
            foreach (var key in _GroupedInvestments.Keys)
            {
                var investment = _GroupedInvestments[key];
                if (investment.Price != null && investment.SharesPIN != null)
                {
                    investment.UpdateValue();
                }

                if (investment.SharesPIN == 0.0)
                {
                    investment.SharesPIN = null;
                }

                listInvestments.Add(investment);
            }
            
            return listInvestments.OrderByDescending(i=>i.Value).ToList();
        }
    }

    public string? AdditionalBackground { get; set; }
    public List<string> Questions { get; set; }

    private int? GetDebts(string? category) {
            int? total = null;
            foreach (var debt in Debts) {
                if (debt.Category == category) {
                    if (total == null) {
                        total = debt.Total;
                    } else {
                        total += debt.Total;
                    }
                }
            }

            return total;
    }

    public int? HighDebts {
        get {
            return GetDebts("High");
        }
    }

    public int? MediumDebts {
        get {
            return GetDebts("Medium");
        }
    }

    public int? LowDebts {
        get {
            return GetDebts("Low");
        }
    }

    public int? UnknownDebts {
        get {
            return GetDebts(null);
        }
    }

    public void UpdatePercentages()
    {
        StockBalance = 0.0;
        InternationalStockBalance = 0.0;
        BondBalance = 0.0;
        CashBalance = 0.0;
        OtherBalance = 0.0;
        OverallER = 0.0;
        InvestmentsMissingER = 0;
        ExpensesTotal = 0;

        double totalValue = this.Value;
        foreach (var account in Accounts)
        {
            account.Percentage = account.Value / totalValue * 100;
            account.UpdatePercentages(totalValue, this);
        }
    }

    public int YearIndex 
    {
        get {
            return Year - 2022;
        }
    }

    public int PersonCount {
        get {
            switch (TaxFilingStatus) {
                case TaxFilingStatus.Single:
                case TaxFilingStatus.HeadOfHousehold:
                case TaxFilingStatus.MarriedFilingSeperately:
                case TaxFilingStatus.MarriedFilingSeperatelyAndLivingApart:
                    return 1;
                case TaxFilingStatus.MarriedFilingJointly:
                    return 2;
                case TaxFilingStatus.ChoiceNeeded:
                default:
                    return 0;
            }
        } 
    }

    public string SaveToJson()
    {
        var options = new JsonSerializerOptions() 
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault,
            IgnoreReadOnlyProperties = true,
            WriteIndented = true,
            Converters =
            {
                new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
            }
        };

        var jsonOut = JsonSerializer.Serialize(this, options);

        return jsonOut;
    }
    public static async Task<FamilyData?> LoadFromJson(IAppData appData, string json, JsonSerializerOptions options) {
        var loadedData = JsonSerializer.Deserialize<FamilyData>(json, options);
        if (loadedData != null) {
            loadedData.AppData = appData;
            loadedData.Year = 2023;
            loadedData.SetBackPointers();
            return loadedData;
        }
        else 
        {
            return null;
        }
    }
    
    public static async Task<FamilyData?> LoadFromJsonStream(IAppData appData, Stream jsonStream, JsonSerializerOptions options) {
        var loadedData = await JsonSerializer.DeserializeAsync<FamilyData>(jsonStream, options);
        if (loadedData != null && appData != null) {
            loadedData.AppData = appData;
            loadedData.Year = 2023;
            loadedData.SetBackPointers();
            return loadedData;
        }
        else 
        {
            return null;
        }
    }
}