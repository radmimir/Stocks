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

namespace ClientApplication
{
    class Client
    {
        private static float latency; // in ms

        private static float average = 0.0F;
        private static float dispersion = 0.0F;
        private static float mode = 0.0F;
        private static float median = 0.0F;
        private static float eta = 0.005F;
        private static ulong n = 0;
        private static int stockCost = 0;
        private static int countSent = 0; // buffer number sent from server
        private static int countReceived = 0; // buffer number received



        private static UdpClient udpClient;
        private static int port;
        private static MulticastOption multicastOption;

        private static IPAddress multicastAddress;
        private static IPAddress localAddress;
        private static IPEndPoint localEndPoint;
        private static IPEndPoint remoteEndPoint;

        private static string configUri = Directory.GetCurrentDirectory() + "./../../clientConfigs/config.xml";


        public static void LoadConfiguration()
        {
            XmlDocument xmldoc = new XmlDocument();
            xmldoc.Load(configUri);

            XmlNode latencySettings = xmldoc.SelectSingleNode("configuration/latencySettings");
            latency = Convert.ToSingle(latencySettings.SelectSingleNode("latency").InnerText);

            XmlNode multiCastSettings = xmldoc.SelectSingleNode("configuration/multiCastSettings");
            multicastAddress = IPAddress.Parse(multiCastSettings.SelectSingleNode("multicastAddress").InnerText);
            localAddress = IPAddress.Any;
            port = Convert.ToInt32(multiCastSettings.SelectSingleNode("port").InnerText);
        }
        public static void ConfigureSocket()
        {
            remoteEndPoint = new IPEndPoint(multicastAddress, port);
            localEndPoint = new IPEndPoint(localAddress, port);

            // Create and configure UdpClient
            udpClient = new UdpClient();
            udpClient.ExclusiveAddressUse = false;
            udpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            udpClient.Client.ReceiveBufferSize = 1024;
            // Bind, Join
            udpClient.Client.Bind(localEndPoint);
            udpClient.JoinMulticastGroup(multicastAddress, localAddress);

            //Configure socket
            udpClient.Client.SetIPProtectionLevel(IPProtectionLevel.Restricted);
            udpClient.Client.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastTimeToLive, 1);
            udpClient.Client.SetSocketOption(SocketOptionLevel.Udp, SocketOptionName.NoChecksum, true);
            //Configure multicast group
            //multicastOption = new MulticastOption(multicastAddress,localAddress);
            //udpClient.Client.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.AddMembership, multicastOption);
        }
        static float calcAverage(float nextValue)
        {
            return average + (nextValue - average) / n;
        }
        static float calcMedian(float nextValue)
        {
            return median + eta * Math.Sign(nextValue - median);
        }
        static void calcStatistics()
        {
            average = calcAverage(1);
        }
        static void Receive()
        {
            while (true)
            {
                //IPEndPoint ipEndPoint = new IPEndPoint(IPAddress.Any, 0);
                byte[] data = udpClient.Receive(ref remoteEndPoint);
                int[] buf = new int[1];
                Buffer.BlockCopy(data, 0, buf, 0, sizeof(int));
                stockCost = buf[0];
                Buffer.BlockCopy(data, 0, buf, 0, sizeof(int));
                countSent = buf[0];
                countReceived += 1;
                Console.WriteLine("GET {0} {1} {2} {3}%", stockCost, countSent, countReceived, 100*(countReceived/countSent));
            }
        }
        static void Main(string[] args)
        {
            int stockCost;
            LoadConfiguration();
            ConfigureSocket();
            Thread calculate = new Thread(Receive);
            calcStatistics();
        }
    }
}
