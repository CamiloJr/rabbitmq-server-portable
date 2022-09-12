using System;
using System.IO;
using System.Reflection;

namespace rabbitmq_server_portable.Main
{
    public sealed class AppDirectories
    {
        private readonly string appDirectory;
        private readonly string sysHomeDirectory;
        private readonly string sysHomeDrive;
        private string dataDirectory;
        private string erlangDirectory;
        private string ertsDirectory;
        private string rmqDirectory;

        private static AppDirectories _appDirectories;

        public AppDirectories()
        {
            var uri = new UriBuilder(Assembly.GetExecutingAssembly().CodeBase);
            appDirectory = Path.GetDirectoryName(Uri.UnescapeDataString(uri.Path));
            sysHomeDrive = appDirectory[1] == ':' ? appDirectory.Substring(0, 2) : "C:";
            sysHomeDirectory = appDirectory[1] == ':' ? appDirectory.Substring(2) + @"\data\" : "\\";

            GetSoftwaresDirectories();
        }

        public string AppDirectory => appDirectory;

        public string SysHomeDirectory => sysHomeDirectory;

        public string SysHomeDrive => sysHomeDrive;

        public string DataDirectory { get => dataDirectory; set => dataDirectory = value; }
        public string ErlangDirectory { get => erlangDirectory; set => erlangDirectory = value; }
        public string ErtsDirectory { get => ertsDirectory; set => ertsDirectory = value; }
        public string RmqDirectory { get => rmqDirectory; set => rmqDirectory = value; }

        public static AppDirectories GetInstance()
        {
            if (_appDirectories != null)
                return _appDirectories;
            _appDirectories = new AppDirectories();
            return _appDirectories;
        }

        private void GetSoftwaresDirectories()
        {
            // Get all root directories
            var appDirectories = Directory.GetDirectories(appDirectory);
            foreach (var directory in appDirectories)
            {
                // Remove the entire directory path and keep only the name of the folders inside it
                var folderName = directory.Replace(appDirectory, "").Replace("\\", "");

                // if this is erlang directory 
                if (folderName.ToLower().StartsWith("erl")) 
                {
                    erlangDirectory = directory;
                    // Get all erlang directories
                    var erlangDirectories = Directory.GetDirectories(erlangDirectory);
                    foreach (var currentErlangDirectory in erlangDirectories)
                    {
                        // Remove the entire directory path and keep only the name of the folders inside it
                        var erp = currentErlangDirectory.Replace(erlangDirectory, "").Replace("\\", "");
                        if (erp.StartsWith("erts"))
                            ertsDirectory = Path.Combine(erlangDirectory, erp + "\\bin");
                    }
                }
                else if (folderName.ToLower().StartsWith("rabbit"))
                {
                    rmqDirectory = directory;
                }
                else if (folderName.ToLower().Equals("data"))
                {
                    dataDirectory = directory;
                }

                dataDirectory = Path.Combine(appDirectory, "data");
                if (string.IsNullOrEmpty(dataDirectory)) ;
                    Directory.CreateDirectory(dataDirectory);
            }
        }

        public (bool reply, string message) CheckDirectories()
        {
            if (string.IsNullOrEmpty(erlangDirectory))
            {
                return (false, "Cant find erlang directory.");
            }

            if (string.IsNullOrEmpty(ertsDirectory))
            {
                return (false, "Cant find erts directory inside erlang directory. Look like you have invalid or uncomplete erlang directory.");
            }

            if (string.IsNullOrEmpty(rmqDirectory))
            {
                return (false, "Cant find rabbitmq directory.");
            }

            if (string.IsNullOrEmpty(dataDirectory))
            {
                return (false, "Cant find data directory.");
            }

            return (true, "");
        }

        public override string ToString()
        {
            return $"Application Directory: {appDirectory} \n" +
                   $"Sys Home Directory: {SysHomeDirectory} \n" +
                   $"Sys Home Drive: {SysHomeDrive} \n" +
                   $"Erlang Directory: {erlangDirectory} \n" +
                   $"Erlang ERTS Directory: {ertsDirectory} \n" +
                   $"RabbitMQ Directory: {rmqDirectory} \n" +
                   $"Data Directory: {dataDirectory}";
        }
    }
}
