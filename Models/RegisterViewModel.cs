using System.ComponentModel.DataAnnotations;

namespace ActiveDirectoryManager.Models;

public class RegisterViewModel
{
    [Required, Display(Name = "Username")] public string UserName { get; set; } = default!;
    [Required] public string Password { get; set; } = default!;
    [Required, Display(Name = "Confirm Password")] public string ConfirmPassword { get; set; } = default!;
    [Required] public string Email { get; set; } = default!;
    [Required, Display(Name = "First Name")] public string FirstName { get; set; } = default!;
    [Required, Display(Name = "Last Name")] public string LastName { get; set; } = default!;
    [Required, Display(Name = "Display Name")] public string DisplayName { get; set; } = default!;
    public string? Message { get; set; }
}
