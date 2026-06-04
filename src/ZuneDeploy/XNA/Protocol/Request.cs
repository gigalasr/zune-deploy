using System.Text;
using ZuneDeploy.Transport;

namespace ZuneDeploy.XNA.Protocol;


internal static class Request {
    private static void ExpectType<T>(object val) {
        if (val is not T) {
            throw new ArgumentException($"Expected {typeof(T).Name}, but got {val.GetType().Name}");
        }
    }

    public static void WriteToStream(ServiceStream stream, RemoteProcedure proc, params object[] args) {
        if (proc.Parameters.Count != args.Length) {
            throw new ArgumentException($"Invalid number of arguments for '{proc.Name}'", "args");
        }

        BinaryWriter writer = new BinaryWriter(stream, Encoding.Unicode);

        // Header
        writer.Write(Message.HeaderMagicValue);
        writer.Write((byte)MessageType.Request);
        writer.Write(proc.Name);
        writer.Write((byte)proc.Parameters.Count);

        // Args
        for (int i = 0; i < args.Length; i++) {
            var definition = proc.Parameters[i];
            var value = args[i];

            if (value == null) {
                throw new ArgumentException($"Argument '{definition.Name}' at idx={i} cannot be null", "args");
            }

            // The original driver sends 0 instead of 1 for boolean, probably a mistake but works out because of the size
            // We'll send a 1 for now, as that should be the correct value
            writer.Write(definition.Name);
            writer.Write((byte)definition.Type);

            switch (definition.Type) {
                case ParameterType.Byte:
                    ExpectType<byte>(value);
                    writer.Write((byte)value);
                    break;
                case ParameterType.Boolean:
                    ExpectType<bool>(value);
                    writer.Write((bool)value);
                    break;
                case ParameterType.Int16:
                    ExpectType<short>(value);
                    writer.Write((short)value);
                    break;
                case ParameterType.Int32:
                    ExpectType<int>(value);
                    writer.Write((int)value);
                    break;
                case ParameterType.Int64:
                    ExpectType<long>(value);
                    writer.Write((long)value);
                    break;
                case ParameterType.Single:
                    ExpectType<float>(value);
                    writer.Write((float)value);
                    break;
                case ParameterType.Double:
                    ExpectType<double>(value);
                    writer.Write((double)value);
                    break;
                case ParameterType.DateTime:
                    ExpectType<DateTime>(value);
                    writer.Write(((DateTime)value).Ticks);
                    break;
                case ParameterType.String:
                    ExpectType<string>(value);
                    writer.Write((string)value);
                    break;
                case ParameterType.Guid:
                    ExpectType<Guid>(value);
                    writer.Write(((Guid)value).ToByteArray());
                    break;
                case ParameterType.Stream:
                    ExpectType<Stream>(value);
                    // TODO: Check if we really need to to i + 1
                    writer.Write((byte)(i + 1));
                    writer.Write((int)((Stream)value).Length);
                    break;
            }
        }

        writer.Flush();
    }

    private const int MAX_CHUNK_BYTES = 0x1FFFB;
    public static void WriteDataStreamToStream(ServiceStream target, Stream source) {
        if (source.Length > int.MaxValue) {
            throw new Exception("Parameter Streams can not be longer than 2 GB");
        }

        BinaryWriter bw = new BinaryWriter(target);

        // The Zune will choke with chunks > 128 KiB
        int len = (int)Math.Min(source.Length, MAX_CHUNK_BYTES);
        byte[] buffer = new byte[len];

        while (source.Length - source.Position > 0) {
            int read = source.Read(buffer, 0, len);
            if (read == 0) {
                throw new Exception("Unexpected end of stream reached");
            }

            Console.WriteLine($"Sending Chunk len={read}. Progress: {source.Position}/{source.Length}");

            // If we ever wanted to cancel the transfer, we can send a message only containg TRUE to stop 
            bw.Write(false);
            bw.Write(read);
            bw.Write(buffer, 0, read);
            bw.Flush();
        }
    }
}