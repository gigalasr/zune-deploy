using System.Runtime.InteropServices;

namespace ZuneDeploy;

class Program
{
    static void Main(string[] args)
    {
        int result = MTP.OpenConnection(out IntPtr device);

        Console.WriteLine("Result = " + result);
        Console.WriteLine("Ptr = " + device);

        byte[] buffer = new byte[1264];

        while (true)
        {
            Thread.Sleep(200);
            int reuslt = MTP.PollData(device, buffer, buffer.Length, out int length);
            Console.WriteLine("Polling " + result + " len " + length);
        }

    }
}
