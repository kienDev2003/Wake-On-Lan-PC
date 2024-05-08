using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot.Args;
using Telegram.Bot;
using System.Net;
using System.Net.Sockets;
using System.Configuration;
using System.Net.Mail;

namespace ConsoleApp1
{
    internal class Program
    {
        static TelegramBotClient botClient;

        static string token_Bot = ConfigurationManager.AppSettings["token_Bot"].ToString();
        static string macAddress = ConfigurationManager.AppSettings["macAddress"].ToString();
        static string domain = ConfigurationManager.AppSettings["domain"].ToString();
        static async Task Main(string[] args)
        {
            // Chuyển đổi tên miền thành địa chỉ IP
            string ipAddressStr = await GetIPAddressFromDomain(domain);
            domain = ipAddressStr;


            botClient = new TelegramBotClient(token_Bot);
            botClient.StartReceiving();
            botClient.OnMessage += Bot_OnMessage;

            Console.ReadLine();

            Console.WriteLine($"Đã gửi gói tin Wake-on-LAN đến địa chỉ MAC: {macAddress} tại địa chỉ IP: {ipAddressStr}");
        }

        private static void Bot_OnMessage(object sender, MessageEventArgs e)
        {
            string mess = e.Message.Text;
            string chatID = e.Message.Chat.Id.ToString();

            try
            {
                Console.WriteLine(mess + "\n");

                if (mess == "ON")
                {
                    // Gửi gói tin WOL
                    SendWOL(macAddress, domain);
                    botClient.SendTextMessageAsync(chatID, $"Đã gửi gói tin Wake-on-LAN đến địa chỉ {domain}");
                }
                else
                {
                    botClient.SendTextMessageAsync(chatID, "Sai cú pháp !");
                }
            }
            catch( Exception ex)
            {
                botClient.SendTextMessageAsync(chatID, "Sai cú pháp !");
            }
            
        }

        static async Task<string> GetIPAddressFromDomain(string domain)
        {
            IPAddress[] addresses = await Dns.GetHostAddressesAsync(domain);
            return addresses.FirstOrDefault(a => a.AddressFamily == AddressFamily.InterNetwork)?.ToString();
        }

        static void SendWOL(string macAddress, string ipAddressStr)
        {
            byte[] macBytes = macAddress.Split('-').Select(s => Convert.ToByte(s, 16)).ToArray();
            byte[] wolPacket = new byte[102];

            for (int i = 0; i < 6; i++)
            {
                wolPacket[i] = 0xFF;
            }

            for (int i = 6; i < 102; i += 6)
            {
                macBytes.CopyTo(wolPacket, i);
            }

            IPAddress ipAddress = IPAddress.Parse(ipAddressStr);
            IPEndPoint endPoint = new IPEndPoint(ipAddress, 9);

            using (UdpClient client = new UdpClient())
            {
                client.Send(wolPacket, wolPacket.Length, endPoint);
            }
        }
    }
}
