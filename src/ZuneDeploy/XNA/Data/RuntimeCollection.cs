using System.Collections.ObjectModel;
using ZuneDeploy.Transport;

namespace ZuneDeploy.XNA.Data;

public class RuntimeCollection {
    public ReadOnlyCollection<RuntimeContainer> runtimes;

    public ReadOnlyCollection<string> SearchPaths { init; get; }

    public RuntimeCollection() {
        SearchPaths = [
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "DeployableRuntimes")
        ];

        runtimes = SearchPaths
            .Where(Directory.Exists)
            .SelectMany(Directory.GetDirectories)
            .Select(loc => RuntimeContainer.FromFolder(new DirectoryInfo(loc)))
            .ToList().AsReadOnly();
    }

    public RuntimeContainer? GetLatestContainerForToken(string token, DeviceFamily deviceFamily) {
        return runtimes
            .Where(r => r.RuntimeToken == token && r.IsCompatible(deviceFamily))
            .OrderBy(r => r.Version)
            .FirstOrDefault();
    }
}
