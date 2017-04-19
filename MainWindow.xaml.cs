using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace NamedPipe_Server
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        struct WIN32_FIND_DATA
        {
            public uint dwFileAttributes;
            public System.Runtime.InteropServices.ComTypes.FILETIME ftCreationTime;
            public System.Runtime.InteropServices.ComTypes.FILETIME ftLastAccessTime;
            public System.Runtime.InteropServices.ComTypes.FILETIME ftLastWriteTime;
            public uint nFileSizeHigh;
            public uint nFileSizeLow;
            public uint dwReserved0;
            public uint dwReserved1;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string cFileName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 14)]
            public string cAlternateFileName;
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        static extern IntPtr FindFirstFile(string lpFileName, out WIN32_FIND_DATA lpFindFileData);


        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        static extern bool FindNextFile(IntPtr hFindFile, out WIN32_FIND_DATA
           lpFindFileData);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool FindClose(IntPtr hFindFile);

        NamedPipeServerStream server;
        NamedPipeClientStream client;
        string ServerName = string.Empty;
        string ServerIP = string.Empty;
        public MainWindow()
        {
            InitializeComponent();
        }

        private void rdbServer_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                expClient.IsEnabled = false;
                expClient.IsExpanded = false;
                expServer.IsEnabled = true;
                expServer.IsExpanded = true;
                btnSend.IsEnabled = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show("rdbServer_Checked : " + ex.Message);
            }
        }

        private void rdbClient_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                expServer.IsEnabled = false;
                expServer.IsExpanded = false;
                expClient.IsEnabled = true;
                expClient.IsExpanded = true;
                btnSend.IsEnabled = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("rdbClient_Checked : " + ex.Message);
            }
        }

        private void btnServerCreate_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                PipeSecurity pipeSa = new PipeSecurity();


                pipeSa.SetAccessRule(new PipeAccessRule("Everyone", PipeAccessRights.ReadWrite, System.Security.AccessControl.AccessControlType.Allow));
                server = new NamedPipeServerStream(txtServer.Text, PipeDirection.InOut,10, PipeTransmissionMode.Byte, PipeOptions.Asynchronous,1024,1024,pipeSa);
                ServerName = txtServer.Text;
                Thread TServer = new Thread(StartServer);
                TServer.Start();
                btnServerCreate.IsEnabled = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show("btnServerCreate_Click : " + ex.Message);
            }
        }

        private void StartServer()
        {
            try
            {

                MessageBox.Show("Server : " + ServerName + " is created.");
                server.WaitForConnection();
                ReadData();

                //lblStatus.Content = "Server : " + ServerName + " is created.";
            }
            catch (Exception ex)
            {
                MessageBox.Show("StartServer : " + ex.Message);
            }
        }
        private void ReadData()
        {
            try
            {

                var bytes = new byte[4096];
                while (server.IsConnected)
                {
                    server.Read(bytes, 0, bytes.Length);
                    string request = ASCIIEncoding.ASCII.GetString(Decode(bytes));
                    this.Dispatcher.Invoke(() =>
          {
              if (string.IsNullOrEmpty(tblChat.Text))
                  tblChat.Text = request + Environment.NewLine;
              else
                  tblChat.Text = tblChat.Text + request + Environment.NewLine;
          });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("ReadData : " + ex.Message);
            }
        }

        public byte[] Decode(byte[] packet)
        {
            try
            {
                var i = packet.Length - 1;
                while (packet[i] == 0)
                {
                    --i;
                }
                
            var temp= new byte[i + 1];
                Array.Copy(packet, temp, i + 1);
                return temp;
            }
            catch(Exception ex)
            {
                MessageBox.Show("Decode : " + ex.Message);
                return null;
            }
        }

        private void SendData()
        {
            try
            {
                if (server.IsConnected)
                {
                    byte[] buffer = ASCIIEncoding.ASCII.GetBytes(txtContent.Text);
                    server.Write(buffer, 0, buffer.Length);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("SendData : " + ex.Message);
            }
        }
        private void btnConnectServer_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ServerName = lstServers.SelectedValue.ToString();
                ServerIP = txtServerInfo.Text;
                Thread TClient = new Thread(ConnectServer);
                TClient.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show("btnConnectServer_Click : " + ex.Message);
            }
        }

        private void ConnectServer()
        {
            try
            {
                client = new NamedPipeClientStream(ServerIP, "NPServer", PipeDirection.InOut, PipeOptions.Asynchronous, TokenImpersonationLevel.None, HandleInheritability.None);
                if (client != null)
                {
                    client.Connect();
                    //lblClientStatus.Content = "Connected to server";
                    //ReadData();
                    MessageBox.Show("Server Connection Established");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("ConnectServer : " + ex.Message);
            }
        }

        private void btnServerClose_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                server.Close();
                lblStatus.Content = "Server : " + ServerName + " is closed.";
                btnServerCreate.IsEnabled = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("btnServerClose_Click : " + ex.Message);
            }
        }

        private void expClient_Expanded(object sender, RoutedEventArgs e)
        {
            try
            {
                GetPipes();
            }
            catch (Exception ex)
            {
                MessageBox.Show("expClient_Expanded : " + ex.Message);
            }
        }

        private void GetPipes()
        {
            try
            {
                var namedPipes = new List<string>();
                WIN32_FIND_DATA lpFindFileData;

                var ptr = FindFirstFile(@"\\.\pipe\*", out lpFindFileData);
                namedPipes.Add(lpFindFileData.cFileName);
                while (FindNextFile(ptr, out lpFindFileData))
                {
                    namedPipes.Add(lpFindFileData.cFileName);
                }
                FindClose(ptr);

                namedPipes.Sort();

                foreach (var v in namedPipes)
                    lstServers.Items.Add(v.ToString());
            }
            catch (Exception ex)
            {
                MessageBox.Show("GetPipes : " + ex.Message);
            }
        }

        private void btnRefreshServer_Click(object sender, RoutedEventArgs e)
        {
            lstServers.Items.Clear();
            expClient_Expanded(null, null);
        }

        private void btnSend_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (client.IsConnected)
                {
                    byte[] buffer = ASCIIEncoding.ASCII.GetBytes(txtContent.Text);
                    client.Write(buffer, 0, buffer.Length);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("SendData : " + ex.Message);
            }
        }

        private void btnDisconServer_Click(object sender, RoutedEventArgs e)
        {
            client.Close();
            MessageBox.Show("Server Connection Disconnected");
        }
    }
}
