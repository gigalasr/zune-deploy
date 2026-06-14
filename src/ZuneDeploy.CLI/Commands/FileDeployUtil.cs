using ZuneDeploy.XNA.Channels;
using ZuneDeploy.XNA.Data;

internal static class FileDeployUtil {
    public static void DeployFile(IFileDeployChannel channel, ContainerFile file) {
        DeployFileInternal(channel, file, false);
    }

    public static void DeployThumbnail(GameDeployChannel channel, ContainerFile file) {
        DeployFileInternal(channel, file, true);
    }

    private static void DeployFileInternal(IFileDeployChannel channel, ContainerFile file, bool isThumbnail) {
        Spinner.Start($"Deploying {file.PathInContainer}");
        long maxBytes = 0;
        long totalBytes = 0;
        string currentFilePath = "n/a";
        void SpinnerUpdate(object? _, ushort bytes) {
            totalBytes += bytes;
            int progress = (int)(totalBytes / (double)maxBytes * 100);
            Spinner.SetLabel($"Deploying {currentFilePath} ({progress}%)");
        }

        channel.OnBytesWritten += SpinnerUpdate;
        try {
            using var fs = file.Open();
            currentFilePath = file.PathInContainer;
            maxBytes = fs.Length;
            totalBytes = 0;
            UploadFileToDevice(channel, file, fs, isThumbnail);
        } catch (Exception e) {
            Spinner.Stop($"File Deploy Failed: {e.Message}", true);
        } finally {
            channel.OnBytesWritten -= SpinnerUpdate;
        }

        Spinner.Stop($"Deployed {file.PathInContainer}");
    }

    private static void UploadFileToDevice(IFileDeployChannel channel, ContainerFile file, Stream fs, bool isThumbnail) {
        if (isThumbnail) {
            if (channel is GameDeployChannel gameDeployChannel) {
                gameDeployChannel.PutThumbnailInContainer(fs);
            } else {
                throw new Exception("Cannot set thumbnail for runtime container");
            }
        } else {
            channel.PutFileInContainer(file.PathInContainer, fs);
        }
    }
}
