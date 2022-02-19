using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using ActiveDirectoryManager.Models;
using ActiveDirectoryManager.Services;
using Microsoft.AspNetCore.Identity;

namespace ActiveDirectoryManager.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly AdUserManager _userManager;
    private readonly SignInManager<UserInfo> _signInManager;
    private readonly SmtpService _smtpService;

    public HomeController(ILogger<HomeController> logger, AdUserManager userManager, SignInManager<UserInfo> signInManager, SmtpService smtpService)
    {
        _logger = logger;
        _userManager = userManager;
        _signInManager = signInManager;
        _smtpService = smtpService;
    }

    public IActionResult Index()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
