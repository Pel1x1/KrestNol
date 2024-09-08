using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;


class ServerKN
{
    private static TcpListener listener;
    private static TcpClient[] clients = new TcpClient[2];
    private static string[] playerNames = new string[2];
    private static char[] board = new char[9];
    private static int currentPlayer = 0;
    private static bool gameRunning = true;

    static void Main(string[] args)
    {
        IPAddress serverIp = IPAddress.Any;
        int port = 12345;

        listener = new TcpListener(serverIp, port);
        listener.Start();
        Console.WriteLine($"Сервер запущен на {serverIp}:{port}. Ждём человеков...");

        for (int i = 0; i < 2; i++)
        {
            clients[i] = listener.AcceptTcpClient();
            Console.WriteLine($"Игрок {i + 1} подключился.");
            Thread clientThread = new Thread(HandleClient);
            clientThread.Start(i);
        }

        InitializeBoard();
        BroadcastGameState();

        while (gameRunning)
        {
            HandleTurn();
        }

        listener.Stop();
    }

    private static void HandleClient(object obj)
    {
        int playerIndex = (int)obj;
        NetworkStream stream = clients[playerIndex].GetStream();
        byte[] buffer = new byte[256];
        int bytesRead = stream.Read(buffer, 0, buffer.Length);
        playerNames[playerIndex] = Encoding.UTF8.GetString(buffer, 0, bytesRead);
        Console.WriteLine($"Игрок {playerIndex + 1} это {playerNames[playerIndex]}");
    }

    private static void InitializeBoard()
    {
        for (int i = 0; i < board.Length; i++)
        {
            board[i] = ' ';
        }
    }

    private static void BroadcastGameState()
    {
        string gameState = $"{string.Join(",", board)}|{playerNames[0]}|{playerNames[1]}|{currentPlayer}";
        byte[] data = Encoding.UTF8.GetBytes(gameState);
        foreach (var client in clients)
        {
            client.GetStream().Write(data, 0, data.Length);
        }
    }

    private static void HandleTurn()
    {
        NetworkStream stream = clients[currentPlayer].GetStream();
        byte[] buffer = new byte[256];
        int bytesRead = stream.Read(buffer, 0, buffer.Length);
        string move = Encoding.UTF8.GetString(buffer, 0, bytesRead);
        int position = int.Parse(move);

        if (board[position] == ' ')
        {
            board[position] = currentPlayer == 0 ? 'X' : 'O';
            if (CheckWin())
            {
                gameRunning = false;
                Console.WriteLine($"Игрок {currentPlayer + 1} ({(currentPlayer == 0 ? 'X' : 'O')}) победил!");
                BroadcastGameState();
                BroadcastResult($"Игрок {playerNames[currentPlayer]} победил!");
                return;
            }
            else if (Array.IndexOf(board, ' ') == -1)
            {
                gameRunning = false;
                Console.WriteLine("Игра закончилась ничьей!");
                BroadcastGameState();
                BroadcastResult("Игра закончилась ничьей!");
                return;
            }
            else
            {
                currentPlayer = 1 - currentPlayer;
            }
            BroadcastGameState();
        }
    }

    private static bool CheckWin()
    {
        int[,] winConditions = new int[,]
        {
            {0, 1, 2}, {3, 4, 5}, {6, 7, 8}, // горизонтальные
            {0, 3, 6}, {1, 4, 7}, {2, 5, 8}, // вертикальные
            {0, 4, 8}, {2, 4, 6}             // диагональные
        };

        for (int i = 0; i < winConditions.GetLength(0); i++)
        {
            if (board[winConditions[i, 0]] != ' ' &&
                board[winConditions[i, 0]] == board[winConditions[i, 1]] &&
                board[winConditions[i, 1]] == board[winConditions[i, 2]])
            {
                return true;
            }
        }
        return false;
    }
    private static void BroadcastResult(string resultMessage)
    {
        byte[] data = Encoding.UTF8.GetBytes("RESULT|" + resultMessage);
        foreach (var client in clients)
        {
            client.GetStream().Write(data, 0, data.Length);
        }
    }
}