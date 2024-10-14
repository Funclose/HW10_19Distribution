using System.Net.Sockets;
using System.Text;

namespace HW10_19Distribution
{
     class Program
    {
        static void Main(string[] args)
        {
            try
            {
                using (TcpClient client = new TcpClient("127.0.0.1", 8888))
                {
                    Console.WriteLine("Server connected");
                    NetworkStream stream = client.GetStream();

                    while (true)
                    {
                        Console.WriteLine("\nВведите команду (ADD для добавления заказа, STATUS <id> для проверки статуса, CANCEL <id> для отмены заказа):");
                        string input = Console.ReadLine();

                        if (input.ToLower() == "exit")
                        {
                            break; 
                        }

                        
                        byte[] data = Encoding.UTF8.GetBytes(input);
                        stream.Write(data, 0, data.Length);

                        
                        byte[] buffer = new byte[1024];
                        int bytesRead = stream.Read(buffer, 0, buffer.Length);
                        string response = Encoding.UTF8.GetString(buffer, 0, bytesRead);

                        Console.WriteLine($"Ответ сервера: {response}");
                    }

                    stream.Close();
                    client.Close();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка: {ex.Message}");
            }
        }
    }
}

