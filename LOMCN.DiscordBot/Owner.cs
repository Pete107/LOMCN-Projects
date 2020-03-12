using System;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;

namespace LOMCN.DiscordBot
{
    [RequireOwner]
    internal class Owner
    {
        private Dependencies dep;

        public Owner(Dependencies d)
        {
            dep = d;
        }

        [Command("shutdown")]
        public async Task ShutdownAsync(CommandContext ctx)
        {
            await ctx.RespondAsync("Shutting down!");
            dep.Cts.Cancel();
        }

        [Command("addserver"), Description("Add a server : servername serveraddress servertype showusercount livedate [statusport 3000] [gameport 7000]")]
        public async Task AddNewServer(CommandContext ctx,
            string serverName, string serverAddress, string serverType, bool showUserCount, DateTime liveDate, int statusPort = 3000, int gamePort = 7000)
        {
            if (!Enum.TryParse(serverType, out MirServerType mirServerType))
            {
                return;
            }
            serverName = serverName.Replace("`", " ");
            DbHandler.Instance.NewServer(liveDate, serverName, serverAddress, DateTime.Now, showUserCount,
                mirServerType, statusPort, gamePort);
            await ctx.RespondAsync($"Server : {serverName} added.");
        }

        [Command("deleteserver")]
        public async Task DeleteServer(CommandContext ctx, string serverName)
        {

            if (string.IsNullOrEmpty(serverName))
            {
                await ctx.RespondAsync($"Invalid command");
                return;
            }
            serverName = serverName.Replace("`", " ");
            var existing = DbHandler.Instance.FindByServerName(serverName);
            if (existing == null)
            {
                await ctx.RespondAsync($"Server not found");
                return;
            }

            DbHandler.Instance.DeleteServer(existing);
            await ctx.RespondAsync($"Server : {serverName} removed");
        }

        [Command("addsubdays")]
        public async Task AddSubscription(CommandContext ctx, string serverName, int days)
        {
            if (string.IsNullOrEmpty(serverName))
            {
                await ctx.RespondAsync("Invalid command");
                return;
            }

            serverName = serverName.Replace("`", " ");
            var existing = DbHandler.Instance.FindByServerName(serverName);
            if (existing == null)
            {
                await ctx.RespondAsync($"Server {serverName} not found!");
                return;
            }

            var currentSub = existing.SubscriptionTill;
            if (currentSub >= DateTime.MaxValue)
            {
                await ctx.RespondAsync($"Cannot add any more days to {serverName}");
                return;
            }
            if (currentSub < DateTime.Now)
                currentSub = DateTime.Now + TimeSpan.FromDays(days);
            else
            {
                try
                {
                    if (currentSub > DateTime.Now)
                        currentSub += TimeSpan.FromDays(days);
                }
                catch (ArgumentException)
                {
                    currentSub = DateTime.MaxValue;
                    // ignored
                }
            }
            

            DbHandler.Instance.UpdateSubscription(existing.Id, currentSub);
            existing = DbHandler.Instance.FindById(existing.Id);
            await ctx.RespondAsync(
                $"{existing.ServerName} subscription changed, it will expire {existing.SubscriptionTill:D} ({(existing.SubscriptionTill - DateTime.Now).TotalDays} days)");
        }
    }
}