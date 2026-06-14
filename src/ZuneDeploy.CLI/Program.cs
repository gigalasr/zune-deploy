using System.CommandLine;
using System.CommandLine.Parsing;
using ZuneDeploy.CLI.Commands;

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
            Option<bool> skipRuntimeCheckOpt = new("--skip-runtime-deploy", "-s") {
                Description = "Skip checking and deploying the runtime container"
            };
            Option<string> runtimeTokenOverrideOpt = new("--runtime-token", "-r") {
                Description = "Override the runtime to use and deploy",
                Required = false
            };
            Argument<DirectoryInfo> pathArg = new("path") {
                Description = "Path to folder or .ccgame to deploy to the Zune",
                CustomParser = DirectoryArgumentValidation,
            };
            Command deployCommand = new("deploy", "Deploy an application to the Zune") {
                launchOpt, infoOpt, skipRuntimeCheckOpt, pathArg, runtimeTokenOverrideOpt
            };
            deployCommand.SetAction((parsed) => {
                bool launch = parsed.GetValue(launchOpt);
                bool info = parsed.GetValue(infoOpt);
                bool deployRuntime = !parsed.GetValue(skipRuntimeCheckOpt);
                DirectoryInfo folder = parsed.GetValue(pathArg)!;
                string? runtimeTokenOverride = parsed.GetValue(runtimeTokenOverrideOpt);
                DeployApplicationCommand.Run(folder, launch, info, deployRuntime, runtimeTokenOverride);
            });
            rootCommand.Subcommands.Add(deployCommand);
        }

        { // Runtime Deploy Command
            Argument<DirectoryInfo> folderArg = new("folder") {
                Description = "Path to runtime folder to deploy to the Zune",
                CustomParser = DirectoryArgumentValidation,
            };
            Option<bool> infoOpt = new("--info", "-i") {
                Description = "Print container information after import"
            };
            Command runtimeDeployCommand = new("runtime", "Deploy a runtime container to the Zune") {
                folderArg, infoOpt
            };
            runtimeDeployCommand.SetAction(parsed => {
                DirectoryInfo folder = parsed.GetValue(folderArg)!;
                bool info = parsed.GetValue(infoOpt);
                DeployRuntimeCommand.Run(folder, info);
            });
            rootCommand.Subcommands.Add(runtimeDeployCommand);
        }


        { // Runtimes Command
            Command runtimesCommand = new("runtimes", "List available runtimes");
            runtimesCommand.SetAction(parsed => {
                ManageRuntimesCommand.Run();
            });
            rootCommand.Subcommands.Add(runtimesCommand);
        }

        Console.CursorVisible = false;
        rootCommand.Parse(args).Invoke();
        Console.CursorVisible = true;

        return 0;
    }


    static DirectoryInfo DirectoryArgumentValidation(ArgumentResult result) {
        var folder = new DirectoryInfo(result.Tokens.Single().Value);
        if (!folder.Exists) {
            result.AddError($"Folder '{result.Tokens.Single().Value}' does not exist");
        }
        return folder;
    }
}
