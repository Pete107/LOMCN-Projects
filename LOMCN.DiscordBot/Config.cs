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