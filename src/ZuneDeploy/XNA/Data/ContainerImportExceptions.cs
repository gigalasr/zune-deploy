namespace ZuneDeploy.XNA.Data;

public class ContainerImportException : Exception {
    public ContainerImportException() { }
    public ContainerImportException(string reason) : base(reason) { }
}

public class MissingConfigurationException : ContainerImportException {
    public MissingConfigurationException(string file) : base($"Missing configuration file: {file}") { }
    public MissingConfigurationException(string[] files) : base($"Folder has no configuration file. Please create {String.Join(" or ", files)}") { }
}

public class ParseConfigurationException(string reason, int line)
    : ContainerImportException($"Syntax error on line {line}: {reason}") { }

public class InvalidValueException(string key, string reason)
    : ContainerImportException($"Invalid value for key '{key}': {reason}") { }

public class ContainerPathNotFoundException(string path, string resource)
    : ContainerImportException($"Could not find path: '{path}' for resource '{resource}'.") { }
