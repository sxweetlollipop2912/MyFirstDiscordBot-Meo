using System;
using System.Threading.Tasks;
using Discord.Addons.Interactive;
using Discord.Commands;

/*public class SampleModule : InteractiveBase
{
    // DeleteAfterAsync will send a message and asynchronously delete it after the timeout has popped
    // This method will not block.
    [Command("delete")]
    public async Task<RuntimeResult> Test_DeleteAfterAsync()
    {
        await ReplyAndDeleteAsync("this message will delete in 10 seconds", timeout: TimeSpan.FromSeconds(10));
        return Ok();
    }

    // NextMessageAsync will wait for the next message to come in over the gateway, given certain criteria
    // By default, this will be limited to messages from the source user in the source channel
    // This method will block the gateway, so it should be ran in async mode.
    [Command("next", RunMode = RunMode.Async)]
    public async Task Test_NextMessageAsync()
    {
        await ReplyAsync("What is 2+2?");
        var response = await NextMessageAsync();
        if (response != null)
            await ReplyAsync($"You replied: {response.Content}");
        else
            await ReplyAsync("You did not reply before the timeout");
    }

    // PagedReplyAsync will send a paginated message to the channel
    // You can customize the paginator by creating a PaginatedMessage object
    // You can customize the criteria for the paginator as well, which defaults to restricting to the source user
    // This method will not block.
    [Command("paginator")]
    public async Task Test_Paginator()
    {
        var pages = new[] { "Page 1", "Page 2", "Page 3", "aaaaaa", "Page 5" };
        await PagedReplyAsync(pages);
    }//

    [Command("How are you?", RunMode = RunMode.Async)]
    public async Task Fine()
    {
        await Context.Channel.TriggerTypingAsync();
        await ReplyAsync("I'm fine, thank you. And you?");

        var response = await NextMessageAsync(timeout: TimeSpan.FromSeconds(60));

        await Context.Channel.TriggerTypingAsync();
        if (response != null)
        {
            if (response.Content.Contains("good"))
                await ReplyAsync("And for the rest of your day too!");
            else
                await ReplyAsync("Awww. Is there anything I can do to cheer you up?");
        }
        else
            await ReplyAsync("Awww, you're gone. :frowning:");
    }
}*/