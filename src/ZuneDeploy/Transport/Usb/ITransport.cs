namespace ZuneDeploy.Transport.Usb;

/// <summary>
/// USB transport
/// </summary>
internal interface ITransport : IDisposable {
    /// <summary>
    /// Sends data
    /// </summary>
    void Write(ReadOnlySpan<byte> data, int timeoutMs);

    /// <summary>
    /// Receive data
    /// </summary>
    int Read(Span<byte> buffer, int timeoutMs);
}
