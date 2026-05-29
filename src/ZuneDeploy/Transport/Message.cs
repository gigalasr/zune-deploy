using System;

namespace ZuneDeploy.Transport;

internal class Message {
    public readonly byte StreamId;

    public readonly ReadOnlyMemory<byte> Data;

    public Message(byte streamId, ReadOnlyMemory<byte> buffer) {
        StreamId = streamId;
        Data = buffer;
    }
}
