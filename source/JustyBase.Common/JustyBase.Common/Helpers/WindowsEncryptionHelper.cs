using JustyBase.Common.Contracts;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

namespace JustyBase.Common.Helpers;
public sealed class WindowsEncryptionHelper : IEncryptionHelper
{
    //Windows only !!
    //https://codingvision.net/c-safe-encryption-decryption-using-dpapi
    public string Encrypt(string text)
    {
        if (!OperatingSystem.IsWindows())
        {
            throw new PlatformNotSupportedException();
        }

        // first, convert the text to byte array 
        byte[] originalText = Encoding.Unicode.GetBytes(text);

        // then use Protect() to encrypt your data 
        byte[] encryptedText = ProtectedData.Protect(originalText, null, DataProtectionScope.CurrentUser);

        //and return the encrypted message 
        return Convert.ToBase64String(encryptedText);
    }
    public string Decrypt(string text)
    {
        if (!OperatingSystem.IsWindows())
        {
            throw new PlatformNotSupportedException();
        }
        // the encrypted text, converted to byte array 
        byte[] encryptedText = Convert.FromBase64String(text);

        // calling Unprotect() that returns the original text 
        byte[] originalText = ProtectedData.Unprotect(encryptedText, null, DataProtectionScope.CurrentUser);

        // finally, returning the result 
        return Encoding.Unicode.GetString(originalText);
    }
    public string GetEncodedContentOfTextFile(string realFilePath)
    {
        string content = File.ReadAllText(realFilePath);
        content = Decrypt(content);

        return content;
    }
    public void SaveTextFileEncoded(string filePath, string fileContent)
    {
        fileContent = Encrypt(fileContent);
        File.WriteAllText(filePath, fileContent);
    }
}