// [log]
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

using Discord;
using Discord.Audio;
using Discord.WebSocket;

using NAudio.Wave;

public class AudioServiceNAudio
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
                DiscordWrapper.Log($"[info][{LogSeverity.Info}] Connected to voice on {guild.Name}.");
            }
        }
        catch (Exception ex)
        {
            DiscordWrapper.Log($"[log] {ex}");
        }
    }


    public async Task LeaveAudio(IGuild guild)
    {
        try
        {
            IAudioClient client;
            if (ConnectedChannels.TryRemove(guild.Id, out client))
            {
                await CancelNAudio();
                await client.StopAsync();
                DiscordWrapper.Log($"[info][{LogSeverity.Info}] Disconnected from the voice on {guild.Name}.");
            }
        }
        catch (Exception ex)
        {
            DiscordWrapper.Log($"[log] {ex}");
        }
    }


    public async Task SendAudioAsync(IGuild guild, string path)
    {
        try
        {
            IAudioClient client;
            if (ConnectedChannels.TryGetValue(guild.Id, out client))
            {
                var OutFormat = new WaveFormat(48000, 16, 2);

                var reader = new Mp3FileReader(path);
                var naudio = WaveFormatConversionStream.CreatePcmStream(reader);

                byte[] buffer = new byte[naudio.Length];

                int rest = (int)(naudio.Length - naudio.Position);
                await naudio.ReadAsync(buffer, 0, rest);

                using (var dstream = client.CreatePCMStream(AudioApplication.Music))
                {
                    try
                    {
                        Global.cts = new CancellationTokenSource();
                        Global.streaming = true;
                        await dstream.WriteAsync(buffer, 0, rest, Global.cts.Token);
                    }
                    finally
                    {
                        await dstream.FlushAsync();
                        Global.streaming = false;
                    }
                }
            }
            else
            {
                DiscordWrapper.Log("[log] Voice channel is not added.");
            }
        }
        catch (Exception ex)
        {
            DiscordWrapper.Log($"[log] {ex}");
        }
    }


    public async Task CancelNAudio()
    {
        Global.cts.Cancel();

        DiscordWrapper.Log("[log] Playback terminated.");
        await Task.Delay(TimeSpan.FromMilliseconds(1000));
    }
}