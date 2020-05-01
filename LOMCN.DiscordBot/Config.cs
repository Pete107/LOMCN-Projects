using System;
using System.IO;
using DSharpPlus.Entities;
using Newtonsoft.Json;

namespace LOMCN.DiscordBot
{
    internal class Config
    {
        [JsonProperty("token")] internal string Token { get; set; } = string.Empty;

        [JsonProperty("prefix")] internal string Prefix { get; set; } = ";;";

        [JsonProperty("color")] private string _color { get; set; } = "#7289DA";
        internal DiscordColor Color => new DiscordColor(_color);

        [JsonProperty("mongo_host")] internal string DbHost { get; set; } = "192.168.0.11";
        [JsonProperty("mongo_port")] internal int DbPort { get; set; } = 27017;

        [JsonProperty("output_delay")] internal TimeSpan OutputDelay { get; set; } = TimeSpan.FromMinutes(15);
        [JsonProperty("update_delay")] internal TimeSpan UpdateDelay { get; set; } = TimeSpan.FromMinutes(10);
        [JsonProperty("guildId")] internal ulong GuildId { get; set; } = 0;
        [JsonProperty("channelId")] internal ulong ChannelId { get; set; } = 0;
        [JsonProperty("output_string_format")] internal string OutputFormat { get; set; } = "<$SERVERNAME$> [$USERCOUNT$][$STATUS$]";

        [JsonProperty("status_request_endpoint")]
        internal string StatusURL { get; set; } = "https://www.lomcn.org/forum/siggen/siggen_getdata.php";
        

        public static Config LoadFromFile(string path)
        {
            using var sr = new StreamReader(path);
            return JsonConvert.DeserializeObject<Config>(sr.ReadToEnd());
        }

        public void SaveToFile(string path)
        {
            using var sw = new StreamWriter(path);
            sw.Write(JsonConvert.SerializeObject(this));
        }
    }
}