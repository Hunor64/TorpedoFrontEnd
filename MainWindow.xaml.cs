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
        private const string ServerIp = "127.0.0.1";
        private const int ServerPort = 65432;
        private TcpClient client;
        private GameViewModel gameViewModel;
        public int playerID = 0;

        public MainWindow()
        {
            InitializeComponent();
            ConnectToServer();
            SendMessageToServer("GetPlayerID");
        }

        public async void SendMessageToServer(string message)
        {
            if (client == null || !client.Connected) return;

            try
            {
                NetworkStream stream = client.GetStream();
                byte[] data = Encoding.UTF8.GetBytes(message);
                await stream.WriteAsync(data, 0, data.Length);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error sending message: {ex.Message}");
            }
        }

        private void ConnectToServer()
        {
            client = new TcpClient();
            try
            {
                client.Connect(ServerIp, ServerPort);
                ReceiveMessages();
            }
            catch (Exception)
            {
                MessageBox.Show("Server is not connected, please try again later.");
                this.Close();
            }
        }

        private async void ReceiveMessages()
        {
            NetworkStream stream = client.GetStream();
            byte[] buffer = new byte[4096];
            int bytesRead;

            while (true)
            {
                try
                {
                    bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                    if (bytesRead == 0) break;

                    string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    Dispatcher.Invoke(() =>
                    {
                        if (message.StartsWith("PlayerID:") && int.TryParse(message.Substring(9), out int id))
                        {
                            playerID = id;

                            // Instantiate GameViewModel now that playerID is set
                            gameViewModel = new GameViewModel(this);
                            DataContext = gameViewModel;

                            if (playerID == 1)
                            {
                                txbLocalPlayer.Text = "Player 1";
                                txbRemotePlayer.Text = "Player 2";
                            }
                            else if (playerID == 2)
                            {
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
                        else if (message.StartsWith("FIRE_RESULT_HIT_") || message.StartsWith("FIRE_RESULT_MISS_"))
                        {
                            gameViewModel.HandleFireResult(message);
                        }
                        else if (message.StartsWith("OPPONENT_HIT_") || message.StartsWith("OPPONENT_MISS_"))
                        {
                            gameViewModel.HandleOpponentAction(message);
                        }
                        else if (message.StartsWith("NEXT_TURN_"))
                        {
                            int turnPlayerId = int.Parse(message.Substring("NEXT_TURN_".Length));
                            gameViewModel.UpdateTurn(turnPlayerId == playerID);
                        }
                        else if (message.StartsWith("SHIP_SUNK_"))
                        {
                            gameViewModel.HandleShipSunk(message);
                        }
                        else if (message.StartsWith("YOUR_SHIP_SUNK_"))
                        {
                            gameViewModel.HandleYourShipSunk(message);
                        }
                        else if (message.StartsWith("GAME_OVER_"))
                        {
                            MessageBox.Show(message.Replace('_', ' '));
                            this.Close();
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
    }
}