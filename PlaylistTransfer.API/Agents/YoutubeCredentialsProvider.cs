namespace PlaylistTransfer.API.Agents;

public class YoutubeCredentialsProvider(AzureKeyVaultService keyVaultService)
{
    public async Task<(string clientId, string clientSecret)> GetYoutubeCredentialsAsync()
    {
        var clientId = await keyVaultService.GetSecretAsync("YoutubeClientId");
        var clientSecret = await keyVaultService.GetSecretAsync("YoutubeClientSecret");

        return (clientId, clientSecret);
    }
}