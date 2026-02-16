using System.ComponentModel.DataAnnotations;

namespace Authentication
{
    /// <summary>
    /// Status information for Two-Factor Authentication.
    /// </summary>
    public class TwoFactorStatusModel
    {
        /// <summary>Whether two-factor authentication is enabled for the user.</summary>
        public bool Enabled { get; set; }
        /// <summary>Whether an authenticator key has been provisioned for the user.</summary>
        public bool HasAuthenticatorKey { get; set; }
        /// <summary>The number of remaining recovery codes.</summary>
        public int RecoveryCodesLeft { get; set; }
    }

    /// <summary>
    /// Request to generate a number of new recovery codes.
    /// </summary>
    public class GenerateRecoveryCodesRequest
    {
        /// <summary>The number of recovery codes to generate.</summary>
        [Range(1, 100)]
        public int Count { get; set; } = 10;
    }

    /// <summary>
    /// Request used by admins to disable two-factor on a target user.
    /// </summary>
    public class AdminDisable2FARequest
    {
        /// <summary>The target user's ID.</summary>
        public string? UserId { get; set; }
        /// <summary>The target user's email address.</summary>
        public string? Email { get; set; }
    }

    /// <summary>
    /// Request to confirm and enable two-factor using an authenticator code.
    /// </summary>
    public class ConfirmAuthenticatorRequest
    {
        /// <summary>The 6-digit TOTP code from the authenticator app.</summary>
        [Required]
        public string Code { get; set; } = string.Empty;
    }
}
