using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Xml;
using System.Runtime.Remoting.Metadata.W3cXsd2001;

namespace ClientApplication
{
    class Client
    {
        private static float latency; // in ms

        private static float average = 0.0F;
        private static float dispersion = 0.0F;
        private static float mode = 0.0F;
        private static float median = 0.0F;
        private static float M2 = 0.0F;
        private static float eta = 0.005F;
        private static ulong n = 0;
        private static int stockCost = 0;
        private static int countSent = 0; // buffer number sent from server
        private static int countReceived = 0; // buffer number received


        
        private static UdpClient udpClient;
        private static Socket socket;
        private static int port;
        private static MulticastOption multicastOption;

        private static IPAddress serverAddress;
        private static IPAddress localAddress;
        private static IPEndPoint localEndPoint;
        private static IPEndPoint remoteEndPoint;
        private static IPEndPoint endPoint;

        private static Dictionary<int, int> values;

        private static string configUri = Directory.GetCurrentDirectory() + "./../../clientConfigs/config.xml";


        public static void LoadConfiguration()
        {
            XmlDocument xmldoc = new XmlDocument();
            xmldoc.Load(configUri);

            XmlNode latencySettings = xmldoc.SelectSingleNode("configuration/latencySettings");
            latency = Convert.ToSingle(latencySettings.SelectSingleNode("latency").InnerText);

            XmlNode multiCastSettings = xmldoc.SelectSingleNode("configuration/multiCastSettings");
            serverAddress = IPAddress.Parse(multiCastSettings.SelectSingleNode("serverAddress").InnerText);
            localAddress = IPAddress.Any;
            port = Convert.ToInt32(multiCastSettings.SelectSingleNode("port").InnerText);
        }
        public static void ConfigureSocket()
        {
            remoteEndPoint = new IPEndPoint(serverAddress, port);
            localEndPoint = new IPEndPoint(localAddress, port);

            // Create and configure UdpClient
            
            udpClient = new UdpClient();
            udpClient.ExclusiveAddressUse = false;
            // Bind, Join
            udpClient.Client.Bind(localEndPoint);
            udpClient.JoinMulticastGroup(serverAddress);

            //Configure socket
            udpClient.Ttl = 1;
            udpClient.Client.SetIPProtectionLevel(IPProtectionLevel.Restricted);
            udpClient.Client.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastTimeToLive, 1);
            udpClient.Client.SetSocketOption(SocketOptionLevel.Udp, SocketOptionName.NoChecksum, true);
            udpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            udpClient.Client.ReceiveBufferSize = 1024;
            //udpClient.Client.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.AddMembership, new MulticastOption(serverAddress));
            /*
            socket = new Socket(AddressFamily.InterNetwork,
                                         SocketType.Dgram,
                                         ProtocolType.Udp);
            endPoint = new IPEndPoint(serverAddress, port);
            //Configure socket
            socket.SetIPProtectionLevel(IPProtectionLevel.Restricted);
            socket.SetSocketOption(SocketOptionLevel.Udp, SocketOptionName.NoDelay, true);
            socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastTimeToLive, 2);
            socket.ReceiveBufferSize = 1024;
            socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.SendTimeout, 0);
            socket.SetSocketOption(SocketOptionLevel.Udp, SocketOptionName.NoChecksum, true);
            //Configure multicast group
            multicastOption = new MulticastOption(serverAddress, localAddress);
            socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.AddMembership, multicastOption);*/
        }
        static float calcAverage(int nextValue)
        {
            return average + (nextValue - average) / n;
        }
        static float calcMedian(int nextValue)
        {
            return median + eta * Math.Sign(nextValue - median);
        }
        static float calcDispersion(int nextValue, float prevAverage) // WellFord's algorithm
        {
            M2 += (nextValue - prevAverage) * (nextValue - average);
            return M2 / n;
        }
        static void calcStatistics(int nextValue)
        {
            n += 1;
            float prevAverage = average;
            average = calcAverage(nextValue);
            dispersion = calcDispersion(nextValue, prevAverage);
            median = calcMedian(nextValue);
        }
        static void Receive()
        {
            Console.WriteLine("Receiving started ");
            byte[] data = new byte[1024];
            while (true)
            {
                //IPEndPoint ipEndPoint = new IPEndPoint(IPAddress.Any, 0);
                data = udpClient.Receive(ref localEndPoint);//,0,data.Length,SocketFlags.Multicast);
                int[] buf = new int[1];
                Buffer.BlockCopy(data, 0, buf, 0, sizeof(int));
                stockCost = buf[0];
                Buffer.BlockCopy(data, sizeof(int), buf, 0, sizeof(int));
                countSent = buf[0];
                countReceived += 1;
                values[stockCost] += 1;
                Console.WriteLine("GET {0} {1} {2} {3}%", stockCost, countSent, countReceived, 100*(countReceived/countSent));
            }
        }
        static void Main(string[] args)
        {
            LoadConfiguration();
            ConfigureSocket();
            Thread calculate = new Thread(Receive);
            calculate.Start();
            while (true)
            {
                calcStatistics(stockCost);
            }
        }
    }
}
