using System.Net;
using System.Net.Sockets;
using System.Text;

namespace ClientSide
{
    public partial class ClientForm : Form
    {
        private bool isConnected = false;
        private TcpClient client = null;
        private string ipAddr = "127.0.0.1"; // default
        private int port = 8899; // default
        public ClientForm()
        {
            InitializeComponent();
        }
        private void ConnectBtn_Click(object sender, EventArgs e)
        {
            if (isConnected) // If connected  -> disconnect with server
            {
                ConfigRunOnForm();
                SendExitMessage();
            }
            else // Connect to server
            {
                ConfigRunOffForm();
                // Getting ip and port from textboxes input
                try
                {
                    ipAddr = HostTextBox.Text;
                    port = int.Parse(PortTextBox.Text);
                }
                catch (Exception ex)
                {
                    ConfigRunOnForm();
                    LogRichBox.Text += $"[Error]_> (ip:port) {ex.Message}\n";
                    return;
                }
                // Starting client
                StartClient(ipAddr, port);
            }
        }

        private void SendBtn_Click(object sender, EventArgs e)
        {
            // Send message to server in next formt (username|message)
            string userMessage = messageRichBox.Text;

            if (userMessage == "exit()" || userMessage == "q()" || userMessage == "")
            {
                ConfigRunOnForm();
                SendExitMessage();
            }
            else
            {
                byte[] buffer = Encoding.ASCII.GetBytes(String.Format("point|{0}|{1}", userNameTextBox.Text, userMessage));
                NetworkStream stream = client.GetStream();
                stream.Write(buffer, 0, buffer.Length);
            }
            messageRichBox.Text = "";
        }

        private void StartClient(string ipAddr, int port)
        {
            try
            {
                // new client
                client = new TcpClient(ipAddr, port);
                // operations with client
                LogRichBox.Text = $"[Info]_> Connected to the server.\n";
                // creating new thread for working with client
                new Thread(ReadMessageThread).Start();
            }
            catch (Exception ex)
            {
                LogRichBox.Text += $"[Error]_> (launching) {ex.Message} \n";
#if DEBUG
                MessageBox.Show($"['Launching server' error] {ex}");
#endif
                ConfigRunOnForm();
                SendExitMessage();
            }
        }

        private void ReadMessageThread()
        {
            while (true)
            {
                // if client disconnect himself - destroy thread
                if (!isConnected)
                    break;

                try
                {
                    // getting message from other clients (server)
                    NetworkStream stream = client.GetStream();
                    byte[] buffer = new byte[2048];
                    int byte_count = stream.Read(buffer, 0, buffer.Length);
                    string data = Encoding.ASCII.GetString(buffer, 0, byte_count);
                    string[] splitData = data.Split("|");

                    // Special word `point` for checking is correct received data
                    if (splitData[0] == "point")
                    {
                        LogRichBox.Text += string.Format("[{0}]_> {1}", splitData[1], splitData[2]); // username and message
                    }
                }
                catch (IOException) { }

                catch (Exception ex)
                {
                    LogRichBox.Text += $"[Error]_> (getting message) {ex.Message}  \n";
#if DEBUG
                    MessageBox.Show($"['Getting from server msg' error] {ex}");
#endif
                    return;
                }
            }
        }

        private void SendExitMessage()
        {
            if (client != null && client.Connected)
            {
                try
                {
                    // Sending last exit message to server
                    byte[] buffer = Encoding.ASCII.GetBytes("exit()");
                    NetworkStream stream = client.GetStream();
                    stream.Write(buffer, 0, buffer.Length);

                    // Shutdown socket connection
                    client.Client.Shutdown(SocketShutdown.Both);
                    client.Close();
                }
                catch (Exception ex) { MessageBox.Show($"['Sending exit msg' error] {ex}"); }
            }
            
        }

        private void ClientForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            // Send message to disconnect
            if (isConnected)
            {
                SendExitMessage();
                ConfigRunOnForm();
            }
            
        }

        private void MessageRichBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                SendBtn_Click(this, new EventArgs());
            }
        }
        private void ConfigRunOnForm()
        {
            // Enabling user input when disconnected
            ConnectBtn.Text = "Connect";
            isConnected = false;
            userNameTextBox.Enabled = true;
            HostTextBox.Enabled = true;
            PortTextBox.Enabled = true;
            messageRichBox.Enabled = false;
            SendBtn.Enabled = false;
        }

        private void ConfigRunOffForm()
        {
            // Disabling user input when connected
            ConnectBtn.Text = "Disconnect";
            userNameTextBox.Enabled = false;
            HostTextBox.Enabled = false;
            PortTextBox.Enabled = false;
            messageRichBox.Enabled = true;
            SendBtn.Enabled = true;
            isConnected = true;
        }

        private void clearChatToolStripMenuItem_Click(object sender, EventArgs e)
        {
            LogRichBox.Text = "";
        }
    }
}