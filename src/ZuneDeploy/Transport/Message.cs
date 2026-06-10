namespace ZuneDeploy.Transport;

internal class Message(byte streamId, byte[] buffer) {
    public readonly byte StreamId = streamId;
    public readonly MemoryStream Data = new(buffer);

    /// <summary>
    /// The remaining length of the message, including its header (1 byte streamId, 2 bytes length)
    /// Note: The header will be added by the <see cref="PacketWriter"/> when creating the packet containing the message
    /// </summary>
    public int RemainingLengthIncludingHeader => (int)(Data.Length - Data.Position + HeaderLength);

    /// <summary>
    /// The remaining length of the message
    /// </summary>
    public int RemainingLength => (int)(Data.Length - Data.Position);


    public const int HeaderLength = 3;

    public const int MinBlockSize = 4;
}
