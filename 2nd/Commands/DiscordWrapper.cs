using Discord;
using Discord.Commands;
using Discord.Rest;
using Discord.WebSocket;
using System;
using System.Threading.Tasks;


public class DiscordWrapper : ModuleBase<SocketCommandContext> {
    public static void Log(string log) {
        Console.WriteLine(log);
    }

    public static async Task<RestUserMessage> SendMessage(SocketCommandContext context, string content = null, Embed embed = null, ISocketMessageChannel Channel = null) {
        if (Channel == null) Channel = context.Channel;
        await Channel.TriggerTypingAsync();
        return await Channel.SendMessageAsync(text: content, embed: embed);
    }
}