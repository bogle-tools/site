public class MonthlyQuotes
{
    public MonthlyQuotes(int index, string date, string close, string adjustedClose, string dividend)
    {
        Index = index;
        Date = date;
        Close = close;
        AdjustedClose = adjustedClose;
        Dividend = dividend;
    }
    public int Index { get; set; }
    public string Date { get; set; } 
    public string Close { get; set; } 
    public string AdjustedClose { get; set; } 
    public string Dividend { get; set; } 

    public static async Task<MonthlyQuotes[]> GetMonthlyQuotesAsync(HttpClient httpClient, string url)
    {
        var csvContent = await httpClient.GetStringAsync(url);
        List<MonthlyQuotes> monthlyQuotes = new ();
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
                MonthlyQuotes monthlyQuote = new (index++,chunks[0],chunks[1],chunks[2],chunks[3]);
                monthlyQuotes.Add(monthlyQuote);
            }
        }

        return monthlyQuotes.ToArray<MonthlyQuotes>();
    }
}
