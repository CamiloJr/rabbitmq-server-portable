using System;
using System.Diagnostics;
using System.IO;

namespace rabbitmq_server_portable.Main
{
    class RabbitMqServer
    {
        private AppDirectories appDirectories;
        private Process process;
        private string batStart = string.Empty;
        private bool batConfigured = false;
        private bool serverStarted = false;

        private DataReceivedEventHandler processErrorDataReceived; 
        private DataReceivedEventHandler processOutputDataReceived;

        public DataReceivedEventHandler ProcessErrorDataReceived { get => processErrorDataReceived; 
                                                                   set => processErrorDataReceived = value; }
        public DataReceivedEventHandler ProcessOutputDataReceived { get => processOutputDataReceived; 
                                                                    set => processOutputDataReceived = value; }
        public bool ServerStarted { get => serverStarted; set => serverStarted = value; }

        public RabbitMqServer()
        {
            appDirectories = AppDirectories.GetInstance();
            if (appDirectories.CheckDirectories().reply)
            {
                ConfigureBat();
            }
        }

        private void ConfigureBat()
        {
            batStart = "@setlocal\n";
            batStart += "@set ERLANG_HOME=" + appDirectories.ErlangDirectory + "\\\n";
            batStart += "@set RABBITMQ_BASE=" + appDirectories.AppDirectory + "\\data\\\n";
            batStart += "@set RABBITMQ_CONFIG_FILE=" + appDirectories.AppDirectory + "\\data\\rabbitmq\n";
            batStart += "@set RABBITMQ_LOG_BASE=" + appDirectories.AppDirectory + "\\data\\log\n";
            batStart += "@set LOGS=\n";

            batStart += "@set HOMEDRIVE=" + appDirectories.SysHomeDrive + "\n";
            batStart += "@set HOMEPATH=" + appDirectories.SysHomeDirectory + "\n";
            batStart += "@cmd.exe";
            batConfigured = true;
        }

        private (bool reply, string message) SaveBat()
        {
            try
            {
                if(batConfigured)
                {
                    File.WriteAllText(Path.Combine(appDirectories.RmqDirectory, @"sbin\startShell.bat"), batStart);
                    return (true, "");
                }
            } catch (Exception ex)
            {
                return (false, ex.Message);
            }

            return (false, "Error saving configuration file! Configuration file not configured or problems trying to save!");
        }

        public (bool reply, string message) StartServer()
        {
            var saveBat = SaveBat();
            if (!saveBat.reply)
                return (saveBat.reply, saveBat.message);

            try
            {
                KillRabbitMQOpenedService();

                process = new Process();
                process.StartInfo.EnvironmentVariables["ERLANG_HOME"] = appDirectories.ErlangDirectory + @"\"; 
                process.StartInfo.EnvironmentVariables["RABBITMQ_BASE"] = appDirectories.AppDirectory + @"\data\"; // RabbitMQ logs and database
                process.StartInfo.EnvironmentVariables["RABBITMQ_CONFIG_FILE"] = appDirectories.AppDirectory + @"\data\rabbitmq"; // Where is config file
                process.StartInfo.EnvironmentVariables["RABBITMQ_LOG_BASE"] = appDirectories.AppDirectory + @"\data\log"; // Where are log files
                process.StartInfo.EnvironmentVariables.Remove("LOGS");
                process.StartInfo.EnvironmentVariables["HOMEDRIVE"] = appDirectories.SysHomeDrive; // Erlang need this for cookie file
                process.StartInfo.EnvironmentVariables["HOMEPATH"] = appDirectories.SysHomeDirectory; // Erlang need this for cookie file
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.CreateNoWindow = true;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;
                process.OutputDataReceived += processOutputDataReceived;
                process.ErrorDataReceived += processErrorDataReceived;
                process.StartInfo.FileName = "cmd.exe";
                process.StartInfo.Arguments = "/c \"" + Path.Combine(appDirectories.RmqDirectory, @"sbin\rabbitmq-server.bat") + "\"";

                process.Start();
                process.BeginOutputReadLine();

                serverStarted = true;

                return (true, "Server Started!");
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }

        public (bool reply, string message) StopServer()
        {
            try
            {
                if (process != null && !process.HasExited)
                {
                    process.Kill();

                    // Check if erlang leave something behind
                    KillRabbitMQOpenedService();
                }

                process = null;
                serverStarted = false;
                return (true, "Server Stopped!");
            }
            catch (Exception ex)
            {
                return (false, "Error stoping server\n\n" + ex.Message);
            }
        }

        private void KillRabbitMQOpenedService()
        {
            var allProcesses = Process.GetProcesses();
            foreach (var process in allProcesses)
            {
                try
                {
                    var fullPath = process.MainModule.FileName;
                    if (fullPath != null && fullPath.StartsWith(appDirectories.ErlangDirectory))
                    {
                        process.Kill();
                    }
                }
                catch (Exception)
                { }
            }
        }
    }
}
