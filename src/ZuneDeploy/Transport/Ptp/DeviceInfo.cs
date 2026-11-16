using System.Buffers.Binary;

namespace ZuneDeploy.Transport.Ptp;

/// <summary>
/// Operations and Properties from GetDeviceInfo()
/// </summary>
internal sealed class DeviceInfo {
    public required HashSet<ushort> Operations { get; init; }
    public required HashSet<ushort> DeviceProperties { get; init; }

    public static DeviceInfo Parse(byte[] data) {
        int p = 0;

        HashSet<ushort> ReadArray16() {
            var count = BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan(p));
            p += 4;
            var set = new HashSet<ushort>();
            for (; count > 0; count--) {
                set.Add(BinaryPrimitives.ReadUInt16LittleEndian(data.AsSpan(p)));
                p += 2;
            }
            return set;
        }

        p += 2 + 4 + 2;         // standard version, vendor extension id, vendor extension version
        p += 1 + data[p] * 2;   // vendor extension description (PTP string)
        p += 2;                 // functional mode
        var operations = ReadArray16();
        ReadArray16();          // events
        var properties = ReadArray16();

        return new DeviceInfo {
            Operations = operations,
            DeviceProperties = properties,
        };
    }
}
