using System;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;

using Newtonsoft.Json;

using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Discord.Addons.Interactive;

using Microsoft.Extensions.Configuration;

public class ChessModule : InteractiveBase
{
    readonly IConfigurationRoot _config;

    public ChessModule(IConfigurationRoot config, DiscordSocketClient client)
    {
        _config = config;
        _client = client;
    }
    static string log = "[chess]";


    private class ChessBoard
    {
        public ulong[] Player = new ulong[2];
        public static int WhitePlayerIndex = 0;
        public static int BlackPlayerIndex = 1;
        public int CurrentPlayerIndex = WhitePlayerIndex;
        public bool IsWhitePlayer(ulong Id)
        {
            return Player[WhitePlayerIndex] == Id;
        }
        public bool IsBlackPlayer(ulong Id)
        {
            return Player[BlackPlayerIndex] == Id;
        }
        public int PlayerIndex(ulong Id)
        {
            return IsWhitePlayer(Id) ? WhitePlayerIndex : IsBlackPlayer(Id) ? BlackPlayerIndex : -1;
        }

        public static int King = 0;
        public static int Queen = 1;
        public static int Rook = 2;
        public static int Bishop = 3;
        public static int Knight = 4;
        public static int Pawn = 5;
        public static int Empty = -1;
        public static int BlackRank = 7;
        public static int WhiteRank = 0;

        public bool IsOver = false;
        public bool IsDraw = false;
        public bool IsStaleMate = false;
        public bool IsCheckMate = false;
        public ulong WinnerId = 0;
        public void SetGameResult(bool HasNoMove, ulong Winner_Id = 0)
        {
            IsOver = true;
            if (Winner_Id == 0)
            {
                IsDraw = true;
                IsStaleMate = HasNoMove;
            }
            else
            {
                WinnerId = Winner_Id;
                IsCheckMate = HasNoMove;
            }
        }

        public static bool IsBlack(int Pawn)
        {
            return Pawn > 5;
        }
        public static bool IsWhite(int Pawn)
        {
            return Pawn != Empty && !IsBlack(Pawn);
        }
        public bool IsCheck(int PlayerIndex)
        {
            return Control[PlayerIndex ^ 1, KingPos[PlayerIndex, 0], KingPos[PlayerIndex, 1]] != 0;
        }

        public static string Convert(int x1, int y1, int x2, int y2)
        {
            //return $"{(char)(y1 + 'a')}{(char)(x1 + '1')} {(char)(y2 + 'a')}{(char)(x2 + '1')}";
            char[] CharArray = { (char)(y1 + 'a'), (char)(x1 + '1'), ' ', (char)(y2 + 'a'), (char)(x2 + '1') };
            return new string(CharArray);
        }

        public static string[] Icon = new string[12];
        public int[,] Board = new int[8, 8];
        public int[,,] Control = new int[2, 8, 8];
        public int[,,] Step = new int[2, 8, 8];
        public int[,] KingPos = new int[2, 2];

        public List<Tuple<int, string>> History = new List<Tuple<int, string>>();

        public ChessBoard()
        {
            Icon[King] = "<:yellow_king:720576105379135488>";     //king
            Icon[Queen] = "<:yellow_Queen:720578287725969410>";   //queen
            Icon[Rook] = "<:yellow_Rook:720577208762105896>";     //rook
            Icon[Bishop] = "<:yellow_Bishop:720577059444752386>"; //bishop
            Icon[Knight] = "<:yellow_knight:720573503778062356>"; //knight
            Icon[Pawn] = "<:yellow_pawn:720577535947047004>";     //pawn
            Icon[King + 6] = "<:red_king:720581903463481437>";    // black, +6
            Icon[Queen + 6] = "<:red_Queen:720581902859239475>";
            Icon[Rook + 6] = "<:red_Rook:720581902775353385>";
            Icon[Bishop + 6] = "<:red_Bishop:720581902775484438>";
            Icon[Knight + 6] = "<:red_knight:720581903161360395>";
            Icon[Pawn + 6] = "<:red_pawn:720581903006040184>";

            for (var i = 0; i < 8; i++) for (var j = 0; j < 8; j++) 
                    Board[i, j] = Empty;
            Board[BlackRank, 0] = Board[BlackRank, 7] = Rook + 6;    Board[WhiteRank, 0] = Board[WhiteRank, 7] = Rook;
            Board[BlackRank, 1] = Board[BlackRank, 6] = Knight + 6;  Board[WhiteRank, 1] = Board[WhiteRank, 6] = Knight;
            Board[BlackRank, 2] = Board[BlackRank, 5] = Bishop + 6;  Board[WhiteRank, 2] = Board[WhiteRank, 5] = Bishop;
            Board[BlackRank, 3] = Queen + 6;                         Board[WhiteRank, 3] = Queen;
            Board[BlackRank, 4] = King + 6;                          Board[WhiteRank, 4] = King;
            for (var i = 0; i < 8; i++)
            {
                Board[BlackRank - 1, i] = Pawn + 6;
                Board[WhiteRank + 1, i] = Pawn;
            }

            for (var i = 0; i < 8; i++) for (var j = 0; j < 8; j++)
                {
                    Control[0, i, j] = Control[1, i, j] = 0;
                    Step[0, i, j] = Step[1, i, j] = 0;
                }

            KingPos[WhitePlayerIndex, 0] = WhiteRank;
            KingPos[BlackPlayerIndex, 0] = BlackRank;
            KingPos[WhitePlayerIndex, 1] = KingPos[BlackPlayerIndex, 1] = 4;
        }
    }


    public async Task<IEnumerable<IMessage>> Print(ISocketMessageChannel Channel = null)
    {
        if (Channel == null) Channel = Context.Channel;
        ChessBoard chess = Load();

        string[] sqr = { ":white_large_square:", ":black_large_square:" };
        string[] numbers = { ":one:", ":two:", ":three:", ":four:", ":five:", ":six:", ":seven:", ":eight:" };
        string[] letters =
            { ":regional_indicator_a:", ":regional_indicator_b:", ":regional_indicator_c:", ":regional_indicator_d:", ":regional_indicator_e:", ":regional_indicator_f:", ":regional_indicator_g:", ":regional_indicator_h:" };

        if (!chess.IsOver)
        {
            var embed = new EmbedBuilder
            {
                Description = ((chess.IsCheck(chess.CurrentPlayerIndex)) ? "**Check!**\n":"") + 
                              $"*Đang chơi:* {MentionUtils.MentionUser(chess.Player[ChessBoard.WhitePlayerIndex])} *(trắng)* vs. " +
                              $"{MentionUtils.MentionUser(chess.Player[ChessBoard.BlackPlayerIndex])} *(đen)*",
                Color = Color.Magenta
            };
            embed.AddField("*Người chơi hiện tại:*",
                $"{MentionUtils.MentionUser(chess.Player[chess.CurrentPlayerIndex])}, {(chess.CurrentPlayerIndex == ChessBoard.WhitePlayerIndex ? "trắng" : "đen")}.")
                .AddField("*Nước đi trước đó:*",
                (chess.History.Count != 0) ? $"{ChessBoard.Icon[chess.History.Last().Item1]} {chess.History.Last().Item2}." : "Chưa có.");

            //await Channel.SendMessageAsync(embed: embed.Build());
            await DiscordWrapper.SendMessage(Context, embed: embed.Build(), Channel: Channel);
        }
        else
        {
            if (chess.IsStaleMate)
            {
                var embed = new EmbedBuilder
                {
                    Description = "**Stalemate!**\n" +
                    $"Trận đấu giữa { MentionUtils.MentionUser(chess.Player[ChessBoard.WhitePlayerIndex]) } *(trắng)* và { MentionUtils.MentionUser(chess.Player[ChessBoard.BlackPlayerIndex]) } *(đen)* kết thúc với kết quả * Hoà *.",
                    Color = Color.Orange
                };
                //await Channel.SendMessageAsync(embed: embed.Build());
                await DiscordWrapper.SendMessage(Context, embed: embed.Build(), Channel: Channel);
            }
            else if (chess.IsDraw)
            {
                var embed = new EmbedBuilder
                {
                    Description = $"Trận đấu giữa { MentionUtils.MentionUser(chess.Player[ChessBoard.WhitePlayerIndex]) } *(trắng)* và { MentionUtils.MentionUser(chess.Player[ChessBoard.BlackPlayerIndex]) } *(đen)* kết thúc với kết quả *Hoà*.",
                    Color = Color.Orange
                };
                //await Channel.SendMessageAsync(embed: embed.Build());
                await DiscordWrapper.SendMessage(Context, embed: embed.Build(), Channel: Channel);
            }
            else if (chess.IsCheckMate)
            {
                var embed = new EmbedBuilder
                {
                    Description = "**Checkmate!**\n" +
                            $"Trận đấu giữa {MentionUtils.MentionUser(chess.Player[ChessBoard.WhitePlayerIndex])} *(trắng)* và {MentionUtils.MentionUser(chess.Player[ChessBoard.BlackPlayerIndex])} *(đen)* kết thúc với kết quả: {MentionUtils.MentionUser(chess.WinnerId)} *thắng*!",
                    Color = Color.Red
                };
                //await ReplyAsync(embed: embed.Build());
                await DiscordWrapper.SendMessage(Context, embed: embed.Build());
            }
            else
            {
                var embed = new EmbedBuilder
                {
                    Description = $"Trận đấu giữa {MentionUtils.MentionUser(chess.Player[ChessBoard.WhitePlayerIndex])} và {MentionUtils.MentionUser(chess.Player[ChessBoard.BlackPlayerIndex])} kết thúc với kết quả: {MentionUtils.MentionUser(chess.WinnerId)} *thắng*!",
                    Color = Color.Red
                };
                //await ReplyAsync(embed: embed.Build());
                await DiscordWrapper.SendMessage(Context, embed: embed.Build());
            }
        }

        // white below black
        string output = "";
        for (var i = 7; i >= 0; i--)
        {
            output += numbers[i];
            for (var j = 0; j < 8; j++)
            {
                if (chess.Board[i, j] != ChessBoard.Empty)
                    output += $" {ChessBoard.Icon[chess.Board[i, j]]}";
                else output += $" {sqr[(i + j) % 2]}";
            }
            output += "\n";

            if (i % 3 == 2)
            {
                await Channel.SendMessageAsync(output);
                output = "";
            }
        }

        output += sqr[1];
        for (var i = 0; i < 8; i++)
            output += $" {letters[i]}";

        await Channel.SendMessageAsync(output);
        return await Channel.GetMessagesAsync(4).FlattenAsync();
    }

    private bool IsValidMove(string move)
    {
        try
        {
            ChessBoard chess = Load();

            move = move.ToLower();
            int x1 = move[1] - '1', y1 = move[0] - 'a', x2 = move[4] - '1', y2 = move[3] - 'a',
                pawn = chess.Board[x1, y1],
                color = ChessBoard.IsBlack(pawn) ? 6 : 0;
            int PlayerIndex = chess.CurrentPlayerIndex;
            ulong PlayerId = chess.Player[PlayerIndex];

            if (pawn == ChessBoard.Empty)
                return false;
            if (ChessBoard.IsBlack(pawn) != chess.IsBlackPlayer(PlayerId))
                return false;
            if (chess.Board[x2, y2] != ChessBoard.Empty &&
                ChessBoard.IsBlack(chess.Board[x2, y2]) == chess.IsBlackPlayer(PlayerId))
                return false;
            
            if (pawn == ChessBoard.King + color)
            {
                // Castling
                if (x1 == x2 && ((y2 - y1) * (y2 - y1) == 4))
                {
                    if (x1 != (ChessBoard.IsBlack(pawn)? ChessBoard.BlackRank : ChessBoard.WhiteRank))
                        return false;

                    if (chess.Step[PlayerIndex, x1, y1] != 0 ||
                        chess.IsCheck(PlayerIndex) ||
                        chess.Control[PlayerIndex ^ 1, x2, y2] != 0)
                        return false;

                    int xr = x1, yr = (y2 < y1) ? 0 : 7;
                    if ((chess.Board[xr, yr] != ChessBoard.Rook     && ChessBoard.IsBlack(pawn) && 
                         chess.Board[xr, yr] != ChessBoard.Rook + 6 && ChessBoard.IsWhite(pawn)) ||
                        chess.Step[PlayerIndex, xr, yr] != 0)
                        return false;

                    for (var j = y1 + ((y1 < yr) ? 1 : -1); j != yr; j += (y1 < yr) ? 1 : -1)
                    {
                        if (chess.Board[x1, j] != ChessBoard.Empty)
                            return false;
                    }
                    for (var j = y1 + ((y1 < y2) ? 1 : -1); j != y2; j += (y1 < y2) ? 1 : -1)
                    {
                        if (chess.Control[PlayerIndex ^ 1, x1, j] != 0)
                            return false;
                    }

                    return true;
                }

                if (((x2 - x1) * (x2 - x1)) + ((y2 - y1) * (y2 - y1)) != 1)
                    return false;

                if (chess.Control[PlayerIndex ^ 1, x2, y2] != 0)
                    return false;

                return true;
            }

            else if (pawn == ChessBoard.Pawn + color)
            {
                if (y1 == y2 && 
                    chess.Step[PlayerIndex, x1, y1] == 0 && 
                    (x2 == x1 + (chess.IsBlackPlayer(PlayerId) ? -2 : 2)))
                    return true;

                if ((x2 != x1 + (chess.IsBlackPlayer(PlayerId) ? -1 : 1)) ||
                    (((y2 - y1) * (y2 - y1)) > 1))
                    return false;

                if (y2 == y1)
                    return true;

                if (chess.Board[x2, y2] != ChessBoard.Empty)
                    return true;

                // en passant

                if (((chess.Board[x1, y2] != ChessBoard.Pawn     && chess.IsBlackPlayer(PlayerId)) &&
                     (chess.Board[x1, y2] != ChessBoard.Pawn + 6 && chess.IsWhitePlayer(PlayerId))) ||
                    chess.History.Last().Item2 != ChessBoard.Convert(x1 + (chess.IsBlackPlayer(PlayerId) ? -2 : 2), y2, x1, y2))
                    return false;

                return true;
            }

            else if (pawn == ChessBoard.Knight + color)
            {
                if (((x2 - x1) * (x2 - x1)) + ((y2 - y1) * (y2 - y1)) != 5)
                    return false;

                return true;
            }

            else if (((pawn == ChessBoard.Queen + color) && x1 != x2 && y1 != y2) ||
                pawn == ChessBoard.Bishop + color)
            {
                if ((x1 - x2) != (y1 - y2) && 
                    (x1 + y1) != (x2 + y2))
                    return false;

                for (int i = x1 + ((x1 < x2) ? 1 : -1), j = y1 + ((y1 < y2) ? 1 : -1); 
                    i != x2; 
                    i += (x1 < x2) ? 1 : -1, j += ((y1 < y2) ? 1 : -1))
                {
                    if (chess.Board[i, j] != ChessBoard.Empty)
                        return false;
                }

                return true;
            }

            else if (pawn == ChessBoard.Queen + color ||
                pawn == ChessBoard.Rook + color)
            {
                if (x1 != x2 && y1 != y2)
                    return false;

                if (x1 != x2)
                    for (var i = x1 + ((x1 < x2) ? 1 : -1); i != x2; i += (x1 < x2) ? 1 : -1)
                    {
                        if (chess.Board[i, y1] != ChessBoard.Empty)
                            return false;
                    }
                if (y1 != y2)
                    for (var j = y1 + ((y1 < y2) ? 1 : -1); j != y2; j += (y1 < y2) ? 1 : -1)
                    {
                        if (chess.Board[x1, j] != ChessBoard.Empty)
                            return false;
                    }

                return true;
            }

            return false;
        }
        catch (Exception e)
        {
            DiscordWrapper.Log($"{log} {e}");
            return false;
        }
    }

    private static bool SetControlPos(ref ChessBoard chess, int x, int y)
    {
        try
        {
            int pawn = chess.Board[x, y];
            int PlayerIndex = ChessBoard.IsBlack(pawn) ? ChessBoard.BlackPlayerIndex : ChessBoard.WhitePlayerIndex;

            if (pawn == ChessBoard.King)
            {
                int[,] f = { { 1, 0 },{ 0, 1 },{ -1, 0 },{ 0, -1 } };
                for(var i = 0; i < 4; i++)
                {
                    if ((x + f[i, 0] >= 0) && (x + f[i, 0] <= 7) && 
                        (y + f[i, 1] >= 0) && (y + f[i, 1] <= 7))
                        ++chess.Control[PlayerIndex, x + f[i, 0], y + f[i, 1]];
                }
            }

            if (pawn == ChessBoard.Pawn)
            {
                int[] f = { -1, 1 };
                for(var i = 0; i < 2; i++)
                {
                    if (((x + (PlayerIndex == ChessBoard.BlackPlayerIndex ? -1 : 1)) >= 0) && 
                        ((x + (PlayerIndex == ChessBoard.BlackPlayerIndex ? -1 : 1)) <= 7) && 
                        (y + f[i] >= 0) && (y + f[i] <= 7))
                        ++chess.Control[PlayerIndex, x + (PlayerIndex == ChessBoard.BlackPlayerIndex ? -1 : 1), y + f[i]];
                }
                if (chess.Step[PlayerIndex, x, y] == 0)
                    ++chess.Control[PlayerIndex, x + (PlayerIndex == ChessBoard.BlackPlayerIndex ? -2 : 2), y];
            }

            if (pawn == ChessBoard.Knight)
            {
                int[,] f = { { -1, 2 }, { -2, 1 }, { 1, 2 }, { 2, 1 }, { -1, -2 }, { -2, -1 },{ 1, -2 },{ 2, -1 } };

                for (var i = 0; i < 8; i++)
                {
                    if ((x + f[i, 0] >= 0) && (x + f[i, 0] <= 7) &&
                        (y + f[i, 1] >= 0) && (y + f[i, 1] <= 7))
                        ++chess.Control[PlayerIndex, x + f[i, 0], y + f[i, 1]];
                }
            }

            if ((pawn == ChessBoard.Queen) ||
                pawn == ChessBoard.Bishop) {
                int[,] f = { { 1, 1 }, { 1, -1 }, { -1, 1 }, { -1, -1 } };


                for (var k = 0; k < 4; k++)
                    for (int i = x + f[k, 0], j = y + f[k, 1]; i >= 0 && i <= 7 && j >= 0 && j <= 7; i += f[k, 0], j += f[k, 1])
                    {
                        ++chess.Control[PlayerIndex, i, j];
                        if (chess.Board[i, j] != ChessBoard.Empty)
                            break;
                    }
            }

            if ((pawn == ChessBoard.Queen) ||
                pawn == ChessBoard.Rook)
            {
                int[,] f = { { 1, 0 }, { 0, -1 }, { -1, 0 }, { 0, 1 } };


                for (var k = 0; k < 4; k++)
                    for (int i = x + f[k, 0], j = y + f[k, 1]; i >= 0 && i <= 7 && j >= 0 && j <= 7; i += f[k, 0], j += f[k, 1])
                    {
                        ++chess.Control[PlayerIndex, i, j];
                        if (chess.Board[i, j] != ChessBoard.Empty)
                            break;
                    }
            }

            return true;
        }
        catch (Exception e) 
        {
            DiscordWrapper.Log($"{log} {e}");
            return false;
        }
    }

    private static bool SetControl(ref ChessBoard chess)
    {
        try
        {
            for (var i = 0; i < 8; i++) for (var j = 0; j < 8; j++)
                    chess.Control[0, i, j] = chess.Control[1, i, j] = 0;

            for (var i = 0; i < 8; i++) for (var j = 0; j < 8; j++)
                {
                    if (chess.Board[i, j] != ChessBoard.Empty &&
                        !SetControlPos(ref chess, i, j))
                        return false;
                }

            return true;
        }
        catch (Exception e)
        {
            DiscordWrapper.Log($"{log} {e}");
            return false;
        }
    }

    private bool TryMove(string move, bool save)
    {
        ChessBoard chess = Load();
        try
        {
            move = move.ToLower();
            int x1 = move[1] - '1', y1 = move[0] - 'a', x2 = move[4] - '1', y2 = move[3] - 'a',
                pawn = chess.Board[x1, y1],
                color = ChessBoard.IsBlack(pawn) ? 6 : 0;
            int PlayerIndex = chess.CurrentPlayerIndex;

            chess.Board[x1, y1] = ChessBoard.Empty;

            if      (pawn == ChessBoard.King + color)
            {
                chess.Board[x2, y2] = pawn;
                chess.Step[PlayerIndex, x2, y2] = chess.Step[PlayerIndex, x1, y1] + 1;

                chess.KingPos[PlayerIndex, 0] = x2;
                chess.KingPos[PlayerIndex, 1] = y2;

                // Castling
                if (((y2 - y1) * (y2 - y1) == 4))
                {
                    int xr = x1, yr1 = (y2 < y1) ? 0 : 7, yr2 = (yr1 == 0) ? 3 : 5;
                    chess.Board[xr, yr1] = ChessBoard.Empty;
                    chess.Board[xr, yr2] = (PlayerIndex == ChessBoard.WhitePlayerIndex) ? ChessBoard.Rook : ChessBoard.Rook + 6;
                    chess.Step[PlayerIndex, xr, yr2] = chess.Step[PlayerIndex, xr, yr1] + 1;
                }
            }
            else if (pawn == ChessBoard.Pawn + color)
            {
                // en passant
                if (y1 != y2 &&
                    ((chess.Board[x1, y2] == ChessBoard.Pawn && PlayerIndex == ChessBoard.BlackPlayerIndex) ||
                    (chess.Board[x1, y2] == ChessBoard.Pawn + 6 && PlayerIndex == ChessBoard.WhitePlayerIndex)) &&
                    chess.History.Last().Item2 == ChessBoard.Convert(x1 + (PlayerIndex == ChessBoard.BlackPlayerIndex ? -2 : 2), y2, x1, y2))
                    chess.Board[x1, y2] = ChessBoard.Empty;

                chess.Board[x2, y2] = pawn;
                chess.Step[PlayerIndex, x2, y2] = chess.Step[PlayerIndex, x1, y1] + 1;

                // Promo Queen
                if ((PlayerIndex == ChessBoard.WhitePlayerIndex && x2 == ChessBoard.WhiteRank) ||
                    (PlayerIndex == ChessBoard.BlackPlayerIndex && x2 == ChessBoard.BlackRank))
                {
                    chess.Board[x2, y2] = ChessBoard.Queen + color;
                    chess.Step[PlayerIndex, x2, y2] = chess.Step[PlayerIndex, x1, y1] + 1;
                }
            }
            else
            {
                chess.Board[x2, y2] = pawn;
                chess.Step[PlayerIndex, x2, y2] = chess.Step[PlayerIndex, x1, y1] + 1;
            }

            if (!SetControl(ref chess))
                return false;
            if (!chess.IsCheck(PlayerIndex))
            {
                if (save == true)
                {
                    chess.CurrentPlayerIndex ^= 1;
                    chess.History.Add(new Tuple<int, string>(pawn, move));
                    if (!Save(ref chess))
                        return false;
                }
                return true;
            }
            else
            {
                return false;
            }
        }
        catch (Exception e)
        {
            DiscordWrapper.Log($"{log} {e}");
            return false;
        }
    }

    private async Task<bool> Move(string move, ISocketMessageChannel Channel)
    {
        try
        {
            ChessBoard chess = Load();

            move = move.ToLower();
            int x1 = move[1] - '1', y1 = move[0] - 'a', x2 = move[4] - '1', y2 = move[3] - 'a',
                pawn = chess.Board[x1, y1],
                color = ChessBoard.IsBlack(pawn) ? 6 : 0;
            int PlayerIndex = chess.CurrentPlayerIndex;

            if (!TryMove(move, save: true))
                return false;
            if (pawn == ChessBoard.Pawn + color)
            {
                //promo
                if ((PlayerIndex == ChessBoard.WhitePlayerIndex && x2 == ChessBoard.WhiteRank) ||
                    (PlayerIndex == ChessBoard.BlackPlayerIndex && x2 == ChessBoard.BlackRank))
                {
                    int[] promo = { ChessBoard.Queen + color, ChessBoard.Rook + color, ChessBoard.Bishop + color, ChessBoard.Knight + color };

                    await DiscordWrapper.SendMessage(Context, "move 1: Hậu; 2: Xe; 3: Tượng; 4: Mã");
                    SocketMessage msg;
                    int ans = 0;

                    Stopwatch s = new Stopwatch();
                    s.Start();
                    while (s.Elapsed < TimeSpan.FromMinutes(5))
                    {
                        msg = await NextMessageAsync(fromSourceUser: false, inSourceChannel: true, timeout: TimeSpan.FromMinutes(5));
                        if (msg == null)
                        {
                            return false;
                        }
                        if (msg.Author.Id != chess.Player[PlayerIndex] || !IsValidMessage(msg.Content))
                        {
                            continue;
                        }

                        string subs = msg.Content.Substring(5);
                        if (subs == "1" || subs == "2" || subs == "3" || subs == "4")
                        {
                            ans = subs[0] - '1';
                            break;
                        }
                    }
                    s.Stop();

                    await (Context.Channel as ITextChannel).DeleteMessagesAsync(await Context.Channel.GetMessagesAsync(2).FlattenAsync());
                    chess.Board[x2, y2] = promo[ans];

                    if (!SetControl(ref chess))
                        return false;
                    if (!Save(ref chess))
                        return false;
                }
            }
            return true;
        }
        catch (Exception e)
        {
            DiscordWrapper.Log($"{log} {e}");
            return false;
        }
    }

    private bool HasNoMove(int PlayerIndex)
    {
        ChessBoard chess = Load();

        for(var x = 0; x < 8; x++) for(var y = 0; y < 8; y++)
            {
                if (chess.Board[x, y] != ChessBoard.Empty &&
                    ChessBoard.IsBlack(chess.Board[x, y]) == (PlayerIndex == ChessBoard.BlackPlayerIndex))
                {
                    int pawn = chess.Board[x, y],
                        color = ChessBoard.IsBlack(pawn) ? 6 : 0;

                    if (pawn == ChessBoard.King + color)
                    {
                        int[,] f = { { 1, 0 }, { 0, 1 }, { -1, 0 }, { 0, -1 } };
                        for (var i = 0; i < 4; i++)
                        {
                            if (IsValidMove(ChessBoard.Convert(x, y, x + f[i, 0], y + f[i, 1])) &&
                                TryMove(ChessBoard.Convert(x, y, x + f[i, 0], y + f[i, 1]), save: false))
                                return false;
                        }
                    }

                    if (pawn == ChessBoard.Pawn + color)
                    {
                        int x1 = x + (PlayerIndex == ChessBoard.BlackPlayerIndex ? -1 : 1),
                            y1 = y;
                        if (IsValidMove(ChessBoard.Convert(x, y, x1, y1)) &&
                                TryMove(ChessBoard.Convert(x, y, x1, y1), save: false))
                            return false;

                        int[] f = { -1, 1 };
                        for (var i = 0; i < 2; i++)
                        {
                            x1 = x + (PlayerIndex == ChessBoard.BlackPlayerIndex ? -1 : 1);
                            y1 = y + f[i];
                            if (IsValidMove(ChessBoard.Convert(x, y, x1, y1)) &&
                                TryMove(ChessBoard.Convert(x, y, x1, y1), save: false))
                                return false;
                        }
                        if (chess.Step[PlayerIndex, x, y] == 0)
                        {
                            x1 = x + (PlayerIndex == ChessBoard.BlackPlayerIndex ? -2 : 2);
                            y1 = y;
                            if (IsValidMove(ChessBoard.Convert(x, y, x1, y1)) &&
                                TryMove(ChessBoard.Convert(x, y, x1, y1), save: false))
                                return false;
                        }
                    }

                    if (pawn == ChessBoard.Knight + color)
                    {
                        int[,] f = { { -1, 2 }, { -2, 1 }, { 1, 2 }, { 2, 1 }, { -1, -2 }, { -2, -1 }, { 1, -2 }, { 2, -1 } };

                        for (var i = 0; i < 8; i++)
                        {
                            if (IsValidMove(ChessBoard.Convert(x, y, x + f[i, 0], y + f[i, 1])) &&
                                TryMove(ChessBoard.Convert(x, y, x + f[i, 0], y + f[i, 1]), save: false))
                                return false;
                        }
                    }

                    if ((pawn == ChessBoard.Queen + color) ||
                        pawn == ChessBoard.Bishop + color)
                    {
                        int[,] f = { { 1, 1 }, { 1, -1 }, { -1, 1 }, { -1, -1 } };


                        for (var k = 0; k < 4; k++)
                            for (int x1 = x + f[k, 0], y1 = y + f[k, 1]; 
                                x1 >= 0 && x1 <= 7 && y1 >= 0 && y1 <= 7; 
                                x1 += f[k, 0], y1 += f[k, 1])
                            {
                                if (IsValidMove(ChessBoard.Convert(x, y, x1, y1)) &&
                                TryMove(ChessBoard.Convert(x, y, x1, y1), save: false))
                                    return false;
                            }
                    }

                    if ((pawn == ChessBoard.Queen + color) ||
                        pawn == ChessBoard.Rook + color)
                    {
                        int[,] f = { { 1, 0 }, { 0, -1 }, { -1, 0 }, { 0, 1 } };


                        for (var k = 0; k < 4; k++)
                            for (int x1 = x + f[k, 0], y1 = y + f[k, 1]; 
                                x1 >= 0 && x1 <= 7 && y1 >= 0 && y1 <= 7; 
                                x1 += f[k, 0], y1 += f[k, 1])
                            {
                                if (IsValidMove(ChessBoard.Convert(x, y, x1, y1)) &&
                                TryMove(ChessBoard.Convert(x, y, x1, y1), save: false))
                                    return false;
                            }
                    }
                }
            }
        return true;
    }

    private async Task<bool> Host()
    {
        try
        {
            IEnumerable<IMessage> print;
            ChessBoard chess;

            while (true)
            {
                chess = Load();

                if (HasNoMove(chess.CurrentPlayerIndex))
                {
                    if (chess.IsCheck(chess.CurrentPlayerIndex))
                    {
                        chess.SetGameResult(HasNoMove: true, chess.Player[chess.CurrentPlayerIndex ^ 1]);
                    }
                    else
                    {
                        chess.SetGameResult(HasNoMove: true);
                    }
                    Save(ref chess);
                    await Print();
                    break;
                }

                print = await Print();
                SocketMessage msg;

                Stopwatch s = new Stopwatch();
                s.Start();
                do
                {
                    msg = await NextMessageAsync(fromSourceUser: false, inSourceChannel: true, timeout: TimeSpan.FromMinutes(5));
                    if (msg == null)
                    {
                        return false;
                    }
                    if (msg.Author.Id == chess.Player[chess.CurrentPlayerIndex] && IsValidMessage(msg.Content))
                    {
                        break;
                    }
                } while (s.Elapsed < TimeSpan.FromMinutes(5));
                s.Stop();

                string move = msg.Content.Substring(5);

                if (move == "pause")
                {
                    break;
                }
                else if (move == "draw")
                {
                    ulong AskedPlayer = chess.Player[chess.CurrentPlayerIndex ^ 1];
                    await DiscordWrapper.SendMessage(Context, $"{MentionUtils.MentionUser(AskedPlayer)}, bạn có đồng ý hoà ván đấu này? (Yes/No)");

                    string content = null;
                    SocketMessage msg_draw;

                    s = new Stopwatch();
                    s.Start();
                    do
                    {
                        msg_draw = await NextMessageAsync(fromSourceUser: false, inSourceChannel: true, timeout: TimeSpan.FromMinutes(5));
                        if (msg_draw == null)
                        {
                            break;
                        }

                        content = msg_draw.Content.ToLower();
                        if (msg_draw.Author.Id == AskedPlayer && (content == "yes" || content == "no"))
                        {
                            break;
                        }
                    } while (s.Elapsed < TimeSpan.FromMinutes(5));

                    if (msg_draw == null)
                    {
                        await DiscordWrapper.SendMessage(Context, $"Người chơi {MentionUtils.MentionUser(AskedPlayer)} không phản hồi, trò chơi sẽ tiếp tục.");
                    }
                    else if (content == "no")
                    {
                        await DiscordWrapper.SendMessage(Context, $"Người chơi {MentionUtils.MentionUser(AskedPlayer)} không chấp nhận hoà, trò chơi sẽ tiếp tục.");
                    }
                    else
                    {
                        chess.SetGameResult(HasNoMove: false);
                        Save(ref chess);
                        await Print();
                        break;
                    }
                }
                else if (move == "forfeit" || move == "surrender")
                {
                    chess.SetGameResult(HasNoMove: false, chess.Player[chess.CurrentPlayerIndex ^ 1]);
                    Save(ref chess);
                    await Print();
                    break;
                }

                else if (!IsValidMove(move) ||
                    !await Move(move, Context.Channel))
                {
                    var embed = new EmbedBuilder
                    {
                        Description = $"{MentionUtils.MentionUser(chess.Player[chess.CurrentPlayerIndex])}, Nước đi {move} của bạn không hợp lệ. Hãy thử lại.",
                        Color = Color.Magenta
                    };
                    await Context.Channel.TriggerTypingAsync();
                    await ReplyAndDeleteAsync(content: null, embed: embed.Build(), timeout: TimeSpan.FromSeconds(5));
                }

                await (Context.Channel as ITextChannel).DeleteMessagesAsync(print);
            }

            chess = Load();
            return chess.IsOver;
        }
        catch (Exception e) 
        {
            DiscordWrapper.Log($"{log} {e}");
            return false;
        }
    }


    [Command("chess new", RunMode = RunMode.Async)]
    public async Task New(string str2)
    {
        ulong UserId = Context.Message.Author.Id;
        await DiscordWrapper.SendMessage(Context, $"{MentionUtils.MentionUser(UserId)}, bạn có chắc muốn tạo ván đấu mới? (Yes/No)");

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
            await DiscordWrapper.SendMessage(Context, $"Ván đấu mới sẽ không được tạo.");
            await Task.Delay(TimeSpan.FromSeconds(5));
            await (Context.Channel as ITextChannel).DeleteMessagesAsync(await Context.Channel.GetMessagesAsync(4).FlattenAsync());
            return;
        }
        await (Context.Channel as ITextChannel).DeleteMessagesAsync(await Context.Channel.GetMessagesAsync(2).FlattenAsync());

        ChessBoard chess = new ChessBoard();
        if (!MentionUtils.TryParseUser(str2, out chess.Player[1]))
        {
            var embed = new EmbedBuilder
            {
                Description = "Tên người chơi 2 không hợp lệ.",
                Color = Color.Magenta
            };
            await Context.Channel.TriggerTypingAsync();
            await ReplyAndDeleteAsync(content: null, embed: embed.Build(), timeout: TimeSpan.FromSeconds(5));

            return;
        }
        chess.Player[0] = Context.Message.Author.Id;

        Save(ref chess);
        DiscordWrapper.Log($"chess new {MentionUtils.MentionUser(chess.Player[ChessBoard.WhitePlayerIndex]) } *(white)* vs { MentionUtils.MentionUser(chess.Player[ChessBoard.BlackPlayerIndex]) } *(black)*");

        if (!(await Host()))
        {
            var embed = new EmbedBuilder
            {
                Description = $"Trận đấu giữa {MentionUtils.MentionUser(chess.Player[ChessBoard.WhitePlayerIndex])} và {MentionUtils.MentionUser(chess.Player[ChessBoard.BlackPlayerIndex])} đã tạm dừng.",
                Color = Color.Blue
            };
            //await ReplyAsync(embed: embed.Build());
            await DiscordWrapper.SendMessage(Context, embed: embed.Build());
        }
    }


    [Command("chess continue", RunMode = RunMode.Async)]
    public async Task Continue()
    {
        var chess = Load();
        if (chess.IsOver)
        {
            var embed = new EmbedBuilder
            {
                Description = "Không có trận đấu nào chưa kết thúc.",
                Color = Color.Blue
            };
            await Context.Channel.TriggerTypingAsync();
            await ReplyAndDeleteAsync(content: null, embed: embed.Build(), timeout: TimeSpan.FromSeconds(5));
        }
        else if (!(await Host()))
        {
            var embed = new EmbedBuilder
            {
                Description = $"Trận đấu giữa {MentionUtils.MentionUser(chess.Player[ChessBoard.WhitePlayerIndex])} và {MentionUtils.MentionUser(chess.Player[ChessBoard.BlackPlayerIndex])} đã tạm dừng.",
                Color = Color.Blue
            };
            //await ReplyAsync(embed: embed.Build());
            await DiscordWrapper.SendMessage(Context, embed: embed.Build());
        }
    }


    [Command("chess print")]
    public async Task PrintCmd()
    {
        ChessBoard chess = Load();
        if (chess == null)
        {
            var embed = new EmbedBuilder
            {
                Description = "Không có trận đấu nào đang được lưu.",
                Color = Color.Blue
            };
            await Context.Channel.TriggerTypingAsync();
            await ReplyAndDeleteAsync(content: null, embed: embed.Build(), timeout: TimeSpan.FromSeconds(5));

            return;
        }
        await Print();
    }


    private bool Save(ref ChessBoard chess)
    {
        try
        {
            File.WriteAllText(_config.GetValue<string>("json:ChessBoard.json"), JsonConvert.SerializeObject(chess));

            return true;
        }
        catch (Exception e)
        {
            DiscordWrapper.Log($"{log} {e}");
            return false;
        }
    }

    private ChessBoard Load()
    {
        return JsonConvert.DeserializeObject<ChessBoard>(File.ReadAllText(_config.GetValue<string>("json:ChessBoard.json")));
    }

    private static bool IsValidMessage(string message)
    {
        return message.StartsWith("move ");
    }
}