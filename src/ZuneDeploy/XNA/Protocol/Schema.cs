using System.Collections.ObjectModel;
using System.Text;

namespace ZuneDeploy.XNA.Protocol;

internal class Schema {
    private Dictionary<string, RemoteProcedure> _procedures = new();

    private Schema(List<RemoteProcedure> procs) {
        procs.ForEach(p => _procedures.Add(p.Name, p));
    }

    public RemoteProcedure GetDefinition(string name) {
        return _procedures[name];
    }

    public override string ToString() {
        var sb = new StringBuilder();
        foreach (var def in _procedures.Values) {
            sb.Append($"{def.Name}(");
            for (int i = 0; i < def.Parameters.Count; i++) {
                var param = def.Parameters[i];
                sb.Append($"{param.Type} {param.Name}");
                if (i != def.Parameters.Count - 1) {
                    sb.Append(", ");
                }
            }
            sb.AppendLine(")");
        }
        return sb.ToString();
    }

    public static Schema ReadFromStream(Stream stream) {
        BinaryReader reader = new BinaryReader(stream, Encoding.Unicode);
        Message.ValidateHeaderAndType(reader, MessageType.Schema);

        List<RemoteProcedure> procedures = new();

        byte procCount = reader.ReadByte();
        for (int i = 0; i < procCount; i++) {
            string procName = reader.ReadString();
            byte paramCount = reader.ReadByte();

            List<Parameter> parameters = new(paramCount);
            for (int j = 0; j < paramCount; j++) {
                string parameterName = reader.ReadString();
                var parameterType = (ParameterType)reader.ReadByte();
                parameters.Add(new Parameter(parameterName, parameterType));
            }

            procedures.Add(new RemoteProcedure(procName, parameters.AsReadOnly()));
        }

        return new Schema(procedures);
    }
}