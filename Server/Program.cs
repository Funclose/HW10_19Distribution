using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Server
{
     class Program
    {
        private static ConcurrentQueue<int> queue = new ConcurrentQueue<int>();
        private static Dictionary<int, string> orderStatus = new Dictionary<int, string>();
        private static int orderCounter;
        static object locker = new object();
        private static TcpListener listener;

        static void Main(string[] args)
        {
            StartServer();
        }

        private static void StartServer()
        {
            listener = new TcpListener(IPAddress.Any, 8888);
            listener.Start();
            Console.WriteLine("Сервер запущен и ожидает подключений...");

            while (true)
            {
                TcpClient client = listener.AcceptTcpClient();
                Console.WriteLine("новый клиент подключен");

                Thread clientThread = new Thread(() => HandleClient(client));
                clientThread.Start(); 
            }
        }

        public static void HandleClient(TcpClient client)
        {
            NetworkStream stream = client.GetStream();
            byte[] buffer = new byte[4096];
            int bytesRead;

            try
            {
                while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) != 0)
                {
                    string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    string[] command = message.Split(" ");

                    switch (command[0])
                    {
                        case "ADD":
                            AddOrder(stream);
                            break;
                        case "STATUS":
                            if (int.TryParse(command[1], out int orderId))
                            {
                                CheckOrder(orderId, stream);
                            }
                            break;
                        case "CANCEL":
                            if (int.TryParse(command[1], out int orderToCancel))
                            {
                                CancelOrder(orderToCancel, stream);
                            }
                            break;
                        default:
                            SendMessage(stream, "Неверная команда");
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{ex.Message}");
            }
            finally
            {
                client.Close(); 
            }
        }

        private static void AddOrder(NetworkStream stream)
        {
            int orderId;
            lock (locker)
            {
                orderId = ++orderCounter;
                queue.Enqueue(orderId);
                orderStatus[orderId] = "В очереди";
            }

            SendMessage(stream, $"Заказ №{orderId} добавлен и находится в очереди.");

           
            Thread orderThread = new Thread(() => ProcessOrder(orderId));
            orderThread.Start();
        }

        private static void CheckOrder(int orderId, NetworkStream stream)
        {
            if (orderStatus.TryGetValue(orderId, out string status))
            {
                SendMessage(stream, $"Статус заказа №{orderId}: {status}");
            }
            else
            {
                SendMessage(stream, $"Статус заказа №{orderId} не найден");
            }
        }

        private static void CancelOrder(int orderId, NetworkStream stream)
        {
            lock (locker)
            {
                if (orderStatus.ContainsKey(orderId) && orderStatus[orderId] != "Готов")
                {
                    orderStatus[orderId] = "Отменён";
                    SendMessage(stream, $"Заказ №{orderId} отменён.");
                }
                else if (orderStatus[orderId] == "Готов")
                {
                    SendMessage(stream, $"Заказ №{orderId} уже готов и не может быть отменён.");
                }
                else
                {
                    SendMessage(stream, "Заказ не найден.");
                }
            }
        }

        private static void ProcessOrder(int orderId)
        {
            lock (locker)
            {
                orderStatus[orderId] = "В процессе";
            }

            
            Thread.Sleep(5000);

            lock (locker)
            {
                orderStatus[orderId] = "Готов";
            }

            Console.WriteLine($"Заказ №{orderId} готов.");
        }

        private static void SendMessage(NetworkStream stream, string message)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(message);
            stream.Write(bytes, 0, bytes.Length);
        }
    }
}
