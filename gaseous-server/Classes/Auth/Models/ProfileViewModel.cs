namespace Authentication
{
    public class ProfileBasicViewModel
    {
        public string UserId { get; set; }
        public string UserName { get; set; }
        public string EmailAddress { get; set; }
        public List<String> Roles { get; set; }
    }
}