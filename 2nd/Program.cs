using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Reflection;
using System.Net;
using System.Management;

using Discord;
using Discord.WebSocket;
using Discord.Commands;
using Discord.Addons.Interactive;
using Discord.Rest;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;


class Program
{
    private CancellationTokenSource _cts { get; set; }
    private IConfigurationRoot _config;
    private IConfigurationRoot _stuff;

    private DiscordSocketClient _client;
    private CommandService _commands;
    private CommandHandler _handler;
    private IServiceProvider _services;

    static string log_discord = "[discord]";
    static string log = "[log]";

    public static void Main(string[] args)
    {
        new Program().MainAsync(args)
            .GetAwaiter()
            .GetResult();
    }

    public async Task MainAsync(string[] args)
    {
    //******************** Prepare *********************************
        _cts = new CancellationTokenSource();

        Console.WriteLine($"{log_discord} Loading config file...");
        _config = new ConfigurationBuilder()
               .SetBasePath(Directory.GetCurrentDirectory())
               .AddJsonFile("config.json", optional: false, reloadOnChange: true)
               .Build();
        _stuff = new ConfigurationBuilder()
               .SetBasePath(Directory.GetCurrentDirectory())
               .AddJsonFile("Stuff.json", optional: false, reloadOnChange: true)
               .Build();

        Console.WriteLine($"{log_discord} Creating discord client...");
        _client = new DiscordSocketClient();
        _client.Log += Log;
        _commands = new CommandService();

     //********************* Run *********************************
        _services = new ServiceCollection()
            //.AddSingleton(_Cservices)
            .AddSingleton(new AudioServiceFFmpeg())
            .AddSingleton(new AudioServiceNAudio())
            .AddSingleton<InteractiveService>()
            .AddSingleton(_commands)
            .AddSingleton(_cts)
            .AddSingleton(_config)
            .AddSingleton(_client)
            .BuildServiceProvider();

        _handler = new CommandHandler(
            _client,
            _commands,
            _services,
            _config,
            _stuff);
        await _handler.InstallCommandsAsync();

        RunAsync(args).Wait();

    }

    async Task RunAsync(string[] args)
    {
        var token = _config.GetValue<string>("discord:token");

        await _client.LoginAsync(TokenType.Bot, token);
        await _client.StartAsync();

        while (!_cts.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromSeconds(60));
        }
    }

    private Task Log(LogMessage msg)
    {
        Console.WriteLine($"{log} {msg.ToString()}");
        return Task.CompletedTask;
    }


    // ************** Command Handler *****************************************
    public class CommandHandler
    {
        private readonly DiscordSocketClient _client;
        private readonly CommandService _commands;
        private readonly IServiceProvider _services;
        private readonly IConfigurationRoot _config;
        private readonly IConfigurationRoot _stuff;
        private ManagementEventWatcher _watcher12 = null;
        private ReminderModule _reminderModule;

        public CommandHandler(
            DiscordSocketClient client,
            CommandService commands,
            IServiceProvider services,
            IConfigurationRoot config,
            IConfigurationRoot stuff)
        {
            _commands = commands;
            _client = client;
            _services = services;
            _config = config;
            _stuff = stuff;
            _reminderModule = new ReminderModule(_config, _client);
        }
        

        public async Task InstallCommandsAsync()
        {
            _client.Ready += async () =>
            {
                await LogDiscord("Khởi động Bot.");
            };
            _client.Ready += async () =>
            {
                await _client.SetGameAsync($"meoow | {_config.GetValue<string>("discord:CommandPrefix")}help");
            };
            _client.Ready += ScheduledTask;
            //_client.Ready += TaskReminder;
            _client.MessageReceived += HandleCommandAsync;

            await _commands.AddModulesAsync(assembly: Assembly.GetEntryAssembly(),
                                            services: _services);
        }


        private async Task HandleCommandAsync(SocketMessage messageParam)
        {
            var msg = messageParam as SocketUserMessage;
            if (msg == null || msg.Author.Id == _config.GetValue<ulong>("discord:ClientID")) return;

            int argPos = 0;
            var context = new SocketCommandContext(_client, msg);

        // Attachments
            if ((msg.Channel as SocketGuildChannel).Guild.Id == _config.GetValue<ulong>("guild:Lollipop:ID") && 
                msg.Author.Id != _config.GetValue<ulong>("discord:AuthorID"))
            {
                Atachments(context);
            }

        // Prefix, Mention
            if (!(msg.HasCharPrefix(_config.GetValue<string>("discord:CommandPrefix")[0], ref argPos)))
            {
                if (msg.Content.Contains($"<@!{_config.GetValue<ulong>("discord:ClientID")}>") ||
                    msg.Content.Contains($"<@{_config.GetValue<ulong>("discord:ClientID")}>"))
                {
                    Console.WriteLine($"{log_discord} Activity in guild {context.Guild.Name}");

                    await msg.Channel.TriggerTypingAsync();
                    await msg.Channel.SendMessageAsync($"*meo meoo*. Bạn cần gì nè, {msg.Author.Mention}?");
                }
                else if (msg.Content == "&party")
                {
                    Console.WriteLine($"{log_discord} Activity in guild {context.Guild.Name}");
                    Party_with_goat(context);
                }
                return;
            }

        // Execute command
            Console.WriteLine($"{log_discord} Activity in guild {context.Guild.Name}");

            var result = await _commands.ExecuteAsync(
                context: context,
                argPos: argPos,
                services: _services);

            if (!result.IsSuccess)
            {
                if (result.ErrorReason == "Unknown command.")
                {
                    await msg.Channel.TriggerTypingAsync();
                    await msg.Channel.SendMessageAsync($"Có phải bạn vừa gọi Mèo không, {msg.Author.Mention}?");
                }
                Console.WriteLine($"{log} {result.ErrorReason}");
            }
        }


        private void Atachments(SocketCommandContext context)
        {
            var attachments = context.Message.Attachments;
            if (attachments.Count > 0)
            {
                WebClient client = new WebClient();
                foreach (var attachment in attachments)
                    client.DownloadFileAsync(new Uri(attachment.Url), $"{_config.GetValue<string>("config:downloadPath")}{attachment.Filename}");

                Console.WriteLine($"{log_discord} Downloaded {attachments.Count} file(s) from guild {context.Guild.Name}.");
            }
        }


        private async void Party_with_goat(SocketCommandContext context)
        {
            string[] emotes = new string[5];
            emotes[0] = "\uD83D\uDC10";    // goat
            emotes[1] = "\uD83D\uDC08";    // cat2
            emotes[2] = "\ud83e\udd73";    // partying_face
            emotes[3] = "\uD83C\uDF89";    // tada
            emotes[4] = "\uD83C\uDF8A";    // confetti_ball

            await context.Channel.TriggerTypingAsync();
            var msgReply = await context.Channel.SendMessageAsync(":cat2::partying_face::tada::confetti_ball::confetti_ball::confetti_ball::tada::partying_face::cat2:");

            foreach (var emote in emotes)
            {
                await msgReply.AddReactionAsync(new Emoji(emote));
            }
        }


        private Task ScheduledTask()
        {
            /*if (_watcher12 != null) _watcher12.Stop();

            _watcher12 = new ManagementEventWatcher(new WqlEventQuery
               ("__InstanceModificationEvent", new TimeSpan(0, 0, 6),
               "TargetInstance isa 'Win32_LocalTime' AND TargetInstance.Hour=0 AND TargetInstance.Minute=0 AND TargetInstance.Second=0"));

            _watcher12.EventArrived += async (object sender, EventArrivedEventArgs e) =>
            {
                var channel = _client.GetChannel(_config.GetValue<ulong>("guild:Lollipop:lollipop")) as ISocketMessageChannel;

                await channel.TriggerTypingAsync();
                await channel.SendMessageAsync(_stuff.GetValue<string>("pr:12AM"));
            };
            _watcher12.Start();*/

            return Task.CompletedTask;
        }


        private async Task TaskReminder()
        {
            while (true)
            {
                try
                {
                    while (true)
                    {
                        while (_reminderModule._TaskList.isLocked() || _reminderModule._TaskList.Empty())
                        {
                            await Task.Delay(TimeSpan.FromSeconds(5));
                        }
                        var task = _reminderModule.Dequeue_and_Save(locked: false);

                        var channel = _client.GetGuild(task.GuildID).GetTextChannel(task.ChannelID);
                        var author = _client.GetUser(task.AuthorID);

                        if (DateTimeOffset.UtcNow.ToUnixTimeSeconds() + 30 > task.unixTime)
                        {
                            await channel.TriggerTypingAsync();
                            await channel.SendMessageAsync($"*{author.Mention}*~\n{task.Message}");
                            continue;
                        }

                        await Task.Delay(TimeSpan.FromSeconds(DateTimeOffset.UtcNow.ToUnixTimeSeconds() - task.unixTime));
                        await channel.TriggerTypingAsync();
                        await channel.SendMessageAsync($"*{author.Mention}*~\n{task.Message}");
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine($"[TaskReminder] {e.ToString()}");
                }
            }
        }


        private async Task<RestUserMessage> LogDiscord(string log)
        {
            var channel = _client.GetChannel(_config.GetValue<ulong>("guild:Test:log")) as ISocketMessageChannel;

            await channel.TriggerTypingAsync();
            var msg = await channel.SendMessageAsync(log);

            return msg;
        }
    }

    private async Task<RestUserMessage> LogDiscord(string log)
    {
        var channel = _client.GetChannel(_config.GetValue<ulong>("guild:Test:log")) as ISocketMessageChannel;

        await channel.TriggerTypingAsync();
        var msg = await channel.SendMessageAsync(log);

        return msg;
    }
}