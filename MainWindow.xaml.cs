using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Windows;
using System.Windows.Input;


namespace TorpedoFrontEnd
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private const string ServerIp = "127.0.0.1"; // Change this if your server is on a different machine
        private const int ServerPort = 65432;
        private TcpClient client;

        public int playerID = 1;

        public MainWindow()
        {
            InitializeComponent();
            ConnectToServer();
            SendMessageToServer("GetPlayerID");
            DataContext = new GameViewModel(this);
        }

        public async void SendMessageToServer(string message)
        {
            if (client == null || !client.Connected) return;

            try
            {
                NetworkStream stream = client.GetStream();
                byte[] data = Encoding.UTF8.GetBytes(message);
                await stream.WriteAsync(data, 0, data.Length);

                byte[] buffer = new byte[1024];
                int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                string response = Encoding.UTF8.GetString(buffer, 0, bytesRead);

                // Log the response for debugging
                MessageBox.Show($"Server response: {response}");

                if (message == "GetPlayerID" && int.TryParse(response, out int id))
                {
                    playerID = id;
                    MessageBox.Show($"You are player {playerID}");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error sending message: {ex.Message}");
            }
        }

        private async void ConnectToServer()
        {
            client = new TcpClient();
            try
            {
                await client.ConnectAsync(ServerIp, ServerPort);
                //ResponseTextBox.AppendText("Connected to server.\n");
                ReceiveMessages();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error connecting to server: {ex.Message}");
            }
        }

        private async void ReceiveMessages()
        {
            NetworkStream stream = client.GetStream();
            byte[] buffer = new byte[1024];
            int bytesRead;

            while (true)
            {
                try
                {
                    bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                    if (bytesRead == 0) break; // Connection closed

                    string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);

                    Dispatcher.Invoke(() =>
                    {
                        if (message == "READY")
                        {
                            // Notify GameViewModel that ships have been placed
                            if (DataContext is GameViewModel gameViewModel)
                            {
                                gameViewModel.SetPlacementPhase(false);
                            }
                        }
                        else if (message == "TURN")
                        {
                            // It's this player's turn
                            if (DataContext is GameViewModel gameViewModel)
                            {
                                gameViewModel.IsPlayerTurn = true;
                            }
                        }
                        else if (message.StartsWith("HIT_") || message.StartsWith("MISS_"))
                        {
                            // Result of our shot
                            var parts = message.Split('_');
                            int x = int.Parse(parts[1]);
                            int y = int.Parse(parts[2]);
                            bool isHit = message.StartsWith("HIT");

                            if (DataContext is GameViewModel gameViewModel)
                            {
                                gameViewModel.ProcessShotResult(x, y, isHit);
                            }
                        }
                        else if (message.StartsWith("SHOOT_"))
                        {
                            // Opponent is firing at us
                            var parts = message.Split('_');
                            int x = int.Parse(parts[1]);
                            int y = int.Parse(parts[2]);

                            if (DataContext is GameViewModel gameViewModel)
                            {
                                bool isHit = gameViewModel.IsShipAtCoordinates(x, y);
                                string response = isHit ? $"HIT_{x}_{y}" : $"MISS_{x}_{y}";
                                SendMessageToServer(response);
                                gameViewModel.ProcessIncomingShot(x, y, isHit);
                            }
                        }
                    });
                }
                catch (Exception ex)
                {
                    Dispatcher.Invoke(() => MessageBox.Show($"Error receiving message: {ex.Message}"));
                    break;
                }
            }
        }

        private async void SendButton_Click(object sender, RoutedEventArgs e)
        {
            if (client == null || !client.Connected) return;

            string message = MessageTextBox.Text;
            if (string.IsNullOrWhiteSpace(message)) return;

            try
            {
                NetworkStream stream = client.GetStream();
                byte[] data = Encoding.UTF8.GetBytes(message);
                await stream.WriteAsync(data, 0, data.Length);
                MessageTextBox.Clear();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error sending message: {ex.Message}");
            }
        }
    }
    }