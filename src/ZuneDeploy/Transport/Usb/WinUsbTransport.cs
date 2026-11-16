using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Microsoft.Win32.SafeHandles;
using NativeGen;

namespace ZuneDeploy.Transport.Usb;

/// <summary>
/// WinUSB transport (Windows).
/// </summary>
[SupportedOSPlatform("windows")]
internal sealed partial class WinUsbTransport : ITransport {
    // GUID_DEVINTERFACE_USB_DEVICE
    private static readonly Guid UsbDeviceInterface = new("A5DCBF10-6530-11D2-901F-00C04FB951ED");

    private readonly SafeFileHandle _file;
    private readonly IntPtr _handle;
    private readonly byte _bulkIn, _bulkOut;
    private readonly ushort _maxPacketOut;
    private int _timeoutIn = -1, _timeoutOut = -1;

    private WinUsbTransport(SafeFileHandle file, IntPtr handle, byte bulkIn, byte bulkOut, ushort maxPacketOut) {
        _file = file;
        _handle = handle;
        _bulkIn = bulkIn;
        _bulkOut = bulkOut;
        _maxPacketOut = maxPacketOut;
    }

    public static Result Open(ushort vendorId, ushort productId, out WinUsbTransport? transport) {
        transport = null;
        var id = $"vid_{vendorId:x4}&pid_{productId:x4}";
        var result = Result.ErrorNoDevice;

        foreach (var path in DevicePaths()) {
            if (!path.Contains(id, StringComparison.OrdinalIgnoreCase)) {
                continue;
            }

            var file = CreateFileW(path, GenericRead | GenericWrite, FileShareRead | FileShareWrite, 0, OpenExisting, FileFlagOverlapped, 0);
            if (file.IsInvalid) {
                // For example if another program has the Zune
                var error = Marshal.GetLastPInvokeError();
                result = error is ErrorAccessDenied or ErrorSharingViolation ? Result.ErrorDeviceBusy : Result.ErrorConnectionFailed;
                continue;
            }
            if (!WinUsb_Initialize(file, out var handle)) {
                file.Dispose();
                result = Result.ErrorConnectionFailed;
                continue;
            }

            transport = Create(file, handle);
            if (transport != null) {
                return Result.Ok;
            }
            result = Result.ErrorConnectionFailed;
        }
        return result;
    }

    private static WinUsbTransport? Create(SafeFileHandle file, IntPtr handle) {
        byte bulkIn = 0, bulkOut = 0;
        ushort maxPacketOut = 512;
        if (WinUsb_QueryInterfaceSettings(handle, 0, out var descriptor)) {
            for (byte i = 0; i < descriptor.NumEndpoints; i++) {
                if (!WinUsb_QueryPipe(handle, 0, i, out var pipe) || pipe.PipeType != PipeTypeBulk) {
                    continue;
                }
                if ((pipe.PipeId & 0x80) != 0) {
                    bulkIn = bulkIn == 0 ? pipe.PipeId : bulkIn;
                } else if (bulkOut == 0) {
                    bulkOut = pipe.PipeId;
                    maxPacketOut = pipe.MaximumPacketSize;
                }
            }
        }
        if (bulkIn == 0 || bulkOut == 0) {
            WinUsb_Free(handle);
            file.Dispose();
            return null;
        }

        WinUsb_ResetPipe(handle, bulkIn);
        WinUsb_ResetPipe(handle, bulkOut);
        return new WinUsbTransport(file, handle, bulkIn, bulkOut, maxPacketOut);
    }

    public unsafe void Write(ReadOnlySpan<byte> data, int timeoutMs) {
        SetTimeout(_bulkOut, ref _timeoutOut, timeoutMs);
        fixed (byte* p = data) {
            if (!WinUsb_WritePipe(_handle, _bulkOut, p, (uint)data.Length, out var written, 0) || written != data.Length) {
                throw new IOException($"USB write failed: {new Win32Exception(Marshal.GetLastPInvokeError()).Message}");
            }
            if (data.Length > 0 && data.Length % _maxPacketOut == 0) {
                WinUsb_WritePipe(_handle, _bulkOut, p, 0, out _, 0);
            }
        }
    }

    public unsafe int Read(Span<byte> buffer, int timeoutMs) {
        SetTimeout(_bulkIn, ref _timeoutIn, timeoutMs);
        fixed (byte* p = buffer) {
            if (!WinUsb_ReadPipe(_handle, _bulkIn, p, (uint)buffer.Length, out var read, 0)) {
                throw new IOException($"USB read failed: {new Win32Exception(Marshal.GetLastPInvokeError()).Message}");
            }
            return (int)read;
        }
    }

    public void Dispose() {
        WinUsb_Free(_handle);
        _file.Dispose();
    }

    private void SetTimeout(byte pipe, ref int current, int timeoutMs) {
        if (current != timeoutMs) {
            uint value = (uint)timeoutMs;
            WinUsb_SetPipePolicy(_handle, pipe, PipeTransferTimeout, sizeof(uint), in value);
            current = timeoutMs;
        }
    }

    private static List<string> DevicePaths() {
        var paths = new List<string>();
        var set = SetupDiGetClassDevsW(in UsbDeviceInterface, 0, 0, DigcfPresent | DigcfDeviceInterface);
        if (set == -1) {
            return paths;
        }
        try {
            var data = new SpDeviceInterfaceData { Size = Marshal.SizeOf<SpDeviceInterfaceData>() };
            for (uint i = 0; SetupDiEnumDeviceInterfaces(set, 0, in UsbDeviceInterface, i, ref data); i++) {
                SetupDiGetDeviceInterfaceDetailW(set, ref data, 0, 0, out var size, 0);
                if (size <= 0) {
                    continue;
                }
                var detail = Marshal.AllocHGlobal(size);
                try {
                    Marshal.WriteInt32(detail, IntPtr.Size == 8 ? 8 : 6);
                    if (SetupDiGetDeviceInterfaceDetailW(set, ref data, detail, size, out _, 0)) {
                        paths.Add(Marshal.PtrToStringUni(detail + 4) ?? "");
                    }
                } finally {
                    Marshal.FreeHGlobal(detail);
                }
            }
        } finally {
            SetupDiDestroyDeviceInfoList(set);
        }
        return paths;
    }

    private const uint GenericRead = 0x80000000, GenericWrite = 0x40000000;
    private const uint FileShareRead = 1, FileShareWrite = 2, OpenExisting = 3, FileFlagOverlapped = 0x40000000;
    private const uint DigcfPresent = 0x02, DigcfDeviceInterface = 0x10;
    private const uint PipeTransferTimeout = 0x03;
    private const int PipeTypeBulk = 2;
    private const int ErrorAccessDenied = 5, ErrorSharingViolation = 32;

    [StructLayout(LayoutKind.Sequential)]
    private struct SpDeviceInterfaceData {
        public int Size;
        public Guid InterfaceClassGuid;
        public int Flags;
        public IntPtr Reserved;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct UsbInterfaceDescriptor {
        public byte Length, DescriptorType, InterfaceNumber, AlternateSetting, NumEndpoints,
            InterfaceClass, InterfaceSubClass, InterfaceProtocol, Interface;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct WinUsbPipeInformation {
        public int PipeType;
        public byte PipeId;
        public ushort MaximumPacketSize;
        public byte Interval;
    }

    [LibraryImport("setupapi.dll", SetLastError = true)]
    private static partial IntPtr SetupDiGetClassDevsW(in Guid classGuid, IntPtr enumerator, IntPtr parent, uint flags);

    [LibraryImport("setupapi.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool SetupDiEnumDeviceInterfaces(IntPtr set, IntPtr deviceInfo, in Guid interfaceGuid, uint index, ref SpDeviceInterfaceData data);

    [LibraryImport("setupapi.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool SetupDiGetDeviceInterfaceDetailW(IntPtr set, ref SpDeviceInterfaceData data, IntPtr detail, int size, out int requiredSize, IntPtr deviceInfo);

    [LibraryImport("setupapi.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool SetupDiDestroyDeviceInfoList(IntPtr set);

    [LibraryImport("kernel32.dll", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
    private static partial SafeFileHandle CreateFileW(string name, uint access, uint share, IntPtr security, uint disposition, uint flags, IntPtr template);

    [LibraryImport("winusb.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool WinUsb_Initialize(SafeFileHandle file, out IntPtr handle);

    [LibraryImport("winusb.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool WinUsb_Free(IntPtr handle);

    [LibraryImport("winusb.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool WinUsb_QueryInterfaceSettings(IntPtr handle, byte alternate, out UsbInterfaceDescriptor descriptor);

    [LibraryImport("winusb.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool WinUsb_QueryPipe(IntPtr handle, byte alternate, byte index, out WinUsbPipeInformation pipe);

    [LibraryImport("winusb.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool WinUsb_SetPipePolicy(IntPtr handle, byte pipe, uint policy, uint length, in uint value);

    [LibraryImport("winusb.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool WinUsb_ResetPipe(IntPtr handle, byte pipe);

    [LibraryImport("winusb.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static unsafe partial bool WinUsb_ReadPipe(IntPtr handle, byte pipe, byte* buffer, uint length, out uint transferred, IntPtr overlapped);

    [LibraryImport("winusb.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static unsafe partial bool WinUsb_WritePipe(IntPtr handle, byte pipe, byte* buffer, uint length, out uint transferred, IntPtr overlapped);
}
