using System;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;

using Newtonsoft.Json;

using Discord;
using Discord.Commands;
using Discord.Rest;
using Discord.WebSocket;
using Discord.Addons.Interactive;

using Microsoft.Extensions.Configuration;

public class PuzzleModule : InteractiveBase
{
    readonly IConfigurationRoot _config;
    readonly DiscordSocketClient _client;

    public PuzzleModule (IConfigurationRoot config, DiscordSocketClient client)
    {
        _config = config;
        _client = client;
    }
    static string log = "[puzzle]";

    private class Puzzle
    {
        public static int Size = 3;
        private static string[] Icon3 = new string[9];
        private static string[] Icon4 = new string[12];
        public static string IconFull;
        public static string ArrowUp;
        public static string ArrowDown;
        public static string ArrowLeft;
        public static string ArrowRight;
        private static string EmptySqrStr = ":black_large_square:";

        public ulong PlayerId;
        public string[,] Board = new string[4, 4];
        public int MoveCount;
        public int EmptySqrPos;
        public bool IsOver = false;


        public int Conv(int x, int y)
        {
            return x * Size + y;
        }
        public int Row(int Idx)
        {
            return Idx / Size;
        }
        public int Col(int Idx)
        {
            return Idx % Size;
        }

        public bool IsCorrect()
        {
            for (var i = 0; i < Size; i++) for (var j = 0; j < Size; j++)
                {
                    if (Board[i, j] != (Size == 3 ? Icon3[Conv(i, j)] : Icon4[Conv(i, j)]))
                        return false;
                }
            IsOver = true;
            return true;
        }
        public bool IsValidMove(int StPos, int EnPos)
        {
            if (StPos < 0 || StPos >= Size * Size || EnPos < 0 || EnPos >= Size * Size)
                return false;
            if (Board[Row(EnPos), Col(EnPos)] != EmptySqrStr)
                return false;
            if ((EnPos - StPos) * (EnPos - StPos) == (Size * Size))
                return true;
            if (((EnPos - StPos) * (EnPos - StPos)) == 1 && (Row(StPos) == Row(EnPos)))
                return true;
            return false;
        }
        public bool Move(int StPos, int EnPos)
        {
            if (!IsValidMove(StPos, EnPos))
                return false;

            Board[Row(EnPos), Col(EnPos)] = Board[Row(StPos), Col(StPos)];
            Board[Row(StPos), Col(StPos)] = EmptySqrStr;
            EmptySqrPos = StPos;
            ++MoveCount;
            //Console.WriteLine("valid");
            IsCorrect();
            return true;
        }


        public Puzzle(ulong Id)
        {
            PlayerId = Id;

            Icon3[0] = "<:goat_1:727535909008048211>";
            Icon3[1] = "<:goat_2:727536155356168272>";
            Icon3[2] = "<:goat_3:727536155184070738>";
            Icon3[3] = "<:goat_4:727536155037401159>";
            Icon3[4] = "<:goat_5:727536155737849966>";
            Icon3[5] = "<:goat_6:727536155507294289>";
            Icon3[6] = EmptySqrStr;
            Icon3[7] = "<:goat_8:727536155117092885>";
            Icon3[8] = "<:goat_9:727536155620540416>";
            IconFull = "<:goat_full:727537880154767400>";

            ArrowUp = "\u2B06\uFE0F";
            ArrowDown = "\u2B07\uFE0F";
            ArrowLeft = "\u2B05\uFE0F";
            ArrowRight = "\u27A1\uFE0F";

            do
            {
                var set = new HashSet<int>();
                var rand = new Random();
                for (var i = 0; i < Size; i++) for (var j = 0; j < Size; j++)
                    {
                        int num;
                        do
                        {
                            num = rand.Next(0, Size * Size);
                        } while (set.Contains(num));
                        set.Add(num);
                        Board[i, j] = Size == 3 ? Icon3[num] : Icon4[num];

                        if (Board[i, j] == EmptySqrStr)
                            EmptySqrPos = Conv(i, j);
                    }
            } while (IsCorrect());

            MoveCount = 0;
        }
    }


    public async Task<IEnumerable<IMessage>> Print(ISocketMessageChannel Channel = null)
    {
        if (Channel == null) Channel = Context.Channel;
        Puzzle puzzle = Load();

        if (puzzle.IsOver)
        {
            var embed = new EmbedBuilder
            {
                Description = $"*{ MentionUtils.MentionUser(puzzle.PlayerId) }* đã kết thúc trò chơi trong ***{puzzle.MoveCount}*** bước! :partying_face::tada::confetti_ball:",
                Color = Color.Red
            };
            await DiscordWrapper.SendMessage(Context, embed: embed.Build(), Channel: Channel);
        }
        else
        {
            var embed = new EmbedBuilder
            {
                Description = $"*Người chơi: { MentionUtils.MentionUser(puzzle.PlayerId) }*\nSố bước đã đi: {puzzle.MoveCount}.",
                Color = Color.Magenta
            };
            await DiscordWrapper.SendMessage(Context, embed: embed.Build(), Channel: Channel);
        }

        string output = "";
        for (var i = 0; i < Puzzle.Size; i++)
        {
            for (var j = 0; j < Puzzle.Size; j++)
            {
                output += $"{puzzle.Board[i, j]} ";
            }
            if (i == 0)
                output += $"{Puzzle.IconFull}\n";
            else output += ":black_large_square:\n";
        }
        await Channel.SendMessageAsync(output);
        return await Channel.GetMessagesAsync(2).FlattenAsync();
    }


    public async Task<int> WaitForMove(IMessage msg) // return StPos, -1 if no reply
    {
        var emo_up    = new Emoji(Puzzle.ArrowUp);
        var emo_down  = new Emoji(Puzzle.ArrowDown);
        var emo_left  = new Emoji(Puzzle.ArrowLeft);
        var emo_right = new Emoji(Puzzle.ArrowRight);
        var emo_pause = new Emoji("\u23F8\uFE0F");

        Puzzle puzzle = Load();

        int EnPos = puzzle.EmptySqrPos;
        //up
        if (puzzle.IsValidMove(EnPos + Puzzle.Size, EnPos))
        {
            await msg.AddReactionAsync(emo_up);
        }
        //down
        if (puzzle.IsValidMove(EnPos - Puzzle.Size, EnPos))
        {
            await msg.AddReactionAsync(emo_down);
        }
        //left
        if (puzzle.IsValidMove(EnPos + 1, EnPos))
        {
            await msg.AddReactionAsync(emo_left);
        }
        //right
        if (puzzle.IsValidMove(EnPos -1, EnPos))
        {
            await msg.AddReactionAsync(emo_right);
        }
        //pause
        await msg.AddReactionAsync(emo_pause);

        Stopwatch s = new Stopwatch();
        s.Start();
        while (s.Elapsed < TimeSpan.FromMinutes(3))
        {
            //up
            var reactedUsers = await msg.GetReactionUsersAsync(emo_up, 5).FlattenAsync();
            foreach(var user in reactedUsers)
            {
                if (user.Id == puzzle.PlayerId)
                    return puzzle.EmptySqrPos + Puzzle.Size;
            }

            //down
            reactedUsers = await msg.GetReactionUsersAsync(emo_down, 5).FlattenAsync();
            foreach (var user in reactedUsers)
            {
                if (user.Id == puzzle.PlayerId)
                    return puzzle.EmptySqrPos - Puzzle.Size;
            }

            //left
            reactedUsers = await msg.GetReactionUsersAsync(emo_left, 5).FlattenAsync();
            foreach (var user in reactedUsers)
            {
                if (user.Id == puzzle.PlayerId)
                    return puzzle.EmptySqrPos + 1;
            }

            //right
            reactedUsers = await msg.GetReactionUsersAsync(emo_right, 5).FlattenAsync();
            foreach (var user in reactedUsers)
            {
                if (user.Id == puzzle.PlayerId)
                    return puzzle.EmptySqrPos - 1;
            }

            //pause
            reactedUsers = await msg.GetReactionUsersAsync(emo_pause, 5).FlattenAsync();
            foreach (var user in reactedUsers)
            {
                if (user.Id == puzzle.PlayerId)
                    return -1;
            }

            //await Task.Delay(TimeSpan.FromMilliseconds(70));
        }
        s.Stop();

        return -1;
    }


    private async Task<bool> Host()
    {
        try
        {
            IEnumerable<IMessage> print;
            Puzzle puzzle;

            while (true)
            {
                puzzle = Load();

                print = await Print();
                IMessage msg = print.First();

                int EnPos = puzzle.EmptySqrPos, StPos = await WaitForMove(msg);
                //Console.WriteLine($"move {StPos}");

                if (StPos == -1)
                {
                    break;
                }
                else if (!puzzle.Move(StPos, EnPos))
                {
                    await Context.Channel.TriggerTypingAsync();
                    await ReplyAndDeleteAsync(content: $"{MentionUtils.MentionUser(puzzle.PlayerId)}, có lỗi xảy ra. Bạn vui lòng thử lại.", timeout: TimeSpan.FromSeconds(5));
                }
                else
                {
                    Save(ref puzzle);
                    if (puzzle.IsOver)
                    {
                        await Print();
                        break;
                    }
                }

                await (Context.Channel as ITextChannel).DeleteMessagesAsync(print);
            }

            puzzle = Load();
            return puzzle.IsOver;
        }
        catch (Exception e)
        {
            Console.WriteLine($"{log} {e.ToString()}");
            return false;
        }
    }


    [Command("puzzle new", RunMode = RunMode.Async)]
    public async Task New()
    {
        ulong UserId = Context.Message.Author.Id;
        await DiscordWrapper.SendMessage(Context, $"{MentionUtils.MentionUser(UserId)}, bạn có chắc muốn tạo trò chơi mới? (Yes/No)");

        string content = null;
        SocketMessage msg;

        Stopwatch s = new Stopwatch();
        s.Start();
        do
        {
            msg = await NextMessageAsync(fromSourceUser: true, inSourceChannel: true, timeout: TimeSpan.FromMinutes(5));
            if (msg == null)
            {
                break;
            }

            content = msg.Content.ToLower();
            if (content == "yes" || content == "no")
            {
                break;
            }
        } while (s.Elapsed < TimeSpan.FromMinutes(5));

        if (msg == null)
        {
            await DiscordWrapper.SendMessage(Context, $"Mèo không nhận được phản hồi của {MentionUtils.MentionUser(UserId)}... :cry:");
            return;
        }
        else if (content == "no")
        {
            await DiscordWrapper.SendMessage(Context, $"Trò chơi mới sẽ không được tạo.");
            await Task.Delay(TimeSpan.FromSeconds(5));
            await (Context.Channel as ITextChannel).DeleteMessagesAsync(await Context.Channel.GetMessagesAsync(4).FlattenAsync());
            return;
        }
        await (Context.Channel as ITextChannel).DeleteMessagesAsync(await Context.Channel.GetMessagesAsync(2).FlattenAsync());

        Puzzle puzzle = new Puzzle(UserId);
        Save(ref puzzle);

        DiscordWrapper.Log($"puzzle new | {Context.Message.Author.Username} ({Context.Message.Author.Id})");

        if (!(await Host()))
        {
            var embed = new EmbedBuilder
            {
                Description = $"Trò chơi của {MentionUtils.MentionUser(puzzle.PlayerId) } đã tạm dừng.",
                Color = Color.Blue
            };
            await DiscordWrapper.SendMessage(Context, embed: embed.Build());
        }
    }


    [Command("puzzle continue", RunMode = RunMode.Async)]
    public async Task Continue()
    {
        var puzzle = Load();
        if (puzzle.IsOver)
        {
            var embed = new EmbedBuilder
            {
                Description = "Không có trò chơi nào chưa hoàn thành.",
                Color = Color.Blue
            };
            await Context.Channel.TriggerTypingAsync();
            await ReplyAndDeleteAsync(content: null, embed: embed.Build(), timeout: TimeSpan.FromSeconds(5));
        }
        else if (!(await Host()))
        {
            var embed = new EmbedBuilder
            {
                Description = $"Trò chơi của {MentionUtils.MentionUser(puzzle.PlayerId) } đã tạm dừng.",
                Color = Color.Blue
            };
            await DiscordWrapper.SendMessage(Context, embed: embed.Build());
        }
    }


    [Command("puzzle print")]
    public async Task PrintCmd()
    {
        Puzzle puzzle = Load();
        if (puzzle == null)
        {
            var embed = new EmbedBuilder
            {
                Description = "Không có trò chơi nào đang được lưu.",
                Color = Color.Blue
            };
            await Context.Channel.TriggerTypingAsync();
            await ReplyAndDeleteAsync(content: null, embed: embed.Build(), timeout: TimeSpan.FromSeconds(5));

            return;
        }
        await Print();
    }


    private bool Save(ref Puzzle puzzle)
    {
        try
        {
            File.WriteAllText(_config.GetValue<string>("json:Puzzle.json"), JsonConvert.SerializeObject(puzzle));
            return true;
        }
        catch (Exception e)
        {
            Console.WriteLine($"{log} {e.ToString()}");
            return false;
        }
    }

    private Puzzle Load()
    {
        return JsonConvert.DeserializeObject<Puzzle>(File.ReadAllText(_config.GetValue<string>("json:Puzzle.json")));
    }
}