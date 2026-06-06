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
            Option<bool> deployNoLaunchOpt = new("--no-launch") {
                Description = "Do not launch the application after deploying"
            };
            Option<DirectoryInfo> deployFolderOpt = new("--folder") {
                Description = "The folder to deploy to the Zune",
                Required = true,
                CustomParser = result => {
                    var folder = new DirectoryInfo(result.Tokens.Single().Value);
                    if (!folder.Exists) {
                        result.AddError($"Folder '{result.Tokens.Single().Value}' does not exist");
                    }
                    return folder;
                },
            };
            Command deployCommand = new("deploy", "Interface with Zune XNA Sessions") {
                deployNoLaunchOpt, deployFolderOpt
            };
            rootCommand.Subcommands.Add(deployCommand);

            deployCommand.SetAction(async (parsed) => {
                bool launch = !parsed.GetValue(deployNoLaunchOpt);
                DirectoryInfo folder = parsed.GetValue(deployFolderOpt)!;
                await Deploy(folder, launch);
            });
        }

        Console.CursorVisible = false;
        rootCommand.Parse(args).Invoke();
        Console.CursorVisible = true;

        return 0;
    }

    static async Task Deploy(DirectoryInfo folder, bool launch) {
        Spinner spinner = new Spinner();

        spinner.Start("Connecting to Zune");
        Zune zune = new Zune();
        spinner.Stop("Connected");

        spinner.Start("Importing Folder");
        ApplicationContainer container = ApplicationContainer.FromFolder(folder);
        spinner.Stop($"Imported Folder '{folder.Name}'");

        spinner.Start("Opening Deploy Channel");
        using (GameDeployChannel deployChan = zune.OpenXNAGameDeployChannel()) {
            spinner.SetLabel("Opening Game Container");
            deployChan.OpenContainer(container);
            spinner.Stop("Opened Game Container");

            string prefix = "Deploying...";
            long maxBytes = 0;
            long totalBytes = 0;
            EventHandler<ushort> spinnerUpdate = (_, bytes) => {
                totalBytes += bytes;
                int progress = (int)(totalBytes / (double)maxBytes * 100);
                spinner.SetLabel($"{prefix} ({progress}%)");
            };

            deployChan.OnBytesWritten += spinnerUpdate;
            foreach (var fileInfo in container.Files) {
                spinner.Start($"Deploying {fileInfo.PathInContainer}");
                using (var fs = fileInfo.Open()) {
                    prefix = fileInfo.PathInContainer;
                    maxBytes = fs.Length;
                    totalBytes = 0;
                    deployChan.PutFileInContainer(fileInfo.PathInContainer, fs);
                }
                spinner.Stop($"Deployed {fileInfo.PathInContainer}");
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
    }
}
