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
        private static int latency; // in ms

        private static float average = 0.0F;
        private static float deviation = 0.0F;
        private static float mode = 0.0F;
        private static float median = 0.0F;
        private static float eta = 0.0F;
        private static float M2 = 0.0F;
        private static int n = 0;
        private static int stockCost = 0;
        private static int countSent = 0; // buffer number sent from server
        private static int countReceived = 0; // buffer number received
        private static int packetLost = 0;
        private static Object locker = new Object();


        
        private static UdpClient udpClient;
        private static int port;

        private static IPAddress serverAddress;
        private static IPAddress localAddress;
        private static IPEndPoint localEndPoint;

        private static Dictionary<int, int> values = new Dictionary<int, int>();

        private static string configUri = Directory.GetCurrentDirectory() + "./../../clientConfigs/config.xml";


        public static void LoadConfiguration()
        {
            XmlDocument xmldoc = new XmlDocument();
            xmldoc.Load(configUri);

            XmlNode latencySettings = xmldoc.SelectSingleNode("configuration/latencySettings");
            latency = Convert.ToInt32(latencySettings.SelectSingleNode("latency").InnerText);

            XmlNode multiCastSettings = xmldoc.SelectSingleNode("configuration/multiCastSettings");
            serverAddress = IPAddress.Parse(multiCastSettings.SelectSingleNode("serverAddress").InnerText);
            localAddress = IPAddress.Any;
            port = Convert.ToInt32(multiCastSettings.SelectSingleNode("port").InnerText);
        }
        public static void ConfigureSocket()
        {
            localEndPoint = new IPEndPoint(localAddress, port);

            // Create and configure UdpClient

            udpClient = new UdpClient();
            udpClient.ExclusiveAddressUse = false;
            // Bind, Join
            udpClient.Client.Bind(localEndPoint);
            udpClient.JoinMulticastGroup(serverAddress);

            //Configure socket
            udpClient.Client.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastTimeToLive, 1);
            udpClient.Client.SetSocketOption(SocketOptionLevel.Udp, SocketOptionName.NoChecksum, true);
            udpClient.Client.SetSocketOption(SocketOptionLevel.Udp, SocketOptionName.NoDelay, true);
            udpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            udpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveTimeout, 0);
            udpClient.Client.ReceiveBufferSize = 1024;
        }
        static float calcAverage(int nextValue)
        {
            return average + (nextValue - average) / Convert.ToSingle(n);
        }
        static float calcMedian(int nextValue)
        {
            int[] l = values.Keys.ToArray<int>();
            if (l.Length % 2 == 1)
                return l[l.Length / 2];
            else
                return 0.5f * (l[l.Length / 2 - 1] + l[l.Length / 2]);
        }
        static float calcDeviation(int nextValue, float prevAverage) // WellFord's algorithm
        {
            M2 += (nextValue - prevAverage) * (nextValue - average);
            return M2 / Convert.ToSingle(n);
        }
        static float calcMode(Dictionary<int,int> dictValues)
        {
            return dictValues.Aggregate((x, y) => x.Value > y.Value ? x : y).Key;
        }
        static void calcStatistics()
        {
            n += 1;
            int nextValue = stockCost;
            float prevAverage = average;
            average = calcAverage(nextValue);
            deviation = calcDeviation(nextValue, prevAverage);
            median = calcMedian(nextValue);
            lock (locker)
            {
                if (values.Count > 0)
                    mode = calcMode(values);
            }
        }
        static void Receive()
        {
            byte[] data = new byte[1024];
            while (true)
            {
                data = udpClient.Receive(ref localEndPoint);
                int[] buf = new int[1];
                Buffer.BlockCopy(data, 0, buf, 0, sizeof(int));
                stockCost = buf[0];
                Buffer.BlockCopy(data, sizeof(int), buf, 0, sizeof(int));
                countSent = buf[0];
                countReceived += 1;
                lock (locker)
                {
                    if (values.ContainsKey(stockCost))
                        values[stockCost] += 1;
                    else
                        values[stockCost] = 1;
                }
                packetLost = countSent - countReceived;
                Thread calc = new Thread(calcStatistics);
                calc.Start();
                Thread.Sleep(latency);
            }
        }
        static void Print()
        {
            while (true)
            {
                Console.ReadLine();
                Console.Write("Mean: {0} Deviation: {1} Median: {2} Mode: {3} Packets lost: {4} ",
                    average, deviation, median, mode, packetLost);
            }
        }
        static void Main(string[] args)
        {
            LoadConfiguration();
            ConfigureSocket();
            Thread receive = new Thread(Receive);
            receive.Start();
            Thread print = new Thread(Print);
            print.Start();
            Console.WriteLine("Client successfully started. Press Enter to print statistics ");
        }
    }
}
