using Hik.Communication.Scs.Communication.EndPoints.Tcp;
using Hik.Communication.ScsServices.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UDINet
{
    static class StandAlone
    {
        private static void Main(string[] args)
        {
            Console.WriteLine(
                "UDIMAS UDINet Remote Control Interpreter Standalone |\n" +
                "----------------------------------------------------+\n");

            string ip;
            if (args.Length < 1)
            {
                Console.WriteLine("No address specified, connecting to localhost.");
                ip = "localhost";
            }
            else { ip = args[0]; }

            IScsServiceClient<IUdinetServerService> server;
            Console.WriteLine($"Connecting to {ip}..");
            try
            {
                server = ScsServiceClientBuilder.CreateClient<IUdinetServerService>(new ScsTcpEndPoint(ip, 10151), new UdinetClientInstance());
                server.Timeout = -1;
                server.ConnectTimeout = 1000;
                server.Connect();
            }
            catch (Exception e)
            {
                Console.WriteLine("Could not connect: " + e.Message);
                AnyKey();
                return;
            }
            Console.WriteLine("Connected. Write '!' to disconnect");
            Console.WriteLine();

            while (true)
            {
                Console.Write($"-@{ip}->");
                string data = Console.ReadLine();
                Console.WriteLine();
                string cmd = data.Split(' ')[0];
                if (cmd == "!") break;
                string[] a = data.Split(' ').Skip(1).ToArray();

                try
                {
                    server.ServiceProxy.Execute(cmd, a);
                }
                catch (Hik.Communication.Scs.Communication.CommunicationException)
                {
                    Console.WriteLine("Disconnected.");
                    AnyKey();
                    return;
                }
                catch (Exception e)
                {
                    Console.WriteLine("Error: " + e.Message);
                    AnyKey();
                    break;
                }
            }

            Console.WriteLine("Disconnecting..");
            server.Disconnect();
            Console.Write("Disconnected.");
        }
        private static void AnyKey()
        {
            Console.Write("Any key to continue.");
            Console.ReadKey(true);
        }
    }
}
