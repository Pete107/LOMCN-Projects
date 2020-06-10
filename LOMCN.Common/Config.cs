using System;
using System.IO;
using DSharpPlus.Entities;
using Newtonsoft.Json;

namespace LOMCN.Common
{
    public class Config
    {
        [JsonProperty("token")] public string Token { get; set; } = string.Empty;

        [JsonProperty("prefix")] public string Prefix { get; set; } = ";;";

        [JsonProperty("color")] private string _color { get; set; } = "#7289DA";
        internal DiscordColor Color => new DiscordColor(_color);

        [JsonProperty("mongo_host")] public string DbHost { get; set; } = "192.168.0.11";
        [JsonProperty("mongo_port")] public int DbPort { get; set; } = 27017;

        [JsonProperty("output_delay")] public TimeSpan OutputDelay { get; set; } = TimeSpan.FromMinutes(15);
        [JsonProperty("update_delay")] public TimeSpan UpdateDelay { get; set; } = TimeSpan.FromMinutes(10);
        [JsonProperty("guildId")] public ulong GuildId { get; set; } = 0;
        [JsonProperty("channelId")] public ulong ChannelId { get; set; } = 0;
        [JsonProperty("output_string_format")] public string OutputFormat { get; set; } = "<$SERVERNAME$> [$USERCOUNT$][$STATUS$]\r\n";

        [JsonProperty("status_request_endpoint")]
        public string StatusURL { get; set; } = "https://www.lomcn.org/forum/siggen/siggen_getdata.php";
        

        public static Config LoadFromFile(string path)
        {
            string result;
            using (var sr = new StreamReader(path))
            {
                result = sr.ReadToEnd();
            }
            return JsonConvert.DeserializeObject<Config>(result);
        }

        public void SaveToFile(string path)
        {
            using (var sw = new StreamWriter(path))
            {
                sw.Write(JsonConvert.SerializeObject(this));
            }
        }
    }
}