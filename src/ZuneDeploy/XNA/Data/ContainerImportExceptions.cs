namespace ZuneDeploy.XNA.Data;

public class ContainerImportException : Exception {
    public ContainerImportException() { }
    public ContainerImportException(string reason) : base(reason) { }
}

public class MissingConfigurationException : ContainerImportException {
    public MissingConfigurationException() { }
}

public class ParseConfigurationException(string reason, int line)
    : ContainerImportException($"Syntax error on line {line}: {reason}") { }

public class ContainerPathNotFoundException(string path, string resource)
    : ContainerImportException($"Could not find path: '{path}' for resource '{resource}'.") { }
