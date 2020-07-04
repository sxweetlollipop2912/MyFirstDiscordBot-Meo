using System;
using System.IO;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Microsoft.Extensions.Configuration;


public class AudioModule : ModuleBase<ICommandContext>
{
    private readonly AudioService _service;
    private readonly IConfigurationRoot _config;

    private IConfigurationRoot _songs = new ConfigurationBuilder()
               .SetBasePath(Directory.GetCurrentDirectory())
               .AddJsonFile("songs.json", optional: false, reloadOnChange: true)
               .Build();
    public AudioModule(AudioService service, IConfigurationRoot config)
    {
        _service = service;
        _config = config;
    }


    [Command("join", RunMode = RunMode.Async)]
    [Alias("connect")]
    public async Task JoinCmd()
    {
        var channel = (Context.User as IGuildUser)?.VoiceChannel;
        if (channel == null)
        {
            await Context.Channel.TriggerTypingAsync();
            await Context.Channel.SendMessageAsync("Bạn cần ở trong một kênh thoại!"); 
            return;
        }

        await _service.JoinAudio(Context.Guild, (Context.User as IVoiceState).VoiceChannel);
    }


    [Command("leave", RunMode = RunMode.Async)]
    [Alias("disconnect")]
    public async Task LeaveCmd()
    {
        await _service.LeaveAudio(Context.Guild);
    }


    [Command("play", RunMode = RunMode.Async)]
    [Alias("p")]
    public async Task PlayCmd([Remainder] string file_name)
    {
        if (!_service.isGuildAdded(Context.Guild).Result)
        {
            var channel = (Context.User as IGuildUser)?.VoiceChannel;
            if (channel == null)
            {
                await Context.Channel.TriggerTypingAsync();
                await Context.Channel.SendMessageAsync("Mèo cần ở trong một kênh thoại!");
                return;
            }
            await _service.JoinAudio(Context.Guild, (Context.User as IVoiceState).VoiceChannel);
        }

        string Path = _config.GetValue<string>("config:songPath");
        if (!File.Exists($"{Path}{file_name}"))
        {
            file_name = _songs.GetValue<string>($"filename:{file_name}");
        }
        if (file_name == null)
        {
            await Context.Channel.TriggerTypingAsync();
            await ReplyAsync($"Tên bài hát không hợp lệ.");
            return;
        }

        var metadata = TagLib.File.Create($"{Path}{file_name}");
        var embed = new EmbedBuilder
        {
            Title = "Hiện đang phát",
            Description = $"{metadata.Tag.Title} - {metadata.Tag.FirstPerformer}",
            Color = Color.Blue
        };
        embed.WithFooter(footer => footer.Text = $"[{Context.Message.Author.Username}]")
            .WithCurrentTimestamp();
        var msg = await ReplyAsync(embed: embed.Build());

        await _service.SendAudioAsync(Context.Guild, $"{Path}{file_name}");

        await msg.DeleteAsync();
    }


    [Command("stop")]
    public async Task StopCmd()
    {
        await _service.CancelFfmpeg();
    }


    [Command("playing")] //debug
    public async Task PlayingCmd()
    {
        if (AudioService.Global.streaming == false)
            await ReplyAsync("Not playing.");
        else
            await ReplyAsync("Playing!");
    }


    [Command("connecting")] //debug
    public async Task ConnectingCmd()
    {
        if (_service.connectedVChannel(Context.Guild).Result == null)
            await ReplyAsync("Not connecting.");
        else
            await ReplyAsync("Connected!");
    }


    [Command("killffmpeg")] //debug
    public async Task KillFfmpegCmd()
    {
        await _service.KillFfmpeg();
    }
}