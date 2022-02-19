using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using System.ComponentModel.DataAnnotations;
using System.DirectoryServices.AccountManagement;
using System.Text;

namespace ActiveDirectoryManager.Services;

public record UserInfo
{
    public UserInfo(string userName, string? firstName, string? lastName, string? middleName, string? displayName, string emailAddress, bool? enabled)
    {
        UserName = userName;
        FirstName = firstName;
        LastName = lastName;
        MiddleName = middleName;
        DisplayName = displayName;
        EmailAddress = emailAddress;
        Enabled = enabled;
    }

    public UserInfo(string userName, string? firstName, string? lastName, string? middleName, string? displayName, string distinguishedName, string emailAddress, string id, bool? enabled)
    {
        UserName = userName;
        FirstName = firstName;
        LastName = lastName;
        MiddleName = middleName;
        DisplayName = displayName;
        DistinguishedName = distinguishedName;
        EmailAddress = emailAddress;
        Id = id;
        Enabled = enabled;
    }

    public string UserName { get; set; } = default!;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? MiddleName { get; set; }
    public string? DisplayName { get; set; }
    public string DistinguishedName { get; set; } = default!;
    public string EmailAddress { get; set; } = default!;
    public string Id { get; set; } = default!;
    public bool? Enabled { get; set; }
    public string? PasswordToUpdate { get; set; }
}

public class AdOptions
{
    [Required] public string Domain { get; set; } = default!;
    [Required] public string Entry { get; set; } = default!;
    [Required] public string Username { get; set; } = default!;
    [Required] public string Password { get; set; } = default!;
}

[DirectoryRdnPrefix("CN"), DirectoryObjectClass("inetOrgPerson")]
public class InetOrgPersonPrincipal : UserPrincipal
{
    public InetOrgPersonPrincipal(PrincipalContext context) : base(context) { }

    public InetOrgPersonPrincipal(PrincipalContext context, string username, string password, string? firstName, string? lastName, string? middleName, string? displayName, string email, bool enabled) : base(context, username, password, enabled)
    {
        GivenName = firstName;
        Surname = lastName;
        MiddleName = middleName;
        EmailAddress = email;
        DisplayName = displayName;
        PasswordNeverExpires = true;
    }

    public new static InetOrgPersonPrincipal FindByIdentity(PrincipalContext context, IdentityType identityType, string identityValue)
    {
        return (InetOrgPersonPrincipal)FindByIdentityWithType(context, typeof(InetOrgPersonPrincipal), identityType, identityValue);
    }

    public new static InetOrgPersonPrincipal FindByIdentity(PrincipalContext context, string identityValue)
    {
        return (InetOrgPersonPrincipal)FindByIdentityWithType(context, typeof(InetOrgPersonPrincipal), identityValue);
    }

}

public sealed class AdUserStore : IUserEmailStore<UserInfo>, IQueryableUserStore<UserInfo>, IUserPasswordStore<UserInfo>
{
    private readonly PrincipalContext _context;

    public AdUserStore(PrincipalContext context)
    {
        _context = context;
    }

    private IEnumerable<UserInfo> EnumerateUsers()
    {
        using var filter = new InetOrgPersonPrincipal(_context);
        using var searcher = new PrincipalSearcher(filter);
        var result = searcher.FindAll();
        foreach (InetOrgPersonPrincipal u in result)
        {
            yield return new UserInfo(u.SamAccountName, u.GivenName, u.Surname, u.MiddleName, u.DisplayName, u.DistinguishedName, u.EmailAddress, u.Guid!.Value.ToString(), u.Enabled);
            u.Dispose();
        }
    }

    public IQueryable<UserInfo> Users => EnumerateUsers().AsQueryable();

    private static string GenerateRandomPassword()
    {
        const string charTable = "qwertyuiopasdfghjklzxcvbnmQWERTYUIOPASDFGHJKLZXCVBNM1234567890!@#$%^&*()_+-={}[]|,.<>?";
        var sb = new StringBuilder();
        for (var i = 0; i < 12; i++)
        {
            switch (i)
            {
                case < 3:
                    sb.Append(charTable[Random.Shared.Next(0, 26)]);
                    break;
                case >= 3 and < 6:
                    sb.Append(charTable[Random.Shared.Next(26, 26 + 26)]);
                    break;
                case >= 6 and < 9:
                    sb.Append(charTable[Random.Shared.Next(26 + 26, 26 + 26 + 10)]);
                    break;
                case >= 9:
                    sb.Append(charTable[Random.Shared.Next(26 + 26 + 10, charTable.Length)]);
                    break;
            }
        }

        return string.Concat(sb.ToString().OrderBy(x => Random.Shared.Next()));
    }

    public Task<IdentityResult> CreateAsync(UserInfo user, CancellationToken cancellationToken)
    {
        using var principal = new InetOrgPersonPrincipal(_context, user.UserName, user.PasswordToUpdate ?? GenerateRandomPassword(), user.FirstName, user.LastName, user.MiddleName, user.DisplayName, user.EmailAddress, true);
        try
        {
            principal.Save();
        }
        catch (Exception ex)
        {
            return Task.FromResult(IdentityResult.Failed(new IdentityError { Code = ex.HResult.ToString(), Description = ex.Message }));
        }

        user.PasswordToUpdate = null;
        return Task.FromResult(IdentityResult.Success);
    }

    public Task<IdentityResult> DeleteAsync(UserInfo user, CancellationToken cancellationToken)
    {
        using var u = UserPrincipal.FindByIdentity(_context, IdentityType.Guid, user.Id);

        try
        {
            u?.Delete();
        }
        catch (Exception ex)
        {
            return Task.FromResult(IdentityResult.Failed(new IdentityError { Code = ex.HResult.ToString(), Description = ex.Message }));
        }

        return Task.FromResult(IdentityResult.Success);
    }

    public void Dispose()
    {
    }

    public Task<UserInfo> FindByEmailAsync(string normalizedEmail, CancellationToken cancellationToken)
    {
        using var filter = new InetOrgPersonPrincipal(_context) { EmailAddress = normalizedEmail };
        using var searcher = new PrincipalSearcher(filter);
        using var result = searcher.FindOne();
        return result is not InetOrgPersonPrincipal u
            ? Task.FromResult<UserInfo>(null!)
            : Task.FromResult(new UserInfo(u.Name, u.GivenName, u.Surname, u.MiddleName, u.DisplayName, u.DistinguishedName, u.EmailAddress, u.Guid!.Value.ToString(), u.Enabled));
    }

    public Task<UserInfo> FindByIdAsync(string userId, CancellationToken cancellationToken)
    {
        using var u = InetOrgPersonPrincipal.FindByIdentity(_context, IdentityType.Guid, userId);
        return u is null
            ? Task.FromResult<UserInfo>(null!)
            : Task.FromResult(new UserInfo(u.Name, u.GivenName, u.Surname, u.MiddleName, u.DisplayName, u.DistinguishedName, u.EmailAddress, u.Guid!.Value.ToString(), u.Enabled));
    }

    public Task<UserInfo> FindByNameAsync(string normalizedUserName, CancellationToken cancellationToken)
    {
        using var u = InetOrgPersonPrincipal.FindByIdentity(_context, IdentityType.SamAccountName, normalizedUserName);
        return u is null
            ? Task.FromResult<UserInfo>(null!)
            : Task.FromResult(new UserInfo(u.Name, u.GivenName, u.Surname, u.MiddleName, u.DisplayName, u.DistinguishedName, u.EmailAddress, u.Guid!.Value.ToString(), u.Enabled));
    }

    public Task<string> GetEmailAsync(UserInfo user, CancellationToken cancellationToken)
    {
        return Task.FromResult(user.EmailAddress);
    }

    public Task<bool> GetEmailConfirmedAsync(UserInfo user, CancellationToken cancellationToken)
    {
        return Task.FromResult(true);
    }

    public Task<string> GetNormalizedEmailAsync(UserInfo user, CancellationToken cancellationToken)
    {
        return Task.FromResult(user.EmailAddress);
    }

    public Task<string> GetNormalizedUserNameAsync(UserInfo user, CancellationToken cancellationToken)
    {
        return Task.FromResult(user.UserName);
    }

    public Task<string> GetUserIdAsync(UserInfo user, CancellationToken cancellationToken)
    {
        return Task.FromResult(user.Id);
    }

    public Task<string> GetUserNameAsync(UserInfo user, CancellationToken cancellationToken)
    {
        return Task.FromResult(user.UserName);
    }

    public Task SetEmailAsync(UserInfo user, string email, CancellationToken cancellationToken)
    {
        user.EmailAddress = email;
        return Task.CompletedTask;
    }

    public Task SetEmailConfirmedAsync(UserInfo user, bool confirmed, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public Task SetNormalizedEmailAsync(UserInfo user, string normalizedEmail, CancellationToken cancellationToken)
    {
        user.EmailAddress = normalizedEmail;
        return Task.CompletedTask;
    }

    public Task SetNormalizedUserNameAsync(UserInfo user, string normalizedName, CancellationToken cancellationToken)
    {
        user.UserName = normalizedName;
        return Task.CompletedTask;
    }

    public Task SetUserNameAsync(UserInfo user, string userName, CancellationToken cancellationToken)
    {
        user.UserName = userName;
        return Task.CompletedTask;
    }

    public Task<IdentityResult> UpdateAsync(UserInfo user, CancellationToken cancellationToken)
    {
        using var u = InetOrgPersonPrincipal.FindByIdentity(_context, IdentityType.Guid, user.Id);
        if (u is null) return Task.FromResult(IdentityResult.Failed(new IdentityError { Code = "404", Description = "User not found." }));
        if (u.EmailAddress != user.EmailAddress)
            u.EmailAddress = user.EmailAddress;
        if (u.Name != user.UserName)
            u.Name = user.UserName;
        if (u.SamAccountName != user.UserName)
            u.SamAccountName = user.UserName;
        if (u.GivenName != user.FirstName)
            u.GivenName = user.FirstName;
        if (u.Surname != user.LastName)
            u.Surname = user.LastName;
        if (u.MiddleName != user.MiddleName)
            u.MiddleName = user.MiddleName;
        if (u.DisplayName != user.DisplayName)
            u.DisplayName = user.DisplayName;
        if (u.Enabled != user.Enabled)
            u.Enabled = user.Enabled;

        try
        {
            if (!string.IsNullOrEmpty(user.PasswordToUpdate))
            {
                u.SetPassword(user.PasswordToUpdate);
                u.PasswordNeverExpires = true;
                user.PasswordToUpdate = null;
            }

            u.Save();
        }
        catch (Exception ex)
        {
            return Task.FromResult(IdentityResult.Failed(new IdentityError { Code = ex.HResult.ToString(), Description = ex.Message }));
        }

        return Task.FromResult(IdentityResult.Success);
    }

    public Task SetPasswordHashAsync(UserInfo user, string passwordHash, CancellationToken cancellationToken)
    {
        user.PasswordToUpdate = passwordHash;
        return Task.CompletedTask;
    }

    public Task<string> GetPasswordHashAsync(UserInfo user, CancellationToken cancellationToken)
    {
        return Task.FromResult(user.PasswordToUpdate ?? "");
    }

    public Task<bool> HasPasswordAsync(UserInfo user, CancellationToken cancellationToken)
    {
        return Task.FromResult(true);
    }
}

public sealed class AdPasswordHasher : IPasswordHasher<UserInfo>
{
    private readonly PrincipalContext _context;

    public AdPasswordHasher(PrincipalContext context)
    {
        _context = context;
    }

    public string HashPassword(UserInfo user, string password)
    {
        return password;
    }

    public PasswordVerificationResult VerifyHashedPassword(UserInfo user, string hashedPassword, string providedPassword)
    {
        // Workaround: Options will be corrupted so pass it explicitly.
        return _context.ValidateCredentials(user.UserName, providedPassword, _context.Options)
            ? PasswordVerificationResult.Success : PasswordVerificationResult.Failed;
    }
}

public sealed class AdUserNormalizer : ILookupNormalizer
{
    public string NormalizeName(string name) => name;

    public string NormalizeEmail(string email) => email;
}

public sealed class AdUserManager : UserManager<UserInfo>
{
    private readonly PrincipalContext _context;

    public AdUserManager(
        IUserStore<UserInfo> store,
        IOptions<IdentityOptions> optionsAccessor,
        IPasswordHasher<UserInfo> passwordHasher,
        IEnumerable<IUserValidator<UserInfo>> userValidators,
        IEnumerable<IPasswordValidator<UserInfo>> passwordValidators,
        ILookupNormalizer keyNormalizer,
        IdentityErrorDescriber errors,
        IServiceProvider services,
        ILogger<UserManager<UserInfo>> logger,
        PrincipalContext context) : base(store, optionsAccessor, passwordHasher, userValidators, passwordValidators, keyNormalizer, errors, services, logger)
    {
        _context = context;
    }
}
