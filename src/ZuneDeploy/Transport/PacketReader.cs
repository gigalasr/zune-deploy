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
 * Terminator -> Last 3 bytes of the payload have to be 0 !
 * [0][0][0]
 */
internal class PacketReader {
    private const int PACKET_LENGTH = 1264;
    private const int PAYLOAD_LENGTH = 1236;
    private const int SEQID_LENGTH = 4;
    private const int PAYLOAD_END = SEQID_LENGTH + PAYLOAD_LENGTH - 1;

    private uint _currentSequenceId;

    public PacketReader(uint sequenceId = 0) {
        _currentSequenceId = sequenceId;
    }


    // TODO: Add events for recievable commands again 
    // TODO: Add ParseAndDeliver() Method that delivers messages and invokes command handlers

    public void FromDeviceBuffer(byte[] buffer, out List<Message> messages, out List<ReceivableCommand> commands) {
        if (buffer.Length != PACKET_LENGTH) {
            throw new ArgumentException($"A packet buffer must have a length of {PACKET_LENGTH}");
        }

        Packet.ValidatePacket(buffer, _currentSequenceId);
        Deserialize(buffer, out messages, out commands);
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

            var data = buffer.AsSpan(offset + 3, payloadLen);

            if (streamId == 0) {
                commands.Add(CommandFactory.FromDeviceBuffer(data));
            } else {
                messages.Add(new Message(streamId, data.ToArray()));
            }

            offset += payloadLen + 3;
        }
    }
}