using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Audio;
using Discord.WebSocket;

public class AudioService
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
                await CancelFfmpeg();
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
                await CancelFfmpeg();

                Console.WriteLine($"[info][{LogSeverity.Debug}] Starting playback of {path} in {guild.Name}");

                using (var ffmpeg = CreateProcess(path))
                using (var stream = client.CreatePCMStream(AudioApplication.Music))
                {
                    try
                    {
                        Global.cts = new CancellationTokenSource();
                        Global.streaming = true;
                        await ffmpeg.StandardOutput.BaseStream.CopyToAsync(stream, Global.cts.Token);
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
            FileName = "ffmpeg.exe",
            Arguments = $"-hide_banner -loglevel panic -i \"{path}\" -af volume=0.9 -ac 2 -f s16le -ar 48000 pipe:1",
            UseShellExecute = false,
            RedirectStandardOutput = true
        });
    }


    public async Task CancelFfmpeg()
    {
        Global.cts.Cancel();

        Console.WriteLine("[log] Playback terminated.");
        await Task.Delay(TimeSpan.FromMilliseconds(1000));
    }


    public async Task KillFfmpeg()
    {
        Process killFfmpeg = new Process();
        ProcessStartInfo taskkillStartInfo = new ProcessStartInfo
        {
            FileName = "taskkill",
            Arguments = "/F /IM ffmpeg.exe",
            UseShellExecute = false,
            CreateNoWindow = true
        };

        killFfmpeg.StartInfo = taskkillStartInfo;
        killFfmpeg.Start();

        Console.WriteLine("[log] Playback terminated.");
        await Task.Delay(TimeSpan.FromMilliseconds(1000));
    }

}