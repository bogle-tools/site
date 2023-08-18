using System.Text.Json.Serialization;

public class RSUGrant {
    public DateOnly? Date { get; set; }
    public string? Ticker { get; set; }
    public double? VestPercent { get; set; }

    private int? _VestEventsCount;
    public int? VestEventsCount { 
        get { return _VestEventsCount; }
        set {
            _VestEventsCount = value;
            VestPercent = _VestEventsCount != null ? 100.0 / (double)_VestEventsCount.Value : 0;
        }
     }
    public int? FirstVestMonth { get; set;}
    public int? VestPeriodMonths { get; set;}
    public double Amount { get; set; }
    public double? Price { get; set; }
    public int? Shares { get; set; }
    public bool Edit { get; set; }

    private double? _LastPrice;
    public double? LastPrice { 
        get { return _LastPrice; }
        set {
            _LastPrice = value;
            foreach (var vestEvent in VestEvents) {
                vestEvent.Price = _LastPrice;
            }
        }
    }

    [JsonIgnore]
    public List<RSUVestEvent> VestEvents { get; set; } = new();
    public void CalculateShares() {
        if (Price != null) {
            Shares = (int)(Amount/Price);
        }
    }
}