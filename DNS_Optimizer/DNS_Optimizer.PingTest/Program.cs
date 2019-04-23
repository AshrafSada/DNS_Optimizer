using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading;

namespace DNS_Optimizer.PingTest
{
    internal class Program
    {
        private const string urlBase = "https://";

        private static Ping xPing;

        private static List<PingResults> pingResults = new List<PingResults>();

        private static string DnsDataFile
        {
            get
            {
                return AppDomain.CurrentDomain.BaseDirectory + "dns_serv_list.dat";
            }
        }

        private static void Main(string[] args)
        {
            // Testing networking and INTERNET availability
            Console.WriteLine("DNS testing version 1.0 \n");
            Console.WriteLine("*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-* \n");
            Console.WriteLine("Establishing Internet and server connections, please wait ... \n");
            ConsoleStyling();

            if (CheckNet() == true)
            {
                TestDNS();
            }
            else
            {
                Console.WriteLine("This application requires working Internet connection! \n");
            }

            Console.Write("Press Enter to exit");
            Console.Read();
        }

        /// <summary>
        /// DNS servers ping test
        /// </summary>
        private static void TestDNS()
        {
            pingResults.Clear();
            List<string> dnsIP = GetDNSData();

            if (dnsIP.Count > 0)
            {
                Console.WriteLine("Loading DNS servers list, please wait ... \n");
                Console.WriteLine("Testing DNS started ... \n");
                for (int i = 0; i < dnsIP.Count; i++)
                {
                    Console.WriteLine($"DNS Server {i + 1} IP {dnsIP[i]}");
                }

                // instantiating new instance of Pint class and setting parametric testing
                xPing = new Ping();
                int xTimeOut = 3500;
                byte[] xBuffer = new byte[32];

                Console.WriteLine("\n\r");
                Console.WriteLine("Starting ping tests ...");
                Console.WriteLine("\n\r");
                try
                {
                    for (int i = 0; i < dnsIP.Count; i++)
                    {
                        PingReply xRep = xPing.Send(dnsIP[i].ToString(), xTimeOut, xBuffer);
                        if (xRep.Status == IPStatus.Success)
                        {
                            Console.WriteLine($"DNS Server IP = {xRep.Address}");
                            Console.WriteLine($"DNS Server Buffer Size = {xRep.Buffer.Length.ToString()}");
                            Console.WriteLine($"DNS Server TTL = {xRep.Options.Ttl.ToString()}");
                            Console.WriteLine($"DNS Server RTT = {xRep.RoundtripTime.ToString()} ms");
                            Console.WriteLine($"DNS Server Test Result = {xRep.Status} \n");

                            AddResults(
                                  xRep.Address.ToString(),
                                  xRep.Buffer.Length.ToString(),
                                  xRep.Options.Ttl,
                                  xRep.RoundtripTime,
                                  xRep.Status.ToString()
                                );
                        }
                        else
                        {
                            Console.WriteLine($"DNS Server Error = {xRep.Status} \n");
                        }
                    }
                    Console.Beep();
                    Thread.Sleep(1000);
                    Console.Beep();
                    Thread.Sleep(1000);
                    Console.Beep();
                    Console.WriteLine("Test finished successfully \n");
                    SaveResults();
                    ProcessResults();
                    
                }
                catch (PingException)
                {
                    Console.WriteLine("Some DNS servers might be down or under maintenance \n");
                }
            }
            else
            {
                Console.WriteLine("Database empty or SQL error");
            }
        }

        private static void ProcessResults()
        {
            double[] rtt = pingResults
                .Select(r => Convert.ToDouble(r.RTT))
                .ToArray();

            double rttAverage = Math.Round(rtt.Average(), 2);

            double rttMin = rtt.Min();

            string bestDNS = pingResults
                .Where(r => r.RTT == rttMin)
                .Select(r => r.IPAddress)
                .First();

            Console.WriteLine($"DNS RTT Average = {rttAverage.ToString()}");

            Console.WriteLine($"Based on test results the best DNS is {bestDNS} with RTT {rttMin} ");

            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Would you like to set your DNS to {bestDNS} (Yes,No)?");
            string userChoice = "No";
            userChoice = Console.ReadLine();

            if (userChoice.ToLower().Equals("yes"))
            {
                // Reset DNS to auto
                SetDNSautoDHCP();
                // Adding DNS to current network interface
                Process p = new Process();
                ProcessStartInfo psi = new ProcessStartInfo();
                psi.FileName = "netsh";
                psi.Arguments = $"interface ip set dns name=\"Wi-Fi\" source=\"static\" address=\"{bestDNS}\"";
                psi.Arguments = $"interface ip set dns name=\"Ethernet\" source=\"static\" address=\"{bestDNS}\"";
                psi.Arguments = $"interface ip set dns name=\"Wireless Network Connection\" source=\"static\" address=\"{bestDNS}\"";
                p.StartInfo = psi;
                p.Start();
                RestartNetworks();
                xPing.Dispose();
            }
        }

        private static void RestartNetworks()
        {
            Process pDisable = new Process();

            ProcessStartInfo psiDisable = new ProcessStartInfo();
            psiDisable.FileName = "netsh";
            psiDisable.Arguments = "interface set interface Wireless Network Connection disabled";
            psiDisable.Arguments = "interface set interface Wi-Fi disabled";
            psiDisable.Arguments = "interface set interface Ethernet disabled";
            pDisable.StartInfo = psiDisable;
            pDisable.Start();

            Thread.Sleep(3000);

            Process pEnable = new Process();
            ProcessStartInfo psiEnable = new ProcessStartInfo();
            psiEnable.FileName = "netsh";
            psiEnable.Arguments = "interface set interface Wireless Network Connection enable";
            psiEnable.Arguments = "interface set interface Wi-Fi enable";
            psiEnable.Arguments = "interface set interface Ethernet enable";
            pEnable.StartInfo = psiEnable;
            pEnable.Start();
        }

        private static void AddResults(string ipAddress, string bufferSize, double TTL, double RTT, string status)
        {
            PingResults results = new PingResults()
            {
                IPAddress = ipAddress,
                BufferSize = bufferSize,
                TTL = TTL,
                RTT = RTT,
                Status = status
            };

            pingResults.Add(results);
        }

        /// <summary>
        /// Saving test results
        /// </summary>
        private static void SaveResults()
        {
            // formatting results
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < pingResults.Count; i++)
            {
                sb.AppendLine($"DNS Server IP = {pingResults[i].IPAddress}");
                sb.AppendLine($"DNS Server Buffer Size = {pingResults[i].BufferSize}");
                sb.AppendLine($"DNS Server TTL = {pingResults[i].TTL.ToString()}");
                sb.AppendLine($"DNS Server RTT = {pingResults[i].RTT.ToString()} ms");
                sb.AppendLine($"DNS Server Test Result = {pingResults[i].Status} \n");
            }

            // Saving results to file
            string filePath = AppDomain.CurrentDomain.BaseDirectory + "ping_results.dat";

            try
            {
                File.WriteAllText(filePath, sb.ToString());
            }
            catch (IOException ioe)
            {
                throw new Exception(ioe.Message);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        /// <summary>
        /// Set the DNS on the current active network connection to auto using NETSH commands
        /// </summary>
        private static void SetDNSautoDHCP()
        {
            Process process = new Process();
            ProcessStartInfo processInfo = new ProcessStartInfo();
            processInfo.FileName = "netsh";
            processInfo.Arguments = "interface ipv4 set DNS \"Wireless Network Connection\" DHCP";
            processInfo.Arguments = "interface ipv4 set DNS \"Wi-Fi\" DHCP";
            processInfo.Arguments = "interface ipv4 set DNS \"Ethernet\" DHCP";
            process.StartInfo = processInfo;
            process.Start();
        }

        /// <summary>
        /// Check INTERNET availability/connectivity
        /// </summary>
        /// <returns>True: available/connected False: not connected</returns>
        private static bool CheckNet()
        {
            Console.WriteLine("Enter Web URL? or press enter to use default URL");
            string testUrl = urlBase + Console.ReadLine();

            if (testUrl.Length == 8)
            {
                // can be replaced with any working URL
                testUrl = urlBase + "www.msn.com";
            }

            try
            {
                var client = new WebClient();
                client.OpenRead(testUrl);

                Console.WriteLine("Internet connections established \n");
                Console.WriteLine("\n" + client.ResponseHeaders.ToString());

                return true;
            }
            catch (WebException)
            {
                Console.WriteLine("Networking error, INTERNET currently unavailable or disconnected \n");

                return false;
            }
        }

        /// <summary>
        /// Setup console colors for better viewing experience
        /// </summary>
        private static void ConsoleStyling()
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.BackgroundColor = ConsoleColor.Black;
            Console.Title = "DNS Test";
            Console.Beep();
        }

        private static List<string> GetDNSData()
        {
            List<string> content = File.ReadAllLines(DnsDataFile, Encoding.UTF8).ToList();

            return content;
        }
    }
}