using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LOMCN.Common.Database;
using LOMCN.Common.Database.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace LOMCN.StatisticalApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class MirStatsController : ControllerBase
    {
        private readonly ILogger<MirStatsController> _logger;
        private readonly ServerStatusRepository _repo;

        public MirStatsController(ILogger<MirStatsController> logger, ServerStatusRepository repo)
        {
            _logger = logger;
            _repo = repo;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return new JsonResult(true);
        }

        [HttpGet]
        [ResponseCache(Duration = 60 * 60)]
        [Route("servers")]
        public async Task<IActionResult> GetServers()
        {
            return new JsonResult((await _repo.GetAllAsync())
                .Select(a => new ServerSelectEntry {ServerId = a.ServerId, ServerName = a.ServerName})
                .ToList());
        }

        [HttpGet]
        [ResponseCache(Duration = 60 * 60)]
        [Route("sevendays")]
        public async Task<IActionResult> GetSevenDays()
        {
            var servers = await _repo.GetAllAsync(a => a.EntryTime >= DateTime.Now.AddDays(-7));
            var result = new List<ServerEntryDto>();
            foreach (var serverEntry in servers)
            {
                if (serverEntry.History.Any(a => a.UserCount != -1))
                {
                    var model = new ServerEntryResult(serverEntry);
                    var historyResult = new List<ServerHistoryResult>();
                    var daysRemaining = 0;
                    while (daysRemaining <= 7)
                    {
                        var currentTime = DateTime.Now.AddHours(-daysRemaining);
                        var results = serverEntry.History.Where(a =>
                            a.EntryTime.Day == currentTime.Day &&
                            a.EntryTime.Month == currentTime.Month &&
                            a.EntryTime.Year == currentTime.Year).ToList();
                        if (results.Count == 0)
                        {
                            daysRemaining++;
                            continue;
                        }

                        var totalOnline = results.Count(a => a.Online);
                        var totalUsersInHour = results.Sum(a => a.UserCount);
                        var totalAverageUsers = totalUsersInHour / results.Count;
                        historyResult.Add(new ServerHistoryResult
                        {
                            EntryTime = currentTime,
                            UserCount = totalAverageUsers,
                            Online = totalOnline >= results.Count / 2
                        });
                        daysRemaining++;
                    }
                    model.ServerHistory =
                        historyResult.OrderBy(a => a.EntryTime).Select(a => new ServerHistoryResult
                        {
                            EntryTime = a.EntryTime,
                            Online = a.Online,
                            UserCount = a.UserCount
                        }).ToList();
                    result.Add(new ServerEntryDto(model));
                }
            }
            return new JsonResult(result);
        }

        [HttpGet]
        [ResponseCache(Duration = 60 * 60)]
        [Route("onemonth")]
        public async Task<IActionResult> GetOneMonth()
        {
            var servers = await _repo.GetAllAsync(a => a.EntryTime >= DateTime.Now.AddMonths(-1));
            var result = new List<ServerEntryDto>();
            foreach (var serverEntry in servers)
            {
                if (serverEntry.History.Any(a => a.UserCount != -1))
                {
                    var model = new ServerEntryResult(serverEntry);
                    var historyResult = new List<ServerHistoryResult>();
                    var daysRemaining = 0;
                    while (daysRemaining <= 30)
                    {
                        var currentTime = DateTime.Now.AddHours(-daysRemaining);
                        var results = serverEntry.History.Where(a =>
                            a.EntryTime.Day == currentTime.Day &&
                            a.EntryTime.Month == currentTime.Month &&
                            a.EntryTime.Year == currentTime.Year).ToList();
                        if (results.Count == 0)
                        {
                            daysRemaining++;
                            continue;
                        }

                        var totalOnline = results.Count(a => a.Online);
                        var totalUsersInHour = results.Sum(a => a.UserCount);
                        var totalAverageUsers = totalUsersInHour / results.Count;
                        historyResult.Add(new ServerHistoryResult
                        {
                            EntryTime = currentTime,
                            UserCount = totalAverageUsers,
                            Online = totalOnline >= results.Count / 2
                        });
                        daysRemaining++;
                    }
                    model.ServerHistory =
                        historyResult.OrderBy(a => a.EntryTime).Select(a => new ServerHistoryResult
                        {
                            EntryTime = a.EntryTime,
                            Online = a.Online,
                            UserCount = a.UserCount
                        }).ToList();
                    result.Add(new ServerEntryDto(model));
                }
            }
            return new JsonResult(result);
        }

        [HttpGet]
        [ResponseCache(Duration = 60 * 60)]
        [Route("oneday")]
        public async Task<IActionResult> GetOneDay()
        {
            var servers = await _repo.GetAllAsync(a => a.EntryTime >= DateTime.Now.AddDays(-1));
            var result = new List<ServerEntryDto>();
            foreach (var serverEntry in servers)
            {
                if (serverEntry.History.Any(a => a.UserCount != -1))
                {
                    var model = new ServerEntryResult(serverEntry);
                    var historyResult = new List<ServerHistoryResult>();

                    var hoursRemaining = 0;
                    while (hoursRemaining <= 24)
                    {
                        var currentTime = DateTime.Now.AddHours(-hoursRemaining);
                        var results = serverEntry.History.Where(a =>
                            a.EntryTime.Day == currentTime.Day &&
                            a.EntryTime.Month == currentTime.Month &&
                            a.EntryTime.Hour == currentTime.Hour &&
                            a.EntryTime.Year == currentTime.Year).ToList();
                        if (results.Count == 0)
                        {
                            hoursRemaining++;
                            continue;
                        }

                        var totalOnline = results.Count(a => a.Online);
                        var totalUsersInHour = results.Sum(a => a.UserCount);
                        var totalAverageUsers = totalUsersInHour / results.Count;
                        historyResult.Add(new ServerHistoryResult
                        {
                            EntryTime = currentTime,
                            UserCount = totalAverageUsers,
                            Online = totalOnline >= results.Count / 2
                        });
                        hoursRemaining++;
                    }

                    model.ServerHistory =
                        historyResult.OrderBy(a => a.EntryTime).Select(a => new ServerHistoryResult
                        {
                            EntryTime = a.EntryTime,
                            Online = a.Online,
                            UserCount = a.UserCount
                        }).ToList();
                    result.Add(new ServerEntryDto(model));
                }
            }

            return new JsonResult(result);
        }

        [HttpGet]
        [Route("getcolour/{serverId}")]
        public async Task<IActionResult> GetColour(int serverId)
        {
            var server = await _repo.FindById(serverId);
            return server == null ? new JsonResult(false) : new JsonResult(new {Color = server.RgbColor});
        }

        [HttpPost]
        [Route("set-colour/{serverId}")]
        public async Task<IActionResult> SetColourById(int serverId, [FromBody] ColorSelection colorSelection)
        {
            var server = await _repo.FindById(serverId);
            if (server == null) return new JsonResult(false);
            server.RgbColor = colorSelection.Color;
            await _repo.UpdateRgbColour(server);
            return new JsonResult(true);
        }

        internal class ServerSelectEntry
        {
            public int ServerId { get; set; }
            public string ServerName { get; set; }
        }
        internal class ServerEntryResult
        {
            public string ServerName { get; set; }
            public string RgbColour { get; set; }
            
            public List<ServerHistoryResult> ServerHistory { get; set; } = new List<ServerHistoryResult>();

            public ServerEntryResult(ServerEntry entry)
            {
                ServerName = entry.ServerName;
                RgbColour = entry.RgbColor;
            }
        }

        internal class ServerHistoryResult
        {
            public DateTime EntryTime { get; set; }
            public bool Online { get; set; }
            public int UserCount { get; set; }

            public ServerHistoryResult()
            {
                
            }
            public ServerHistoryResult(ServerEntryStatusHistory history)
            {
                EntryTime = history.EntryTime;
                Online = history.Online;
                UserCount = history.UserCount;
            }
        }

        internal class ServerEntryDto
        {
            public string ServerName { get; set; }
            public string RgbColour { get; set; }
            public List<ServerHistoryDto> ServerHistory { get; set; } = new List<ServerHistoryDto>();

            public ServerEntryDto(ServerEntryResult result, byte timeSelected = 0 )
            {
                ServerName = result.ServerName;
                RgbColour = result.RgbColour;
                ServerHistory = result.ServerHistory.Select(a => new ServerHistoryDto(a, timeSelected)).ToList();
            }
        }

        internal class ServerHistoryDto
        {
            public string Time { get; set; }
            public int UserCount { get; set; }
            public bool Online { get; set; }
            public ServerHistoryDto(ServerHistoryResult entry, byte timeSelected)
            {
                Time = timeSelected == 0 ? entry.EntryTime.ToString("dd/MM/yy HH:mm") : timeSelected == 1 ? entry.EntryTime.ToString("(ddd) d MMM yy") : "d MMM yy";
                UserCount = entry.UserCount;
                Online = entry.Online;
            }
        }

        public class ColorSelection
        {
            public string Color { get; set; }
        }
    }
}