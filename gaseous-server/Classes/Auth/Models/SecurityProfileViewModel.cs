using System.ComponentModel.DataAnnotations;

namespace Authentication
{
    public class SecurityProfileViewModel
    {
        public AgeRestrictionItem AgeRestrictionPolicy { get; set; } = new AgeRestrictionItem{ 
            MaximumAgeRestriction = gaseous_server.Classes.Metadata.AgeRatings.AgeGroups.AgeRestrictionGroupings.Adult,
            IncludeUnrated = true 
        };

        public class AgeRestrictionItem
        {
            public gaseous_server.Classes.Metadata.AgeRatings.AgeGroups.AgeRestrictionGroupings MaximumAgeRestriction { get; set; }
            public bool IncludeUnrated { get; set; }
        }
    }
}