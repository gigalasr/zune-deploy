internal abstract class Packet {
    public const int PACKET_LENGTH = 1264;
    public const int PAYLOAD_LENGTH = 1236;
    public const int RANDOM_BYTES_OFFSET = 1240;
    public const int RANDOM_BYTES_LENGTH = 4;
    public const int HASH_OFFSET = 1244;
    public const int SEQID_LENGTH = 4;
    public const int PAYLOAD_END = SEQID_LENGTH + PAYLOAD_LENGTH - 1;
}