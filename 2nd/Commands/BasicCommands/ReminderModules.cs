// problem with Save method, cannot create and populate priority_queue. Consider manually add element to priority_queue later?
using System;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;

using Newtonsoft.Json;
using Priority_Queue;

using Discord;
using Discord.Commands;
using Discord.Rest;
using Discord.WebSocket;
using Discord.Addons.Interactive;

using Microsoft.Extensions.Configuration;

public class ReminderModule : InteractiveBase
{
    public class RemindedTask
    {
        public long unixTime;
        public string Message;
        public ulong AuthorID;
        public ulong GuildID;
        public ulong ChannelID;

        public RemindedTask(long _unixTime = -1, string _Message = null, ulong _AuthorID = 0, ulong _GuildID = 0, ulong _ChannelID = 0)
        {
            unixTime = _unixTime;
            Message  = _Message;
            AuthorID   = _AuthorID;
            GuildID = _GuildID;
            ChannelID  = _ChannelID;
        }
    }

    public class TaskListJson
    {
        public List<RemindedTask> List;
        public bool _isLocked;

        public TaskListJson()
        {
            List = new List<RemindedTask>();
            _isLocked = false;
        }
    }

    public class TaskList
    {
        public SimplePriorityQueue<RemindedTask> List;
        private bool _isLocked;

        public bool Lock()
        {
            if (_isLocked == true) return false;
            _isLocked = true;
            return true;
        }
        public bool Unlock()
        {
            if (_isLocked == false) return false;
            _isLocked = false;
            return true;
        }
        public bool isLocked()
        {
            return _isLocked;
        }
        public bool Empty()
        {
            return List.Count == 0;
        }

        public bool Enqueue(RemindedTask task)
        {
            if (List.Contains(task))
                return false;
            List.Enqueue(task, task.unixTime);
            return true;
        }
        public RemindedTask Dequeue()
        {
            if (Empty())
                return new RemindedTask();
            return List.Dequeue();
        }

        public TaskList(bool __isLocked = false)
        {
            List = new SimplePriorityQueue<RemindedTask>();
            _isLocked = __isLocked;
        }

        public TaskList(TaskListJson _taskListJson)
        {
            List = new SimplePriorityQueue<RemindedTask>();
            _isLocked = _taskListJson._isLocked;

            foreach (var task in _taskListJson.List)
                List.Enqueue(task, task.unixTime);
        }
    }


    readonly IConfigurationRoot _config;
    readonly DiscordSocketClient _client;
    public TaskList _TaskList;
    static string log = "[reminder]";

    public ReminderModule(IConfigurationRoot config, DiscordSocketClient client)
    {
        _config = config;
        _client = client;
        //_TaskList = Load();
    }


    [Command("remind", RunMode = RunMode.Async)]
    [Summary("reminder using date, UTC+7")]
    public async Task Remind(int h, int m, int day, int month, int year, [Remainder]string text)
    {
        try
        {
            for (int i = 0; i < 5 && _TaskList.isLocked(); i++)
            {
                var msg = await SendMessage("Có lỗi xảy ra. Tự động thử lại sau 5s.");
                await Task.Delay(TimeSpan.FromSeconds(3));
                await msg.DeleteAsync();
            }
            if (_TaskList.isLocked())
            {
                await LogDiscord($"Reminder: {h} {m} {day} {month} {year} {text} *from* {Context.Message.Author.Username} failed due to locked TaskList.");
                await Context.Channel.TriggerTypingAsync();
                await ReplyAndDeleteAsync($"{Context.Message.Author.Mention}, đã thử lại quá 5 lần. Bạn vui lòng hãy đợi 1 phút và thử lại.", timeout: TimeSpan.FromMinutes(1));
                return;
            }

            _TaskList.Lock();
            DateTimeOffset dto = new DateTimeOffset(year, month, day, h, m, 0, TimeSpan.FromHours(7));
            RemindedTask task = new RemindedTask(dto.ToUnixTimeSeconds(), text, Context.Message.Author.Id, Context.Guild.Id, Context.Channel.Id);

            if (task.unixTime < DateTimeOffset.UtcNow.ToUnixTimeSeconds() + 30)
            {
                await LogDiscord($"Reminder: {h} {m} {day} {month} {year} {text} *from* {Context.Message.Author.Username} failed due to invalid time.");
                await Context.Channel.TriggerTypingAsync();
                await ReplyAndDeleteAsync($"{Context.Message.Author.Mention}, thời gian không hợp lệ. Hãy đặt thời gian cách hiện tại ít nhất 1 phút.", timeout: TimeSpan.FromMinutes(1));
                return;
            }
            if (!_TaskList.Enqueue(task))
            {
                await Context.Channel.TriggerTypingAsync();
                await ReplyAndDeleteAsync($"{Context.Message.Author.Mention}, bạn đã lưu lời nhắc này trước đây.", timeout: TimeSpan.FromSeconds(10));
                return;
            }

            Console.WriteLine("saving");
            if (!Save(_TaskList))
            {
                await LogDiscord($"Reminder: {h} {m} {day} {month} {year} {text} *from* {Context.Message.Author.Username} save failed.");
                await Context.Channel.TriggerTypingAsync();
                await ReplyAndDeleteAsync($"{Context.Message.Author.Mention}, có lỗi xảy ra trong quá trình lưu. Bạn hãy thử lại.", timeout: TimeSpan.FromMinutes(1));

                _TaskList.Unlock();
                return;
            }
            _TaskList.Unlock();
            Console.WriteLine("saved");

            await LogDiscord($"Reminder: {h} {m} {day} {month} {year} {text} *from* {Context.Message.Author.Username} saved.");
            await Context.Channel.TriggerTypingAsync();
            await ReplyAndDeleteAsync($"{Context.Message.Author.Mention}, đã lưu lời nhắc thành công!", timeout: TimeSpan.FromMinutes(1));
        }
        catch (Exception e)
        {
            Console.WriteLine($"{log} {e.ToString()}");

            _TaskList.Unlock();
            await LogDiscord($"Reminder: {h} {m} {day} {month} {year} {text} *from* {Context.Message.Author.Username} save failed.");
            await Context.Channel.TriggerTypingAsync();
            await ReplyAndDeleteAsync($"{Context.Message.Author.Mention}, có lỗi xảy ra. Bạn hãy thử lại.", timeout: TimeSpan.FromMinutes(1));
        }
    }


    public RemindedTask Dequeue_and_Save(bool locked = false)
    {
        if (!locked && _TaskList.isLocked())
            return new RemindedTask();

        _TaskList.Lock();
        var task = _TaskList.Dequeue();
        Save(_TaskList);
        if (!locked) _TaskList.Unlock();

        return task;
    }

    private bool Save(TaskList taskList)
    {
        try
        {
            taskList.Unlock();
            File.WriteAllText(_config.GetValue<string>("json:ReminderTaskList.json"), JsonConvert.SerializeObject(taskList));
            return true;
        }
        catch (Exception e)
        {
            Console.WriteLine($"{log} {e.ToString()}");
            return false;
        }
    }

    private TaskList Load()
    {
        try
        {
            Console.WriteLine("loading");
            var taskListJson = JsonConvert.DeserializeObject<TaskListJson>(File.ReadAllText(_config.GetValue<string>("json:ReminderTaskList.json")));

            var taskList = new TaskList(taskListJson);
            if (taskList.isLocked())
                taskList.Unlock();
            return taskList;
        }
        catch (NullReferenceException)
        {
            return new TaskList();
        }
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