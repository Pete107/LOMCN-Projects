using System;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;

namespace LOMCN.DiscordBot
{
    internal class Interactivity
    {
        private const string ConfirmRegex = "\\b[Yy][Ee]?[Ss]?\\b|\\b[Nn][Oo]?\\b";
        private const string YesRegex = "[Yy][Ee]?[Ss]?";
        private const string NoRegex = "[Nn][Oo]?";

        private Dependencies dep;

        public Interactivity(Dependencies d)
        {
            dep = d;
        }

        [Command("shutdown")]
        public async Task ShutdownAsync(CommandContext ctx)
        {
            if (ctx.Member.Id != 121672783989178368 &&
                ctx.Member.Id != 216147797676785664)
                return;
            await ctx.RespondAsync("Shutting down!");
            dep.Cts.Cancel();
        }


        [Command("setcolour")]
        public async Task SetServerColor(CommandContext ctx, string serverName, byte r, byte g, byte b)
        {
            if (ctx.Member.Id != 121672783989178368 && ctx.Member.Id != 216147797676785664)
                return;
            try
            {
                if (string.IsNullOrEmpty(serverName))
                {
                    await ctx.RespondAsync("Invalid server name");
                    return;
                }

                var server = await Program.ServerStatusRepository.FindByServerName(serverName);
                if (server == null)
                {
                    await ctx.RespondAsync("Server not found!");
                    return;
                }

                server.RgbColor = $"rgb({r}, {g}, {b})";
                await Program.ServerStatusRepository.UpdateRgbColour(server);
                await ctx.RespondAsync("Operation completed successfully.");
            }
            catch (Exception ex)
            {
                Program.Log(ex);
            }
        }

        [Command("uptime")]
        public async Task GetServerUptime(CommandContext ctx, params string[] serverNameQuery)
        {
            var serverName = serverNameQuery.Aggregate(string.Empty, (current, s) => current + s + " ");

            serverName = serverName.Remove(serverName.Length - 1, 1);
            try
            {
                if (string.IsNullOrEmpty(serverName))
                {
                    await ctx.RespondAsync("Invalid server name");
                    return;
                }
                var server = await Program.ServerStatusRepository.FindByServerName(serverName);
                if (server == null)
                {
                    await ctx.RespondAsync("Server not found!");
                    return;
                }

                var timesOnline = server.History.Count(a => a.Online);
                var totalUserCount = server.History.Sum(serverEntryStatusHistory =>
                    serverEntryStatusHistory.UserCount != -1 ? serverEntryStatusHistory.UserCount : 0);
                var averageUserCount = totalUserCount / server.History.Count;
                var onlinePercent = timesOnline * 100m / server.History.Count;
                await ctx.RespondAsync($"{serverName} has an up-time of : {onlinePercent:##.##}% with an average User count of : {averageUserCount}.");
            }
            catch (Exception e)
            {
                Program.Log(e);
            }
        }
    }
}