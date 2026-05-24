using System;

namespace ZuneDeploy.Messaging;

internal enum CommandType {
    RequestConnect = 161,
    AcceptRequest = 162,
    CancelRequest = 163,
    AcknowledgeAccept = 164,
    AcknowledgeCancel = 165,
    RefuseRequest = 166,
    Disconnect = 177,
    AcknowledgeDisconnect = 178,
    StreamClosed = 193,
    HostError = 225,
    ClientError = 226,
    HostRebooting = 241,
    KeepAlive = 209,
    DataConsumed = 210,
}

internal class Command {
    private ReadOnlyMemory<byte> _data;
    public CommandType Type { get; init; }

    public static Command FromBuffer(ReadOnlyMemory<byte> data) {
        if (data.Length < 1) {
            throw new ArgumentException("Command buffer needs a length of at least 1");
        }

        byte type = data.Span[0];
        return new Command((CommandType)type, data.Slice(1));
    }

    private Command(CommandType type, ReadOnlyMemory<byte> data) {
        _data = data;
        Type = type;
    }
}
