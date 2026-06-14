using ZuneDeploy.Transport;
using ZuneDeploy.XNA.Channels;
using ZuneDeploy.XNA.Data;

namespace ZuneDeploy.CLI.Commands;

internal static class DeployApplicationCommand {
    public static void Run(DirectoryInfo folder, bool launch, bool printContainerInfo, bool deployRuntime, string? runtimeTokenOverride) {
        Zune? zune = null;

        try {
            var container = Spinner.SpinFor($"Importing '{folder.Name}'",
                () => ApplicationContainer.FromFolder(folder)
            );

            if (runtimeTokenOverride != null) {
                container.RuntimeToken = runtimeTokenOverride;
            }

            if (printContainerInfo) {
                Console.Write(container.ToString());
            }

            zune = Spinner.SpinFor("Connecting to Zune",
                () => new Zune(),
                z => $"Connected to {z.DeviceFamily.AsWellKnownName()}"
            );


            if (!container.IsCompatible(zune.DeviceFamily)) {
                throw new Exception("Provided Application is not compatible with device");
            }


            if (deployRuntime) {
                DeployRuntime(zune, container.RuntimeToken, printContainerInfo);
            }

            DeployApplication(zune, container);

            if (launch) {
                LaunchApplication(zune, container);
            }
        } catch (ContainerImportException e) {
            Spinner.Stop($"Failed to import application container: {e.Message}");
        } catch (Exception e) {
            Spinner.Stop($"Deploy Failed: {e.Message}", true);
        } finally {
            zune?.Dispose();
        }
    }

    private static void DeployRuntime(Zune zune, string runtimeToken, bool printContainerInfo) {
        RuntimeCollection collection = new();

        var container = collection.GetLatestContainerForToken(runtimeToken, zune.DeviceFamily)
            ?? throw new Exception($@"Failed to find runtime container '{runtimeToken}'.
Please deploy the runtime manually or provide the runtime at one of the following paths:
{String.Join("\n", collection.SearchPaths)}");

        if (printContainerInfo) {
            Console.Write(container.ToString());
        }

        DeployRuntimeCommand.DeployRuntime(zune, container);
    }

    private static void DeployApplication(Zune zune, ApplicationContainer container) {
        Spinner.Start("Opening Deploy Channel");
        using GameDeployChannel deployChan = zune.OpenXNAGameDeployChannel();

        Spinner.SpinFor("Opening App Container", () => {
            deployChan.OpenContainer(container);
        });

        foreach (var file in container.Files) {
            FileDeployUtil.DeployFile(deployChan, file);
        }

        if (container.Thumbnail != null) {
            FileDeployUtil.DeployThumbnail(deployChan, container.Thumbnail);
        }

        Spinner.SpinFor("Uploading Container Metadata", () => {
            deployChan.PutGamePropertiesEx(container);
        });
        Spinner.SpinFor("Closing Game Container", deployChan.CloseGameContainer);
    }

    private static void LaunchApplication(Zune zune, ApplicationContainer container) {
        using LaunchChannel launchChan = zune.OpenXnaLaunchChannel();
        Spinner.SpinFor("Launching Application", () => {
            launchChan.LaunchInMode(container.ContainerId, "", true);
        });
        Spinner.SpinFor("Running", () => {
            do {
                Thread.Sleep(1000);
            } while (launchChan.IsTitleRunning());
        }, "Done");
    }
}
