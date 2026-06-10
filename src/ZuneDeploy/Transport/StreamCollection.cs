namespace ZuneDeploy.Transport;

internal record StreamInformation {
    public required ServiceStream Stream { init; get; }
    public required ushort HostBufferSize { get; set; }
    public required ServiceStreamState State { get; set; }
}

internal class StreamCollection() {
    private readonly Dictionary<byte, StreamInformation> _streams = [];
    private readonly Queue<byte> _freeStreamIds = [];

    // streamid=0 is reserved for commands
    private byte _nextStreamId = 1;

    /// <summary>
    /// Creates a new Stream with the next free StreamId
    /// </summary>
    /// <param name="closeStreamCallback">The method that should be called when the stream wants to close</param>
    /// <returns>New <see cref="ServiceStream"/></returns>
    /// <remarks>
    /// This only creates the stream on our side, and does not request to open the stream on the Zune yet.
    /// This happens in the <see cref="Client"/>.
    /// </remarks
    public ServiceStream CreateStream(ServiceStream.CloseStream? closeStreamCallback) {
        byte streamId = GetNextStreamId();

        var stream = new ServiceStream(streamId, closeStreamCallback);
        _streams.Add(streamId, new StreamInformation {
            Stream = stream,
            HostBufferSize = 0,
            State = ServiceStreamState.Opening
        });

        return stream;
    }

    /// <summary>
    /// Either the Zune or we can initiate to close a stream.
    /// As such, we first go into the closing state and then into the closed state.
    /// The second time close is called, we know that both parties requested to close the stream and we can close it for real.
    /// </summary>
    /// <remarks>
    /// As such, this method has to be called twice, once by us, once "by the Zune".
    /// </remarks>
    /// <param name="streamId">The stream to close</param>
    /// <returns>True, if the stream entered the <see cref="ServiceStreamState.Closed"/> state</returns>
    public bool CloseStream(byte streamId) {
        var info = _streams[streamId];

        switch (info.State) {
            case ServiceStreamState.Opening:
            case ServiceStreamState.Open:
                info.State = ServiceStreamState.Closing;
                return true;
            case ServiceStreamState.Closing:
                info.State = ServiceStreamState.Closed;
                _streams.Remove(streamId);
                _freeStreamIds.Enqueue(streamId);
                break;
            case ServiceStreamState.Closed:
                Console.WriteLine($"Warn: Tried to close stream id={streamId} twice");
                break;
        }

        return false;
    }


    /// <summary>
    /// Should be called, when the Zune sends a <see cref="StreamOpenedCommand"/>.
    /// This will set the stream into the <see cref="ServiceStreamState.Open"/> and configure the
    /// initial buffer size of the service on the Zune.
    /// </summary>
    /// <param name="streamId">Stream that was opened</param>
    /// <param name="initalBufferSize">The buffer size that was transmitted in the <see cref="StreamOpenedCommand"/></param>
    public void OnStreamOpened(byte streamId, ushort initalBufferSize) {
        var info = _streams[streamId];
        info.State = ServiceStreamState.Open;
        info.HostBufferSize = initalBufferSize;
    }

    /// <summary>
    /// Should be called, when the Zune sends a <see cref="DataProcessedCommand"/>.
    /// </summary>
    /// <param name="streamId">The stream this commands belongs to</param>
    /// <param name="bufferDelta">How many bytes are now available in the buffer</param>
    public void OnDataProcessed(byte streamId, ushort bufferDelta) {
        var info = _streams[streamId];
        info.Stream.InvokeOnBytesWritten(bufferDelta);
        info.HostBufferSize += bufferDelta;
    }

    /// <summary>
    /// Deliver a packet from the Zune into the correct <see cref="ServiceStream._incomingPackets"/>.
    /// </summary>
    /// <param name="message">The message to deliver</param>
    /// <exception cref="Exception">Thrown when the target stream is closed</exception>
    public void DeliverMessageToStream(Message message) {
        var info = _streams[message.StreamId];
        if (info.State == ServiceStreamState.Closed) {
            throw new Exception($"Cannot deliver message to closed stream: {info.Stream.StreamId}");
        }
        info.Stream.DeliverMessage(message);
    }

    /// <summary>
    /// Collect all messages from streams <see cref="ServiceStream._outgoingPackets"/>.
    /// </summary>
    /// <param name="deliverTo">The list to save the messages into</param>
    /// <exception cref="Exception">Thrown when a stream has unsent messages but is closed</exception>
    public void CollectMessagesFromStreams(List<Message> deliverTo) {
        foreach (StreamInformation info in _streams.Values) {
            if (info.State == ServiceStreamState.Closed) {
                throw new Exception($"Stream id={info.Stream.StreamId} is closed but still had unsent data!");
            }

            while (info.Stream.CollectMessage(out Message? message)) {
                if (message != null) {
                    deliverTo.Add(message);
                }
            }
        }
    }

    /// <summary>
    /// Get the buffer capacity for a stream.
    /// This is the capacity remaining on the Zune for a given stream
    /// </summary>
    /// <param name="streamId">The stream the buffer belongs to</param>
    /// <returns>Remaining capacity in bytes</returns>
    public ushort GetBufferCapacityForStream(byte streamId) {
        return _streams[streamId].HostBufferSize;
    }

    /// <summary>
    /// Decrement the buffer capacity for a stream.
    /// </summary>
    /// <remarks>
    /// This method is called by the <see cref="PacketWriter.GeneratePacket"/> method
    /// when writing messages to a stream.
    /// </remarks>
    /// <param name="streamId">The stream that data was written to</param>
    /// <param name="delta">Size of the written data in bytes</param>
    /// <exception cref="ArgumentException">Thrown when delta is bigger than the remaining capacity</exception>
    public void DecrementBufferCapacityForStream(byte streamId, ushort delta) {
        var info = _streams[streamId];
        if (delta > info.HostBufferSize) {
            throw new ArgumentException($"Delta {delta} is bigger than capcaity {info.HostBufferSize}");
        }

        info.HostBufferSize -= delta;
    }

    /// <summary>
    /// Get a stream by its id.
    /// </summary>
    /// <param name="streamId">Id of the requested Stream</param>
    /// <returns><see cref="ServiceStream"/></returns>
    public ServiceStream GetStream(byte streamId) {
        return _streams[streamId].Stream;
    }

    private byte GetNextStreamId() {
        if (_freeStreamIds.Count > 0) {
            return _freeStreamIds.Dequeue();
        }

        if (_nextStreamId > 255) {
            throw new Exception("No streams available");
        }

        return _nextStreamId++;
    }
}
