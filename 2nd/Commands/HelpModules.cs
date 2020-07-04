using System.Threading.Tasks;
using Discord;
using Discord.Commands;


public class HelpMudule : ModuleBase<SocketCommandContext>
{
    [Command("help")]
    [Alias("command", "commands")]
    [Summary("help")]
    public async Task Help()
    {
        //return;

        var embed = new EmbedBuilder
        {
            Description = "**>> Cùng điểm qua những câu lệnh Mèo có nào!**",
            Color = Color.Magenta
        };
        embed.AddField("*'hi*",
                       "Cùng chào nhau nhé!")
            .AddField("*'react <ID tin nhắn> <emoji_1> <emoji_2> ... <emoji_n>*",
                      "Mèo sẽ \"thả tym\" vào tin nhắn có ID đó!")
            .AddField("*'say <true/false>(true nếu ẩn danh, mặc định là false) <thời gian chờ>(theo giây, mặc định là 0) <tin nhắn>*",
                      "Mèo sẽ giúp bạn gửi tin nhắn đó!")
            .AddField("*'party*",
                      "Đến lúc quẩy rồi!!! :partying_face:")
            .AddField("*'remind <giờ>(0 - 23) <phút>(0 - 59) <giây>(0 - 59, mặc định là 0) <lời nhắc>*",
                      "Mèo sẽ nhắc nhở bạn vào thời điểm đó!")
            .AddField("*'wait <giây> <lời nhắn>*",
                      "Mèo sẽ gửi một tin nhắn y hệt lời nhắn đó sau số giây nhất định!")
            .AddField("Ngoài ra, bạn có thể thử *'join*, *'play*, *'stop* và *'leave*~ :notes:", "*^^Chúc bạn một ngày tốt lành!^^*")

            // Exam
            .AddField("*>> 'exam*", "**>> Những lệnh liên quan đến các bài kiểm tra trong lớp T.T:**")
            .AddField("*'exam add <môn học> <thứ>(2 - 6) <thời lượng kiểm tra> <ghi chú>(nếu có)*", "Thêm bài kiểm tra vào danh sách của Mèo~")
            .AddField("*'exam get <thứ>(2 - 6)*", "Xem tất cả các bài kiểm tra có trong ngày hôm đó.")
            .AddField("*'exam get <môn học>*", "Xem tất cả các bài kiểm tra của môn học đó.")
            .AddField("*'exam all*", "Xem tất cả các bài kiểm tra có trong 7 ngày tới!")
            .AddField("*'exam rm <thứ>(2 - 6)*", "Xoá các bài kiểm tra của ngày đó trong danh sách của Mèo! :partying_face:")
            .AddField("*'exam rm <môn học> <thứ>(2 - 6) <thời lượng kiểm tra> <ghi chú>(nếu có)*", "Xoá bài kiểm tra trong danh sách của Mèo! :partying_face:")
            .AddField("*'exam clear*", "Xoá toàn bộ các bài kiểm tra trong danh sách của Mèo!!! :partying_face::tada::confetti_ball:")
            .AddField("^^Cố gắng lên nào!^^ :fist::fist:", "*^^Chúc bạn làm bài thật tốt!^^*");

        var embedChess = new EmbedBuilder
        {
            Color = Color.LightOrange
        };
        embedChess.AddField("*>> 'chess*", 
                      "**>> Những lệnh liên quan đến trò chơi :chess_pawn:~:**")
            .AddField("*'chess new <tên người chơi 2>:*", 
                      "Bắt đầu một ván đấu mới.")
            .AddField("*'chess continue*", 
                      "Tiếp tục ván đấu được lưu (nếu chưa kết thúc).")
            .AddField("*'chess print*", 
                      "In ra bàn cờ và trạng thái của ván đấu được lưu.")
            // Move
            .AddField("*>> move*", "**>> Những lệnh trong ván đấu:**")
            .AddField("*move <nước đi (vd a2 b3)>*", 
                      "Di chuyển quân cờ.")
            .AddField("*move pause*", 
                      "Tạm ngưng trò chơi.")
            .AddField("*move draw*", 
                      "Đề xuất hoà ván đấu.")
            .AddField("*move surrender*", 
                      "Kết thúc trò chơi với kết quả thua. :cry:")
            .AddField("^^Chơi hết mình nhé!^^ :fist::fist:", "*^^Chúc mọi người chơi vui vẻ!^^*");

        await Context.Channel.TriggerTypingAsync();
        await ReplyAsync(embed: embed.Build());
        await ReplyAsync(embed: embedChess.Build());
    }
}