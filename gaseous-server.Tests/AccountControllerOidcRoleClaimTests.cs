using System;
using System.Reflection;
using gaseous_server.Controllers;
using Xunit;

namespace gaseous_server.Tests
{
    public class AccountControllerOidcRoleClaimTests
    {
        private static object? ParseOidcRoleClaim(string value)
        {
            MethodInfo method = typeof(AccountController).GetMethod("ParseOidcRoleClaim", BindingFlags.NonPublic | BindingFlags.Static)!;
            return method.Invoke(null, new object[] { value });
        }

        [Fact]
        public void ParseOidcRoleClaim_WithoutAgeRestriction_ReturnsNull()
        {
            Assert.Null(ParseOidcRoleClaim("Gamer"));
        }

        [Theory]
        [InlineData("Gamer:Teen", false)]
        [InlineData("Gamer:Teen+", true)]
        [InlineData("Gamer_Teen", false)]
        [InlineData("Gamer_Teen_Plus", true)]
        public void ParseOidcRoleClaim_WithAgeRestriction_ParsesRoleAndUnratedFlag(string value, bool includeUnrated)
        {
            object? parsedClaim = ParseOidcRoleClaim(value);
            Assert.NotNull(parsedClaim);
            Type claimType = parsedClaim.GetType();

            Assert.Equal("Gamer", claimType.GetProperty("Role")!.GetValue(parsedClaim));
            Assert.Equal("Teen", claimType.GetProperty("AgeRestriction")!.GetValue(parsedClaim)!.ToString());
            Assert.Equal(includeUnrated, claimType.GetProperty("IncludeUnrated")!.GetValue(parsedClaim));
        }
    }
}