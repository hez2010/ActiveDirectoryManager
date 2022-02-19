using System.ComponentModel.DataAnnotations;

namespace ActiveDirectoryManager.Models;

public class AccountViewModel
{
    [Required, Display(Name = "First Name")] public string? FirstName { get; set; }
    [Required, Display(Name = "Last Name")] public string? LastName { get; set; }
    [Display(Name = "Middle Name")] public string? MiddleName { get; set; }
    [Required, Display(Name = "Display Name")] public string? DisplayName { get; set; }
    [Required] public string Email { get; set; } = default!;
    public string? Message { get; set; }
}
