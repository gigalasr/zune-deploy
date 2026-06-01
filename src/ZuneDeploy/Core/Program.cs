using ZuneDeploy.Transport;
using NativeGen;
using ZuneDeploy.XNA;

namespace ZuneDeploy.Core;

class Program {
    private static Client? _client;

    static void Main(string[] args) {
        Console.CancelKeyPress += OnExit;

        var result = Client.TryConnect(out Client? device);
        if (result != Result.Ok || device == null) {
            Console.WriteLine($"Could not connect to deivce: {result}");
            return;
        }

        _client = device;

        // try {
        //     device.ConnectToService("lolorofl");
        // } catch (Exception e) {
        //     Console.WriteLine(e.Message);
        // }

        //Channel chan = new Channel(device, Guid.Empty);

        using (ServiceStream stream = device.ConnectToService("XnaChannelBroker")) {
            Thread.Sleep(5000);
        }

        Task.Run(() => {
            Console.WriteLine("brokerB");
            device.ConnectToService("XnaChannelBroker");
            Console.WriteLine("broker");
        });
        Task.Run(() => {
            Console.WriteLine("brokerC");
            device.ConnectToService("XnaChan1");
            Console.WriteLine("yuh");
        });

        while (true) { }
    }

    private static void OnExit(object? sender, ConsoleCancelEventArgs e) {
        if (_client != null) {
            _client.Close();
            _client = null;
        }
    }
}
