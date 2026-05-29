namespace ZuneDeploy.Transport;

internal class StreamCollection() {
    private Dictionary<byte, ServiceStream> _streams = new();
    private Dictionary<byte, ushort> _hostBufferSizes = new();
    private Queue<byte> _freeStreamIds = new();
    private byte _nextStreamId = 0;

    public ServiceStream OpenStream(string serviceId) {
        byte streamId = GetNextStreamId();

        ServiceStream stream = new ServiceStream(streamId) {
            State = ServiceStreamState.Opening
        };
        _streams.Add(streamId, stream);

        return stream;
    }

    public void CloseStream(byte streamId) {
        if (_streams.TryGetValue(streamId, out ServiceStream? stream)) {
            stream.State = ServiceStreamState.Closing;
        } else {
            throw new Exception($"Tried to access stream {streamId}, but that id is not known");
        }
    }

    public void OnStreamClosed(byte streamId) {
        if (_streams.TryGetValue(streamId, out ServiceStream? stream)) {
            stream.State = ServiceStreamState.Closed;
            _streams.Remove(streamId);
            _freeStreamIds.Enqueue(streamId);
        } else {
            throw new Exception($"Tried to access stream {streamId}, but that id is not known");
        }
    }

    public void OnStreamOpened(byte streamId, ushort initalBufferSize) {
        if (_streams.TryGetValue(streamId, out ServiceStream? stream)) {
            stream.State = ServiceStreamState.Open;
            _hostBufferSizes.Add(streamId, initalBufferSize);
        } else {
            throw new Exception($"Tried to access stream {streamId}, but that id is not known");
        }
    }

    public void OnDataProcessed(byte streamId, ushort bufferDelta) {
        if (_hostBufferSizes.TryGetValue(streamId, out ushort originalBufferSize)) {
            _hostBufferSizes.Add(streamId, (ushort)(originalBufferSize + bufferDelta));
        } else {
            throw new Exception($"Tried to access stream {streamId}, but that id is not known");
        }
    }

    public void DeliverMessageToStream(Message message) {
        if (_streams.TryGetValue(message.StreamId, out ServiceStream? stream)) {
            if (stream.State != ServiceStreamState.Open) {
                throw new Exception($"Cannot deliver message to closed stream: {stream.StreamId}");
            }

            stream.DeliverMessage(message);
        } else {
            throw new Exception($"Tried to access stream {message.StreamId}, but that id is not known");
        }
    }

    public ushort GetBufferCapacityForStream(byte streamId) {
        if (_hostBufferSizes.TryGetValue(streamId, out ushort capacity)) {
            return capacity;
        } else {
            throw new Exception($"Tried to access stream {streamId}, but that id is not known");
        }
    }

    public void DecrementBufferCapacityForStream(byte streamId, ushort delta) {
        if (_hostBufferSizes.TryGetValue(streamId, out ushort capacity)) {
            if (delta > capacity) {
                throw new ArgumentException($"Delta {delta} is bigger than capcaity {capacity}");
            }

            _hostBufferSizes.Add(streamId, (ushort)(capacity - delta));
        } else {
            throw new Exception($"Tried to access stream {streamId}, but that id is not known");
        }
    }

    public void CollectMessagesFromStreams(Queue<Message> deliverTo) {
        foreach (ServiceStream stream in _streams.Values) {
            if (stream.State != ServiceStreamState.Open) {
                continue;
            }

            while (stream.CollectMessage(out Message? message)) {
                if (message != null) {
                    deliverTo.Enqueue(message);
                }
            }
        }
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