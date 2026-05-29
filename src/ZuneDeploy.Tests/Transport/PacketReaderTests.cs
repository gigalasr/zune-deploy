using ZuneDeploy.Transport;

namespace ZuneDeploy.Tests;

public class PacketReaderTests {

    private static void AssertCommandsEqual(ReceivableCommand expected, ReceivableCommand actual) {
        // Check they're the same type
        Assert.Equal(expected.GetType(), actual.GetType());

        // Compare all public fields
        var fields = expected.GetType().GetFields(
            System.Reflection.BindingFlags.Public |
            System.Reflection.BindingFlags.Instance
        );

        foreach (var field in fields) {
            var expectedValue = field.GetValue(expected);
            var actualValue = field.GetValue(actual);
            Assert.Equal(expectedValue, actualValue);
        }
    }

    private static byte[] FillWithZeros(byte[] buffer) {
        if (buffer.Length > Packet.PACKET_LENGTH) {
            throw new ArgumentException("Provided buffer is too large");
        }

        var packet = new byte[Packet.PACKET_LENGTH];
        buffer.CopyTo(packet, 0);

        return packet;
    }

    public static IEnumerable<object[]> GetParsingTestData() {
        // Packet with 2 Commands, no messages
        yield return new object[] {
            FillWithZeros([
                0x00, 0x00, 0x00, 0x06, 0x00, 0x00, 0x04, 0xa2,
                0x02, 0x10, 0x00, 0x00, 0x00, 0x02, 0xc1, 0x01
            ]),
            6,
            new ReceivableCommand[] {
                new StreamOpenedCommand(2, 4096),
                new StreamClosedCommand(1)
            },
            new Message[] {}
        };

        // Packet with XNAFTW ok response
        yield return new object[] {
            FillWithZeros([
                0x00, 0x00, 0x00, 0x07, 0x02, 0x00, 0x0e, 0x58,
                0x4e, 0x41, 0x46, 0x54, 0x57, 0x02, 0x00, 0x00,
                0x03, 0x02, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            ]),
            7,
            new ReceivableCommand[] {},
            new Message[] {
                new Message(2, new ReadOnlyMemory<byte>([0x58, 0x4e, 0x41, 0x46, 0x54, 0x57, 0x2, 0x0, 0x0, 0x3, 0x2, 0x0, 0x0, 0x0]))
            }
        };
    }

    [Theory]
    [MemberData(nameof(GetParsingTestData))]
    internal void ParsePackets(byte[] rawPacketBytes, int sequenceId, ReceivableCommand[] expectedCommands, Message[] expectedMessages) {
        PacketReader reader = new PacketReader(sequenceId);
        reader.FromDeviceBuffer(rawPacketBytes, out List<Message> actualMessages, out List<ReceivableCommand> actualCommands);

        // Check Messages were split correctly:
        Assert.Equal(expectedMessages.Length, actualMessages.Count);
        for (int i = 0; i < expectedMessages.Length; i++) {
            Assert.Equal(expectedMessages[i].StreamId, actualMessages[i].StreamId);
            Assert.Equal(expectedMessages[i].Data, actualMessages[i].Data);
        }

        // Check Commands were parsed correctly
        Assert.Equal(expectedCommands.Length, actualCommands.Count);
        for (int i = 0; i < expectedCommands.Length; i++) {
            AssertCommandsEqual(expectedCommands[i], actualCommands[i]);
        }
    }
}
