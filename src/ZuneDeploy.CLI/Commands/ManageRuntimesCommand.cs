using ZuneDeploy.XNA.Data;

namespace ZuneDeploy.CLI.Commands;

public static class ManageRuntimesCommand {
    public static void Run() {
        RuntimeCollection collection = new();
        Console.WriteLine($"Search Paths:\n - {String.Join("\n - ", collection.SearchPaths)}");

        foreach (RuntimeContainer container in collection.runtimes) {
            Console.WriteLine();
            Console.WriteLine(container.ToString());
        }
    }
}
