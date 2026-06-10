namespace ZuneDeploy.XNA.Data;

public class ContainerFile(string pathOnDisk, string pathInContainer) {
    private readonly string _pathOnDisk = pathOnDisk;
    public string PathInContainer { init; get; } = pathInContainer;

    public Stream Open() {
        return File.OpenRead(_pathOnDisk);
    }
}
