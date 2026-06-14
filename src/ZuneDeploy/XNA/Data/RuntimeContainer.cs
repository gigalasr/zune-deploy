using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.Json;
using ZuneDeploy.Transport;

namespace ZuneDeploy.XNA.Data;

public record RuntimeContainer : RuntimeContainerConfig {
    public static readonly string DefaultRuntimeToken = "Zune.v4.0.Beta";
    public static readonly long DefaultVersion = 0x3102bb64;
    private static readonly string _configName = "runtime.json";

    public required ReadOnlyCollection<ContainerFile> Files { init; get; }
    public required bool IsDefault { init; get; }

    [SetsRequiredMembers]
    private RuntimeContainer(RuntimeContainerConfig config, ReadOnlyCollection<ContainerFile> files, bool isDefault) : base(config) {
        Files = files;
        IsDefault = isDefault;
    }

    public static RuntimeContainer FromFolder(DirectoryInfo folder) {
        RuntimeContainerConfig config;
        bool isDefault = true;

        // Override Defaults if config file exists
        string configPath = Path.Join(folder.FullName, _configName);
        if (Path.Exists(configPath)) {
            isDefault = false;
            config = RuntimeContainerConfig.FromFile(configPath);
        } else {
            isDefault = true;
            config = RuntimeContainerConfig.FromDefaults();
            config.Save(configPath);
        }

        var files = ContainerFile.CollectFromFolder(folder.FullName);
        return new RuntimeContainer(config, files, isDefault);
    }

    public bool IsCompatible(DeviceFamily family) {
        if (Compatibility == DeviceCompatibility.ANY) {
            return true;
        } else if (Compatibility == DeviceCompatibility.ZUNE_HD_ONLY) {
            return family == DeviceFamily.Pavo;
        } else if (Compatibility == DeviceCompatibility.ZUNE_SD_ONLY) {
            return family is DeviceFamily.Draco or DeviceFamily.Keel or DeviceFamily.Scorpius;
        }

        throw new UnreachableException();
    }

    public override string ToString() {
        var sb = new StringBuilder();
        sb.AppendLine("Runtime Container:");
        sb.AppendLine($"Token:         {RuntimeToken}");
        sb.AppendLine($"Version:       0x{Version:X}");
        sb.AppendLine($"Compatibility: {Compatibility}");
        return sb.ToString();
    }
}

public record RuntimeContainerConfig {
    public required string RuntimeToken { init; get; }
    public required long Version { init; get; }
    public required DeviceCompatibility Compatibility { init; get; }

    public static RuntimeContainerConfig FromFile(string path) {
        return FromJsonText(File.ReadAllText(path));
    }

    public static RuntimeContainerConfig FromJsonText(string text) {
        return JsonSerializer.Deserialize<RuntimeContainerConfig>(text)!;
    }

    public void Save(string path) {
        File.WriteAllText(path, JsonSerializer.Serialize(this));
    }

    public static RuntimeContainerConfig FromDefaults() {
        return new RuntimeContainerConfig {
            RuntimeToken = RuntimeContainer.DefaultRuntimeToken,
            Version = RuntimeContainer.DefaultVersion,
            Compatibility = DeviceCompatibility.ANY
        };
    }
}
