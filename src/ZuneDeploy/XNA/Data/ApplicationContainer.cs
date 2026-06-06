using System.Collections.ObjectModel;
using System.Dynamic;
using System.Security.Cryptography.X509Certificates;
using ZuneDeploy.XNA.Channels;

namespace ZuneDeploy.XNA.Data;


public class ApplicationContainer {
    public ReadOnlyCollection<ContainerFile> Files { init; get; }
    public Guid ContainerId { init; get; }
    public string DisplayName { init; get; }

    // Hardcoded for now
    public string Description { init; get; }
    public string EntryPoint { init; get; }
    public string RuntimeToken { init; get; }


    public static ApplicationContainer FromFolder(DirectoryInfo folder, Guid? guid = null) {
        string folderPath = folder.FullName;
        string NormalizeFilePath(string path) {
            return path.Substring(folderPath.Length + 1).Replace("/", "\\");
        }

        var files = Directory.EnumerateFiles(folderPath, "*.*", SearchOption.AllDirectories)
            .Select(f => new ContainerFile(f, NormalizeFilePath(f)))
            .ToList();

        return new ApplicationContainer(guid ?? Guid.NewGuid(), folder.Name, files.AsReadOnly());
    }

    private ApplicationContainer(Guid guid, string name, ReadOnlyCollection<ContainerFile> files) {
        Files = files;
        ContainerId = guid;
        DisplayName = name;
        Description = "Default Description";
        EntryPoint = "exploiter.exe";
        RuntimeToken = "Zune.v4.0.Beta";
    }
}