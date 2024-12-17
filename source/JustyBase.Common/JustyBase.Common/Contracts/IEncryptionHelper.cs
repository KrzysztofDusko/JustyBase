namespace JustyBase.Common.Contracts
{
    public interface IEncryptionHelper
    {
        string Decrypt(string text);
        string Encrypt(string text);
        string GetEncodedContentOfTextFile(string realFilePath);
        void SaveTextFileEncoded(string filePath, string fileContent);
    }
}