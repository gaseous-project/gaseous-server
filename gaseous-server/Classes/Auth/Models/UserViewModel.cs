namespace Authentication
{
    public class UserViewModel
    {
        public string Id { get; set; }
        public string EmailAddress { get; set; }
        public bool LockoutEnabled { get; set; }
        public DateTimeOffset? LockoutEnd { get; set; }
        public List<string> Roles { get; set; }
        public SecurityProfileViewModel SecurityProfile { get; set; }
        public string HighestRole {
            get
            {
                string _highestRole = "";
                foreach (string role in Roles)
                {
                    switch (role)
                    {
                        case "Admin":
                            // there is no higher
                            _highestRole = role;
                            break;
                        case "Gamer":
                            // only one high is Admin, so check for that
                            if (_highestRole != "Admin")
                            {
                                _highestRole = role;
                            }
                            break;
                        case "Player":
                            // make sure _highestRole isn't already set to Gamer or Admin
                            if (_highestRole != "Admin" && _highestRole != "Gamer")
                            {
                                _highestRole = role;
                            }
                            break;
                        default:
                            _highestRole = "Player";
                            break;
                    }
                }

                return _highestRole;
            }
        }
    }
}