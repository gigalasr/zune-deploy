
using System.Buffers.Binary;
using ZuneDeploy.Transport;

namespace ZuneDeploy.Tests;

public class PacketTests {

    [Fact]
    public void GarbagePacket() {
        byte[] packet = new byte[Packet.PACKET_LENGTH];
        Random.Shared.NextBytes(packet);
        Assert.Throws<Exception>(() => {
            Packet.ValidateMessageList(packet);
        });
    }

    [Fact]
    public void OneBigMessage() {
        byte[] packet = new byte[Packet.PACKET_LENGTH];
        var payload = Packet.UseablePayloadSpan(packet);
        Random.Shared.NextBytes(payload);
        BinaryPrimitives.WriteUInt16BigEndian(payload.Slice(1, 2), (ushort)(payload.Length - Message.HeaderLength));
        Packet.ValidateMessageList(packet);
    }

    [Fact]
    public void MultipleMessages_NotTilEnd() {
        byte[] packet = new byte[Packet.PACKET_LENGTH];
        var payload = Packet.UseablePayloadSpan(packet);

        int offset = 0;
        for (int i = 0; i < 4; i++) {
            int length = 10 * (i + 1);
            var message = payload.Slice(offset, length + Message.HeaderLength);
            Random.Shared.NextBytes(message);
            BinaryPrimitives.WriteUInt16BigEndian(message.Slice(1, 2), (ushort)length);
        }

        Packet.ValidateMessageList(packet);
    }

    [Fact]
    public void MultipleMessages_TilEnd_Random() {
        byte[] packet = new byte[Packet.PACKET_LENGTH];
        var payload = Packet.UseablePayloadSpan(packet);

        int offset = 0;
        while (true) {
            int remaining = payload.Length - offset;
            if (remaining < Message.HeaderLength + Message.MinBlockSize) {
                break;
            }

            int length = Math.Min(remaining, Random.Shared.Next(Message.HeaderLength + Message.MinBlockSize, 64));
            var message = payload.Slice(offset, length);
            Random.Shared.NextBytes(message);
            BinaryPrimitives.WriteUInt16BigEndian(message.Slice(1, 2), (ushort)(length - Message.HeaderLength));
            offset += message.Length;
        }

        Packet.ValidateMessageList(packet);
    }

    [Fact]
    public void MultipleMessages_IncorrectLength() {
        byte[] packet = new byte[Packet.PACKET_LENGTH];
        var payload = Packet.UseablePayloadSpan(packet);

        int offset = 0;
        for (int i = 0; i < 4; i++) {
            int length = 10 * (i + 1);
            var message = payload.Slice(offset, length + Message.HeaderLength);
            Random.Shared.NextBytes(message);
            BinaryPrimitives.WriteUInt16BigEndian(message.Slice(1, 2), (ushort)Random.Shared.Next());
        }

        Assert.Throws<Exception>(() => {
            Packet.ValidateMessageList(packet);
        });
    }
}