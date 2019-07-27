using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Lib.AspNetCore.ServerSentEvents;
using Microsoft.AspNetCore.SignalR;

namespace server.Hubs
{
    public class StreamHub : Hub
    {
        private readonly IServerSentEventsService _serverSentEventsService;
        private static readonly ConcurrentDictionary<string, ChannelWriter<string>> listeners = new ConcurrentDictionary<string, ChannelWriter<string>>();

        public StreamHub(IServerSentEventsService serverSentEventsService)
        {
            _serverSentEventsService = serverSentEventsService;
        }

        public async Task SendFrame(string frame)
        {
            var html = frame.Replace("\n", "<br/>");

            // Push frame to SSE listeners
            await _serverSentEventsService.SendEventAsync(html);

            // Push frame to IO.Channels listeners
            await BroadcastToStreams(html);
        }



        public ChannelReader<string> GetVideoStream()
        {
            var channel = Channel.CreateUnbounded<string>();

            listeners.AddOrUpdate(Context.ConnectionId, channel.Writer,
                (key, oldValue) => {
                    oldValue.TryComplete();
                    return channel.Writer;
                });
            return channel.Reader;
        }

        public override Task OnDisconnectedAsync(Exception exception)
        {
            if(listeners.TryRemove(Context.ConnectionId, out var writer))
                writer.TryComplete();

            return base.OnDisconnectedAsync(exception);
        }

        private async Task BroadcastToStreams(string frame)
        {
            foreach (var writer in listeners.Values)
                await writer.WriteAsync(frame);
        }
    }
}