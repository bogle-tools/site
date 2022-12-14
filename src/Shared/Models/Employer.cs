using IRS;

namespace Employer {
    // Root myDeserializedClass = JsonConvert.DeserializeObject<Employer>(myJsonResponse);
    public class Employer401k
    {
        public bool Offered { get; set; }
        public List<MatchRule> MatchRules { get; set; }
        public int? MatchLimit { get; set; }
    }

    public class HSA
    {
        public int EmployerContribution { get; set; }
    }

    public class MatchRule
    {
        public int MatchPercentage { get; set; }
        public int? ForNextPercent { get; set; }
    }

    public class MegaBackdoorRoth
    {
        public int ContributionLimit { get; set; }
    }

    public class RetirementSavings
    {
        public Employer401k Employer401k { get; set; }
        public HSA HSA { get; set; }
        public MegaBackdoorRoth MegaBackdoorRoth { get; set; }
    }

    public class Employer : DataDocument
    {
        public string Company { get; set; }
        public int Year { get; set; }
        public RetirementSavings RetirementSavings { get; set; }
    }
}
