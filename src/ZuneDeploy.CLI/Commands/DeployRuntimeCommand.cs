
using ZuneDeploy.Transport;
using ZuneDeploy.XNA.Channels;
using ZuneDeploy.XNA.Data;

namespace ZuneDeploy.CLI.Commands;

internal static class DeployRuntimeCommand {
    public static void Run(DirectoryInfo folder, bool printContainerInfo) {
        Zune? zune = null;

        try {
            var container = Spinner.SpinFor($"Importing Runtime Container '{folder.Name}'",
                () => RuntimeContainer.FromFolder(folder)
            );

            if (printContainerInfo) {
                Console.Write(container.ToString());
            }

            zune = Spinner.SpinFor("Connecting to Zune",
                () => new Zune(),
                z => $"Connected to {z.DeviceFamily.AsWellKnownName()}"
            );

            DeployRuntime(zune, container);
        } catch (ContainerImportException e) {
            Spinner.Stop($"Failed to import runtime container: {e.Message}");
        } catch (Exception e) {
            Spinner.Stop($"Deploy Failed: {e.Message}", true);
        } finally {
            zune?.Dispose();
        }
    }

    public static void DeployRuntime(Zune zune, RuntimeContainer runtimeContainer) {
        Spinner.Start("Checking if runtime is available");

        if (!runtimeContainer.IsCompatible(zune.DeviceFamily)) {
            throw new Exception("Provided Runtime Container is not compatible with device");
        }

        using RuntimeDeployChannel channel = zune.OpenXNARuntimeDeployChannel();

        if (channel.IsRuntimeAvailable(runtimeContainer)) {
            Spinner.Stop($"Runtime '{runtimeContainer.RuntimeToken}' version=0x{runtimeContainer.Version:X} is already available.");
            return;
        }
        Spinner.Stop($"Runtime '{runtimeContainer.RuntimeToken}' version=0x{runtimeContainer.Version:X} is not available and will be deployed now.");

        Spinner.SpinFor("Opening Runtime Container", () => {
            channel.OpenRuntimeContainer(runtimeContainer.RuntimeToken, runtimeContainer.Version);
        });

        foreach (var file in runtimeContainer.Files) {
            FileDeployUtil.DeployFile(channel, file);
        }

        Spinner.SpinFor(
            "Closing Runtime Container",
            channel.CloseRuntimeContainer,
            $"Deployed Runtime token='{runtimeContainer.RuntimeToken}' version={runtimeContainer.Version:X}"
        );
    }
}
