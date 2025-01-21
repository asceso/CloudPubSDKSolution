using CloudPubSDK;

internal class Program
{
    private static async Task Main()
    {
        CloudPubTunnel cloudPubTunnel = new();
        await cloudPubTunnel.InitCloudPub();
        await cloudPubTunnel.SetToken("YOUR_TOKEN");
        string publicAddress = cloudPubTunnel.OpenTunnel(CloudPubTunnel.TunnelType.HTTPS, 8443);
        Uri publicUri = new(publicAddress);
        Console.WriteLine(publicUri.AbsoluteUri);
        Console.WriteLine(publicUri.Port);
        await Task.Delay(TimeSpan.FromSeconds(5));
        cloudPubTunnel.CloseTunnel();
    }
}
