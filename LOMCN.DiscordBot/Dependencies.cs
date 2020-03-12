using System.Threading;
using DSharpPlus.Interactivity;

namespace LOMCN.DiscordBot
{
    internal class Dependencies
    {
        internal InteractivityModule Interactivty { get; set; }
        internal StartTimes StartTimes { get; set; }
        internal CancellationTokenSource Cts { get; set; }
    }
}