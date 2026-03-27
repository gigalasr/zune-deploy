using System.Runtime.InteropServices;

namespace zune_deploy;

class Program
{
    static void Main(string[] args)
    {
        int path = Native.TestMethod();
        Console.WriteLine("Hello, World!");
        Console.WriteLine("Path is: " + path);
    }
}
