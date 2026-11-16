using System.Runtime.InteropServices;
using NativeGen;

namespace ZuneDeploy.Transport.Usb;

/// <summary>
/// Android File Transfer for Linux USB transport
/// </summary>
internal sealed partial class AftlTransport : ITransport {
    private readonly IntPtr _handle;

    private AftlTransport(IntPtr handle) {
        _handle = handle;
    }

    public static Result Open(ushort vendorId, ushort productId, out AftlTransport? transport) {
        var result = (Result)OpenTransport(vendorId, productId, out IntPtr handle);
        transport = result == Result.Ok ? new AftlTransport(handle) : null;
        return result;
    }

    public unsafe void Write(ReadOnlySpan<byte> data, int timeoutMs) {
        fixed (byte* p = data) {
            if ((Result)TransportWrite(_handle, p, (nuint)data.Length, timeoutMs) != Result.Ok) {
                throw new IOException("USB write failed");
            }
        }
    }

    public unsafe int Read(Span<byte> buffer, int timeoutMs) {
        fixed (byte* p = buffer) {
            if ((Result)TransportRead(_handle, p, (nuint)buffer.Length, out nuint read, timeoutMs) != Result.Ok) {
                throw new IOException("USB read failed");
            }
            return (int)read;
        }
    }

    public void Dispose() {
        CloseTransport(_handle);
    }

    [LibraryImport("zune-deploy-native")]
    private static partial int OpenTransport(ushort vendorId, ushort productId, out IntPtr transport);

    [LibraryImport("zune-deploy-native")]
    private static partial void CloseTransport(IntPtr transport);

    [LibraryImport("zune-deploy-native")]
    private static unsafe partial int TransportWrite(IntPtr transport, byte* buffer, nuint size, int timeoutMs);

    [LibraryImport("zune-deploy-native")]
    private static unsafe partial int TransportRead(IntPtr transport, byte* buffer, nuint size, out nuint bytesRead, int timeoutMs);
}
