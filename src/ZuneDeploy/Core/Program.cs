using ZuneDeploy.Messaging;
using NativeGen;

namespace ZuneDeploy.Core;

/*
 * Next Steps:
 * - Implement Command Parsing
 *   - Classes for at least the following commands:
 *   - OR implement as events in Device.cs
 *     - RequestConnect
 *     - AcceptRequest
 *     - AcknowledgeAccept
 *     - Disconnect
 *     - StreamClosed
 *     - KeepAlive
 *     - DataConsumed
 * - Implement Command Creation
 * - Implement Reader & Writer with control flow 
 * - Implement XNA Brokered Channel 
 * - Implement XNA Message Request & Response Parsing
 * - Implement Deploy, Launch, Container XNA Brokered Channels 
 */

class Program {
    private static Device? _device;

    static void Main(string[] args) {
        Console.CancelKeyPress += OnExit;

        var result = Device.TryConnect(out Device? device);
        if (result != Result.Ok || device == null) {
            Console.WriteLine($"Could not connect to deivce: {result}");
            return;
        }

        _device = device;

        while (true) { }
    }

    private static void OnExit(object? sender, ConsoleCancelEventArgs e) {
        if (_device != null) {
            _device.Close();
            _device = null;
        }
    }
}
