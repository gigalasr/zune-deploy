using System.Buffers.Binary;
using System.Security.Cryptography;
using ZuneDeploy.Transport;

internal static class Packet {
    public const int PACKET_LENGTH = 1264;
    public const int PAYLOAD_LENGTH = 1236;
    // The last 3 bytes of a payload have to be 0
    public const int USABLE_PAYLOAD_LENGTH = 1233;
    public const int RANDOM_BYTES_OFFSET = 1240;
    public const int RANDOM_BYTES_LENGTH = 4;
    public const int HASH_OFFSET = 1244;
    public const int SEQID_LENGTH = 4;
    public const int PAYLOAD_END = SEQID_LENGTH + PAYLOAD_LENGTH - 1;

    /// <summary>
    /// Create a slice that includes the packet's hash.
    /// </summary>
    /// <param name="buffer">span to create slice from</param>
    /// <returns>Slice that only includes the packets hash area</returns>
    public static Span<byte> HashSpan(Span<byte> buffer) {
        return buffer.Slice(HASH_OFFSET, PACKET_LENGTH - HASH_OFFSET);
    }

    /// <summary>
    /// Create a slice that includes the packet's payload area.
    /// This is the area that contains messages and commands.
    /// </summary>
    /// <param name="buffer">span to create slice from</param>
    /// <returns>Slice that only includes the payload area</returns>
    public static Span<byte> PayloadSpan(Span<byte> buffer) {
        return buffer.Slice(SEQID_LENGTH, PAYLOAD_LENGTH);
    }

    /// <summary>
    /// Create a slice that includes the packet's useable payload area.
    /// I.e. tje same as <see cref="Packet.PayloadSpan"/>, but 3 bytes less,
    /// as the last 3 bytes have to be 0
    /// </summary>
    /// <param name="buffer">span to create slice from</param>
    /// <returns>Slice that only includes the payload area</returns>
    public static Span<byte> UseablePayloadSpan(Span<byte> buffer) {
        return buffer.Slice(SEQID_LENGTH, USABLE_PAYLOAD_LENGTH);
    }



    /// <summary>
    /// Create a slice that includes the packet's payload area and sequence Id.
    /// This is essentially the same as <see cref="Packet.PayloadSpan"/>, but it starts 4 bytes earlier.
    /// This span is mainly used to compute the hash of the packet.
    /// </summary>
    /// <param name="buffer">span to create slice from</param>
    /// <returns>Slice that includes the sequence id and payload area</returns>
    public static Span<byte> HashContentsSpan(Span<byte> buffer) {
        return buffer.Slice(0, SEQID_LENGTH + PAYLOAD_LENGTH + RANDOM_BYTES_LENGTH);
    }

    /// <summary>
    /// Create a slice that includes the packet's random bytes area.
    /// This area contains 4 random bytes - useful when the packets are encrypted.
    /// </summary>
    /// <param name="buffer">span to create slice from</param>
    /// <returns>Slice that only includes the random bytes area</returns>
    public static Span<byte> RandomBytesSpan(Span<byte> buffer) {
        return buffer.Slice(RANDOM_BYTES_OFFSET, RANDOM_BYTES_LENGTH);
    }

    // <summary>
    /// Create a slice that only includes the packet's sequence id.
    /// </summary>
    /// <param name="buffer">span to create slice from</param>
    /// <returns>Slice that only includes the sequence id area</returns>
    public static Span<byte> SequenceIdSpan(Span<byte> buffer) {
        return buffer.Slice(0, SEQID_LENGTH);
    }

    public static void ValidatePacket(Span<byte> packet, uint expectedSequenceId) {
        ValidateSequenceId(packet, expectedSequenceId);
        ValidateHash(packet);
        ValidateMessageList(packet);
    }

    public static void ValidateHash(Span<byte> packet) {
        var calculatedHash = SHA1.HashData(Packet.HashContentsSpan(packet));
        var actualHash = Packet.HashSpan(packet);
        if (!calculatedHash.SequenceEqual(actualHash)) {
            throw new Exception("Hashes do not match!");
        }
    }

    public static void ValidateSequenceId(Span<byte> packet, uint expectedSequenceId) {
        uint actualSequenceId = BinaryPrimitives.ReadUInt32BigEndian(Packet.SequenceIdSpan(packet));
        if (actualSequenceId != expectedSequenceId) {
            throw new Exception($"Invalid Sequence Id. Expected {expectedSequenceId}, got {actualSequenceId}");
        }
    }

    public static void ValidateMessageList(Span<byte> buffer) {
        var payload = Packet.PayloadSpan(buffer);
        int offset = 0;
        int entryLen = 0;

        while (offset + Message.HeaderLength <= payload.Length) {
            entryLen = BinaryPrimitives.ReadUInt16BigEndian(payload.Slice(offset + 1, 2));
            offset += entryLen + Message.HeaderLength;

            if (entryLen == 0) {
                break;
            }
        }

        if (!(entryLen == 0)) {
            throw new Exception($"Message list is invalid. Ended at len={entryLen} offset={offset}");
        }
    }


}
