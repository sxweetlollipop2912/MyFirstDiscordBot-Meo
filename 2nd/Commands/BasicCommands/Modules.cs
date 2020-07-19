using System;
using System.Threading.Tasks;
using System.Management;

using Discord;
using Discord.Commands;
using Discord.Rest;
using Discord.WebSocket;

using Microsoft.Extensions.Configuration;

public class BasicModule : ModuleBase<SocketCommandContext>
{
    readonly IConfigurationRoot _config;
    readonly DiscordSocketClient _client;

    public BasicModule(IConfigurationRoot config, DiscordSocketClient client)
    {
        _config = config;
        _client = client;
    }


    [Command("wait", RunMode = RunMode.Async)]
    public async Task Wait(int s, [Remainder]string text)
    {
        await Task.Delay(TimeSpan.FromMilliseconds(Math.Max(0, s * 1000 - 1900)));
        await SendMessage($"*{Context.Message.Author.Mention}:*\n{text}");
    }


    [Command("remind")]
    [Summary("params: h, m, s")]
    public async Task Remind(int h, int m, int s, [Remainder]string text)
    {
        await Context.Message.DeleteAsync();
        await LogDiscord
            ($"Lời nhắc của {Context.Message.Author.Username} ({Context.Message.Author.Id}) ở guild {Context.Guild.Name} ({h}:{m}:{s} / {text})");

        try
        {
            WqlEventQuery query = new WqlEventQuery
               ("__InstanceModificationEvent", new TimeSpan(0, 0, 6),
               $"TargetInstance isa 'Win32_LocalTime' AND TargetInstance.Hour={h} AND TargetInstance.Minute={m} AND TargetInstance.Second={s}");

            ManagementEventWatcher watcher = new ManagementEventWatcher(query);
            watcher.EventArrived += async (object sender, EventArrivedEventArgs e) =>
            {
                await SendMessage($"*Đây là lời nhắc dành cho {Context.Message.Author.Mention}:*\n{text}");
            };
            watcher.Start();
        }
        catch (Exception e)
        {
            Console.WriteLine($"[log] ScheduledTask in Module: {e}");
            await LogDiscord($"Lỗi {e} ở lời nhác của {Context.Message.Author.Id} ở guild {Context.Guild.Name} ({h}:{m}:{s} / {text})");

            await SendMessage($"Có lỗi xảy ra. Bạn nhắc lại giúp Mèo thời gian và lời nhắn với, {Context.Message.Author.Mention}.");
        }
    }


    [Command("remind")]
    [Summary("params: h, m")]
    public async Task Remind(int h, int m, [Remainder]string text)
    {
        await Context.Message.DeleteAsync();
        await LogDiscord
            ($"Lời nhắc của {Context.Message.Author.Username} ({Context.Message.Author.Id}) ở guild {Context.Guild.Name} ({h}:{m} / {text})");

        try
        {
            WqlEventQuery query = new WqlEventQuery
               ("__InstanceModificationEvent", new TimeSpan(0, 0, 60),
               $"TargetInstance isa 'Win32_LocalTime' AND TargetInstance.Hour={h} AND TargetInstance.Minute={m} AND TargetInstance.Second=0");

            ManagementEventWatcher watcher = new ManagementEventWatcher(query);
            watcher.EventArrived += async (object sender, EventArrivedEventArgs e) =>
            {
                await SendMessage($"*Đây là lời nhắc dành cho {Context.Message.Author.Mention}:*\n{text}");
            };
            watcher.Start();
        }
        catch (Exception e)
        {
            Console.WriteLine($"[log] ScheduledTask in Module: {e}");
            await LogDiscord($"Lỗi {e} ở lời nhác của {Context.Message.Author.Id} ở guild {Context.Guild.Name} ({h}:{m} / {text})");

            await SendMessage($"Có lỗi xảy ra. Bạn nhắc lại giúp Mèo thời gian và lời nhắn với, {Context.Message.Author.Mention}.");
        }
    }


    [Command("party", RunMode = RunMode.Async)]
    public async Task Party()
    {
        string[] emotes = new string[4];
        emotes[0] = "\uD83D\uDC08";    // cat2
        emotes[1] = "\ud83e\udd73";    // partying_face
        emotes[2] = "\uD83C\uDF89";    // tada
        emotes[3] = "\uD83C\uDF8A";    // confetti_ball

        var msg = await SendMessage(":cat2::partying_face::tada::confetti_ball::confetti_ball::confetti_ball::tada::partying_face::cat2:");

        foreach (var emote in emotes)
        {
            await msg.AddReactionAsync(new Emoji(emote));
        }

    }


    [Command("hi")]
    [Alias("hello", "greetings")]
    [Summary("greetings")]
    public async Task Hi()
    {
        await SendMessage($"Chào {Context.Message.Author.Mention}! Bạn khoẻ chứ? :cat:");
    }


    [Command("meo")]
    [Alias("meoo","meooo","meow")]
    public async Task Meo()
    {
        await SendMessage($"{Context.Message.Author.Mention}, *meow meeeow* <:heart_smile:685414591039012900>");
    }


    [Command("react", RunMode = RunMode.Async)]
    [Summary("react a list of emojis to the message with the specified ID")]
    public async Task React(ulong MsgID, params string[] emotes)
    {
        await Context.Message.DeleteAsync();

        var msg = (RestUserMessage)await Context.Channel.GetMessageAsync(MsgID);
        foreach (var s in emotes)
        {
            Console.WriteLine(s);
            if (!Emote.TryParse(s, out var emote))
                await msg.AddReactionAsync(new Emoji(s));
            else
                await msg.AddReactionAsync(emote);
        }
    }


    [Command("say", RunMode = RunMode.Async)]
    [Alias("speak","talk","repeat","chat")]
    [Summary("repeat the author by sending a text | params: anonymous & timer")]
    public async Task Say(bool anonymous, int wait_sec, [Remainder]string text)
    {
        await Context.Message.DeleteAsync();

        string author = "Ẩn danh";
        if (anonymous == false) author = Context.Message.Author.Mention;
        if (text.StartsWith("||") && text.EndsWith("||")) text = text.Substring(2, text.Length - 4);

        await Task.Delay(TimeSpan.FromMilliseconds(Math.Max(0, wait_sec * 1000 - 1900)));
        await SendMessage($"*Từ {author}:*\n{text}");
    }


    [Command("say")]
    [Alias("speak", "talk", "repeat")]
    [Summary("param: anonymous")]
    public async Task Say(bool anonymous, [Remainder]string text)
    {
        await Context.Message.DeleteAsync();

        string author = "Ẩn danh";
        if (anonymous == false) author = Context.Message.Author.Mention;
        if (text.StartsWith("||") && text.EndsWith("||")) text = text.Substring(2, text.Length - 4);
        await SendMessage($"*Từ {author}:*\n{text}");
    }


    [Command("say", RunMode = RunMode.Async)]
    [Alias("speak", "talk", "repeat")]
    [Summary("param: timer")]
    public async Task Say(int wait_sec, [Remainder]string text)
    {
        await Context.Message.DeleteAsync();

        string author = Context.Message.Author.Mention;
        if (text.StartsWith("||") && text.EndsWith("||")) text = text.Substring(2, text.Length - 4);

        await Task.Delay(TimeSpan.FromMilliseconds(Math.Max(0, wait_sec * 1000 - 1900)));
        await SendMessage($"*Từ {author}:*\n{text}");
    }


    [Command("say")]
    [Alias("speak", "talk", "repeat")]
    [Summary("no param")]
    public async Task Say([Remainder]string text)
    {
        await Context.Message.DeleteAsync();

        string author = Context.Message.Author.Mention;
        if (text.StartsWith("||") && text.EndsWith("||")) text = text.Substring(2, text.Length - 4);
        await SendMessage($"*Từ {author}:*\n{text}");
    }


    [Command("parrot")]
    [Alias("echo")]
    public async Task Parrot([Remainder]string text)
    {
        await Context.Message.DeleteAsync();
        await SendMessage(text);
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