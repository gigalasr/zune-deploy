namespace ZuneDeploy.XNA.Channels;

public interface IFileDeployChannel {
    public void PutFileInContainer(string filePath, Stream fileContent);
    public event EventHandler<ushort>? OnBytesWritten;
}
