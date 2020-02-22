using System;
using System.IO.Ports;
using System.Net.Sockets;
using System.Threading;
using z80gdbserver.Gdb;
using z80gdbserver.Interfaces;

namespace GDBwraper
{
    class Program
    {
        
        static void Main(string[] args)
        {
            DebugProto debug = new DebugProto();
            bool detected = false;

            do
            {
                detected = debug.ping();
                if (detected)
                    Console.WriteLine("Target detected");
                else
                    Console.WriteLine("No device detected");

                Thread.Sleep(500);
            } while (!detected);
            GDBNetworkServer server = new GDBNetworkServer(debug, 3333);
            var waiter = server.StartServer();
            waiter.Wait();
            Console.Write("Waiting for a connection on port 3333...");
        }



    }
}
