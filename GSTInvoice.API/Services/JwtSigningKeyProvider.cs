using System.Security.Cryptography;
using GSTInvoice.API.Options;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace GSTInvoice.API.Services;

public interface IJwtSigningKeyProvider
{
    SecurityKey SigningKey { get; }

    SecurityKey ValidationKey { get; }
}

public sealed class JwtSigningKeyProvider : IJwtSigningKeyProvider
{
    private readonly Lazy<SecurityKey> signingKey;
    private readonly Lazy<SecurityKey> validationKey;

    public JwtSigningKeyProvider(IOptions<JwtOptions> optionsAccessor, IWebHostEnvironment environment)
    {
        var options = optionsAccessor.Value;
        var privatePem = (Environment.GetEnvironmentVariable("JWT_PRIVATE_KEY_PEM") ?? options.PrivateKeyPem ?? string.Empty)
            .Replace("\\n", "\n", StringComparison.Ordinal);
        var publicPem = (Environment.GetEnvironmentVariable("JWT_PUBLIC_KEY_PEM") ?? options.PublicKeyPem ?? string.Empty)
            .Replace("\\n", "\n", StringComparison.Ordinal);

        if (!string.IsNullOrWhiteSpace(privatePem) && !string.IsNullOrWhiteSpace(publicPem))
        {
            signingKey = new Lazy<SecurityKey>(() =>
            {
                var rsa = RSA.Create();
                rsa.ImportFromPem(privatePem);
                return new RsaSecurityKey(rsa);
            });

            validationKey = new Lazy<SecurityKey>(() =>
            {
                var rsa = RSA.Create();
                rsa.ImportFromPem(publicPem);
                return new RsaSecurityKey(rsa);
            });
            return;
        }

        if (!environment.IsDevelopment())
        {
            throw new InvalidOperationException("JWT_PRIVATE_KEY_PEM and JWT_PUBLIC_KEY_PEM are required outside Development.");
        }

        var devRsa = RSA.Create(2048);
        signingKey = new Lazy<SecurityKey>(() => new RsaSecurityKey(devRsa.ExportParameters(true)));
        validationKey = new Lazy<SecurityKey>(() => new RsaSecurityKey(devRsa.ExportParameters(false)));
    }

    public SecurityKey SigningKey => signingKey.Value;

    public SecurityKey ValidationKey => validationKey.Value;
}
