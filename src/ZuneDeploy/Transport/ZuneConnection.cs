using NativeGen;
using ZuneDeploy.Transport.Mtpz;
using ZuneDeploy.Transport.Ptp;
using ZuneDeploy.Transport.Usb;

namespace ZuneDeploy.Transport;

/// <summary>
/// Connection to Zune with XNA session
/// </summary>
internal sealed class ZuneConnection {
    private readonly ITransport _transport;
    private readonly PtpSession _ptp;
    private DeviceInfo? _deviceInfo;
    private bool _trustedApp;

    private ZuneConnection(ITransport transport) {
        _transport = transport;
        _ptp = new PtpSession(transport);
    }

    public byte DeviceFamily { get; private set; }

    public static Result Open(out ZuneConnection? connection) {
        connection = null;
        // Try Zune HD
        var result = OpenTransport(0x045E, 0x063E, out var transport);
        // SD?
        if (result == Result.ErrorNoDevice) {
            result = OpenTransport(0x045E, 0x0710, out transport);
        }
        if (result != Result.Ok) {
            return result;
        }

        var zune = new ZuneConnection(transport!);
        try {
            result = zune.Connect();
        } catch {
            result = Result.ErrorConnectionFailed;
        }

        if (result != Result.Ok) {
            zune.Close();
            return result;
        }
        connection = zune;
        return Result.Ok;
    }

    private static Result OpenTransport(ushort vendorId, ushort productId, out ITransport? transport) {
        if (OperatingSystem.IsWindows()) {
            var result = WinUsbTransport.Open(vendorId, productId, out var winUsb);
            transport = winUsb;
            return result;
        } else {
            var result = AftlTransport.Open(vendorId, productId, out var aftl);
            transport = aftl;
            return result;
        }
    }

    private Result Connect() {
        _ptp.TryRunTransaction(0x1002, 1); // OpenSession

        if (!_ptp.TryRunTransaction(0x1001, [], null, out var deviceInfo)) { // GetDeviceInfo
            return Result.ErrorConnectionFailed;
        }
        _deviceInfo = DeviceInfo.Parse(deviceInfo!);

        _ptp.TryRunTransaction(0x1015, [0xD21A], null, out var identification); // GetDevicePropValue
        DeviceFamily = identification![3];

        // Fail if session not authenticated
        if (_ptp.TryRunTransaction(0x9220)) { // XNAOpenSession
            return Result.Ok;
        }
        return Authenticate();
    }

    private Result Authenticate() {
        ushort[] mtpzOperations = [0x9212, 0x9213, 0x9214, 0x9215, 0x9216];
        var keyPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".mtpz-data");
        if (!File.Exists(keyPath)) {
            keyPath = ".mtpz-data";
        }
        if (!mtpzOperations.All(_deviceInfo!.Operations.Contains) || !File.Exists(keyPath)) {
            return Result.ErrorHandshakeFailed;
        }
        var keys = MtpzKeys.Load(keyPath);

        if (_deviceInfo.DeviceProperties.Contains(0xD406)) {
            const string initiator = "zune-deploy - MTPZClassDriver";
            var value = new byte[1 + (initiator.Length + 1) * 2];
            value[0] = (byte)(initiator.Length + 1);
            System.Text.Encoding.Unicode.GetBytes(initiator, 0, initiator.Length, value, 1);
            _ptp.TryRunTransaction(0x1016, [0xD406], value, out _); // SetDevicePropValue
        }

        _ptp.TryRunTransaction(0x9216); // EndTrustedAppSession
        _trustedApp = true;

        var certificate = MtpzCrypto.GenerateCertificateMessage(keys);
        if (!_ptp.TryRunTransaction(0x9212, [], certificate.Message, out _)) {
            return Result.ErrorHandshakeFailed;
        }
        if (!_ptp.TryRunTransaction(0x9213, [], null, out var response)) { // GetWMDRMPDAppResponse
            return Result.ErrorHandshakeFailed;
        }
        var cmacKey = MtpzCrypto.VerifyResponse(keys, response!, certificate.Challenge);
        if (!_ptp.TryRunTransaction(0x9212, [], MtpzCrypto.SignResponse(cmacKey), out _)) {
            return Result.ErrorHandshakeFailed;
        }

        _ptp.TryRunTransaction(0x1005, 0x10001); // GetStorageInfo
        return _ptp.TryRunTransaction(0x9220, MtpzCrypto.SignSessionRequest(cmacKey)) // XNAOpenSession
            ? Result.Ok
            : Result.ErrorHandshakeFailed;
    }

    public Result PollData(Span<byte> buffer, out int bytesRead) {
        bytesRead = 0;
        try {
            if (!_ptp.TryRunTransaction(0x9223, [], null, out var data)) { // XNAReceiveData
                return Result.ErrorReadFailed;
            }
            if (data == null) {
                return Result.Ok;
            }
            bytesRead = data.Length;
            if (buffer.Length < data.Length) {
                return Result.ErrorBufferTooSmall;
            }
            data.CopyTo(buffer);
            return Result.Ok;
        } catch {
            return Result.ErrorReadFailed;
        }
    }

    public Result SendData(ReadOnlySpan<byte> data) {
        try {
            return _ptp.TryRunTransaction(0x9222, [], data.ToArray(), out _) ? Result.Ok : Result.ErrorSendFailed; // XNASendData
        } catch {
            return Result.ErrorSendFailed;
        }
    }

    public void Close() {
        ushort[] operations = _trustedApp ? [0x9221, 0x9215, 0x9216, 0x1003] : [0x9221, 0x1003];
        foreach (var operation in operations) {
            try {
                _ptp.TryRunTransaction(operation);
            } catch {
                // Ignore I guess?
            }
        }
        _transport.Dispose();
    }
}
