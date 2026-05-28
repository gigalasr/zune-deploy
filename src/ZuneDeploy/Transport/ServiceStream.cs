
using System.Collections.Concurrent;

namespace ZuneDeploy.Transport;

using StreamPacket = byte[];

/// <summary>
/// Stream to send and recieve bytes from a service over a packet stream 
/// </summary>
internal class ServiceStream : Stream {
    public override bool CanRead => true;
    public override bool CanWrite => true;
    public override bool CanSeek => false;

    // private ChannelReader<StreamPacket> _reader;
    // private ChannelWriter<StreamPacket> _writer;
    private BlockingCollection<StreamPacket> _incomingPackets = new();
    private BlockingCollection<StreamPacket> _outgoingPackets = new();

    private Queue<MemoryStream> _readQueue = new();
    private MemoryStream _writeBuffer = new();

    public byte StreamId { init; get; }

    public ServiceStream(byte streamId) {
        StreamId = streamId;
    }

    public override int Read(byte[] buffer, int offset, int count) {
        // Drain incoming packets
        while (_incomingPackets.TryTake(out StreamPacket? packet)) {
            if (packet != null) {
                _readQueue.Enqueue(new MemoryStream(packet));
            }
        }

        // If we still don't have any data, block to avoid the BinaryReader calling the Read function repeatedly 
        if (_readQueue.Count == 0) {
            _readQueue.Enqueue(new MemoryStream(_incomingPackets.Take()));
        }

        var current = _readQueue.Peek();
        int bytesRead = current.Read(buffer, offset, count);

        if (current.Position >= current.Length) {
            _readQueue.Dequeue();
        }

        return bytesRead;
    }

    public override void Write(byte[] buffer, int offset, int count) {
        _writeBuffer.Write(buffer, offset, count);
    }

    public override void Flush() {
        var buffer = _writeBuffer.ToArray();
        _outgoingPackets.Add(buffer);
        _writeBuffer.SetLength(0);
        _writeBuffer.Position = 0;
    }

    public override long Length => throw new NotSupportedException();
    public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
    public override void SetLength(long value) => throw new NotSupportedException();
}