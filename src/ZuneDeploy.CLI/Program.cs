using System.CommandLine;
using System.CommandLine.Parsing;
using NativeGen;
using ZuneDeploy.Transport;
using ZuneDeploy.XNA.Channels;
using ZuneDeploy.XNA.Data;

namespace ZuneDeploy.CLI;

class Program {
    static async Task<int> Main(string[] args) {
        RootCommand rootCommand = new("Zune Deploy");

        { // Deploy Command
            Option<bool> launchOpt = new("--launch", "-l") {
                Description = "Launch the application after deploying"
            };
            Option<bool> infoOpt = new("--info", "-i") {
                Description = "Print container information after import"
            };
            Argument<DirectoryInfo> pathArg = new("path") {
                Description = "Path to folder or .ccgame to deploy to the Zune",
                CustomParser = result => {
                    var folder = new DirectoryInfo(result.Tokens.Single().Value);
                    if (!folder.Exists) {
                        result.AddError($"Folder '{result.Tokens.Single().Value}' does not exist");
                    }
                    return folder;
                },
            };
            Command deployCommand = new("deploy", "Deploy an application to the Zune") {
                launchOpt, pathArg
            };
            rootCommand.Subcommands.Add(deployCommand);

            deployCommand.SetAction((parsed) => {
                bool launch = parsed.GetValue(launchOpt);
                bool info = parsed.GetValue(infoOpt);
                DirectoryInfo folder = parsed.GetValue(pathArg)!;
                Deploy(folder, launch, info);
            });
        }

        Console.CursorVisible = false;
        rootCommand.Parse(args).Invoke();
        Console.CursorVisible = true;

        return 0;
    }

    static void Deploy(DirectoryInfo folder, bool launch, bool printContainerInfo) {
        Spinner spinner = new Spinner();

        try {
            var container = spinner.SpinFor(
                $"Importing '{folder.Name}'",
                $"Imported '{folder.Name}'",
                () => ApplicationContainer.FromFolder(folder)
            );

            if (printContainerInfo) {
                Console.Write(container.ToString());
            }

            var zune = spinner.SpinFor(
                "Connecting to Zune",
                "Connected",
                () => new Zune()
            );

            spinner.Start("Opening Deploy Channel");
            using (GameDeployChannel deployChan = zune.OpenXNAGameDeployChannel()) {
                spinner.SetLabel("Opening Game Container");
                deployChan.OpenContainer(container);
                spinner.Stop("Opened Game Container");

                string currentFilePath = "Deploying...";
                long maxBytes = 0;
                long totalBytes = 0;
                EventHandler<ushort> spinnerUpdate = (_, bytes) => {
                    totalBytes += bytes;
                    int progress = (int)(totalBytes / (double)maxBytes * 100);
                    spinner.SetLabel($"Deploying {currentFilePath} ({progress}%)");
                };

                deployChan.OnBytesWritten += spinnerUpdate;
                foreach (var fileInfo in container.Files) {
                    spinner.Start($"Deploying {fileInfo.PathInContainer}");
                    using (var fs = fileInfo.Open()) {
                        currentFilePath = fileInfo.PathInContainer;
                        maxBytes = fs.Length;
                        totalBytes = 0;
                        deployChan.PutFileInContainer(fileInfo.PathInContainer, fs);
                    }
                    spinner.Stop($"Deployed {fileInfo.PathInContainer}");
                }

                if (container.ThumbnailPath != null) {
                    spinner.Start($"Setting Thumbnail");
                    currentFilePath = container.ThumbnailFileName!;
                    using (var fs = File.OpenRead(container.ThumbnailPath)) {
                        maxBytes = fs.Length;
                        totalBytes = 0;
                        deployChan.PutThumbnailInContainer(fs);
                    }
                    spinner.Stop($"Deployed {container.ThumbnailFileName!}");
                }

                deployChan.OnBytesWritten -= spinnerUpdate;

                spinner.Start("Setting Container Metadata");
                deployChan.PutGamePropertiesEx(container);
                spinner.Stop("Set Container Metadata");

                spinner.Start("Closing Game Container");
                deployChan.CloseGameContainer();
            }
            spinner.Stop("Closed Game Container");

            if (launch) {
                spinner.Start("Opening Launch Channel");
                using (LaunchChannel launchChan = zune.OpenXnaLaunchChannel()) {
                    spinner.SetLabel("Launching Game");
                    launchChan.LaunchInMode(container.ContainerId, "", true);
                    spinner.SetLabel("Running");
                }
                spinner.Stop("Done");
            }
        } catch (ContainerImportException e) {
            spinner.Stop($"Failed to import game container: {e.Message}");
        } catch (Exception e) {
            spinner.Stop($"Deploy Failed: {e.Message}", true);
        }
    }
}
