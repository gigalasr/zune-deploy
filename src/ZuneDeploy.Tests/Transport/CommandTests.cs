using ZuneDeploy.Transport;

namespace ZuneDeploy.Tests.Transport;


public class CommandTests {
    [Fact]
    public void FromByte() {
        byte[] actual = CommandFactory.FromByte(CommandType.CloseStream, 0x42);
        byte[] expected = [(byte)CommandType.CloseStream, 0x42];
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void FromByteAndString() {
        byte[] actual = CommandFactory.FromByteAndString(CommandType.OpenStream, 0x02, "XnaChannelBroker");
        byte[] expected = [
            0xa1, 0x2, 0x10, 0x0, 0x58, 0x0, 0x6e, 0x0,
            0x61, 0x0, 0x43, 0x0, 0x68, 0x0, 0x61, 0x0,
            0x6e, 0x0, 0x6e, 0x0, 0x65, 0x0, 0x6c, 0x0,
            0x42, 0x0, 0x72, 0x0, 0x6f, 0x0, 0x6b, 0x0,
            0x65, 0x0, 0x72
        ];
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ParseByte() {
        CommandFactory.ParseByte(new ReadOnlySpan<byte>([42]), out byte actual);
        Assert.Equal(42, actual);
    }

    [Fact]
    public void ParseByteAndUShort() {
        CommandFactory.ParseByteAndUShort(new ReadOnlySpan<byte>([
            0x01, 0x17, 0xb5
        ]), out byte byteArg, out ushort ushortArg);
        Assert.Equal(1, byteArg);
        Assert.Equal(6069, ushortArg);
    }

    [Theory]
    [MemberData(nameof(GetParsingTestData))]
    internal void FromDeviceBuffer(byte[] buffer, ReceivableCommand expectedCommand) {
        var actualCommand = CommandFactory.FromDeviceBuffer(new ReadOnlySpan<byte>(buffer));
        TestUtil.AssertCommandsEqual(expectedCommand, actualCommand);
    }

    public static IEnumerable<object[]> GetParsingTestData() {
        yield return new object[] {
            new byte[] { (byte)CommandType.StreamOpened, 0x01, 0x17, 0xb5 }, new StreamOpenedCommand(1, 6069),
        };

        yield return new object[] {
            new byte[] { (byte)CommandType.AckCancel, 0x42 }, new AckCancelCommand(0x42)
        };

        yield return new object[] {
            new byte[] { (byte)CommandType.RequestRefused, 0x42 }, new RequestRefusedCommand(0x42)
        };

        yield return new object[] {
            new byte[] { (byte)CommandType.AckDisconnect, 0x42 }, new AckDisconnectCommand(0x42)
        };

        yield return new object[] {
            new byte[] { (byte)CommandType.Rebooting, 0x42 }, new RebootingCommand(0x42)
        };

        yield return new object[] {
            new byte[] { (byte)CommandType.KeepAlive, 0x42 }, new KeepAliveCommand(0x42)
        };

        yield return new object[] {
            new byte[] { (byte)CommandType.DataProcessed, 0x01, 0x17, 0xb5 }, new DataProcessedCommand(1, 6069)
        };

        yield return new object[] {
            new byte[] { (byte)CommandType.CloseStream, 0x01,  }, new StreamClosedCommand(1)
        };
    }
}
