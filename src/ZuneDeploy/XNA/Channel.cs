using ZuneDeploy.Transport;

namespace ZuneDeploy.XNA;

public class Channel {

    public Channel(Client client, Guid channelId) {
        using (ServiceStream stream = client.ConnectToService(Service.ChannelBroker)) {
            Thread.Sleep(5000);
        }
    }

}