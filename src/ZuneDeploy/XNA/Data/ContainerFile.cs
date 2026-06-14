using System.Collections.ObjectModel;

namespace ZuneDeploy.XNA.Data;

public class ContainerFile(string root, string filePath) {
    private readonly string _pathOnDisk = filePath;
    public string PathInContainer { init; get; } = Path.GetRelativePath(root, filePath).Replace("/", "\\");

    public static ReadOnlyCollection<ContainerFile> CollectFromFolder(string root) {
        return Directory.EnumerateFiles(root, "*.*", SearchOption.AllDirectories)
            .Select(f => new ContainerFile(root, f))
            .ToList()
            .AsReadOnly();
    }

    public Stream Open() {
        return File.OpenRead(_pathOnDisk);
    }
}
