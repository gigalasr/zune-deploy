using System.Diagnostics.CodeAnalysis;

namespace ZuneDeploy.XNA.Data;

public class ContainerImportException : Exception {
    public ContainerImportException() { }
    public ContainerImportException(string reason) : base(reason) { }
}

public class MissingConfigurationException : ContainerImportException {
    public MissingConfigurationException() { }
}

public class ParseConfigurationException : ContainerImportException {
    public ParseConfigurationException(string reason, int line) : base($"Syntax error on line {line}: {reason}") { }
}

public class ContainerPathNotFoundException : ContainerImportException {
    public ContainerPathNotFoundException(string path, string resource) : base($"Could not find path: '{path}' for resource '{resource}'.") { }
}

// public class MissingKeyException : ContainerImportException {

//     public MissingKeyException(string key) : base($"The required key '{key}' is missing in the configuration") { }
// }