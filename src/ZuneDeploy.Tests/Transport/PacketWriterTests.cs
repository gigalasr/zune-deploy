using ZuneDeploy.Transport;

namespace ZuneDeploy.Tests;




public class PacketWriterTests {
    [Fact]
    public void SingleCommand() {
        var expectedPacket = TestUtil.CreatePacket([
            0x0, 0x0,  0x0, 0x0,  0x0, 0x0, 0x23, 0xa1,
            0x1, 0x10, 0x0, 0x58, 0x0, 0x6e, 0x0, 0x61,
            0x0, 0x43, 0x0, 0x68, 0x0, 0x61, 0x0, 0x6e,
            0x0, 0x6e, 0x0, 0x65, 0x0, 0x6c, 0x0, 0x42,
            0x0, 0x72, 0x0, 0x6f, 0x0, 0x6b, 0x0, 0x65,
            0x0, 0x72, 0x0, 0x0,  0x0, 0x0,  0x0, 0x0
        ]).AsSpan(); ;

        StreamCollection collection = new StreamCollection();
        PacketWriter writer = new PacketWriter(collection);

        writer.SendCommand(
            new OpenStreamCommand(0, "XnaChannelBroker")
        );

        bool didCreatePacket = writer.GetNextPacket(out byte[]? actualPacket);
        Assert.True(didCreatePacket);
        Assert.NotNull(actualPacket);

        // Compare everything except Hash and Random Bytes
        var actualPacketSpan = actualPacket.AsSpan();
        Assert.Equal(
            expectedPacket.Slice(0, Packet.PAYLOAD_LENGTH + Packet.SEQID_LENGTH),
            actualPacketSpan.Slice(0, Packet.PAYLOAD_LENGTH + Packet.SEQID_LENGTH)
        );
    }


}