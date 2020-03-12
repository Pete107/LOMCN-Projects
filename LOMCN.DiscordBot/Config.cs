using System.IO;
using DSharpPlus.Entities;
using Newtonsoft.Json;

namespace LOMCN.DiscordBot
{
    internal class Config
    {
        [JsonProperty("token")] internal string Token = "NTgwNzE0NzMwNjIxMTA4MjQ0.XmUjCQ.bhQMWnim4JMGvn9PLNce92wG8g4";

        [JsonProperty("prefix")] internal string Prefix = ";;";

        [JsonProperty("color")] private string _color = "#7289DA";
        internal DiscordColor Color => new DiscordColor(_color);

        [JsonProperty("mongo_host")] internal string DbHost = "192.168.0.11";
        [JsonProperty("mongo_port")] internal int DbPort = 27017;

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