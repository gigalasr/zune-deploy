using ZuneDeploy.Transport;
using NativeGen;
using ZuneDeploy.XNA;
using ZuneDeploy.XNA.Channels;

namespace ZuneDeploy.Core;

class Program {
    //private static Client? _client;

    static void Main(string[] args) {
        //  Console.CancelKeyPress += OnExit;


        Zune zune = new Zune();
        var guid = new Guid("d3dae1f0-4404-43c9-825c-b785ec9fe5d4");
        var name = "Dragon";
        // using (var chan = zune.OpenXNAGameDeployChannel()) {

        //     //var folder = "/home/lars/cloud/archive/Zune/Doom [HD] v4.0 [Deploy Kit]/data";
        //     var folder = "/home/lars/cloud/archive/Zune/Dragon Model (Cel Shading) [Deploy Kit]/data";

        //     // 1. Open Container
        //     chan.OpenGameContainer(guid, name);

        //     // 2. Deploy Content
        //     foreach (string file in Directory.EnumerateFiles(folder, "*.*", SearchOption.AllDirectories)) {
        //         using (FileStream fs = File.OpenRead(file)) {
        //             string pathInContainer = file.Substring(folder.Length + 1);
        //             pathInContainer = pathInContainer.Replace('/', '\\');
        //             Console.WriteLine($"Uploading File: {pathInContainer}");
        //             chan.PutFileInContainer(pathInContainer, fs);
        //         }
        //     }

        //     // 3. Set Thumbnail

        //     // 4. Set Metadata
        //     chan.PutGamePropertiesEx(guid, name, "Testing the deploy", "exploiter.exe", "Zune.v4.0.Beta");

        //     // 5. Close Game Container
        //     chan.CloseGameContainer();
        // }


        //var chan1 = zune.OpenXNARuntimeDeployChannel();
        using (var chan2 = zune.OpenXnaLaunchChannel()) {
            //  Console.WriteLine(chan2.Launch(guid, "", ""));
            chan2.LaunchInMode(guid, "", true);

            (string? rname, string? rexec) = chan2.GetRunningTitleInfo();
            Console.WriteLine($"Running Title: {rname} - {rexec}");
            //    Console.WriteLine($"Available Services {chan2.EnumerateAvailableServices()}");
            Console.WriteLine($"IsTitleRunnning() -> {chan2.IsTitleRunning()}");
        }
    }

    private static void OnExit(object? sender, ConsoleCancelEventArgs e) {
        // if (_client != null) {
        //     _client.Close();
        //     _client = null;
        // }
    }
}
