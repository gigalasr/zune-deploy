using System.Security.Cryptography;
using System.Text;

namespace ZuneDeploy.Transport;


internal class PacketWriter {
    private StreamCollection _streamCollection;

    private Queue<SendableCommand> _pendingCommands = new();
    private Queue<Message> _pendingMessages = new();

    private List<SendableCommand> _selectedCommands = new();
    private List<Message> _selectedMessages = new();

    private int _sequenceId = 0;

    public PacketWriter(StreamCollection collection) {
        _streamCollection = collection;
    }

    public void SendCommand(SendableCommand command) {
        _pendingCommands.Enqueue(command);
    }

    public bool GetNextPacket(out byte[]? packet) {
        _streamCollection.CollectMessagesFromStreams(_pendingMessages);

        // No packets to create
        if (_pendingMessages.Count == 0 && _pendingCommands.Count == 0) {
            packet = null;
            return false;
        }

        // Queue commands for packet (commands always come first)
        int budget = Packet.PAYLOAD_LENGTH;
        while (_pendingCommands.Count > 0) {
            if ((budget - _pendingCommands.Peek().LengthIncludingHeader) < 0) {
                break;
            }

            _selectedCommands.Add(_pendingCommands.Dequeue());
        }

        // Queue messages for packet (we check after adding, because messages can be split up)
        while (_pendingMessages.Count > 0) {
            _selectedMessages.Add(_pendingMessages.Dequeue());

            if ((budget - _pendingMessages.Peek().LengthIncludingHeader) < 0) {
                break;
            }
        }

        // Nothing todo :(
        if (_selectedMessages.Count == 0 && _selectedCommands.Count == 0) {
            packet = null;
            return false;
        }

        packet = GeneratePacket();

        // If messages have bytes left, requeue them (round robin)
        foreach (Message message in _selectedMessages) {
            if (message.Data.Position < message.Data.Length) {
                _pendingMessages.Enqueue(message);
            }
        }

        _selectedCommands.Clear();
        _selectedMessages.Clear();

        return true;
    }

    private byte[] GeneratePacket() {
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

        int sequenceId = GetNextSequenceId();
        byte[] packet = new byte[Packet.PACKET_LENGTH];
        int position = 0;

        // Write Sequence Id
        packet[position++] = (byte)(sequenceId >> 24);
        packet[position++] = (byte)(sequenceId >> 16);
        packet[position++] = (byte)(sequenceId >> 8);
        packet[position++] = (byte)sequenceId;

        // Write Commands
        foreach (SendableCommand command in _selectedCommands) {
            int remaining = Packet.PAYLOAD_LENGTH - position + Packet.SEQID_LENGTH;
            if (remaining == 0) {
                break;
            }

            int length = command.RawBytes.Length;
            packet[position++] = 0;
            packet[position++] = (byte)(length >> 8);
            packet[position++] = (byte)length;
            command.RawBytes.CopyTo(packet, position);
            position += length;
        }

        // Write Messages
        foreach (Message message in _selectedMessages) {
            int remaining = Packet.PAYLOAD_LENGTH - position + Packet.SEQID_LENGTH;
            if (remaining == 0) {
                break;
            }

            // Messages can be writen partially 
            int hostBufferSize = _streamCollection.GetBufferCapacityForStream(message.StreamId);
            if (hostBufferSize <= 0) {
                continue;
            }

            int bytesToWrite = Math.Min(hostBufferSize, remaining);
            int bytesCopied = message.Data.Read(packet, position, bytesToWrite);
            position += bytesCopied;
            _streamCollection.DecrementBufferCapacityForStream(message.StreamId, (ushort)bytesCopied);
        }

        var span = packet.AsSpan();

        // Write Random Bytes
        Random.Shared.NextBytes(span.Slice(Packet.RANDOM_BYTES_OFFSET, Packet.RANDOM_BYTES_LENGTH));

        // Compute and write Hash
        SHA1.HashData(
            span.Slice(0, Packet.PAYLOAD_LENGTH + Packet.RANDOM_BYTES_LENGTH),
            span.Slice(Packet.HASH_OFFSET, Packet.PACKET_LENGTH - Packet.HASH_OFFSET)
        );

        return packet;
    }

    private int GetNextSequenceId() {
        return _sequenceId++;
    }
}