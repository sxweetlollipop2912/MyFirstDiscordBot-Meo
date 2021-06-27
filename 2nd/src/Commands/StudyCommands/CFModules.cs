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
            Description = "***>> List of upcoming contests:***",
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

        await DiscordWrapper.SendMessage(Context, embed: embed.Build());
    }



    [Command("cf user", RunMode = RunMode.Async)]
    [Summary("Print rank, rating, maxRating, lastOnlineTime of the user")]
    public async Task UserInfo([Remainder]string handle)
    {
        var userRequest = new UserInfoRequest();
        var user = await userRequest.GetUserInfoAsync(handle);

        var LOTime = DateTimeOffset.FromUnixTimeSeconds(user.LastOnlineTimeSeconds).AddHours(7);
        string LODate = $"{DateTimeFormatInfo.CurrentInfo.GetAbbreviatedMonthName(LOTime.Month)}/{LOTime.Day}/{LOTime.Year} at {LOTime.Hour}:{((LOTime.Minute < 10) ? $"0{LOTime.Minute}" : $"{LOTime.Minute}")} UTC+7";

        var embed = new EmbedBuilder
        {
            Color = RankColor(user.Rank)
        };
        embed.AddField(user.Handle,
                       $"Rank: {user.Rank}\nRating: {user.Rating}\nMax Rating: {user.MaxRating}\nLast Online: {LODate}");

        await DiscordWrapper.SendMessage(Context, embed: embed.Build());
    }


    
    private Color RankColor(string Rank)
    {
        Rank = Rank.ToLower();

        if (Rank == "newbie")
            return Color.LightGrey;
        if (Rank == "pupil")
            return Color.Green;
        if (Rank == "specialist")
            return new Color(0x00FFFF); //Cyan
        if (Rank == "expert")
            return Color.Blue;
        if (Rank == "candidate master")
            return Color.Purple;
        if (Rank == "master")
            return Color.LightOrange;
        if (Rank == "international master")
            return Color.Orange;
        if (Rank == "grandmaster")
            return Color.DarkRed;
        if (Rank == "international grandmaster")
            return Color.DarkRed;
        if (Rank == "legendary grandmaster")
            return Color.Red;
        // Headquarters
        return Color.DarkerGrey;
    }
}