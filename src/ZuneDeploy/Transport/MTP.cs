using System.Runtime.InteropServices;
using NativeGen;

namespace ZuneDeploy.Transport;

static internal partial class MTP {
    internal static int OpenConnection(out IntPtr device) {
        device = IntPtr.Zero;
        var result = ZuneConnection.Open(out var connection);
        if (result == Result.Ok) {
            device = GCHandle.ToIntPtr(GCHandle.Alloc(connection));
        }
        return (int)result;
    }

    internal static void CloseConnection(IntPtr device) {
        var handle = GCHandle.FromIntPtr(device);
        Get(device).Close();
        handle.Free();
    }

    internal static int PollData(IntPtr device, byte[] buffer, int size, out int bytesRead) {
        return (int)Get(device).PollData(buffer.AsSpan(0, size), out bytesRead);
    }

    internal static int SendData(IntPtr device, byte[] buffer, int size) {
        return (int)Get(device).SendData(buffer.AsSpan(0, size));
    }

    internal static byte GetDeviceFamily(IntPtr device) {
        return Get(device).DeviceFamily;
    }

    private static ZuneConnection Get(IntPtr device) {
        return (ZuneConnection)GCHandle.FromIntPtr(device).Target!;
    }
}
