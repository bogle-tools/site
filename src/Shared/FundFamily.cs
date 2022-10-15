public class FundFamily
{
    public FundFamily(int index, string name, string stockFund, string internationalStockFund, string bondFund)
    {
        Index = index;
        Name = name;
        StockFund = stockFund;
        InternationalStockFund = internationalStockFund;
        BondFund = bondFund;
    }
    public int Index { get; set; }
    public string Name { get; set; } 
    public string StockFund { get; set; } 
    public string InternationalStockFund { get; set; } 
    public string BondFund { get; set; } 

    public static async Task<FundFamily[]> GetFundFamilies(HttpClient httpClient)
    {
        var url = "https://raw.githubusercontent.com/rrelyea/3fund-prices/main/data/fundTypes.csv";
        var csvContent = await httpClient.GetStringAsync(url);
        List<FundFamily> fundFamilies = new ();
        var skippedFirstLine = false;
        var index = 0;
        foreach(var line in csvContent.Split('\n'))
        {
            if (!skippedFirstLine) {
                skippedFirstLine = true;
                continue;
            }

            if (!string.IsNullOrEmpty(line))
            {
                var chunks = line.Split(',');
                FundFamily fundFamily = new (index++,chunks[0],chunks[1],chunks[2],chunks[3]);
                fundFamilies.Add(fundFamily);
            }
        }

        return fundFamilies.ToArray<FundFamily>();
    }
}
