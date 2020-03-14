using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading;
using Newtonsoft.Json;

namespace LOMCN.DiscordBot
{
    public class StatusChecker
    {
        private const string CONTENT_TYPE = "application/json; charset=utf-8";
        public static StatusChecker Instance { get; } = new StatusChecker();
        public bool Running { get; private set; }
        private Thread _thread;
        private readonly Config _config;
        private StatusChecker()
        {
            _config = Program.Config;
        }

        public void Start()
        {
            _thread = new Thread(WorkLoop) {IsBackground = true};
            _thread.Start();
        }

        private void WorkLoop()
        {
            Running = true;
            while (Running)
            {
                if (!DbHandler.Ready || !Bot.Ready)
                    Thread.Sleep(TimeSpan.FromSeconds(30));
                var request = (HttpWebRequest) WebRequest.Create(_config.StatusURL);
                request.ContentType = CONTENT_TYPE;
                request.Credentials = CredentialCache.DefaultCredentials;
                var response = request.GetResponse();
                var serverList = new List<ServerModel>();
                using (var stream = response.GetResponseStream())
                {
                    if (stream != null)
                    {
                        using (var reader = new StreamReader(stream))
                        {
                            var result = reader.ReadToEnd();
                            serverList = JsonConvert.DeserializeObject<List<ServerModel>>(result);
                        }
                    }
                }

                response.Close();

                foreach (var serverModel in serverList)
                {
                    DbHandler.Instance.UpdateServerStatus(serverModel);
                }

                Thread.Sleep(Program.Config.OutputDelay);
            }
        }
    }

    public class ServerModel
    {
        public string Name { get; set; }
        public string Online { get; set; }
        public string Type { get; set; }
        public string EXPRate { get; set; }
        public string UserCount { get; set; }
        public string Id { get; set; }
    }
}