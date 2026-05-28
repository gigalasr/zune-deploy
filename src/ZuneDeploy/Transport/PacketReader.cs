namespace ZuneDeploy.Transport;

/**
 * A Packet contains a list of Commands and Messages.
 * Commands are listed first and followed by Messages. 
 * 
 * Structure:
 * 0000 - 0003: Sequence Id
 * 0004 - 1239: Command/Message/Terminator
 * 1240 - 1243: Random Bytes 
 * 1244 - 1263: SHA1 Hash
 *
 * Message
 * [streamId][len_hi][len_low][payload]
 * 
 * Command (len includes type and args)
 * [0][len_hi][len_low][type][args]
 *
 * Terminator
 * [0][0][0]
 */
internal class PacketReader {
    private const int PACKET_LENGTH = 1264;
    private const int PAYLOAD_LENGTH = 1236;
    private const int SEQID_LENGTH = 4;
    private const int PAYLOAD_END = SEQID_LENGTH + PAYLOAD_LENGTH - 1;

    private int _currentSequenceId;

    public PacketReader(int sequenceId = 0) {
        _currentSequenceId = sequenceId;
    }

    public void FromDeviceBuffer(byte[] buffer, out List<Message> messages, out List<ReceivableCommand> commands) {
        if (buffer.Length != PACKET_LENGTH) {
            throw new ArgumentException($"A packet buffer must have a length of {PACKET_LENGTH}");
        }

        var owned = (byte[])buffer.Clone();

        ValidateHash(owned);
        ValidateSequenceId(owned);
        ValidateMessageList(owned);
        Deserialize(owned, out messages, out commands);
    }

    private void ValidateHash(byte[] buffer) {
        // TODO: Build SHA1 Hash and comapre to hash in packet
    }

    private void ValidateSequenceId(byte[] buffer) {
        int sequence = (buffer[0] << 24) | (buffer[1] << 16) | (buffer[2] << 8) | buffer[3];
        if (sequence != _currentSequenceId) {
            throw new Exception($"Invalid Sequence Id. Expected {_currentSequenceId}, got {sequence}");
        }
    }

    private void ValidateMessageList(byte[] buffer) {
        int offset = SEQID_LENGTH;
        int payloadLength = 0;
        while (offset + 3 <= PAYLOAD_END) {
            payloadLength = (buffer[offset + 1] << 8) | buffer[offset + 2];
            offset += payloadLength + 3;
            if (payloadLength == 0) {
                break;
            }
        }

        if (!(payloadLength == 0 && offset <= PAYLOAD_END)) {
            throw new Exception("Message list is invalid");
        }
    }

    private void Deserialize(byte[] buffer, out List<Message> messages, out List<ReceivableCommand> commands) {
        messages = new List<Message>();
        commands = new List<ReceivableCommand>();

        int offset = SEQID_LENGTH;
        while (offset + 2 <= PAYLOAD_END) {
            byte streamId = buffer[offset];
            int payloadLen = (buffer[offset + 1] << 8) | buffer[offset + 2];

            // Message List Terminator
            if (payloadLen == 0) {
                break;
            }

            var data = buffer.AsMemory(offset + 3, payloadLen);

            if (streamId == 0) {
                commands.Add(CommandFactory.FromDeviceBuffer(data.Span));
            } else {
                messages.Add(new Message(streamId, data));
            }

            offset += payloadLen + 3;
        }
    }
}