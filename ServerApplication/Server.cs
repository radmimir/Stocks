using System;
using System.Xml;
using System.IO;
using System.Net;
using System.Net.Sockets;

namespace ServerApplication
{
    class Server
    {
        
        private static int minRange;
        private static int maxRange;
        private static Random random = new Random();

        private static IPAddress serverAddress;
        private static IPEndPoint endPoint;
        private static int port;
        private static Socket socket;
        private static MulticastOption multicastOption;

        private static string configUri = Directory.GetCurrentDirectory() + "./../../serverConfigs/config.xml";


        public static void LoadConfiguration()
        {
            XmlDocument xmldoc = new XmlDocument();
            xmldoc.Load(configUri);

            XmlNode randomSettings = xmldoc.SelectSingleNode("configuration/randomSettings");
            minRange = Convert.ToInt32(randomSettings.SelectSingleNode("minRange").InnerText);
            maxRange = Convert.ToInt32(randomSettings.SelectSingleNode("maxRange").InnerText);

            XmlNode multiCastSettings = xmldoc.SelectSingleNode("configuration/multiCastSettings");
            serverAddress = IPAddress.Parse(multiCastSettings.SelectSingleNode("serverAddress").InnerText);
            port = Convert.ToInt32(multiCastSettings.SelectSingleNode("port").InnerText);
        }
        public static void ConfigureSocket()
        {
            socket = new Socket(AddressFamily.InterNetwork,
                                     SocketType.Dgram,
                                     ProtocolType.Udp);
            endPoint = new IPEndPoint(serverAddress, port);
            //Configure socket
            socket.SetIPProtectionLevel(IPProtectionLevel.Restricted);
            socket.SetSocketOption(SocketOptionLevel.Udp, SocketOptionName.NoDelay, true);
            socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastTimeToLive, 1);
            socket.SendBufferSize = 1024;
            socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.SendTimeout, 0);
            socket.SetSocketOption(SocketOptionLevel.Udp, SocketOptionName.NoChecksum, true);
            //Configure multicast group
            multicastOption = new MulticastOption(serverAddress);
            socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.AddMembership, multicastOption);
        }
        static int GetStockCost()
        {
            return random.Next(minRange,maxRange);
        }
        static void Main(string[] args)
        {
            int stockCost;
            LoadConfiguration();
            ConfigureSocket();
            Console.WriteLine("Server succesfully started");
            Console.WriteLine("Press CTRL + C to exit ");
            byte[] buffer = new byte[1024];
            byte[] stock, count_buf;
            int count = 0;
            while (true)
            {
                stockCost = GetStockCost();
                count += 1;
                stock = BitConverter.GetBytes(stockCost);
                count_buf = BitConverter.GetBytes(count);
                Buffer.BlockCopy(stock, 0, buffer, 0, sizeof(int));
                Buffer.BlockCopy(count_buf, 0, buffer, sizeof(int), sizeof(int));
                socket.SendTo(buffer,SocketFlags.None,endPoint);
            }
        }
    }
}
