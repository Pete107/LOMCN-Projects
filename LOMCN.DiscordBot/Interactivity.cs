using System.Linq;
using System.Text.RegularExpressions;
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

        [Command("hi")]
        public async Task Hi(CommandContext ctx)
        {
            await ctx.RespondAsync($"👋 Hi, {ctx.User.Mention}!");
        }

        [Command("confirmation")]
        public async Task ConfirmationAsync(CommandContext ctx)
        {
            await ctx.RespondAsync("Are you sure?");
            var m = await dep.Interactivty.WaitForMessageAsync(
                x => x.Channel.Id == ctx.Channel.Id &&
                     x.Author.Id == ctx.Member.Id &&
                     Regex.IsMatch(x.Content, ConfirmRegex));
            if (Regex.IsMatch(m.Message.Content, YesRegex))
                await ctx.RespondAsync("Confirmation Received");
            else if (Regex.IsMatch(m.Message.Content, NoRegex))
                await ctx.RespondAsync("Confirmation Cancelled");
            else
                await ctx.RespondAsync("Confirmation Denied!");
        }

        [Command("shutdown")]
        public async Task ShutdownAsync(CommandContext ctx)
        {
            if (ctx.Member.Id != 121672783989178368)
                return;
            await ctx.RespondAsync("Shutting down!");
            dep.Cts.Cancel();
        }

        [Command("uptime")]
        public async Task GetServerUptime(CommandContext ctx, string serverName)
        {
            if (string.IsNullOrEmpty(serverName))
            {
                await ctx.RespondAsync("Invalid server name");
                return;
            }
            var server = DbHandler.Instance.FindByServerName(serverName);
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
            await ctx.RespondAsync($"{serverName} has an up-time of : {onlinePercent}% with an average User count of : {averageUserCount}.");
        }
    }
}