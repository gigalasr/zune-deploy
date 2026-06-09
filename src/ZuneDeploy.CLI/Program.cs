using System.CommandLine;
using System.CommandLine.Parsing;
using NativeGen;
using ZuneDeploy.CLI.Verbs;
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
                launchOpt, infoOpt, pathArg
            };
            deployCommand.SetAction((parsed) => {
                bool launch = parsed.GetValue(launchOpt);
                bool info = parsed.GetValue(infoOpt);
                DirectoryInfo folder = parsed.GetValue(pathArg)!;
                DeployVerb.DeployCommand(folder, launch, info);
            });
            rootCommand.Subcommands.Add(deployCommand);
        }

        Console.CursorVisible = false;
        rootCommand.Parse(args).Invoke();
        Console.CursorVisible = true;

        return 0;
    }
}
