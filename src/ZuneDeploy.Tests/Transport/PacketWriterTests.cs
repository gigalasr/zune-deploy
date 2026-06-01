using System.Buffers.Binary;
using System.Data.SqlTypes;
using System.Reflection.Metadata;
using System.Security.Cryptography;
using ZuneDeploy.Transport;

namespace ZuneDeploy.Tests;




public class PacketWriterTests {
    private static void AssertPayloadMatches(ReadOnlySpan<byte> expected, ReadOnlySpan<byte> actual) {
        // Compare everything except Hash and Random Bytes
        try {
            Assert.Equal(
                expected.Slice(0, Packet.PAYLOAD_LENGTH + Packet.SEQID_LENGTH),
                actual.Slice(0, Packet.PAYLOAD_LENGTH + Packet.SEQID_LENGTH)
            );
        } catch (Exception e) {
            HexDump.DumpDiffPacket(expected, actual);
            throw e;
        }
    }

    private static void AssertHashesMatch(ReadOnlySpan<byte> expected, ReadOnlySpan<byte> actual) {
        // Verify Hash (we do this on the actual packet because it contains the random bytes)
        // If we have reached this point, the rest equals the expected packet anyways
        var expectedHash = SHA1.HashData(actual.Slice(0, Packet.PAYLOAD_LENGTH + Packet.RANDOM_BYTES_LENGTH + Packet.SEQID_LENGTH));
        var actualHash = actual.Slice(Packet.HASH_OFFSET, Packet.PACKET_LENGTH - Packet.HASH_OFFSET);
        Assert.Equal(expectedHash, actualHash);
    }

    [Fact]
    public void SingleCommand() {
        var expectedPacket = TestUtil.FillPacket([
            // Sequence Id
            0x0, 0x0,  0x0, 0x0,  
            // OpenStreamCommand
            0x0, 0x0, 0x23, 0xa1, 0x1, 0x10, 0x0, 0x58,
            0x0, 0x6e, 0x0, 0x61, 0x0, 0x43, 0x0, 0x68,
            0x0, 0x61, 0x0, 0x6e, 0x0, 0x6e, 0x0, 0x65,
            0x0, 0x6c, 0x0, 0x42, 0x0, 0x72, 0x0, 0x6f,
            0x0, 0x6b, 0x0, 0x65, 0x0, 0x72, 0x0, 0x0,
        ]).AsSpan(); ;

        StreamCollection collection = new StreamCollection();
        PacketWriter writer = new PacketWriter(collection);

        writer.SendCommand(
            new OpenStreamCommand(1, "XnaChannelBroker")
        );

        bool didCreatePacket = writer.GetNextPacket(out byte[]? actualPacket);
        Assert.True(didCreatePacket);
        Assert.NotNull(actualPacket);

        Packet.ValidatePacket(actualPacket, 0);

        var actualPacketSpan = actualPacket.AsSpan();
        AssertPayloadMatches(expectedPacket, actualPacketSpan);
        AssertHashesMatch(expectedPacket, actualPacketSpan);
    }

    [Fact]
    public void MutlipleCommands() {
        var expectedPacket = TestUtil.FillPacket([
            // Sequence Id
            0x0, 0x0,  0x0, 0x0, 
            // OpenStreamCommand
            0x0, 0x0, 0x23, 0xa1, 0x1, 0x10, 0x0, 0x58,
            0x0, 0x6e, 0x0, 0x61, 0x0, 0x43, 0x0, 0x68,
            0x0, 0x61, 0x0, 0x6e, 0x0, 0x6e, 0x0, 0x65,
            0x0, 0x6c, 0x0, 0x42, 0x0, 0x72, 0x0, 0x6f,
            0x0, 0x6b, 0x0, 0x65, 0x0, 0x72, 
            // AckOpenCommand
            0x0, 0x0,  0x2, 0xa4, 0x1, 
            // CloseStreamCommand
            0x0, 0x0,  0x2, 0xc1, 0x1,

        ]).AsSpan();

        StreamCollection collection = new StreamCollection();
        PacketWriter writer = new PacketWriter(collection);

        writer.SendCommand(new OpenStreamCommand(1, "XnaChannelBroker"));
        writer.SendCommand(new AckOpenCommand(1));
        writer.SendCommand(new CloseStreamCommand(1));

        bool didCreatePacket = writer.GetNextPacket(out byte[]? actualPacket);
        Assert.True(didCreatePacket);
        Assert.NotNull(actualPacket);

        Packet.ValidatePacket(actualPacket, 0);

        var actualPacketSpan = actualPacket.AsSpan();
        AssertPayloadMatches(expectedPacket, actualPacketSpan);
        AssertHashesMatch(expectedPacket, actualPacketSpan);
    }

    [Fact]
    public void SingleMessage_FittingIntoBuffer() {
        StreamCollection collection = new StreamCollection();
        PacketWriter writer = new PacketWriter(collection);

        ServiceStream stream = collection.OpenStream(null);
        collection.OnStreamOpened(stream.StreamId, 256);

        byte[] message = [0x1, 0x2, 0x3, 0x4, 0x5, 0x6, 0x7, 0x8, 0x9, 0xa];
        var expectedPacket = TestUtil.FillPacket([
            // Sequence Id
            0x0, 0x0,  0x0, 0x0, 
            // Message [stream][lenhi][lenlow] 
            stream.StreamId, (byte)(message.Length >> 8), (byte)(message.Length),
            // Message Contents
            ..message
        ]).AsSpan();


        stream.Write(message);
        stream.Flush();

        bool didCreatePacket = writer.GetNextPacket(out byte[]? actualPacket);

        Assert.True(didCreatePacket);
        Assert.NotNull(actualPacket);

        Packet.ValidatePacket(actualPacket, 0);

        var actualPacketSpan = actualPacket.AsSpan();
        AssertPayloadMatches(expectedPacket, actualPacketSpan);
        AssertHashesMatch(expectedPacket, actualPacketSpan);
    }

    [Fact]
    public void SingleMessage_LargerThanBuffer() {
        StreamCollection collection = new StreamCollection();
        PacketWriter writer = new PacketWriter(collection);

        ServiceStream stream = collection.OpenStream(null);
        ushort CAPACITY = 256;
        collection.OnStreamOpened(stream.StreamId, CAPACITY);

        var message = new byte[500].AsSpan();
        Random.Shared.NextBytes(message);

        var messagePart1 = message.Slice(0, CAPACITY);
        var messagePart2 = message.Slice(messagePart1.Length, message.Length - messagePart1.Length);

        var expectedPacket1 = TestUtil.FillPacket([
            // Sequence Id
            0x0, 0x0,  0x0, 0x0, 
            // Message [stream][lenhi][lenlow] 
            stream.StreamId, (byte)(messagePart1.Length >> 8), (byte)messagePart1.Length,
            // Message Contents
            ..messagePart1
        ]).AsSpan();

        var expectedPacket2 = TestUtil.FillPacket([
            // Sequence Id
            0x0, 0x0,  0x0, 0x1, 
            // Message [stream][lenhi][lenlow] 
            stream.StreamId, (byte)(messagePart2.Length >> 8), (byte)messagePart2.Length,
            // Message Contents
            ..messagePart2
        ]).AsSpan();


        stream.Write(message);
        stream.Flush();

        // First Packet
        bool didCreatePacket = writer.GetNextPacket(out byte[]? actualPacket);

        Assert.True(didCreatePacket);
        Assert.NotNull(actualPacket);


        var actualPacketSpan = actualPacket.AsSpan();
        Packet.ValidatePacket(actualPacket, 0);
        AssertPayloadMatches(expectedPacket1, actualPacketSpan);
        AssertHashesMatch(expectedPacket1, actualPacketSpan);

        collection.OnDataProcessed(stream.StreamId, (ushort)messagePart1.Length);

        // Second Packet
        didCreatePacket = writer.GetNextPacket(out byte[]? actualPacket2);

        Assert.True(didCreatePacket);
        Assert.NotNull(actualPacket2);

        var actualPacketSpan2 = actualPacket2.AsSpan();
        Packet.ValidatePacket(actualPacket2, 1);
        AssertPayloadMatches(expectedPacket2, actualPacketSpan2);
        AssertHashesMatch(expectedPacket2, actualPacketSpan2);
    }

    [Fact]
    public void SingleMessage_LargerThanBuffer_NoDataConsumedCommand() {
        StreamCollection collection = new StreamCollection();
        PacketWriter writer = new PacketWriter(collection);

        ServiceStream stream = collection.OpenStream(null);
        ushort CAPACITY = 256;
        collection.OnStreamOpened(stream.StreamId, CAPACITY);

        var message = new byte[500].AsSpan();
        Random.Shared.NextBytes(message);

        stream.Write(message);
        stream.Flush();

        // We do not call stream.OnDataProcessed - so only one packet should be created
        Assert.True(writer.GetNextPacket(out byte[]? _));
        Assert.Equal(0, collection.GetBufferCapacityForStream(stream.StreamId));
        Assert.False(writer.GetNextPacket(out byte[]? _));
    }

    [Fact]
    public void MultipleStreams_FittingIntoBuffer() {
        StreamCollection collection = new StreamCollection();
        PacketWriter writer = new PacketWriter(collection);

        ServiceStream streamA = collection.OpenStream(null);
        collection.OnStreamOpened(streamA.StreamId, 256);

        ServiceStream streamB = collection.OpenStream(null);
        collection.OnStreamOpened(streamB.StreamId, 256);

        byte[] messageA = [0x1, 0x2, 0x3, 0x4, 0x5, 0x6, 0x7, 0x8, 0x9, 0xa];
        byte[] messageB = [0x11, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77, 0x88, 0x99, 0xaa];

        var expectedPacket = TestUtil.FillPacket([
            // Sequence Id
            0x0, 0x0,  0x0, 0x0, 
            // Message A [stream][lenhi][lenlow] 
            streamA.StreamId, (byte)(messageA.Length >> 8), (byte)(messageA.Length),
            // Message A Contents
            ..messageA,
            // Message B [stream][lenhi][lenlow] 
            streamB.StreamId, (byte)(messageB.Length >> 8), (byte)(messageB.Length),
            // Message B Contents
            ..messageB
        ]).AsSpan();


        streamA.Write(messageA);
        streamA.Flush();

        streamB.Write(messageB);
        streamB.Flush();

        bool didCreatePacket = writer.GetNextPacket(out byte[]? actualPacket);

        Assert.True(didCreatePacket);
        Assert.NotNull(actualPacket);

        var actualPacketSpan = actualPacket.AsSpan();
        Packet.ValidatePacket(actualPacket, 0);
        AssertPayloadMatches(expectedPacket, actualPacketSpan);
        AssertHashesMatch(expectedPacket, actualPacketSpan);
    }

    [Fact]
    public void MultipleStreamsWithCommand_FittingIntoBuffer() {
        StreamCollection collection = new StreamCollection();
        PacketWriter writer = new PacketWriter(collection);

        ServiceStream streamA = collection.OpenStream(null);
        collection.OnStreamOpened(streamA.StreamId, 256);

        ServiceStream streamB = collection.OpenStream(null);
        collection.OnStreamOpened(streamB.StreamId, 256);

        byte[] messageA = [0x1, 0x2, 0x3, 0x4, 0x5, 0x6, 0x7, 0x8, 0x9, 0xa];
        byte[] messageB = [0x11, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77, 0x88, 0x99, 0xaa];

        var expectedPacket = TestUtil.FillPacket([
            // Sequence Id
            0x0, 0x0,  0x0, 0x0, 
            // AckOpenCommand
            0x0, 0x0,  0x2, 0xa4, 0x1, 
            // Close Stream Command
            0x0, 0x0,  0x2, 0xc1, 0x1,
            // Message A [stream][lenhi][lenlow] 
            streamA.StreamId, (byte)(messageA.Length >> 8), (byte)(messageA.Length),
            // Message A Contents
            ..messageA,
            // Message B [stream][lenhi][lenlow] 
            streamB.StreamId, (byte)(messageB.Length >> 8), (byte)(messageB.Length),
            // Message B Contents
            ..messageB
        ]).AsSpan();

        writer.SendCommand(new AckOpenCommand(0x1));
        writer.SendCommand(new CloseStreamCommand(0x1));

        streamA.Write(messageA);
        streamA.Flush();

        streamB.Write(messageB);
        streamB.Flush();

        bool didCreatePacket = writer.GetNextPacket(out byte[]? actualPacket);

        Assert.True(didCreatePacket);
        Assert.NotNull(actualPacket);

        var actualPacketSpan = actualPacket.AsSpan();
        Packet.ValidatePacket(actualPacket, 0);
        AssertPayloadMatches(expectedPacket, actualPacketSpan);
        AssertHashesMatch(expectedPacket, actualPacketSpan);
    }

    [Fact]
    public void ManyCommnads_TooBigForOnePacket() {
        StreamCollection collection = new StreamCollection();
        PacketWriter writer = new PacketWriter(collection);

        int streamLength = Packet.USABLE_PAYLOAD_LENGTH * 2;

        // Write commands into queue
        int totalBytes = 0;
        var cmd = new CloseStreamCommand(0x42);
        while (totalBytes < streamLength) {
            totalBytes += cmd.LengthIncludingHeader;
            writer.SendCommand(cmd);
        }

        // Generate expected packets
        List<byte[]> expectedPackets = new List<byte[]>();
        int numExpectedPackets = (int)Math.Ceiling(totalBytes / (double)Packet.USABLE_PAYLOAD_LENGTH);
        for (int i = 0; i < numExpectedPackets; i++) {
            byte[] packet = TestUtil.FillPacket([
                // Sequence Id
                0x0, 0x0, 0x0, (byte)i
            ]);
            int position = 4;
            while (position - 4 <= Packet.USABLE_PAYLOAD_LENGTH - cmd.LengthIncludingHeader && totalBytes > 0) {
                packet[position++] = 0;
                packet[position++] = (byte)(cmd.RawBytes.Length >> 8);
                packet[position++] = (byte)cmd.RawBytes.Length;
                cmd.RawBytes.CopyTo(packet, position);
                totalBytes -= cmd.LengthIncludingHeader;
                position += cmd.RawBytes.Length;
            }
            expectedPackets.Add(packet);
        }

        // Compare with actual packets
        uint seqid = 0;
        foreach (byte[] expected in expectedPackets) {
            bool didCreatePacket = writer.GetNextPacket(out byte[]? actualPacket);
            Assert.True(didCreatePacket);
            Packet.ValidatePacket(actualPacket, seqid++);
            AssertPayloadMatches(expected, actualPacket);
            AssertHashesMatch(expected, actualPacket);
        }

        // There should be no packets remaning
        bool didCreatePacketLast = writer.GetNextPacket(out byte[]? actualPacketLast);
        Assert.False(didCreatePacketLast);
        Assert.Null(actualPacketLast);
    }

    [Fact]
    public void ManyCommandsTwoMessages_TooLargeForOnePacket() {
        StreamCollection collection = new StreamCollection();
        PacketWriter writer = new PacketWriter(collection);

        ServiceStream streamA = collection.OpenStream(null);
        collection.OnStreamOpened(streamA.StreamId, 4096);

        ServiceStream streamB = collection.OpenStream(null);
        collection.OnStreamOpened(streamB.StreamId, 4096);

        // Fill halve of packet with commands
        var command = new CloseStreamCommand(0x42);
        int numCommands = Packet.PAYLOAD_LENGTH / command.LengthIncludingHeader / 2;
        byte[] commandBuffer = new byte[numCommands * command.LengthIncludingHeader];
        for (int i = 0; i < numCommands; i++) {
            writer.SendCommand(command);
            commandBuffer[i * 5 + 0] = 0;
            commandBuffer[i * 5 + 1] = 0;
            commandBuffer[i * 5 + 2] = (byte)command.RawBytes.Length;
            command.RawBytes.CopyTo(commandBuffer, i * 5 + 3);
        }

        // Create messages and packets
        byte[] messageA = new byte[Packet.USABLE_PAYLOAD_LENGTH];
        Random.Shared.NextBytes(messageA);
        byte[] messageB = new byte[Packet.USABLE_PAYLOAD_LENGTH / 2];
        Random.Shared.NextBytes(messageB);

        streamA.Write(messageA);
        streamA.Flush();

        streamB.Write(messageB);
        streamB.Flush();

        var messageAPart1 = messageA.AsSpan().Slice(0, Packet.USABLE_PAYLOAD_LENGTH - commandBuffer.Length - Message.HeaderLength);
        var messageAPart2 = messageA.AsSpan().Slice(messageAPart1.Length);

        var messageBPart1 = messageB.AsSpan().Slice(0, Packet.USABLE_PAYLOAD_LENGTH - messageAPart2.Length - Message.HeaderLength * 2);
        var messageBPart2 = messageB.AsSpan().Slice(messageBPart1.Length);

        byte[][] expectedPackets = {
            TestUtil.FillPacket([
                0x0, 0x0, 0x0, 0x0,
                ..commandBuffer,
                0x0, ..TestUtil.UShort(messageAPart1.Length), ..messageAPart1,
            ]),
            TestUtil.FillPacket([
                0x0, 0x0, 0x0, 0x1,
                0x0, ..TestUtil.UShort(messageAPart2.Length), ..messageAPart2,
                0x1, ..TestUtil.UShort(messageBPart1.Length), ..messageBPart1,
            ]),
            TestUtil.FillPacket([
                0x0, 0x0, 0x0, 0x2,
                0x1, ..TestUtil.UShort(messageBPart2.Length), ..messageBPart2,
            ])
        };

        for (int i = 0; i < expectedPackets.Length; i++) {
            bool didCreatePacket = writer.GetNextPacket(out byte[]? packet);

            Assert.True(didCreatePacket);
            Packet.ValidatePacket(packet, (uint)i);
            AssertPayloadMatches(expectedPackets[i], packet);
            AssertHashesMatch(expectedPackets[i], packet);
        }
    }
}