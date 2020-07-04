using System;
using System.IO;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using System.Text.Json;

using Discord;
using Discord.Commands;
using Discord.Rest;
using Discord.WebSocket;
using Discord.Addons.Interactive;

using Microsoft.Extensions.Configuration;

public class StudyModule : ModuleBase<SocketCommandContext>
{
    readonly IConfigurationRoot _config;
    readonly DiscordSocketClient _client;

    private static IConfigurationRoot _subjects = new ConfigurationBuilder()
              .SetBasePath(Directory.GetCurrentDirectory())
              .AddJsonFile("subjects.json", optional: false, reloadOnChange: true)
              .Build();

    public StudyModule(IConfigurationRoot config, DiscordSocketClient client)
    {
        _config = config;
        _client = client;
    }

    private class TestSchedule
    {
        public HashSet<string>[] Day { get; set; }
        public HashSet<string>[] Subject { get; set; }
        public TestSchedule()
        {
            Day = new HashSet<string>[5];
            Subject = new HashSet<string>[_subjects.GetValue<int>("Count")];

            for (var i = 0; i < Day.Length; i++) Day[i] = new HashSet<string>();
            for (var i = 0; i < Subject.Length; i++) Subject[i] = new HashSet<string>();
        }
    }


    [Group("exam")]
    public class SubjectModule : InteractiveBase
    {
        readonly IConfigurationRoot _config;
        readonly DiscordSocketClient _client;
        public SubjectModule(IConfigurationRoot config, DiscordSocketClient client)
        {
            _config = config;
            _client = client;
        }


        [Command("assign", RunMode = RunMode.Async)]
        [Alias("add")]
        [Summary("params: subject name, day of week, duration")]
        public async Task Assign(string s_name, int day, int duration, [Remainder]string note = null)
        {
            await Context.Channel.TriggerTypingAsync();
            try
            {
                var schedule = JsonSerializer.Deserialize<TestSchedule>(File.ReadAllText(_config.GetValue<string>("json:TestSchedule.json")));
                var idx = _subjects.GetValue<int>($"idx:{s_name}");
                if (idx == default && s_name != "Toan")
                    throw new NullReferenceException("Subject name not found.");

                if (note != null) note = $" ({note})";
                schedule.Day[day - 2].Add($"{s_name} - {duration}'{note}");
                schedule.Subject[idx].Add($"Thứ {day.ToString()} - {duration}'{note}");

                File.WriteAllText(_config.GetValue<string>("json:TestSchedule.json"), JsonSerializer.Serialize(schedule));

                await LogDiscord($"assign {s_name} {day} {duration}'{note}");
                
                var msg = await ReplyAsync($"Đã thêm bài kiểm tra môn {s_name} thành công! :book::fist:");
                await Task.Delay(TimeSpan.FromSeconds(3));
                await msg.DeleteAsync();
            }
            catch (Exception e)
            {
                Console.WriteLine($"[log] {e.ToString()}");

                var msg = await ReplyAsync("Có lỗi xảy ra. Hãy chắc chắn bạn đã nhập đúng cú pháp, với tên các môn học là: Toan, QP, TD, TCMN, CN, Tin, GDCD, Dia, Su, Anh, Van, Sinh, Hoa, Ly, và các thứ trong ngày là từ 2 đến 6.");
                await Task.Delay(TimeSpan.FromSeconds(3));
                await msg.DeleteAsync();
            }
            finally
            {
                await Context.Message.DeleteAsync();
            }
        }


        [Command("get", RunMode = RunMode.Async)]
        [Summary("param: day")]
        public async Task Get(int day)
        {
            await Context.Channel.TriggerTypingAsync();
            try
            {
                var schedule = JsonSerializer.Deserialize<TestSchedule>(File.ReadAllText(_config.GetValue<string>("json:TestSchedule.json")));

                var embed = new EmbedBuilder
                {
                    Color = Color.LightOrange
                };
                if (schedule.Day[day - 2].Any())
                    embed.AddField($"*Thứ {day}*", string.Join(", ", schedule.Day[day - 2]));
                else
                    embed.AddField($"*Thứ {day}*", $"Không có bài kiểm tra nào cho thứ {day} tới cả! :partying_face:");

                await ReplyAsync(embed: embed.Build());
            }
            catch (Exception e)
            {
                Console.WriteLine($"[log] {e.ToString()}");

                await Context.Channel.TriggerTypingAsync();
                var msg = await ReplyAsync("Có lỗi xảy ra. Hãy chắc chắn bạn đã nhập đúng cú pháp, với các thứ trong ngày là từ 2 đến 6.");
                await Task.Delay(TimeSpan.FromSeconds(3));
                await msg.DeleteAsync();
            }
            finally
            {
                await Context.Message.DeleteAsync();
            }
        }


        [Command("get", RunMode = RunMode.Async)]
        [Summary("param: subject")]
        public async Task Get(string s_name)
        {
            await Context.Channel.TriggerTypingAsync();
            try
            {
                var schedule = JsonSerializer.Deserialize<TestSchedule>(File.ReadAllText(_config.GetValue<string>("json:TestSchedule.json")));
                var idx = _subjects.GetValue<int>($"idx:{s_name}");
                if (idx == default && s_name != "Toan")
                    throw new NullReferenceException("Subject name not found.");

                var embed = new EmbedBuilder
                {
                    Color = Color.LightOrange
                };
                if (schedule.Subject[idx].Any())
                    embed.AddField($"*Môn {s_name}*", string.Join(", ", schedule.Subject[_subjects.GetValue<int>($"idx:{s_name}")]));
                else
                    embed.AddField($"*Môn {s_name}*", $"Không có bài kiểm tra nào cho môn {s_name} 7 ngày tới cả! :partying_face:");

                await ReplyAsync(embed: embed.Build());
            }
            catch (Exception e)
            {
                Console.WriteLine($"[log] {e.ToString()}");

                await Context.Channel.TriggerTypingAsync();
                var msg = await ReplyAsync("Có lỗi xảy ra. Hãy chắc chắn bạn đã nhập đúng cú pháp, với tên các môn học là: Toan, QP, TD, TCMN, CN, Tin, GDCD, Dia, Su, Anh, Van, Sinh, Hoa, Ly.");
                await Task.Delay(TimeSpan.FromSeconds(3));
                await msg.DeleteAsync();
            }
            finally
            {
                await Context.Message.DeleteAsync();
            }
        }


        [Command("remove", RunMode = RunMode.Async)]
        [Alias("rm")]
        [Summary("params: day of week")]
        public async Task Remove(int day)
        {
            await Context.Channel.TriggerTypingAsync();
            try
            {
                var schedule = JsonSerializer.Deserialize<TestSchedule>(File.ReadAllText(_config.GetValue<string>("json:TestSchedule.json")));
                schedule.Day[day - 2].Clear();

                File.WriteAllText(_config.GetValue<string>("json:TestSchedule.json"), JsonSerializer.Serialize(schedule));

                await LogDiscord($"remove {day}");

                var msg = await ReplyAsync($"Đã xoá bài các kiểm tra thứ {day} thành công! :partying_face::tada::confetti_ball:");
                await Task.Delay(TimeSpan.FromSeconds(3));
                await msg.DeleteAsync();
            }
            catch (Exception e)
            {
                Console.WriteLine($"[log] {e.ToString()}");

                var msg = await ReplyAsync("Có lỗi xảy ra. Hãy chắc chắn bạn đã nhập đúng cú pháp, với các thứ trong ngày là từ 2 đến 6.");
                await Task.Delay(TimeSpan.FromSeconds(3));
                await msg.DeleteAsync();
            }
            finally
            {
                await Context.Message.DeleteAsync();
            }
        }


        [Command("remove", RunMode = RunMode.Async)]
        [Alias("rm")]
        [Summary("params: subject name, day of week, duration")]
        public async Task Remove(string s_name, int day, int duration, [Remainder]string note = null)
        {
            await Context.Channel.TriggerTypingAsync();
            try
            {
                var schedule = JsonSerializer.Deserialize<TestSchedule>(File.ReadAllText(_config.GetValue<string>("json:TestSchedule.json")));
                var idx = _subjects.GetValue<int>($"idx:{s_name}");
                if (idx == default && s_name != "Toan")
                    throw new NullReferenceException("Subject name not found.");

                if (note != null) note = $" ({note})";

                schedule.Day[day - 2].Remove($"{s_name} - {duration}'{note}");
                schedule.Subject[idx].Remove($"Thứ {day} - {duration}'{note}");

                File.WriteAllText(_config.GetValue<string>("json:TestSchedule.json"), JsonSerializer.Serialize(schedule));

                await LogDiscord($"remove {s_name} {day} {duration}'{note}");

                var msg = await ReplyAsync($"Đã xoá bài kiểm tra môn {s_name} thành công! :partying_face::tada::confetti_ball:");
                await Task.Delay(TimeSpan.FromSeconds(3));
                await msg.DeleteAsync();
            }
            catch (Exception e)
            {
                Console.WriteLine($"[log] {e.ToString()}");

                var msg = await ReplyAsync("Có lỗi xảy ra. Hãy chắc chắn bạn đã nhập đúng cú pháp, với tên các môn học là: Toan, QP, TD, TCMN, CN, Tin, GDCD, Dia, Su, Anh, Van, Sinh, Hoa, Ly, và các thứ trong ngày là từ 2 đến 6.");
                await Task.Delay(TimeSpan.FromSeconds(3));
                await msg.DeleteAsync();
            }
            finally
            {
                await Context.Message.DeleteAsync();
            }
        }


        [Command("clear", RunMode = RunMode.Async)]
        public async Task Clear()
        {
            await Context.Channel.TriggerTypingAsync();
            var msg1 = await ReplyAsync("Bạn có chắc muốn xoá toàn bộ dữ liệu về lịch kiểm tra chứ? (Yes/No)");
            var response = await NextMessageAsync(timeout: TimeSpan.FromSeconds(15));
            if (response == null || response.Content.ToLower() != "yes")
            {
                await Context.Channel.TriggerTypingAsync();
                var msg2 = await ReplyAsync("Huỷ tác vụ.");
                await Task.Delay(TimeSpan.FromSeconds(3));
                await msg1.DeleteAsync();
                await msg2.DeleteAsync();
                await response.DeleteAsync();
                await Context.Message.DeleteAsync();
                return;
            }

            await Context.Channel.TriggerTypingAsync();
            try
            {
                var schedule = new TestSchedule();
                File.WriteAllText(_config.GetValue<string>("json:TestSchedule.json"), JsonSerializer.Serialize(schedule));

                await LogDiscord("Deleted all scheduled tests.");

                var msg2 = await ReplyAsync("Xoá toàn bộ bài kiểm tra thành công!");
                await Task.Delay(TimeSpan.FromSeconds(3));
                await msg1.DeleteAsync();
                await msg2.DeleteAsync();
                await response.DeleteAsync();
            }
            catch (Exception e)
            {
                Console.WriteLine($"[log] {e.ToString()}");

                var msg = await ReplyAsync("Có lỗi xảy ra. Bạn vui lòng thử lại nhé.");
                await Task.Delay(TimeSpan.FromSeconds(3));
                await msg.DeleteAsync();
            }
            finally
            {
                await Context.Message.DeleteAsync();
            }
        }


        [Command("all", RunMode = RunMode.Async)]
        public async Task All()
        {
            await Context.Channel.TriggerTypingAsync();
            try
            {
                var schedule = JsonSerializer.Deserialize<TestSchedule>(File.ReadAllText(_config.GetValue<string>("json:TestSchedule.json")));
                string[] reply = new string[5];
                for (int i = 0; i < 5; i++)
                {
                    if (schedule.Day[i].Any())
                        reply[i] = string.Join(", ", schedule.Day[i]);
                    else
                        reply[i] = $"Không có bài kiểm tra nào cả! :partying_face:";
                }

                var embed = new EmbedBuilder
                {
                    Description = "**Lịch kiểm tra 7 ngày sắp tới~**",
                    Color = Color.LightOrange
                };
                embed.AddField("*Thứ hai*", reply[0])
                    .AddField("*Thứ ba*", reply[1])
                    .AddField("*Thứ tư*", reply[2])
                    .AddField("*Thứ năm*", reply[3])
                    .AddField("*Thứ sáu*", reply[4])
                    .AddField("^^Cố gắng lên nào!^^", "*^^Chúc bạn làm bài thật tốt!^^*");

                await ReplyAsync(embed: embed.Build());
            }
            catch (Exception e)
            {
                Console.WriteLine($"[log] {e.ToString()}");
                await ReplyAsync("Có lỗi xảy ra. Bạn vui lòng thử lại nhé.");
            }
        }


        public async Task<RestUserMessage> LogDiscord(string log)
        {
            var channel = _client.GetChannel(_config.GetValue<ulong>("guild:Test:log")) as ISocketMessageChannel;

            await channel.TriggerTypingAsync();
            return (await channel.SendMessageAsync(log));
        }
    }


    public async Task<RestUserMessage> LogDiscord(string log)
    {
        var channel = _client.GetChannel(_config.GetValue<ulong>("guild:Test:log")) as ISocketMessageChannel;

        await channel.TriggerTypingAsync();
        return (await channel.SendMessageAsync(log));
    }
}