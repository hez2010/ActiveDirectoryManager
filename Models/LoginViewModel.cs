using System.ComponentModel.DataAnnotations;

namespace ActiveDirectoryManager.Models;

public class LoginViewModel
{
    [Required, Display(Name = "Username")] public string UserName { get; set; } = default!;
    [Required] public string Password { get; set; } = default!;
    public string? Message { get; set; }
}
