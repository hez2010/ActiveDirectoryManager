using ActiveDirectoryManager.Services;
using Microsoft.AspNetCore.Identity;
using System.DirectoryServices.AccountManagement;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.AddSingleton<IPasswordHasher<UserInfo>, AdPasswordHasher>();

builder.Services.AddIdentityCore<UserInfo>(options =>
    {
        options.User.RequireUniqueEmail = true;
        options.User.AllowedUserNameCharacters = null;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequiredLength = 7;
    })
    .AddSignInManager<SignInManager<UserInfo>>()
    .AddUserManager<AdUserManager>()
    .AddUserStore<AdUserStore>()
    .AddDefaultTokenProviders();

builder.Services.AddSingleton<ILookupNormalizer, AdUserNormalizer>();

var adOptions = new AdOptions();
builder.Configuration.GetSection("AD").Bind(adOptions);
builder.Services.AddTransient(sp => new PrincipalContext(ContextType.Domain, adOptions.Domain, adOptions.Entry, adOptions.Username, adOptions.Password));

builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = IdentityConstants.ApplicationScheme;
    options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
}).AddIdentityCookies();

builder.Services.AddAuthorization();

builder.Services.AddSingleton<SmtpService>().Configure<SmtpOptions>(options =>
{
    var smtpOptions = new SmtpOptions();
    builder.Configuration.GetSection("Smtp").Bind(smtpOptions);
    options.Username = smtpOptions.Username;
    options.Password = smtpOptions.Password;
    options.Port = smtpOptions.Port;
    options.Ssl = smtpOptions.Ssl;
    options.Server = smtpOptions.Server;
    options.Sender = smtpOptions.Sender;
    options.Host = smtpOptions.Host;
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
