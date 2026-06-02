using System.Text;

namespace ZuneDeploy;

internal static class HexDump {
    public static void Dump(ReadOnlySpan<byte> bytes) {
        const int bytesPerLine = 16;
        var s = new StringBuilder();

        for (int i = 0; i < bytes.Length; i += bytesPerLine) {
            s.Append($"{i:X8}: ");

            int lineBytes = Math.Min(bytesPerLine, bytes.Length - i);

            // Hex bytes
            for (int j = 0; j < lineBytes; j++)
                s.Append($"{bytes[i + j]:X2} ");

            // Pad incomplete last line so ASCII column is aligned
            for (int j = lineBytes; j < bytesPerLine; j++)
                s.Append("   ");

            // ASCII representation
            s.Append(" |");
            for (int j = 0; j < lineBytes; j++) {
                byte b = bytes[i + j];
                s.Append(b >= 0x20 && b < 0x7F ? (char)b : '.');
            }
            s.Append('|');

            s.AppendLine();
        }

        Console.Write(s);
    }

    public static void DumpDiffPacket(ReadOnlySpan<byte> bytes, ReadOnlySpan<byte> compare) {
        const int bytesPerLine = 16;
        int length = Math.Max(bytes.Length, compare.Length);
        StringBuilder s = new StringBuilder();

        s.Append("--- Sequence Id ---\n");

        int offset = 0;

        for (int i = 0; i < length; i += bytesPerLine) {
            s.Append($"\n{i:X8}: ");

            for (int j = 0; j < bytesPerLine; j++) {
                int idx = i + j + offset;

                if (idx >= length) {
                    break;
                }

                if (idx == Packet.SEQID_LENGTH) {
                    s.Append("\n --- Payload ---");
                    s.Append($"\n{i:X8}: ");
                    offset += j;
                    j = 0;
                } else if (idx == Packet.SEQID_LENGTH + Packet.PAYLOAD_LENGTH) {
                    s.Append("\n --- Random Bytes ---");
                    s.Append($"\n{i:X8}: ");
                    offset += j;
                    j = 0;
                } else if (idx == Packet.SEQID_LENGTH + Packet.PAYLOAD_LENGTH + Packet.RANDOM_BYTES_LENGTH) {
                    s.Append("\n --- Hash ---");
                    s.Append($"\n{i:X8}: ");
                    offset += j;
                    j = 0;
                }

                bool aExists = idx < bytes.Length;
                bool bExists = idx < compare.Length;
                byte aByte = aExists ? bytes[idx] : (byte)0;
                byte bByte = bExists ? compare[idx] : (byte)0;

                if (aExists != bExists || aByte != bByte)
                    s.Append($"[\x1b[91m{aByte:X2}\x1b[39m->\x1b[92m{bByte:X2}\x1b[39m]   ");
                else
                    s.Append($"{aByte:X2}         ");
            }
        }

        Console.WriteLine(s);
    }
}