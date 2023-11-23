using gaseous_server.Classes;
using Microsoft.AspNetCore.Identity;
using System;

namespace Authentication
{
    /// <summary>
    /// Class that implements the ASP.NET Identity
    /// IUser interface 
    /// </summary>
    public class ApplicationUser : IdentityUser
    {
        public SecurityProfileViewModel SecurityProfile { get; set; }
    }
}
