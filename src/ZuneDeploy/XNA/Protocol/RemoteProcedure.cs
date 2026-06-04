using System.Collections.ObjectModel;

namespace ZuneDeploy.XNA.Protocol;

internal enum ParameterType : byte {
    Byte = 0,
    Boolean = 1,
    Int16 = 2,
    Int32 = 3,
    Int64 = 4,
    Single = 5,
    Double = 6,
    DateTime = 7,
    String = 8,
    Guid = 9,
    Stream = 10
}

internal record Parameter(string Name, ParameterType Type);

internal record RemoteProcedure(string Name, ReadOnlyCollection<Parameter> Parameters);