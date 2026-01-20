using System.Security.Cryptography;
using System.Text;
using System.Text.Json;


namespace Quacklibs.AzureDevopsCli.Services
{
    public interface ICredentialStorage
    {
        // string GetCredential(string username);
        // void SetCredential(PersonalAccessToken pat);

        // void Delete();
    }

    internal class CredentialStorage : ICredentialStorage
    {
        private static readonly string SecretFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "azdo", "pat.dat");

        private readonly string tokenFilePath;

        public CredentialStorage()
        {
            this.tokenFilePath = this.GetTokenFile();
        }

        public void Clear()
        {
            if (File.Exists(this.tokenFilePath))
            {
                File.Delete(this.tokenFilePath);
            }
        }

        public void Delete()
        {
            if (File.Exists(this.tokenFilePath))
            {
                File.Delete(this.tokenFilePath);
                Console.WriteLine("Credentials deletet");
            }
            else
            {
                Console.WriteLine("No credentials found to delete");
            }
        }

        public string GetCredential(string username)
        {
            string result = null;

            try
            {
                if (File.Exists(this.tokenFilePath))
                {
                    var credentialsJson = File.ReadAllText(this.tokenFilePath);

                    //TODO: Safe storage
                    //var protetedContentBytes = Convert.FromBase64String(protectedContentBytesBase64);
                    //var contentBytes = ProtectedData.Unprotect(protetedContentBytes, null, DataProtectionScope.CurrentUser);
                    //var jsonContent = Encoding.UTF8.GetString(contentBytes);


                    var credentials = JsonSerializer.Deserialize<Credentials>(credentialsJson);

                    result = credentials.PersonalAccessToken;
                }
            }
            catch
            {
                this.Clear();
            }

            return result;
        }

        public void SetCredential(PersonalAccessToken pat)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(SecretFile)!);
                byte[] secretBytes = Encoding.UTF8.GetBytes(pat.Value);

                // Encrypt using platform-specific method
                byte[] encrypted = Encrypt(secretBytes);

                File.WriteAllBytes(SecretFile, encrypted);
            }
            catch (PlatformNotSupportedException)
            {
                Debug.WriteLine("Could not store credentials");
            }
        }

        private static byte[] Encrypt(byte[] data)
        {
            if (OperatingSystem.IsWindows())
            {
                return ProtectedData.Protect(data, null, DataProtectionScope.CurrentUser);
            }
            else
            {
                // Linux / macOS use AES with a fixed key per user
                using var aes = Aes.Create()!;
                aes.Key = GetUserKey();
                aes.GenerateIV();
                using var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
                byte[] cipher = encryptor.TransformFinalBlock(data, 0, data.Length);
                // Prepend IV for decryption
                return aes.IV.Concat(cipher).ToArray();
            }
        }

        private static byte[] Decrypt(byte[] data)
        {
            if (OperatingSystem.IsWindows())
            {
                return ProtectedData.Unprotect(data, null, DataProtectionScope.CurrentUser);
            }
            else
            {
                using var aes = Aes.Create()!;
                aes.Key = GetUserKey();
                byte[] iv = data[..16];
                byte[] cipher = data[16..];
                using var decryptor = aes.CreateDecryptor(aes.Key, iv);
                return decryptor.TransformFinalBlock(cipher, 0, cipher.Length);
            }
        }


        private static byte[] GetUserKey()
        {
            // Derive a 256-bit key from username and machine
            string keySource = Environment.UserName + "@" + Environment.MachineName;
            using var sha = SHA256.Create();
            return sha.ComputeHash(Encoding.UTF8.GetBytes(keySource));
        }

        private string GetTokenFile()
        {
            string homeUserProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            return Path.Join(homeUserProfile, ".devops", "token.bin");
        }

        private class Credentials
        {
            public string PersonalAccessToken { get; set; }

        }
    }
}