using ZuneDeploy.Transport;
using NativeGen;
using ZuneDeploy.XNA;

namespace ZuneDeploy.Core;

class Program {
    private static Client? _client;

    static void Main(string[] args) {
        Console.CancelKeyPress += OnExit;


        _client = new Client();
        Channel chan = new Channel(_client, Channel.ApplicationLaunchChannel);

        while (true) { }
    }

    private static void OnExit(object? sender, ConsoleCancelEventArgs e) {
        if (_client != null) {
            _client.Close();
            _client = null;
        }
    }
}
