using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace ActiveDirectoryManager.Models;

public class ResetPasswordViewModel
{
    [Required, EmailAddress]
    public string Email { get; set; } = default!;
    public string? Token { get; set; }
    [NotNullIfNotNull(nameof(Token)), Required, Display(Name = "New Password")] public string? NewPassword { get; set; }
    [NotNullIfNotNull(nameof(Token)), Required, Display(Name = "Confirm Password")] public string? ConfirmPassword { get; set; }
    public string? Message { get; set; }
}
