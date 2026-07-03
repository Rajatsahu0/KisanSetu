namespace KisanMitraAI.Core.Security;

/// <summary>
/// Service for encrypting and decrypting sensitive data at rest using AES-256 encryption
/// </summary>
public interface IEncryptionService
{
    /// <summary>
    /// Encrypts plaintext data using AES-256 encryption with AWS KMS managed keys
    /// </summary>
    /// <param name="plaintext">The data to encrypt</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Base64-encoded encrypted data</returns>
    Task<string> EncryptAsync(string plaintext, CancellationToken cancellationToken = default);

    /// <summary>
    /// Decrypts encrypted data using AES-256 encryption with AWS KMS managed keys
    /// </summary>
    /// <param name="ciphertext">Base64-encoded encrypted data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Decrypted plaintext data</returns>
    Task<string> DecryptAsync(string ciphertext, CancellationToken cancellationToken = default);

    /// <summary>
    /// Encrypts multiple fields in a batch operation
    /// </summary>
    /// <param name="plaintexts">Dictionary of field names to plaintext values</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Dictionary of field names to encrypted values</returns>
    Task<Dictionary<string, string>> EncryptBatchAsync(
        Dictionary<string, string> plaintexts, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Decrypts multiple fields in a batch operation
    /// </summary>
    /// <param name="ciphertexts">Dictionary of field names to encrypted values</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Dictionary of field names to decrypted values</returns>
    Task<Dictionary<string, string>> DecryptBatchAsync(
        Dictionary<string, string> ciphertexts, 
        CancellationToken cancellationToken = default);
}
