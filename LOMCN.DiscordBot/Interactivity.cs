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
    }
}