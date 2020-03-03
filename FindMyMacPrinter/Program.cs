/*******************************************
 *  Program :   FindMyPrinter
 *  Desc    :   Locates IP of copiers on
 *              network
 * 
 *  @author :   Tony Brix, Austin Knudsen,
 *              Ryan Coffey
 * 
 * *****************************************/


using SnmpSharpNet;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Reflection;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FindMyMacPrinter
{
    public partial class DeviceDiscovery
    {
        public static int numBroadcasts = 0;
        static List<string> ipList = new List<string>();
        static List<string> nameList = new List<string>();

        public static void Main(string[] args)
        {
            DeviceDiscovery_Load();
        }

        public static void DeviceDiscovery_Load()
        {
            bStart_Click();
        }

        // Method broadcast references the IPEndPoint class - 
        // IPEndPoint contains host and local or remote port information needed by an application to connect to a service on a host.  
        // It does this by combining the host's IP address and port number of a service, the IPEndPoint class forms a connection point to a service.
        // link to msft docs - https://docs.microsoft.com/en-us/dotnet/api/system.net.ipendpoint?view=netframework-4.8
        private static void broadcast(object interfaceIP)
        {
            Spinner spin = new Spinner();
            spin.Start();

            object[] str;
            Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, true);
            socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveTimeout, 5000);
            NetworkInterface.GetAllNetworkInterfaces();
            socket.Bind(new IPEndPoint((IPAddress)interfaceIP, 161)); // 161 = snmp udp port
            IPEndPoint pEndPoint = new IPEndPoint(IPAddress.Broadcast, 161);
            SnmpV1Packet snmpV2Packet = new SnmpV1Packet("public");
            snmpV2Packet.Pdu.VbList.Add("1.3.6.1.2.1.1.1.0");
            socket.SendTo(snmpV2Packet.encode(), pEndPoint);
            IPEndPoint pEndPoint1 = new IPEndPoint(IPAddress.Any, 0);
            byte[] numArray = new byte[32768];
            DateTime now = DateTime.Now;
            while ((DateTime.Now - now).TotalSeconds < 20)
            {
                int num = 0;
                try
                {
                    EndPoint endPoint = pEndPoint1;

                    num = socket.ReceiveFrom(numArray, ref endPoint);

                    pEndPoint1 = (IPEndPoint)endPoint;
                }
                catch
                {
                    SocketException se = new SocketException();
                    num = -1;
                }
                if (num > 0)
                {
                    if ((pEndPoint1.Address.Equals(IPAddress.Broadcast) ? false : !pEndPoint1.Address.Equals((IPAddress)interfaceIP)))
                    {
                        try
                        {
                            snmpV2Packet.decode(numArray, num);
                            DeviceDiscovery.UpdateListViewCallback updateListViewCallback = new DeviceDiscovery.UpdateListViewCallback(UpdateListView);
                            if(snmpV2Packet.Pdu.VbList[0].Value.ToString() != "Null")
                            {
                                string truncatedName = trunc(snmpV2Packet.Pdu.VbList[0].Value.ToString(), 29);
                                ipList.Add(pEndPoint1.Address.ToString());
                                nameList.Add(truncatedName);

                            }

                        }
                        catch
                        {
                            continue;
                        }
                    }
                }
            }
            socket.Close();
            if (ipList.Count == 0)
            {
                Console.WriteLine("No devices found on network.");
            }
            else
            {
                spin.Stop();
                // clear current line
                int currLine = Console.CursorTop;
                Console.SetCursorPosition(0, Console.CursorTop);
                Console.Write(new string(' ', Console.WindowWidth));
                Console.SetCursorPosition(0, currLine);

                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.DarkMagenta;
                Console.WriteLine("{0,-33} {1,25}", "Name", "IP Address  ");
                Console.WriteLine("{0,-33} {1,25}", "---------------------------", "----------- ");

                for(int x=0; x < nameList.Count; x++)
                {
                    Console.ForegroundColor = ConsoleColor.DarkCyan;
                    Console.Write("{0,-33}", (x + 1) + ". " + nameList[x]);

                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("{0,25}", ipList[x]);

                }

                displayMenu();

            }
        }

        // menu
        private static void displayMenu()
        {
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Blue;
            int choice = 0;
            while(choice != 3)
            {
                Console.WriteLine("-----------------------------------------------------------");
                Console.WriteLine("1. Open web server");
                Console.WriteLine("2. Open scan folder location");
                Console.WriteLine("3. Quit program");
                Console.Write("Enter menu selection: ");
                if(int.TryParse(Console.ReadLine(), out choice))
                {
                    int copierChoice;
                    // clear menu otpions
                    for(int x=0; x<4; x++)
                    {
                        Console.SetCursorPosition(0, Console.CursorTop - 1);
                        Console.Write(new string(' ', Console.WindowWidth));
                        Console.SetCursorPosition(0, Console.CursorTop - 1);
                    }

                    if (choice == 1)
                    {
                        Console.Write("Enter the leading number of the copier to open in web: ");
                        if (int.TryParse(Console.ReadLine(), out copierChoice))
                        {
                            if(copierChoice < 1 || copierChoice > nameList.Count)
                            {
                                Console.WriteLine("That copier does not exist. Enter a number between 1 and " + nameList.Count);
                            }
                            else
                            {
                                Console.WriteLine("Opening webpage for " + nameList[copierChoice - 1] + ", IP: " + ipList[copierChoice - 1]);
                                Process.Start("http://" + ipList[copierChoice - 1]);
                            }
                        }
                        else
                        {
                            Console.WriteLine("You must enter a valid integer...");
                        }

                    }
                    else if(choice == 2)
                    {
                        Console.Write("Enter the leading number of the copier to open file share folder: ");
                        if(int.TryParse(Console.ReadLine(), out copierChoice))
                        {
                            string fileShare;
                            if (nameList[copierChoice - 1].StartsWith("TOSHIBA")) {
                                fileShare = string.Concat("smb://", ipList[copierChoice - 1], "/file_share");
                                Process.Start("open", fileShare);

                            }
                            else {
                                //fileShare = string.Concat("smb://", ipList[copierChoice - 1]);
                                Console.WriteLine("Copier must be Toshiba to open file share location.");
                            }
                            //if (!Directory.Exists(fileShare))
                            //{
                            //    Console.WriteLine("Could not find a valid network folder.");
                            //}
                            //else
                            //{
                            //    Console.WriteLine("Opening fileshare for " + nameList[copierChoice - 1] + ", IP: " + ipList[copierChoice - 1]);
                            //Process.Start(fileShare);
                            //}
                        }
                        else
                        {
                            Console.WriteLine("You must enter a valid integer...");
                        }
                    }
                    else if(choice == 3)
                    {
                        Console.WriteLine("Thank you for choosing MCSI to assist you. Have a wonderful day.");
                    }
                    else
                    {
                        Console.WriteLine("You must enter an integer between 1-3.");
                    }
                }
                else
                {
                    Console.WriteLine("Oofta, you must enter an integer between 1-3.");
                }
            }

        }

        // truncate name of copier to n characters
        private static string trunc(string val, int len)
        {
            if (string.IsNullOrEmpty(val))
            {
                return val;
            }
            else
            {
                return val.Length <= len ? val : val.Substring(0, len);
            }
        }

        // will run this method on launch
        private static void bStart_Click()
        {
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(" -----------------------------------------------------------");
            Console.WriteLine("  __  __  _____  _____ _____  ");
            Console.WriteLine(" |  \\/  |/ ____|/ ____|_   _| ");
            Console.WriteLine(" | \\  / | |    | (___   | |   ");
            Console.WriteLine(" | |\\/| | |     \\___ \\  | |   ");
            Console.WriteLine(" | |  | | |____ ____) |_| |_    Your business\'s source");
            Console.WriteLine(" |_|  |_|\\_____|_____/|_____|   for your techonology needs.");
            Console.WriteLine(" -----------------------------------------------------------");
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.DarkMagenta;
            Console.Write("Searching... ");


            NetworkInterface[] allNetworkInterfaces = NetworkInterface.GetAllNetworkInterfaces();
            for (int i = 0; i < (int)allNetworkInterfaces.Length; i++)
            {
                NetworkInterface networkInterface = allNetworkInterfaces[i];
                if ((networkInterface.OperationalStatus != OperationalStatus.Up ? false : networkInterface.NetworkInterfaceType != NetworkInterfaceType.Loopback))
                {
                    foreach (UnicastIPAddressInformation unicastAddress in networkInterface.GetIPProperties().UnicastAddresses)
                    {
                        if (unicastAddress.Address.AddressFamily == AddressFamily.InterNetwork)
                        {
 
                            broadcast(unicastAddress.Address);
                            numBroadcasts++;
                        }
                    }
                }
            }
        }


        private bool IsRunAsAdministrator()
        {
            return (new WindowsPrincipal(WindowsIdentity.GetCurrent())).IsInRole(WindowsBuiltInRole.Administrator);
        }

        private static void UpdateListView(string name, string ipAddress)
        {

            string[] strArrays;
            int num = 0;
            foreach (string item in ipList)
            {
                string text = item;
                char[] chrArray = new char[] { '.' };
                string[] strArrays1 = text.Split(chrArray);
                chrArray = new char[] { '.' };
                string[] strArrays2 = ipAddress.Split(chrArray);
                try
                {
                    int num1 = 0;
                    while (num1 < 4)
                    {
                        if (Convert.ToInt32(strArrays1[num1]) <= Convert.ToInt32(strArrays2[num1]))
                        {
                            num1++;
                        }
                        else
                        {
                            if (!(name is null))
                            {
                                //ListView.ListViewItemCollection items = this.listView1.Items;
                                strArrays = new string[] { name, ipAddress };
                                ipList.Add(name + " " + ipAddress);
                                //Console.WriteLine("Name: " + name);
                                //Console.WriteLine("IP: " + ipAddress);
                            }
                            return;
                        }
                    }
                }
                catch
                {
                    continue;
                }
                num++;
            }
            if (!(name is null))
            {
                Console.WriteLine(ipAddress);
                string nameAndIP = name + " " + ipAddress;

                ipList.Add(nameAndIP);
            }
        }

        public delegate void UpdateListViewCallback(string name, string ipAddress);

        public delegate void EnableStartCallback();

    }

    public class Spinner : IDisposable
    {
        //private const string Seq = @"|/-\";
        private const string Seq = @".oOo";
        private int counter = 0;
        private int leftPos, topPos, delay;
        private bool active;
        private Thread thread;

        public Spinner(int delay = 100)
        {
            this.delay = delay;
            thread = new Thread(Spin);
        }
        private void Spin()
        {
            while (active)
            {
                Turn();
                Thread.Sleep(delay);
            }
        }
        public void Start()
        {
            active = true;
            if (!thread.IsAlive)
            {
                thread.Start();
            }
        }
        public void Stop()
        {
            active = false;
            Draw(' ');
        }
        private void Draw(char x)
        {
            //Console.SetCursorPosition(leftPos, topPos);

            Console.SetCursorPosition(Console.CursorLeft - 1, Console.CursorTop);
            if(Console.CursorTop < 3)
            {
                Console.WriteLine(Console.CursorTop);
            }
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write(x);
        }
        private void Turn()
        {
            Draw(Seq[++counter % Seq.Length]);
        }
        public void Dispose()
        {
            Stop();
        }
    }

}