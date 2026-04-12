using System.Runtime.InteropServices;
using ZuneDeploy.Native;
using NativeGen;
namespace ZuneDeploy.Main;

class Program
{
    static void Main(string[] args)
    {
        var result = (Result)MTP.OpenConnection(out IntPtr device);
        if(result != Result.Ok)
        {
            Console.WriteLine($"Could not connect to deivce: {result}");
            return;
        }


        Console.WriteLine("Result = " + result);
        Console.WriteLine("Ptr = " + device);

        byte[] buffer = new byte[1264];

        while (true)
        {
            Thread.Sleep(200);
            Result reuslt = (Result)MTP.PollData(device, buffer, buffer.Length, out int length);
            Console.WriteLine("Polling " + result + " len " + length);
        }

    }
}
