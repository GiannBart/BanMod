//credits and licenses in the resources folder/
using System;
using System.Security.Cryptography;
using System.Text;

namespace BanMod
{
    internal static class BanModDeviceIdentity
    {
        private const string KeyName = "BanMod.DeviceIdentity.v1";
        private const string PlatformProviderName = "Microsoft Platform Crypto Provider";

        private static readonly object Sync = new object();
        private static ECDsaCng _signer;
        private static string _publicKey = "";
        private static string _keyId = "";
        private static string _provider = "";
        private static bool _initialized;

        public static bool Available
        {
            get
            {
                EnsureInitialized();
                return _signer != null && !string.IsNullOrWhiteSpace(_keyId);
            }
        }

        public static string KeyId
        {
            get
            {
                EnsureInitialized();
                return _keyId ?? "";
            }
        }

        public static string PublicKey
        {
            get
            {
                EnsureInitialized();
                return _publicKey ?? "";
            }
        }

        public static string Provider
        {
            get
            {
                EnsureInitialized();
                return _provider ?? "";
            }
        }

        private static void EnsureInitialized()
        {
            if (_initialized)
                return;

            lock (Sync)
            {
                if (_initialized)
                    return;

                _initialized = true;

                CngProvider platformProvider = new CngProvider(PlatformProviderName);
                CngProvider softwareProvider = CngProvider.MicrosoftSoftwareKeyStorageProvider;

                if (TryInitializeWithProvider(platformProvider, "tpm", false))
                    return;
                if (TryInitializeWithProvider(softwareProvider, "software-ksp", false))
                    return;
                if (TryInitializeWithProvider(platformProvider, "tpm", true))
                    return;

                TryInitializeWithProvider(softwareProvider, "software-ksp", true);
            }
        }

        private static bool TryInitializeWithProvider(
            CngProvider provider,
            string providerLabel,
            bool createIfMissing)
        {
            try
            {
                CngKey key;

                if (CngKey.Exists(KeyName, provider))
                {
                    key = CngKey.Open(KeyName, provider);
                }
                else
                {
                    if (!createIfMissing)
                        return false;

                    CngKeyCreationParameters creation = new CngKeyCreationParameters
                    {
                        Provider = provider,
                        KeyUsage = CngKeyUsages.Signing,
                        ExportPolicy = CngExportPolicies.None,
                        KeyCreationOptions = CngKeyCreationOptions.None
                    };

                    key = CngKey.Create(
                        CngAlgorithm.ECDsaP256,
                        KeyName,
                        creation
                    );
                }

                ECDsaCng signer = new ECDsaCng(key);
                byte[] publicBytes = signer.ExportSubjectPublicKeyInfo();

                using SHA256 sha = SHA256.Create();
                byte[] digest = sha.ComputeHash(publicBytes);

                StringBuilder sb = new StringBuilder(digest.Length * 2);
                foreach (byte b in digest)
                    sb.Append(b.ToString("x2"));

                _signer = signer;
                _publicKey = Convert.ToBase64String(publicBytes);
                _keyId = sb.ToString();
                _provider = providerLabel;
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static string SignActivation(
            string nonce,
            string friendCode,
            string buildId,
            string banModSha256,
            string clientVersion)
        {
            EnsureInitialized();

            if (_signer == null)
                return "";

            try
            {
                string canonical =
                    "BANMOD-DEVICE-V1\n" +
                    Normalize(nonce) + "\n" +
                    Normalize(friendCode).ToLowerInvariant() + "\n" +
                    Normalize(buildId).ToLowerInvariant() + "\n" +
                    Normalize(banModSha256).ToLowerInvariant() + "\n" +
                    Normalize(_keyId).ToLowerInvariant() + "\n" +
                    Normalize(clientVersion).ToLowerInvariant();

                byte[] data = Encoding.UTF8.GetBytes(canonical);
                byte[] signature = _signer.SignData(
                    data,
                    HashAlgorithmName.SHA256,
                    DSASignatureFormat.Rfc3279DerSequence
                );

                return Convert.ToBase64String(signature);
            }
            catch
            {
                return "";
            }
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? "" : value.Trim();
        }
    }
}
