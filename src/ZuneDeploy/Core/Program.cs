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
        using (var chan = zune.OpenXNADeployChannel()) {

            var guid = Guid.NewGuid();
            var name = "Test Game";
            var folder = "/home/lars/cloud/archive/Zune/Doom [HD] v4.0 [Deploy Kit]/data";

            // 1. Open Container
            chan.OpenGameContainer(guid, name);

            // 2. Deploy Content
            foreach (string file in Directory.EnumerateFiles(folder, "*.*", SearchOption.AllDirectories)) {
                using (FileStream fs = File.OpenRead(file)) {
                    string pathInContainer = file.Substring(folder.Length + 1);
                    pathInContainer = pathInContainer.Replace('/', '\\');
                    Console.WriteLine($"Uploading File: {pathInContainer}");
                    chan.PutFileInContainer(pathInContainer, fs);
                }
            }

            // 3. Set Thumbnail

            // 4. Set Metadata
            chan.PutGamePropertiesEx(guid, name, "Testing the deploy", "Doom.exe", "Zune.v4.0.Beta");

            // 5. Close Game Container
            chan.CloseGameContainer();
        }




    }

    private static void OnExit(object? sender, ConsoleCancelEventArgs e) {
        // if (_client != null) {
        //     _client.Close();
        //     _client = null;
        // }
    }
}
