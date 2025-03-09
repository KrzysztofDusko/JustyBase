using JustyBase.Common.Contracts;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

namespace JustyBase.Common.Helpers;
public sealed class WindowsLinuxEncryptionHelper : IEncryptionHelper
{
    //Windows only !!
    //https://codingvision.net/c-safe-encryption-decryption-using-dpapi

    private static readonly byte[] Key = Encoding.UTF8.GetBytes("your-32-char-secret-key-here");
    private static readonly byte[] IV = [0x96, 0x52, 0xd7, 0xa0, 0x1f, 0x7d, 0xee, 0x2d, 0x9b, 0x66, 0x0c, 0x96, 0x5c, 0x06, 0x5c, 0x69];

    static WindowsLinuxEncryptionHelper()
    {
        if (!OperatingSystem.IsWindows())
        {
            Key = SHA256.HashData(File.ReadAllBytes(@"/var/lib/dbus/machine-id"));
        }
    }

    public string Encrypt(string text)
    {
        // first, convert the text to byte array 
        byte[] originalText = Encoding.Unicode.GetBytes(text);
        byte[] encryptedText = null;
        if (!OperatingSystem.IsWindows())
        {
            using (Aes aes = Aes.Create())
            {
                aes.Key = Key;
                aes.IV = IV;

                using (ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV))
                {
                    encryptedText = PerformCryptography(originalText, encryptor);
                }
            }

        }
        else
        {
            //throw new PlatformNotSupportedException();
            encryptedText = ProtectedData.Protect(originalText, null, DataProtectionScope.CurrentUser);
        }

        //and return the encrypted message 
        return Convert.ToBase64String(encryptedText);
    }
    public string Decrypt(string text)
    {
        // the encrypted text, converted to byte array 
        byte[]  encryptedText = Convert.FromBase64String(text);

        byte[] originalText = null;
        if (!OperatingSystem.IsWindows())
        {
            using (Aes aes = Aes.Create())
            {
                aes.Key = Key;
                aes.IV = IV;
                using (ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV))
                {
                    originalText = PerformCryptography(encryptedText, decryptor);
                }
            }
        }
        else
        {
            // calling Unprotect() that returns the original text 
            originalText = ProtectedData.Unprotect(encryptedText, null, DataProtectionScope.CurrentUser);
        }

        // finally, returning the result 
        return Encoding.Unicode.GetString(originalText);
    }
    public string GetEncodedContentOfTextFile(string realFilePath)
    {
        //if (File.Exists(realFilePath))
        //{
        //    Directory.Delete(@"/home/dusko/.config\JustDataEvo", true);
        //}
        string content = File.ReadAllText(realFilePath);
        content = Decrypt(content);
        return content;
    }
    public void SaveTextFileEncoded(string filePath, string fileContent)
    {
        fileContent = Encrypt(fileContent);
        File.WriteAllText(filePath, fileContent);
    }

    private static byte[] PerformCryptography(byte[] data, ICryptoTransform cryptoTransform)
    {
        using (MemoryStream memoryStream = new MemoryStream())
        {
            using (CryptoStream cryptoStream = new CryptoStream(memoryStream, cryptoTransform, CryptoStreamMode.Write))
            {
                cryptoStream.Write(data, 0, data.Length);
                cryptoStream.FlushFinalBlock();
                return memoryStream.ToArray();
            }
        }
    }
}