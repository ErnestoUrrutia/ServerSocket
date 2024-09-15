using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

class Program
{
    static List<Socket> clientSockets = new List<Socket>();
    static object locker = new object();

    static void Main(string[] args)
    {
        int port = 47373;
        IPEndPoint endPoint = new IPEndPoint(IPAddress.Any, port);
        Socket serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

        serverSocket.Bind(endPoint);
        serverSocket.Listen(10);

        Console.WriteLine("Servidor esperando conexiones...");

        Thread acceptThread = new Thread(() => AcceptClients(serverSocket));
        acceptThread.Start();

        while (true)
        {
            // Enviar mensajes a un cliente específico o a todos
            Console.WriteLine("Escribe 'todos' para enviar a todos o 'clienteX' para un cliente en particular:");
            string destino = Console.ReadLine();
            Console.Write("Escribe el mensaje a enviar: ");
            string serverMessage = Console.ReadLine();
            byte[] serverMessageBytes = Encoding.ASCII.GetBytes(serverMessage);

            lock (locker)
            {
                if (destino.ToLower() == "todos")
                {
                    foreach (Socket client in clientSockets)
                    {
                        
                        if (serverMessage=="fondo")
                        {
                            try
                            {
                                string imagePath = "fondo.jpg";
                                byte[] imageBytes = File.ReadAllBytes(imagePath);
                                // Primero enviamos el tamaño de la imagen
                                byte[] sizeInfo = BitConverter.GetBytes(imageBytes.Length);

                                client.Send(serverMessageBytes);
                                client.Send(sizeInfo);

                                // Enviar la imagen
                                client.Send(imageBytes);
                                Console.WriteLine("Imagen enviada.");
                                Console.WriteLine("mandando imagen");
                            }
                            catch(Exception ex)
                            {

                            }
                            
                        }
                        else
                        {
                            client.Send(serverMessageBytes);
                        }
                        
                    }
                }
                else
                {
                    // Asume que 'clienteX' representa el índice del cliente (ej. cliente1, cliente2, etc.)
                    if (int.TryParse(destino.Substring(7), out int clientIndex) && clientIndex >= 1 && clientIndex <= clientSockets.Count)
                    {
                        if (serverMessage == "fondo")
                        {
                            try
                            {
                                string imagePath = "fondo.jpg";
                                byte[] imageBytes = File.ReadAllBytes(imagePath);
                                // Primero enviamos el tamaño de la imagen
                                byte[] sizeInfo = BitConverter.GetBytes(imageBytes.Length);

                                clientSockets[clientIndex - 1].Send(serverMessageBytes);
                                clientSockets[clientIndex - 1].Send(sizeInfo);

                                // Enviar la imagen
                                clientSockets[clientIndex - 1].Send(imageBytes);
                                Console.WriteLine("Imagen enviada.");
                                Console.WriteLine("mandando imagen");
                            }
                            catch (Exception ex)
                            {

                            }

                        }
                        else
                        {
                            clientSockets[clientIndex - 1].Send(serverMessageBytes);
                        }
                        
                    }
                    else
                    {
                        Console.WriteLine("Cliente no válido.");
                    }
                }
            }
        }
    }

    static void AcceptClients(Socket serverSocket)
    {
        while (true)
        {
            Socket clientSocket = serverSocket.Accept();
            lock (locker)
            {
                clientSockets.Add(clientSocket);
            }
            Console.WriteLine("Cliente conectado. Total de clientes: " + clientSockets.Count);

            Thread receiveThread = new Thread(() => ReceiveMessages(clientSocket));
            receiveThread.Start();
        }
    }

    static void ReceiveMessages(Socket clientSocket)
    {
        try
        {
            string nombre = "";
            byte[] buffer = new byte[1024];
            int receivedBytes = clientSocket.Receive(buffer);
            if (receivedBytes > 0)
            {
                string clientMessage = Encoding.ASCII.GetString(buffer, 0, receivedBytes);
                string[] partes = clientMessage.Split(':');
                Console.WriteLine("Mensaje recibido del cliente: " + clientMessage);
                nombre = partes[0];
            }
            while (true)
            {

                try
                {
                    byte[] sizeInfo = new byte[4];
                    clientSocket.Receive(sizeInfo);
                    int imageSize = BitConverter.ToInt32(sizeInfo, 0);
                    byte[] imageBytes = new byte[imageSize];
                    int totalBytesReceived = 0;
                    while (totalBytesReceived < imageSize)
                    {
                        totalBytesReceived += clientSocket.Receive(imageBytes, totalBytesReceived, imageSize - totalBytesReceived, SocketFlags.None);
                    }
                    // Guardar la imagen recibida en un archivo
                    string outputPath = @""+nombre+".jpg"; // Cambia la ruta si es necesario
                    File.WriteAllBytes(outputPath, imageBytes);
                }
                catch (Exception e)
                {
                    
                    Console.WriteLine(e.ToString());
                }

            }
        }
        catch (SocketException)
        {
            Console.WriteLine("Un cliente se ha desconectado.");
            lock (locker)
            {
                clientSockets.Remove(clientSocket);
            }
            clientSocket.Close();
        }
    }
}
