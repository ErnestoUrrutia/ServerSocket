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
        int port = 12345;
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
                        client.Send(serverMessageBytes);
                    }
                }
                else
                {
                    // Asume que 'clienteX' representa el índice del cliente (ej. cliente1, cliente2, etc.)
                    if (int.TryParse(destino.Substring(7), out int clientIndex) && clientIndex >= 1 && clientIndex <= clientSockets.Count)
                    {
                        clientSockets[clientIndex - 1].Send(serverMessageBytes);
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
            while (true)
            {
                byte[] buffer = new byte[1024];
                int receivedBytes = clientSocket.Receive(buffer);
                if (receivedBytes > 0)
                {
                    string clientMessage = Encoding.ASCII.GetString(buffer, 0, receivedBytes);
                    Console.WriteLine("Mensaje recibido del cliente: " + clientMessage);
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
