using Hik.Communication.Scs.Communication.EndPoints.Tcp;
using Hik.Communication.ScsServices.Client;
using Hik.Communication.ScsServices.Service;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UDIMAS;

namespace UDINet
{
    class UDINet : UdimasExternalPlugin
    {
        public override string Name => "UDINet";
        protected override Dictionary<string, dynamic> PluginProperties => new Dictionary<string, dynamic> {
            { "OnNetwork", new Func<List<(string, string)>>(() => OnNetwork?.Values.Select(i => (i.Ip, i.Name)).ToList() ?? new List<(string, string)>()) }
        };
        internal static volatile Dictionary<string, Instance> OnNetwork;
        internal static List<(string ip, string name)> _onNetworkVanilla
        {
            get
            {
                return OnNetwork?.Values.Select(i => (i.Ip, i.Name)).ToList() ?? new List<(string, string)>();
            }
        }
        static UdpClient Server;
        private const string KEY = "hDc9vq:KyLp+8y^EO3hH";
        public System.Timers.Timer Discoverer;
        public Thread Listener;

        /// <summary>
        /// Gets whether UDINet listener is running
        /// </summary>
        private bool IsListenerRunning
        {
            get
            {
                return (Listener?.ThreadState) == System.Threading.ThreadState.Background;
            }
        }

        private const int DiscoveryPort = 10150;
        private const int RCIPort = 10151;
        IScsServiceApplication RCIServer;

        public override void Run()
        {
            Udimas.BootComplete += Udimas_BootComplete;
            CmdInterpreter.RegisterCommand(new TerminalCommand("udinet", Cmd));
            CmdInterpreter.RegisterCommand(new TerminalCommand("rci", Terminal, true));
        }

        public override void Stop()
        {
            StopService();
        }

        private void Udimas_BootComplete()
        {
            Udimas.Settings.udinet_enable = Settings.CheckValue<bool>(Udimas.Settings.udinet_enable, false);
            Udimas.Settings.udinet_name = Settings.CheckValue<string>(Udimas.Settings.udinet_name, "UDIMAS");

            if (Udimas.Settings.udinet_enable)
            {
                StartService();
            }
        }

        private (int, string) Cmd(InterpreterIOPipeline tw, string[] a)
        {
            if (CmdInterpreter.IsWellFormatterArguments(a) || // no arguments or -i flag
                CmdInterpreter.IsWellFormatterArguments(a, "-i|--info"))
            {
                tw.WriteLine("UDINet status:  \t" + (IsListenerRunning ? "Enabled" : "Disabled"));
                tw.WriteLine("Other instances:\t" + (OnNetwork?.Count??0));
                tw.WriteLine("Ip addresses:");
                var ips = GetLocalIPv4(
                    NetworkInterfaceType.Ethernet,
                    NetworkInterfaceType.Wireless80211);
                foreach (LocalAdapterAddress laa in ips)
                {
                    tw.WriteLine($"\t{laa.Ip} ({laa.Name})");
                }
            }
            else if (CmdInterpreter.IsWellFormatterArguments(a, "-l|--list"))
            {
                OnNetwork.Values.ToList().ForEach(x => tw.WriteLine($"[ {x.Ip} ] {x.Name}"));
            }
            else if (CmdInterpreter.IsWellFormatterArguments(a, "-S|--stop"))
            {
                if (IsListenerRunning)
                {
                    StopService();
                    tw.WriteLine("UDINet stopped");
                }
                else
                {
                    tw.WriteLine("UDINet is not running");
                }
            }
            else if (CmdInterpreter.IsWellFormatterArguments(a, "-s|--start"))
            {
                if (!IsListenerRunning)
                {
                    StartService();
                    tw.WriteLine("UDINet started");
                }
                else
                {
                    tw.WriteLine("UDINet is already running");
                }
            }
            else if (CmdInterpreter.IsWellFormatterArguments(a, "-h|--help"))
            {
                tw.WriteLine("Controls and views statistics of the UDINet system.");
                tw.WriteLine("Usage:");
                tw.WriteLine(" -i/--info\tshow network information");
                tw.WriteLine(" -l/--list\tlist other instances on local network");
                tw.WriteLine(" -S/--stop\tstop UDINet");
                tw.WriteLine(" -s/--start\tstart UDINet");
                tw.WriteLine(" -t/--terminal\topen a terminal");
                tw.WriteLine(" -h/--help\tshow help page");
            }
            else
            {
                return (CmdInterpreter.INVALIDARGUMENTS, "-h or --help for help");
            }
            return (CmdInterpreter.SUCCESS, "");
        }

        /// <summary>
        /// The RCI terminal
        /// </summary>
        /// <returns></returns>
        private (int, string) Terminal(InterpreterIOPipeline tw, string[] a)
        {
            if (CmdInterpreter.IsWellFormatterArguments(a, "[A-Za-z0-9.]+"))
            {
                string ip = a[0];
                IScsServiceClient<IUdinetServerService> server;
                tw.WriteLine($"Connecting to {ip}..");
                try
                {
                    server = ScsServiceClientBuilder.CreateClient<IUdinetServerService>(new ScsTcpEndPoint(ip, RCIPort), new UdinetClientInstance());
                    server.Timeout = -1;
                    server.ConnectTimeout = 1000;
                    server.Connect();
                }
                catch (Exception e)
                {
                    tw.WriteLine("Could not connect: " + e.Message);
                    return (0, "");
                }
                tw.WriteLine("Connected. Write '!' to disconnect");
                tw.WriteLine();

                while (true)
                {
                    tw.Write($"-rci@{ip}->");
                    string data = Console.ReadLine();
                    tw.WriteLine();
                    string cmd = data.Split(' ')[0];
                    if (cmd == "!") break;
                    string[] args = data.Split(' ').Skip(1).ToArray();

                    try
                    {
                        var r = server.ServiceProxy.Execute(cmd, args);
                    }
                    catch (Hik.Communication.Scs.Communication.CommunicationException)
                    {
                        tw.WriteLine("Disconnected.");
                        return (0, "");
                    }
                    catch (Exception e)
                    {
                        tw.WriteLine("Error in rci: " + e.Message);
                        break;
                    }
                }

                tw.WriteLine("Disconnecting..");
                server.Disconnect();
            }
            else if (CmdInterpreter.IsWellFormatterArguments(a.Take(3).ToArray(), "-c|--command", "[A-Za-z0-9.]+", "\\w+"))
            {
                string ip = a[1];
                IScsServiceClient<IUdinetServerService> server;
                try
                {
                    server = ScsServiceClientBuilder.CreateClient<IUdinetServerService>(new ScsTcpEndPoint(ip, RCIPort), new UdinetClientInstance());
                    server.Timeout = -1;
                    server.ConnectTimeout = 1000;
                    server.Connect();
                }
                catch (Exception e)
                {
                    tw.WriteLine("Could not connect: " + e.Message);
                    return (0, "");
                }
                string data = string.Join(" ", a.Skip(2));
                string cmd = data.Split(' ')[0];
                string[] args = data.Split(' ').Skip(1).ToArray();

                try
                {
                    var r = server.ServiceProxy.Execute(cmd, args);
                }
                catch (Hik.Communication.Scs.Communication.CommunicationException)
                {
                    tw.WriteLine("Disconnected.");
                    return (0, "");
                }
                catch (Exception e)
                {
                    tw.WriteLine("Error in rci: " + e.Message);
                }
                server.Disconnect();
            }
            else if (CmdInterpreter.IsWellFormatterArguments(a, "-h|--help"))
            {
                tw.WriteLine("Executes commands in other UDIMAS instance. \nRemote instance must have UDINet installed.");
                tw.WriteLine("Usage:");
                tw.WriteLine(" rci [ip]   Connects to a remote instance and opens an active terminal to it");
                tw.WriteLine(" rci [-c|--command] [ip] [cmd]\n\t    Connects to a remote instance and executes a command in it");
            }
            else
            {
                return (1, "rci [ip]");
            }
            return (0, "");
        }

        private void StartService()
        {
            Log(LogLevel.INFO, "Starting UDINet..");
            OnNetwork = new Dictionary<string, Instance>();
            try
            {
                Log(LogLevel.DEBUG, "Starting network discovery");
                Discoverer = new System.Timers.Timer(3000);
                Discoverer.Elapsed += (s, a) => Discover();
                Listener = new Thread(Listen)
                {
                    IsBackground = true
                };
                Server = new UdpClient(DiscoveryPort);
            }

            catch (SocketException e)
            {
                Log(LogLevel.ERROR, "UDINet network discovery has been stopped. Reason: " + e.Message);
                Server = null;
                return;
            }

            try
            {
                Log(LogLevel.DEBUG, "Starting RCI");
                RCIServer = ScsServiceBuilder.CreateService(new ScsTcpEndPoint(RCIPort));
                RCIServer.AddService<IUdinetServerService, UdinetServerInstance>(new UdinetServerInstance());
                RCIServer.ClientConnected += (s, e) => {
                    Log(LogLevel.DEBUG, $"RCI - Instance connected for terminal");
                };
                RCIServer.ClientDisconnected+= (s, e) => {
                    Log(LogLevel.DEBUG, $"RCI - Instance disconnected from terminal");
                };
                RCIServer.Start();
            }

            catch (SocketException e)
            {
                Log(LogLevel.ERROR, "UDINet RCI service has been stopped. Reason: " + e.Message);
                Server = null;
                return;
            }

            Log(LogLevel.INFO, "Succesfully started UDINet");

            //start first-time discover
            BeginDiscover();

            Listener.Start();
        }

        private void StopService()
        {
            if (!IsListenerRunning) return;
            Log(LogLevel.INFO, "Stopping UDINet..");
            Discoverer?.Stop();
            Listener?.Abort();
            Server?.Client?.Close();
            Server?.Close();
            RCIServer?.Stop();
            RCIServer?.RemoveService<IUdinetServerService>();
            RCIServer = null;
            OnNetwork?.Clear();
            //null conditional member access EVERYWHERE!
            Log(LogLevel.INFO, "UDINet stopped.");
        }

        public void BeginDiscover()
        {
            new Task(Discover).Start();
        }

        public void Discover()
        {
            Discoverer?.Stop();
            var Client = new UdpClient();
            var RequestData = Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(new { key = KEY, name = Udimas.Settings.udinet_name }));

            Client.EnableBroadcast = true;
            Client.Send(RequestData, RequestData.Length, new IPEndPoint(IPAddress.Broadcast, DiscoveryPort));

            Dictionary<string, Instance> OnNetwork_tmp = new Dictionary<string, Instance>();

            Stopwatch s = new Stopwatch();
            s.Start();
            while (s.Elapsed < TimeSpan.FromSeconds(3))
            {
                var ServerEp = new IPEndPoint(IPAddress.Any, 0);

                Udimas.TryExecute(() => Client.Receive(ref ServerEp), 3000, out var ServerResponseData);
                if (ServerResponseData == null) break;
                var ServerResponse = Encoding.ASCII.GetString(ServerResponseData);

                (string key, string name) resObj = ParseJSON(ServerResponse);
                if ((resObj.key == null || resObj.key != KEY) || resObj.name == null)
                    continue;
                else
                {
                    //success
                    string ip = ServerEp.Address.ToString();

                    if (!OnNetwork.ContainsKey(ip))
                    {
                        Log(LogLevel.INFO, $"Found a new instance: {ip}, {resObj.name}");
                    }

                    OnNetwork_tmp.Add(ip, new Instance());
                    OnNetwork_tmp[ip].Ip = ip;
                    OnNetwork_tmp[ip].Name = resObj.name;
                }
            }

            OnNetwork = OnNetwork_tmp;

            s.Stop();
            Client.Close();
            Discoverer?.Start();
        }

        public void Listen()
        {
            while (true)
            {
                var ClientEp = new IPEndPoint(IPAddress.Any, 0);
                var ClientRequestData = Server.Receive(ref ClientEp);
                var ClientRequest = Encoding.ASCII.GetString(ClientRequestData);
                var ResponseData = Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(new { key = KEY, name = Udimas.Settings.udinet_name }));

                if (GetLocalIPv4(NetworkInterfaceType.Ethernet,
                    NetworkInterfaceType.Wireless80211)
                    .Any(x => ClientEp.Address.ToString() == x.Ip))
                    continue;

                (string key, string name) resObj = ParseJSON(ClientRequest);
                if ((resObj.key == null || resObj.key != KEY) || resObj.name == null)
                    continue;
                else
                {
                    string ip = ClientEp.Address.ToString();

                    Server.Send(ResponseData, ResponseData.Length, ClientEp);

                    if (!OnNetwork.ContainsKey(ip))
                    {
                        OnNetwork.Add(ip, new Instance());
                        Log(LogLevel.INFO, $"Found a new instance: {ip}, {resObj.name}");
                    }
                    OnNetwork[ip].Name = resObj.name;
                }
            }
        }

        //gets all local network interface addresses
        private static List<LocalAdapterAddress> GetLocalIPv4(params NetworkInterfaceType[] _types)
        {
            List<LocalAdapterAddress> output = new List<LocalAdapterAddress>();
            foreach (NetworkInterface item in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (_types.Contains(item.NetworkInterfaceType) && item.OperationalStatus == OperationalStatus.Up)
                {
                    foreach (UnicastIPAddressInformation ip in item.GetIPProperties().UnicastAddresses)
                    {
                        if (ip.Address.AddressFamily == AddressFamily.InterNetwork)
                        {
                            output.Add(new LocalAdapterAddress { Ip = ip.Address.ToString(), Name = item.Description });
                        }
                    }
                }
            }
            return output;
        }

        //could be done with good new' tuples but whatever
        private struct LocalAdapterAddress
        {
            public string Ip;
            public string Name;
            public override string ToString()
            {
                return Ip;
            }
        }

        private static (string, string) ParseJSON(string data)
        {
            dynamic obj;
            try
            {
                obj = JsonConvert.DeserializeAnonymousType(data, new { key="", name="" });

                if (obj == null || obj.key == "" || obj.name == "")
                    return (null, null);
            }
            catch
            {
                return (null, null);
            }

            return (obj.key, obj.name);
        }

        //a helper method for checking if a UDIMAS setting is there
        private static bool IsPropertyExist(dynamic settings, string name)
        {
            if (settings is ExpandoObject)
                return ((IDictionary<string, object>)settings).ContainsKey(name);

            return settings.GetType().GetProperty(name) != null;
        }
    }
}
