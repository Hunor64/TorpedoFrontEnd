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
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new GameViewModel();
        }
        private void Cell_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is GameViewModel viewModel)
            {
                // Toggle between horizontal and vertical orientation
                viewModel.ShipOrientation = viewModel.ShipOrientation == ShipOrientation.Horizontal
                    ? ShipOrientation.Vertical
                    : ShipOrientation.Horizontal;
            }
        }
    }
}


//namespace TorpedoGameClient
//{
//    class Program
//    {
//        static void Main(string[] args)
//        {
//            ConnectToServer();
//        }

//        public static void ConnectToServer()
//        {
//            try
//            {
//                // Define server endpoint
//                IPHostEntry ipHost = Dns.GetHostEntry(Dns.GetHostName());
//                IPAddress ipAddr = ipHost.AddressList[0]; // Assuming localhost for testing
//                IPEndPoint remoteEP = new IPEndPoint(ipAddr, 11111);

//                // Create a TCP socket
//                Socket sender = new Socket(ipAddr.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

//                // Connect to the server
//                sender.Connect(remoteEP);
//                Console.WriteLine("Connected to the server!");

//                // Start communication
//                while (true)
//                {
//                    Console.Write("Enter command (shoot, move, status, exit): ");
//                    string command = Console.ReadLine();

//                    // Exit the client if the user types "exit"
//                    if (command.ToLower() == "exit")
//                    {
//                        byte[] exitMessage = Encoding.ASCII.GetBytes("exit<EOF>");
//                        sender.Send(exitMessage);
//                        break;
//                    }

//                    // Send command to the server
//                    byte[] message = Encoding.ASCII.GetBytes(command + "<EOF>");
//                    sender.Send(message);

//                    // Receive response from the server
//                    byte[] bytes = new byte[1024];
//                    int numByte = sender.Receive(bytes);
//                    string response = Encoding.ASCII.GetString(bytes, 0, numByte);
//                    Console.WriteLine("Server response: {0}", response);
//                }

//                // Shutdown and close the socket
//                sender.Shutdown(SocketShutdown.Both);
//                sender.Close();
//            }
//            catch (Exception e)
//            {
//                Console.WriteLine($"Error: {e}");
//            }