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
        private GameViewModel gameViewModel;
        public int playerID = 0;

        public MainWindow()
        {
            ConnectToServer();
            DataContext = new GameViewModel(this);
            SendMessageToServer("GetPlayerID");
            InitializeComponent();
            gameViewModel = new GameViewModel(this); // Pass the current instance of MainWindow
            DataContext = gameViewModel;
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

                if (message == "GetPlayerID" && int.TryParse(response, out int id))
                {
                    playerID = id;
                    //Dispatcher.Invoke(() => ResponseTextBox.AppendText($"Player ID received: {playerID}\n"));
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error sending message: {ex.Message}");
            }
        }

        private  void ConnectToServer()
        {
            client = new TcpClient();
            try
            {
                 client.Connect(ServerIp, ServerPort);
                //ResponseTextBox.AppendText("Connected to server.\n");
                ReceiveMessages();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Server is not connected, please try again later.");
                this.Close();
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
                        // Check if the message contains the Player ID
                        if (message.StartsWith("PlayerID:") && int.TryParse(message.Substring(9), out int id))
                        {
                            playerID = id;
                            if (playerID == 1)
                            {
                                InitializeComponent();
                                txbLocalPlayer.Text = "Player 1";
                                txbRemotePlayer.Text = "Player 2";
                            }
                            else if (playerID == 2)
                            {
                                InitializeComponent();
                                txbLocalPlayer.Text = "Player 2";
                                txbRemotePlayer.Text = "Player 1";
                            }
                            else if (playerID == 0)
                            {
                                MessageBox.Show("Server is not connected, please try again later.");
                                this.Close();
                            }
                            else if (playerID == -1)
                            {
                                MessageBox.Show("Server is full, please try again later.");
                                this.Close();
                            }
                        }
                        else if (message.StartsWith("SHIPPOSITIONS:"))
                        {
                            // Extract the JSON part of the message
                            var json = message.Substring("SHIPPOSITIONS:".Length);
                            gameViewModel.UpdateOpponentShips(json);
                        }
                        // You can add more message handling logic here if needed
                    });
                }
                catch (Exception ex)
                {
                    Dispatcher.Invoke(() => MessageBox.Show($"Error receiving message: {ex.Message}"));
                    break;
                }
            }
        }


    }
}