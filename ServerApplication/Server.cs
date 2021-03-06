﻿using System;
using System.Xml;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace ServerApplication
{
    class Server
    {
        
        private static int minRange;
        private static int maxRange;
        private static Random random = new Random();

        private static UdpClient udpClient;
        private static IPAddress serverAddress;
        private static IPAddress localAddress;
        private static IPEndPoint endPoint;
        private static int port;

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
            localAddress = IPAddress.Any;
        }
        public static void ConfigureSocket()
        {
            udpClient = new UdpClient();
            endPoint = new IPEndPoint(serverAddress, port);
            //Configure socket
            udpClient.Client.SetIPProtectionLevel(IPProtectionLevel.Restricted);
            udpClient.Client.SetSocketOption(SocketOptionLevel.Udp, SocketOptionName.NoDelay, true);
            udpClient.Client.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastTimeToLive, 1);
            udpClient.Client.SendBufferSize = 1024;
            udpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.SendTimeout, 0);
            udpClient.Client.SetSocketOption(SocketOptionLevel.Udp, SocketOptionName.NoChecksum, true);
            udpClient.JoinMulticastGroup(serverAddress,localAddress);
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
                udpClient.Send(buffer, buffer.Length, endPoint);
            }
        }
    }
}
