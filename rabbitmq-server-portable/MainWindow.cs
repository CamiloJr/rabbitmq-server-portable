using rabbitmq_server_portable.Main;
using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace rabbitmq_server_portable
{
    public partial class MainWindow : Form
    {
        private AppDirectories appDirectories;
        private RabbitMqServer rabbitMqServer;
        private readonly string rabbitMqUri = "http://localhost:15672/";

        private About about = new About();
        private bool aboutFormOppened = false;

        public MainWindow()
        {
            InitializeComponent();
            appDirectories = AppDirectories.GetInstance();

            var check = appDirectories.CheckDirectories();

            if(!check.reply)
            {
                MessageBox.Show(check.message,
                        Text,
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Stop);
                Close();
            }
        }

        private void MainWindow_Load(object sender, EventArgs e)
        {
            var rmqIconBitmap = rabbitmq_server_portable.Properties.Resources.rmq_icon;
            this.Icon = Icon.FromHandle(rmqIconBitmap.GetHicon());

            rabbitMqServer = new RabbitMqServer();
            rabbitMqServer.ProcessErrorDataReceived += ProcessErrorDataReceived;
            rabbitMqServer.ProcessOutputDataReceived += ProcessOutputDataReceived;

            //WriteLineToOutput(appDirectories.ToString(), Color.Aqua);

            GetVersion();

            var wellcomeMessage = $"############################################################\n" +
                                  $"#                                                          #\n" +
                                  $"#        Hello, welcome to RabbitMT Server Portable!       #\n" +
                                  $"#  To start the server, select the \"Server > Start\" menu.  #\n" +
                                  $"#                                                          #\n" +
                                  $"#   User: guest                                            #\n" +
                                  $"#   Password: guest                                        #\n" +
                                  $"#                                                          #\n" +
                                  $"############################################################\n";

            WriteLineToOutput(wellcomeMessage, Color.Orange);

            StoppedServer();
        }

        private void ProcessErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            try
            {
                if (e.Data != null)
                    Invoke(new MethodInvoker(delegate { WriteLineToOutput(e.Data, Color.Red); }));
            }
            catch (Exception)
            {
            }
        }

        private void ProcessOutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            try
            {
                if (e.Data != null)
                    Invoke(new MethodInvoker(delegate { WriteLineToOutput(e.Data, Color.LimeGreen); }));
            }
            catch (Exception)
            {
            }
        }

        private void WriteLineToOutput(string data, Color color)
        {
            rtbConsole.SelectionColor = color;
            if (data != null)
                rtbConsole.AppendText(data + "\n");
            rtbConsole.ScrollToCaret();
        }

        private void startToolStripMenuItem_Click(object sender, EventArgs e)
        {
            StartServer();
        }

        private void stopToolStripMenuItem_Click(object sender, EventArgs e)
        {
            StopServer();
        }

        private void StartServer()
        {
            if(!rabbitMqServer.ServerStarted)
            {
                var replyRmqs = rabbitMqServer.StartServer();
                if (replyRmqs.reply)
                {
                    WriteLineToOutput(" Server Started, wait for loading... ", Color.Blue);
                    StartedServer();
                }
                else
                {
                    WriteLineToOutput($" ERROR: {replyRmqs.message}", Color.Red);
                }
            }
        }

        private void StopServer()
        {
            if (rabbitMqServer.ServerStarted)
            {
                WriteLineToOutput(" Stopping Server, please wait...  ", Color.Blue);
                var replyRmqs = rabbitMqServer.StopServer();
                if (replyRmqs.reply)
                {
                    WriteLineToOutput(" Server stopped ... ", Color.White);
                    StoppedServer();
                }
                else
                {
                    WriteLineToOutput($" ERROR: {replyRmqs.message}", Color.Red);
                }
            }
        }

        private void StartedServer()
        {
            startToolStripMenuItem.Enabled = false;
            stopToolStripMenuItem.Enabled = true;
        }

        private void StoppedServer()
        {
            startToolStripMenuItem.Enabled = true;
            stopToolStripMenuItem.Enabled = false;
        }

        private void MainWindow_FormClosed(object sender, FormClosedEventArgs e)
        {
            StopServer();
        }

        private void openInBrowserToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Process.Start(rabbitMqUri);
        }

        private void consoleToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var console = new Process
            {
                StartInfo =
                {
                    UseShellExecute = false,
                    FileName = Path.Combine(appDirectories.RmqDirectory, @"sbin\startShell.bat"),
                    WorkingDirectory = Path.Combine(appDirectories.RmqDirectory, "sbin")
                }
            };
            console.Start();
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var formCollection = Application.OpenForms;

            foreach (Form form in formCollection)
            {
                if (form.Name == "About")
                {
                    aboutFormOppened = true;
                    break;
                }
                aboutFormOppened = false;
            }

            if (!aboutFormOppened)
            {
                aboutFormOppened = true;
                about = new About();
                about.Show();
            }
        }

        private void openLogFolderToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenDirectory(appDirectories.DataDirectory + "\\log\\");
        }

        private void openConfigFolderToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenDirectory(appDirectories.DataDirectory);
        }

        private void OpenDirectory(string path)
        {
            Process.Start(new System.Diagnostics.ProcessStartInfo()
            {
                FileName = path,
                UseShellExecute = true,
                Verb = "open"
            });
        }

        private void GetVersion()
        {
            var rmqVersion = appDirectories.RmqDirectory.Replace(appDirectories.AppDirectory, "").Replace("\\", "").Split('-')[1];
            var erlandVersion = appDirectories.ErlangDirectory.Replace(appDirectories.AppDirectory, "").Replace("\\", "").Split('-')[1];

            tssRabbitMQVersion.Text = $"RabbitMQ Server Version: {rmqVersion}   ";
            tssErlangVersion.Text = $"Erlang Version: {erlandVersion}";
        }
    }
}
