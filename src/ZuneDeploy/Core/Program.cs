using ZuneDeploy.Messaging;
using NativeGen;

namespace ZuneDeploy.Core;

/*
 * Next Steps:
 * - NEXT: Split Packet Class Into PacketBuilder and PacketParser
 *   - Device owns PacketBuilder and PacketParser 
 *   - PacketParser can have events for all commands, device subscribes
 *   - Device uses PacketBuilder as a demuxer for the streams
 *   - Device publishes messages from PackerParser to the correct streams   
 * - Implement PacketStream Class -v
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
