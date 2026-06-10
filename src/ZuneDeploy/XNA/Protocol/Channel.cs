using ZuneDeploy.Transport;

namespace ZuneDeploy.XNA.Protocol;


public class Channel : IDisposable {

    private static readonly RemoteProcedure _createChannel = new RemoteProcedure(
        "CreateChannel",
        [new Parameter("ChannelId", ParameterType.Guid)]
    );

    private readonly Schema _schema;
    private readonly ServiceStream _stream;
    private bool _disposed = false;

    public event EventHandler<ushort>? OnBytesWritten;

    public Channel(Client client, Guid channelId) {
        int serviceIdTag = -1;

        using (ServiceStream stream = client.ConnectToService(Service.ChannelBroker)) {
            Request.WriteToStream(stream, _createChannel, channelId);
            serviceIdTag = Response.ReadFromStream<int>(stream);
        }

        string serviceId = Service.XnaChannel(serviceIdTag);
        _stream = client.ConnectToService(serviceId);
        _schema = Schema.ReadFromStream(_stream);

        _stream.OnBytesWritten += (_, bytes) => {
            OnBytesWritten?.Invoke(null, bytes);
        };
    }

    public override string ToString() {
        return _schema.ToString();
    }

    public void Dispose() {
        if (_disposed) {
            return;
        }

        _disposed = true;
        _stream.Dispose();
        GC.SuppressFinalize(this);
    }

    ~Channel() {
        Dispose();
    }

    protected T Invoke<T>(string name, params object[] arguments) {
        return (T)Invoke(name, arguments);
    }

    protected object Invoke(string name, params object[] arguments) {
        ArgumentNullException.ThrowIfNull(arguments);
        RemoteProcedure definition = _schema.GetDefinition(name);

        if (arguments.Length != definition.Parameters.Count) {
            throw new ArgumentException($"Invalid number of argumnets for '{name}'", "arguments");
        }

        Request.WriteToStream(_stream, definition, arguments);
        var response = Response.ReadFromStream(_stream);
        while (response.IsDataStreamRequest) {
            int id = Convert.ToUInt16(response.Value) - 1;

            var stream = arguments[id];
            if (stream is not Stream) {
                throw new Exception($"Parameter at index id={id} is not a stream");
            }

            Request.WriteDataStreamToStream(_stream, (Stream)stream);
            response = Response.ReadFromStream(_stream);
        }

        return response.Value;
    }
}
