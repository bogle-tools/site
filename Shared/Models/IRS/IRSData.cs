using System.Text.Json;

namespace IRS {
    public class IRSData 
    {
        public async static Task<IRSData?> Create(HttpClient httpClient) 
        {
            IRSData? irsData = null;

            var retirementYear1 = await httpClient.GetStreamAsync("https://raw.githubusercontent.com/bogle-tools/financial-variables/main/data/usa/irs/irs.retirement.2022.json");
            var retirementYear2 = await httpClient.GetStreamAsync("https://raw.githubusercontent.com/bogle-tools/financial-variables/main/data/usa/irs/irs.retirement.2023.json");
            var taxRatesYear1 = await httpClient.GetStreamAsync("https://raw.githubusercontent.com/bogle-tools/financial-variables/main/data/usa/irs/irs.tax-rates.2022.json");
            var taxRatesYear2 = await httpClient.GetStreamAsync("https://raw.githubusercontent.com/bogle-tools/financial-variables/main/data/usa/irs/irs.tax-rates.2023.json");
            RetirementData? retirementDataY1 = await JsonSerializer.DeserializeAsync<IRS.RetirementData>(retirementYear1);
            RetirementData? retirementDataY2 = await JsonSerializer.DeserializeAsync<IRS.RetirementData>(retirementYear2);
            TaxRateData? taxRatesY1 = await JsonSerializer.DeserializeAsync<IRS.TaxRateData>(taxRatesYear1);
            TaxRateData? taxRatesY2 = await JsonSerializer.DeserializeAsync<IRS.TaxRateData>(taxRatesYear2);
            if (retirementDataY1 != null && retirementDataY2 != null && taxRatesY1 != null && taxRatesY2 != null) {
                irsData = new IRSData(retirementDataY1, retirementDataY2, taxRatesY1, taxRatesY2);
            }

            return irsData;
        }

        public IRSData(RetirementData retirementDataY1, RetirementData retirementDataY2, TaxRateData taxRatesY1, TaxRateData taxRatesY2)
        {
            RetirementDataY1 = retirementDataY1;
            RetirementDataY2 = retirementDataY2;
            TaxRateDataY1 = taxRatesY1;
            TaxRateDataY2 = taxRatesY2;
        }

        public int Year { get; set; }

        public int YearIndex 
        {
            get {
                return Year - 2022;
            }
        }

        public RetirementData RetirementData
        {
            get {
                if (YearIndex == 1) {
                    return RetirementDataY2;
                } else if (YearIndex == 0) {
                    return RetirementDataY1;
                } else {
                    throw new InvalidDataException("year is not supported");
                }
            }
        }
        public TaxRateData TaxRateData
        {
            get {
                if (YearIndex == 1) {
                    return TaxRateDataY2;
                } else if (YearIndex == 0) {
                    return TaxRateDataY1;
                } else {
                    throw new InvalidDataException("year is not supported");
                }
            }
        }

        public RetirementData RetirementDataY1 { get; private set; }
        public RetirementData RetirementDataY2 { get; private set; }
        public TaxRateData TaxRateDataY1 { get; private set; }
        public TaxRateData TaxRateDataY2 { get; private set; }
    }
}
