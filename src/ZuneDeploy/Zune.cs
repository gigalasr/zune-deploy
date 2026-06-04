using ZuneDeploy.Transport;
using ZuneDeploy.XNA.Channels;
using ZuneDeploy.XNA.Protocol;

namespace ZuneDeploy;

public class Zune {
    private Client _client;

    public Zune() {
        _client = new Client();
    }

    /// <summary>
    /// Opens a new <see cref="ServiceStream"/> to a remote service
    /// </summary>
    /// <param name="serviceId">Id of the service on the Zune. See <see cref="Service"> for valid ids.</param>
    /// <returns><see cref="ServiceStream"/></returns>
    public ServiceStream OpenStream(string serviceId) {
        return _client.ConnectToService(serviceId);
    }

    /// <summary>
    /// Opens a new <see cref="Channel"/> to a remote XNA service
    /// </summary>
    /// <param name="guid">Id of the XNA service</param>
    /// <returns><see cref="Channel"/></returns>
    public Channel OpenXNAChannel(Guid guid) {
        return new Channel(_client, guid);
    }

    /// <summary>
    /// Opens a new <see cref="Channel"/> to the remote XNA Deploy Service.
    /// Use this channel to deploy files, games, applications to the Zune.
    /// </summary>
    /// <returns><see cref="DeployChannel"/></returns>
    public DeployChannel OpenXNADeployChannel() {
        return new DeployChannel(_client);
    }
}
