namespace PlaylistTransfer.API.Agents;

public class SpotifyCredentialsProvider(AzureKeyVaultService keyVaultService)
{
    public async Task<(string clientId, string clientSecret)> GetSpotifyCredentialsAsync()
    {
        var clientId = await keyVaultService.GetSecretAsync("ClientId");
        var clientSecret = await keyVaultService.GetSecretAsync("ClientSecret");

        return (clientId, clientSecret);
    }
}