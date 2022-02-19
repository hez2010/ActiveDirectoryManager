using ActiveDirectoryManager.Models;
using ActiveDirectoryManager.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace ActiveDirectoryManager.Controllers;

public class AccountController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly AdUserManager _userManager;
    private readonly SignInManager<UserInfo> _signInManager;
    private readonly SmtpService _smtpService;

    public AccountController(ILogger<HomeController> logger, AdUserManager userManager, SignInManager<UserInfo> signInManager, SmtpService smtpService)
    {
        _logger = logger;
        _userManager = userManager;
        _signInManager = signInManager;
        _smtpService = smtpService;
    }

    [HttpPost]
    public async Task<IActionResult> Index(AccountViewModel model)
    {
        var u = await _userManager.GetUserAsync(User);
        if (u is null) return RedirectToAction("Index");
        if (ModelState.IsValid)
        {
            if (u.MiddleName != model.MiddleName)
                u.MiddleName = model.MiddleName;
            if (u.LastName != model.LastName)
                u.LastName = model.LastName;
            if (u.DisplayName != model.DisplayName)
                u.DisplayName = model.DisplayName;
            if (u.EmailAddress != model.Email)
                u.EmailAddress = model.Email;
            if (u.FirstName != model.FirstName)
                u.FirstName = model.FirstName;
            var result = await _userManager.UpdateAsync(u);
            if (!result.Succeeded)
            {
                model.Message = result.Errors.FirstOrDefault()?.Description;
            }
        }

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var u = await _userManager.GetUserAsync(User);
        if (u is null) return RedirectToAction("Index");
        var model = new AccountViewModel
        {
            DisplayName = u.DisplayName,
            Email = u.EmailAddress,
            FirstName = u.FirstName,
            LastName = u.LastName,
            MiddleName = u.MiddleName,
        };
        return View(model);
    }

    [HttpGet]
    public IActionResult Login()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (ModelState.IsValid)
        {
            var result = await _signInManager.PasswordSignInAsync(model.UserName, model.Password, false, false);
            if (result.Succeeded)
            {
                return RedirectToAction("Index");
            }
            else
            {
                model.Message = "Incorrect username or password.";
                return View(model);
            }
        }

        return View(model);
    }

    [HttpGet]
    public IActionResult Register()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (ModelState.IsValid)
        {
            if (model.Password == model.ConfirmPassword)
            {
                var result = await _userManager.CreateAsync(new UserInfo(
                    model.UserName,
                    model.FirstName,
                    model.LastName,
                    null,
                    model.DisplayName,
                    model.Email,
                    true), model.Password);
                if (result.Succeeded)
                {
                    await _signInManager.PasswordSignInAsync(model.UserName, model.Password, false, false);
                    return RedirectToAction("Index");
                }
                else
                {
                    model.Message = result.Errors.FirstOrDefault()?.Description;
                    return View(model);
                }
            }
            else
            {
                model.Message = "The confirm password does not match the new password.";
                return View(model);
            }
        }

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Logout(string returnUrl)
    {
        await _signInManager.SignOutAsync();
        return Redirect(returnUrl);
    }

    [HttpPost]
    public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
    {
        if (string.IsNullOrEmpty(model.Email))
        {
            model.Message = "Illegal operation.";
            return View(model);
        }
        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user is not null)
        {
            if (model.Token is not null)
            {
                if (model.NewPassword == model.ConfirmPassword)
                {
                    var result = await _userManager.ResetPasswordAsync(user, model.Token, model.NewPassword);
                    model.Message = result.Succeeded ? "Password has been reset successfully." : result.Errors.FirstOrDefault()?.Description;
                    if (result.Succeeded) model.Token = null;
                    return View(model);
                }
                else
                {
                    model.Message = "The confirm password does not match the new password.";
                    return View(model);
                }
            }
            else
            {
                var token = Uri.EscapeDataString(await _userManager.GeneratePasswordResetTokenAsync(user));
                await _smtpService.SendEmailAsync("ops@matrix.moe", "Active Directory Manager",
                    @$"<h1>Active Directory Password Reset</h1>
<p>Click <a href=""[|host|]{Url.Action("ResetPassword", "Account")}?token={token}&email={model.Email}"">here</a> to reset your active directory password</p>
<p>If above link doesn't work, navigate to below link manually instead:</p>
<p>[|host|]{Url.Action("ResetPassword", "Account")}?token={token}&amp;email={model.Email}</p>
<hr />
<p>&copy; {DateTime.Now.Year} - Active Directory Manager</p>", model.Email);
                model.Message = $"We have sent an email to {model.Email}, please follow the instruction to reset your password.";
            }
        }
        else
        {
            model.Message = $"Email {model.Email} doesn't exist.";
        }

        return View(model);
    }

    [HttpGet]
    public IActionResult ResetPassword(string token, string email)
    {
        return View(new ResetPasswordViewModel { Email = email, Token = token });
    }
}
