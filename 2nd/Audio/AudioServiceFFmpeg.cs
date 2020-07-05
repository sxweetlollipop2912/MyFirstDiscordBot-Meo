using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Audio;
using Discord.WebSocket;

public class AudioServiceFFmpeg
{
    private readonly ConcurrentDictionary<ulong, IAudioClient> ConnectedChannels = new ConcurrentDictionary<ulong, IAudioClient>();

    public static class Global
    {
        public static bool streaming = false;
        public static CancellationTokenSource cts = new CancellationTokenSource();
    }


    public Task<IVoiceChannel> connectedVChannel(IGuild guild)
    {
        SocketGuild socketGuild = guild as SocketGuild;
        var channel = (socketGuild.CurrentUser as IGuildUser)?.VoiceChannel;
        return Task.FromResult(channel as IVoiceChannel);
    }
    public Task<bool> isGuildAdded(IGuild guild)
    {
        return Task.FromResult(ConnectedChannels.TryGetValue(guild.Id, out _));
    }


    public async Task JoinAudio(IGuild guild, IVoiceChannel target)
    {
        try
        {
            if (isGuildAdded(guild).Result)
            {
                if (connectedVChannel(guild).Result != null) return;
                ConnectedChannels.Clear();
            }

            if (target.Guild.Id != guild.Id)
            {
                return;
            }

            var audioClient = await target.ConnectAsync();
            if (ConnectedChannels.TryAdd(guild.Id, audioClient))
            {
                Console.WriteLine($"[info][{LogSeverity.Info}] Connected to voice on {guild.Name}.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[log] {ex.ToString()}");
        }
    }


    public async Task LeaveAudio(IGuild guild)
    {
        try
        {
            IAudioClient client;
            if (ConnectedChannels.TryRemove(guild.Id, out client))
            {
                await CancelFFmpeg();
                await client.StopAsync();
                Console.WriteLine($"[info][{LogSeverity.Info}] Disconnected from the voice on {guild.Name}.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[log] {ex.ToString()}");
        }
    }


    public async Task SendAudioAsync(IGuild guild, string path)
    {
        try
        {
            IAudioClient client;
            if (ConnectedChannels.TryGetValue(guild.Id, out client))
            {
                await CancelFFmpeg();

                Console.WriteLine($"[info][{LogSeverity.Debug}] Starting playback of {path} in {guild.Name}");

                using (var FFmpeg = CreateProcess(path))
                using (var stream = client.CreatePCMStream(AudioApplication.Music))
                {
                    try
                    {
                        Global.cts = new CancellationTokenSource();
                        Global.streaming = true;
                        await FFmpeg.StandardOutput.BaseStream.CopyToAsync(stream, Global.cts.Token);
                    }
                    finally
                    {
                        await stream.FlushAsync();
                        Global.streaming = false;
                    }
                }
            }
            else
            {
                Console.WriteLine("[log] Voice channel is not added.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[log] {ex.ToString()}");
        }
    }


    private Process CreateProcess(string path)
    {
        return Process.Start(new ProcessStartInfo
        {
            FileName = "FFmpeg.exe",
            Arguments = $"-hide_banner -loglevel panic -i \"{path}\" -af volume=0.9 -ac 2 -f wav -ar 48k pipe:1",
            //Arguments = $"-hide_banner -loglevel panic -f dshow -i \"{path}\" -f wav -ar 48k pipe:1",
            UseShellExecute = false,
            RedirectStandardOutput = true
        });
    }


    public async Task CancelFFmpeg()
    {
        Global.cts.Cancel();

        Console.WriteLine("[log] Playback terminated.");
        await Task.Delay(TimeSpan.FromMilliseconds(1000));
    }


    public async Task KillFFmpeg()
    {
        Process killFFmpeg = new Process();
        ProcessStartInfo taskkillStartInfo = new ProcessStartInfo
        {
            FileName = "taskkill",
            Arguments = "/F /IM FFmpeg.exe",
            UseShellExecute = false,
            CreateNoWindow = true
        };

        killFFmpeg.StartInfo = taskkillStartInfo;
        killFFmpeg.Start();

        Console.WriteLine("[log] Playback terminated.");
        await Task.Delay(TimeSpan.FromMilliseconds(1000));
    }

}