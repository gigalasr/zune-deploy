using System.Buffers.Binary;
using ZuneDeploy.Transport.Usb;

namespace ZuneDeploy.Transport.Ptp;

/// <summary>
/// PTP/MTP transactions
/// </summary>
internal sealed class PtpSession(ITransport transport) {
    private const int TimeoutMs = 10000;
    private const ushort TypeCommand = 1, TypeData = 2, TypeResponse = 3;

    private byte[] _rx = new byte[128 * 1024];
    private int _rxLength;
    private uint _nextTransactionId;

    public bool TryRunTransaction(ushort operation, params uint[] parameters) {
        return TryRunTransaction(operation, parameters, null, out _);
    }

    public bool TryRunTransaction(ushort operation, uint[] parameters, byte[]? dataOut, out byte[]? dataIn) {
        dataIn = null;
        // 0 = OpenSession
        uint tid = operation == 0x1002 ? 0 : _nextTransactionId;
        _nextTransactionId = tid + 1;

        transport.Write(Container(TypeCommand, operation, tid, parameters, 0), TimeoutMs);

        if (dataOut != null) {
            // We need the data header and payload
            transport.Write(Container(TypeData, operation, tid, [], dataOut.Length), TimeoutMs);
            transport.Write(dataOut, TimeoutMs);
        }

        while (true) {
            // A container can be multiple bulk transfers
            Fill(12);
            int length = (int)BinaryPrimitives.ReadUInt32LittleEndian(_rx);
            if (length < 12) {
                throw new IOException("Malformed PTP container");
            }
            Fill(length);

            var type = BinaryPrimitives.ReadUInt16LittleEndian(_rx.AsSpan(4));
            var code = BinaryPrimitives.ReadUInt16LittleEndian(_rx.AsSpan(6));
            var containerTid = BinaryPrimitives.ReadUInt32LittleEndian(_rx.AsSpan(8));
            var payload = _rx.AsSpan(12, length - 12).ToArray();
            _rxLength -= length;
            Buffer.BlockCopy(_rx, length, _rx, 0, _rxLength);

            if (containerTid != tid) {
                continue;
            }
            if (type == TypeData) {
                dataIn = payload;
            } else if (type == TypeResponse) {
                return code == 0x2001 || code == 0xA221;
            }
        }
    }

    /// <summary>
    /// Container header and parameters
    /// </summary>
    private static byte[] Container(ushort type, ushort code, uint tid, uint[] parameters, int payloadLength) {
        var container = new byte[12 + parameters.Length * 4];
        BinaryPrimitives.WriteUInt32LittleEndian(container, (uint)(container.Length + payloadLength));
        BinaryPrimitives.WriteUInt16LittleEndian(container.AsSpan(4), type);
        BinaryPrimitives.WriteUInt16LittleEndian(container.AsSpan(6), code);
        BinaryPrimitives.WriteUInt32LittleEndian(container.AsSpan(8), tid);
        for (int i = 0; i < parameters.Length; i++) {
            BinaryPrimitives.WriteUInt32LittleEndian(container.AsSpan(12 + i * 4), parameters[i]);
        }
        return container;
    }

    private void Fill(int needed) {
        while (_rxLength < needed) {
            // Space for one more transfer
            if (_rx.Length - _rxLength < 64 * 1024) {
                Array.Resize(ref _rx, _rx.Length * 2);
            }
            _rxLength += transport.Read(_rx.AsSpan(_rxLength), TimeoutMs);
        }
    }
}
