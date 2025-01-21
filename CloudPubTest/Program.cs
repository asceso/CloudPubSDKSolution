using CloudPubSDK;

internal class Program
{
    private static async Task Main()
    {
        CloudPubTunnel cloudPubTunnel = new();
        await cloudPubTunnel.InitCloudPub();
        await cloudPubTunnel.SetToken("zh16elMtw9c2CBVw47gZa4ka8qDk6LuEdXzb_m2IDoQ");
        string publicAddress = cloudPubTunnel.OpenTunnel(CloudPubTunnel.TunnelType.HTTPS, 8443);
        Uri publicUri = new(publicAddress);
        Console.WriteLine(publicUri.AbsoluteUri);
        Console.WriteLine(publicUri.Port);
        await Task.Delay(TimeSpan.FromSeconds(5));
        cloudPubTunnel.CloseTunnel();
    }
}