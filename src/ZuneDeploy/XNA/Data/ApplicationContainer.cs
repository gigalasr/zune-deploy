using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace ZuneDeploy.XNA.Data;

public record DeployConfiguration {
    public required Guid ContainerId { init; get; }
    public required string DisplayName { init; get; }
    public required string? Description { init; get; }
    public required string EntryPoint { init; get; }
    public required string RuntimeToken { init; get; }
    public required string SourceFolderName { init; get; }
    public required string? ThumbnailFileName { init; get; }

    protected static string DeployKitConfigName = "application.cfg";
    protected static string ZuneDeployConfigName = "app.json";


    public static DeployConfiguration FromDeployKitConfiguration(string path) {
        return FromDeployKitConfiguration(File.ReadAllLines(path));
    }
    public static DeployConfiguration FromDeployKitConfiguration(string[] lines) {
        Dictionary<string, string> config = new();

        int lineNumber = 0;
        foreach (string rawLine in lines) {
            lineNumber++;
            string line = rawLine.Trim();
            if (line.Length == 0 || line.StartsWith('#')) {
                continue;
            }

            string[] parts = line.Split(':', 2);
            if (parts.Length != 2) {
                throw new ParseConfigurationException("Line must contain exactly one key value pair: `key: value`", lineNumber);
            }

            string key = parts[0].Trim().ToLower();
            if (key.Length == 0) {
                throw new ParseConfigurationException("Key must not be empty", lineNumber);
            }

            string value = parts[1].Trim();
            if (value.Length == 0) {
                throw new ParseConfigurationException("Value must not be empty", lineNumber);
            }

            if (config.ContainsKey(key)) {
                throw new ParseConfigurationException($"Duplicate Keys. Key '{key}' was already set on a previous line.", lineNumber);
            }

            config[key] = value;
        }

        config.TryGetValue("thumbnail", out string? thumbnail);
        config.TryGetValue("description", out string? description);

        return new DeployConfiguration() {
            ContainerId = Guid.Parse(config["guid"]),
            DisplayName = config["name"],
            Description = description,
            EntryPoint = config["exec"],
            RuntimeToken = RuntimeContainer.DefaultRuntimeToken,
            SourceFolderName = config["src"],
            ThumbnailFileName = thumbnail
        };
    }

    protected static DeployConfiguration FromZuneDeployConfiguration(string path) {
        throw new NotImplementedException("Zune Deploy Config Files are not supported yet.");
    }
}

public record ApplicationContainer : DeployConfiguration {
    public required ReadOnlyCollection<ContainerFile> Files { init; get; }
    public required ContainerFile? Thumbnail { init; get; }

    [SetsRequiredMembers]
    private ApplicationContainer(DeployConfiguration _config, ReadOnlyCollection<ContainerFile> _files, ContainerFile? thumbnail) : base(_config) {
        Files = _files;
        Thumbnail = thumbnail;
    }

    public static ApplicationContainer FromFolder(DirectoryInfo folder) {
        DeployConfiguration config;

        // TODO: Also support zips
        string deployKitConfig = Path.Join(folder.FullName, DeployKitConfigName);
        string zuneDeployConfig = Path.Join(folder.FullName, ZuneDeployConfigName);

        if (Path.Exists(deployKitConfig)) {
            config = DeployConfiguration.FromDeployKitConfiguration(deployKitConfig);
        } else if (Path.Exists(zuneDeployConfig)) {
            config = DeployConfiguration.FromZuneDeployConfiguration(zuneDeployConfig);
        } else {
            throw new MissingConfigurationException();
        }

        // Check if Thumbnail exists
        ContainerFile? thumbnail = null;

        if (config.ThumbnailFileName != null) {
            string thumbnailPath = Path.Join(folder.FullName, config.ThumbnailFileName);
            if (!File.Exists(thumbnailPath)) {
                throw new ContainerPathNotFoundException(thumbnailPath, "thumbnail");
            }
            thumbnail = new ContainerFile(thumbnailPath, config.ThumbnailFileName);
        }

        // Check if entry point executable exists
        string execPath = Path.Join(folder.FullName, config.SourceFolderName, config.EntryPoint);
        if (!Path.Exists(execPath)) {
            throw new ContainerPathNotFoundException(execPath, "exec");
        }

        // Collect all files in src folder
        string sourcePath = Path.Join(folder.FullName, config.SourceFolderName);
        string NormalizeFilePath(string path) {
            return path.Substring(sourcePath.Length + 1).Replace("/", "\\");
        }
        var files = Directory.EnumerateFiles(sourcePath, "*.*", SearchOption.AllDirectories)
            .Select(f => new ContainerFile(f, NormalizeFilePath(f)))
            .ToList();

        return new ApplicationContainer(config, files.AsReadOnly(), thumbnail);
    }

    public override string ToString() {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine($"Name:          {DisplayName}");
        if (Description != null) {
            sb.AppendLine($"Description:   {Description}");
        }
        sb.AppendLine($"ContainerId:   {ContainerId}");
        sb.AppendLine($"Rumtime Token: {RuntimeToken}");
        sb.AppendLine($"Executable:    {EntryPoint}");
        sb.AppendLine($"Source Folder: {SourceFolderName}");
        if (ThumbnailFileName != null) {
            sb.AppendLine($"Thumbnail:     {ThumbnailFileName}");
        }

        return sb.ToString();
    }

}