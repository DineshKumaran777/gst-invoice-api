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

    public JwtSigningKeyProvider(IOptions<JwtOptions> optionsAccessor, IWebHostEnvironment environment, ILogger<JwtSigningKeyProvider> logger)
    {
        var options = optionsAccessor.Value;
        var privatePem = (Environment.GetEnvironmentVariable("JWT_PRIVATE_KEY_PEM") ?? options.PrivateKeyPem ?? string.Empty)
            .Replace("\\n", "\n", StringComparison.Ordinal);
        var publicPem = (Environment.GetEnvironmentVariable("JWT_PUBLIC_KEY_PEM") ?? options.PublicKeyPem ?? string.Empty)
            .Replace("\\n", "\n", StringComparison.Ordinal);

        var hasPrivateKey = !string.IsNullOrWhiteSpace(privatePem);
        var hasPublicKey = !string.IsNullOrWhiteSpace(publicPem);

        if (hasPrivateKey && hasPublicKey)
        {
            var fallbackRsa = RSA.Create(2048);

            signingKey = new Lazy<SecurityKey>(() =>
            {
                try
                {
                    var rsa = RSA.Create();
                    rsa.ImportFromPem(privatePem);
                    return new RsaSecurityKey(rsa);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Failed to load JWT private key from PEM. Falling back to auto-generated key. Tokens will be invalidated on restart.");
                    return new RsaSecurityKey(fallbackRsa.ExportParameters(true));
                }
            });

            validationKey = new Lazy<SecurityKey>(() =>
            {
                try
                {
                    var rsa = RSA.Create();
                    rsa.ImportFromPem(publicPem);
                    return new RsaSecurityKey(rsa);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Failed to load JWT public key from PEM. Falling back to auto-generated key. Tokens will be invalidated on restart.");
                    return new RsaSecurityKey(fallbackRsa.ExportParameters(false));
                }
            });
            return;
        }

        if (hasPrivateKey != hasPublicKey)
        {
            logger.LogWarning("Only one of JWT_PRIVATE_KEY_PEM / JWT_PUBLIC_KEY_PEM is set. Both are required for persistent keys. Falling back to auto-generated keys.");
        }
        else
        {
            logger.LogWarning("JWT_PRIVATE_KEY_PEM and JWT_PUBLIC_KEY_PEM are not configured. Using auto-generated keys. Tokens will be invalidated on restart.");
        }

        var rsaKey = RSA.Create(2048);
        signingKey = new Lazy<SecurityKey>(() => new RsaSecurityKey(rsaKey.ExportParameters(true)));
        validationKey = new Lazy<SecurityKey>(() => new RsaSecurityKey(rsaKey.ExportParameters(false)));
    }

    public SecurityKey SigningKey => signingKey.Value;

    public SecurityKey ValidationKey => validationKey.Value;
}
