namespace NbnStock.Core.Services;

public interface ICredentialVault
{
    string Encrypt(string plainText);
    string Decrypt(string cipherText);
}