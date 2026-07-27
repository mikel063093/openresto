using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using OpenRestoApi.Core.Application.Services;
using OpenRestoApi.Core.Domain;

namespace OpenRestoApi.Tests.Services;

public class JwtTokenServiceTests
{
    // 32+ char test key (HS256 minimum).
    private const string TestKey = "test-key-must-be-at-least-32-characters-long-for-hs256!!";
    private const string TestIssuer = "openresto-tests";
    private const string TestAudience = "openresto-tests";

    private static IConfiguration BuildConfig(string? key = TestKey) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = key,
                ["Jwt:Issuer"] = TestIssuer,
                ["Jwt:Audience"] = TestAudience,
            })
            .Build();

    private static JwtSecurityToken Decode(string token)
        => new JwtSecurityTokenHandler().ReadJwtToken(token);

    // ── Generate ────────────────────────────────────────────────────────────────

    [Fact]
    public void Generate_Returns_NonNull_NonEmpty_Token()
    {
        var svc = new JwtTokenService(BuildConfig());

        string token = svc.Generate("admin@example.com", AdminRole.SuperAdmin, 7);

        Assert.False(string.IsNullOrWhiteSpace(token));
    }

    [Fact]
    public void Generate_Signs_With_Hs256()
    {
        var svc = new JwtTokenService(BuildConfig());

        JwtSecurityToken jwt = Decode(svc.Generate("admin@example.com", AdminRole.SuperAdmin, 7));

        Assert.Equal("HS256", jwt.SignatureAlgorithm);
    }

    [Fact]
    public void Generate_Sets_Email_And_Admin_Role_Claims()
    {
        var svc = new JwtTokenService(BuildConfig());

        JwtSecurityToken jwt = Decode(svc.Generate("boss@openresto.com", AdminRole.SuperAdmin, 42));

        Claim email = Assert.Single(jwt.Claims, c => c.Type == ClaimTypes.Email);
        Assert.Equal("boss@openresto.com", email.Value);
        Claim role = Assert.Single(jwt.Claims, c => c.Type == ClaimTypes.Role);
        Assert.Equal(nameof(AdminRole.SuperAdmin), role.Value);
        Claim adminCredentialId = Assert.Single(jwt.Claims, c => c.Type == "admin_credential_id");
        Assert.Equal("42", adminCredentialId.Value);
    }

    [Fact]
    public void Generate_Sets_The_Provided_User_Role_Claim()
    {
        var svc = new JwtTokenService(BuildConfig());

        JwtSecurityToken jwt = Decode(svc.Generate("viewer@openresto.com", AdminRole.BookingViewer, 9));

        Claim role = Assert.Single(jwt.Claims, c => c.Type == ClaimTypes.Role);
        Assert.Equal(nameof(AdminRole.BookingViewer), role.Value);
    }

    [Fact]
    public void Generate_Sets_30_Day_Expiry()
    {
        var svc = new JwtTokenService(BuildConfig());
        DateTime before = DateTime.UtcNow;

        JwtSecurityToken jwt = Decode(svc.Generate("admin@example.com", AdminRole.SuperAdmin, 7));

        // Allow 5-second skew; assert it's ~30 days from now, not unbounded.
        Assert.InRange(jwt.ValidTo, before.AddDays(30).AddSeconds(-5), before.AddDays(30).AddSeconds(5));
    }

    [Fact]
    public void Generate_Sets_Issuer_And_Audience_From_Config()
    {
        var svc = new JwtTokenService(BuildConfig());

        JwtSecurityToken jwt = Decode(svc.Generate("admin@example.com", AdminRole.SuperAdmin, 7));

        Assert.Equal(TestIssuer, jwt.Issuer);
        Assert.Contains(TestAudience, jwt.Audiences);
    }

    // ── Config / env fallback ───────────────────────────────────────────────────

    [Fact]
    public void Generate_Falls_Back_To_Jwt_Key_Env_Var_When_Config_Missing()
    {
        string? prev = Environment.GetEnvironmentVariable("JWT_KEY");
        try
        {
            Environment.SetEnvironmentVariable("JWT_KEY", TestKey);
            // Config has no Jwt:Key — env fallback must kick in.
            var svc = new JwtTokenService(BuildConfig(key: null));

            string token = svc.Generate("admin@example.com", AdminRole.SuperAdmin, 7);

            Assert.False(string.IsNullOrWhiteSpace(token));
        }
        finally
        {
            Environment.SetEnvironmentVariable("JWT_KEY", prev);
        }
    }

    [Fact]
    public void Generate_Produces_Verifiable_Signature_Under_Config_Key()
    {
        IConfiguration cfg = BuildConfig();
        var svc = new JwtTokenService(cfg);

        string token = svc.Generate("admin@example.com", AdminRole.SuperAdmin, 7);

        var validation = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = TestIssuer,
            ValidateAudience = true,
            ValidAudience = TestAudience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(TestKey)),
            ValidateLifetime = false, // we don't check NotBefore/Expires skew here
        };
        new JwtSecurityTokenHandler().ValidateToken(token, validation, out _);
        // No exception thrown ⇒ signature + issuer + audience are all valid under the config key.
    }
}
