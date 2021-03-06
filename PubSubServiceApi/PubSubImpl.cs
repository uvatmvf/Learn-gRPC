using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace PubSubServiceApi
{

    public class PubSubImpl : PubSub.PubSubBase
    {
        private readonly BufferBlock<SubscriptionEvent> _buffer = new BufferBlock<SubscriptionEvent>();

        public Dictionary<string, IServerStreamWriter<Event>> SubscriberWritersMap { get; private set; }

        public PubSubImpl()
        {
            SubscriberWritersMap = new Dictionary<string, IServerStreamWriter<Event>>();
        }

        public override Task<Event> GetAnEvent(Empty request, ServerCallContext context) =>
            Task.FromResult(new Event { Value = DateTime.Now.ToLongTimeString() });

        public void Publish(SubscriptionEvent subscriptionEvent) =>
            _buffer.Post(subscriptionEvent);

        public override async Task Subscribe(Subscription request, IServerStreamWriter<Event> responseStream, ServerCallContext context)
        {
            SubscriberWritersMap[request.Id] = responseStream;

            while (SubscriberWritersMap.Count > 0)
            {
                var subscriptionEvent = await _buffer.ReceiveAsync();
                if (SubscriberWritersMap.ContainsKey(subscriptionEvent.SubscriptionId))
                {
                    await SubscriberWritersMap[subscriptionEvent.SubscriptionId].WriteAsync(subscriptionEvent.Event);
                }
            }
        }

        public override Task<Unsubscription> Unsubscribe(Subscription request, ServerCallContext context)
        {
            SubscriberWritersMap.Remove(request.Id);
            return Task.FromResult(new Unsubscription() { Id = request.Id });
        }

    }
}
