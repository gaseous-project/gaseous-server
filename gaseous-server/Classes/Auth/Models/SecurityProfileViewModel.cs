using System.ComponentModel.DataAnnotations;

namespace Authentication
{
    public class SecurityProfileViewModel
    {
        public AgeRestrictionItem AgeRestrictionPolicy { get; set; } = new AgeRestrictionItem{ 
            MaximumAgeRestriction = "Adult",
            IncludeUnrated = true 
        };

        public class AgeRestrictionItem
        {
            public string MaximumAgeRestriction { get; set; }
            public bool IncludeUnrated { get; set; }
        }
    }
}