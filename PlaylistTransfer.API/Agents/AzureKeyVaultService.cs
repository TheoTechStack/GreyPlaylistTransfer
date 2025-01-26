using Azure.Identity;
using Azure.Security.KeyVault.Secrets;

namespace PlaylistTransfer.API.Agents;

public class AzureKeyVaultService(string? keyVaultUri)
{
    private readonly SecretClient _secretClient = new(new Uri(keyVaultUri ?? throw new ArgumentNullException(nameof(keyVaultUri))), new DefaultAzureCredential());

    public async Task<string> GetSecretAsync(string secretName)
    {
        try
        {
            var secret = await _secretClient.GetSecretAsync(secretName);
            return secret.Value.Value;
        }
        catch (Exception ex)
        {
            throw new Exception($"Error fetching secret '{secretName}' from Azure Key Vault.", ex);
        }
    } 
}