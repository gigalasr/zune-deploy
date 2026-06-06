namespace ZuneDeploy.XNA.Data;

public class ContainerFile {
    private readonly string _pathOnDisk;
    public string PathInContainer { init; get; }

    public ContainerFile(string pathOnDisk, string pathInContainer) {
        _pathOnDisk = pathOnDisk;
        PathInContainer = pathInContainer;
    }

    public Stream Open() {
        return File.OpenRead(_pathOnDisk);
    }
}