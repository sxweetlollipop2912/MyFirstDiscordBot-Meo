using System;
using System.IO;
using System.Threading.Tasks;
using System.Linq;
using System.Globalization;

using Discord;
using Discord.Commands;
using Discord.Rest;
using Discord.WebSocket;

using cfapi.Objects;
using cfapi.Methods;

using Microsoft.Extensions.Configuration;

public class CFModule : ModuleBase<SocketCommandContext>
{
    readonly IConfigurationRoot _config;
    readonly DiscordSocketClient _client;

    public CFModule(IConfigurationRoot config, DiscordSocketClient client)
    {
        _config = config;
        _client = client;
    }

    
    [Command("cf contest", RunMode = RunMode.Async)]
    [Summary("Print a list of all available contests")]
    public async Task ContestList(int count = 0)
    {
        var contestRequest = new ContestListRequest();
        var contests = await contestRequest.GetContestListAsync(includeGym: false);
        contests.Reverse();
        if (count <= 0) 
            count = contests.Count();

        var embed = new EmbedBuilder
        {
            Description = "***>> List of incoming contests:***",
            Color = Color.Red
        };

        foreach (var contest in contests.Where(i => i.Phase == ContestPhase.BEFORE))
        {
            if (count == 0)
                break;

            var Name = contest.Name;
            var Url = "https://codeforces.com/contests/" + contest.Id.ToString();
            var Type = contest.Type;
            var StartTime = DateTimeOffset.FromUnixTimeSeconds(contest.StartTime).AddHours(7);
            var DurationMin = (int)TimeSpan.FromSeconds(contest.Duration).TotalMinutes;
            var Author = contest.Author;

            string StartDate = $"*{DateTimeFormatInfo.CurrentInfo.GetAbbreviatedMonthName(StartTime.Month)}/{StartTime.Day}/{StartTime.Year}* at {StartTime.Hour}:{((StartTime.Minute < 10) ? $"0{StartTime.Minute}" : $"{StartTime.Minute}")} UTC+7";
            string Duration = $"{(int)(DurationMin / 60)}h " + ((DurationMin % 60 < 10) ? $"0{DurationMin % 60}m" : $"{DurationMin % 60}m");

            embed.AddField($"{StartDate}",
                           $"[{Name}]({Url})\nLength: {Duration}\nAuthor: {Author}");
            --count;
        }

        await SendMessage(embed: embed.Build());
    }


    private async Task<RestUserMessage> LogDiscord(string log)
    {
        var channel = _client.GetChannel(_config.GetValue<ulong>("guild:Test:log")) as ISocketMessageChannel;
        return await SendMessage(content: log, Channel: channel);
    }

    private async Task<RestUserMessage> SendMessage(string content = null, Embed embed = null, ISocketMessageChannel Channel = null)
    {
        if (Channel == null) Channel = Context.Channel;
        await Channel.TriggerTypingAsync();
        return await Channel.SendMessageAsync(text: content, embed: embed);
    }
}