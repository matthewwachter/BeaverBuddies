using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TimberNet
{
    public class ConnectionFailureException : Exception
    {
        public ConnectionFailureException() : base("Client connection timed out") { }
    }

    public class TimberClient : TimberNetBase
    {
        private const int HEARTBEAT_TIMEOUT_MS = 30000; // 30 seconds

        private readonly ISocketStream client;
        private long lastEventReceivedTicks = DateTime.UtcNow.Ticks;

        public override bool ShouldTick => base.ShouldTick && receivedEvents.Count > 0;

        public bool IsTimedOut
        {
            get
            {
                long elapsed = (DateTime.UtcNow.Ticks - lastEventReceivedTicks) / TimeSpan.TicksPerMillisecond;
                return Started && elapsed > HEARTBEAT_TIMEOUT_MS;
            }
        }

        public TimberClient(ISocketStream client) : base()
        {
            this.client = client;
        }

        public override void DoUserInitiatedEvent(JObject message)
        {
            // Don't actually do the event (i.e. add it to the hash)
            // Wait for the server to confirm w/ adjusted Tick
            SendEvent(client, message);
        }

        protected override void ProcessReceivedEvent(JObject message)
        {
            base.ProcessReceivedEvent(message);
            lastEventReceivedTicks = DateTime.UtcNow.Ticks;
            Log($"Received event: {message[TYPE_KEY]?.ToString() ?? "<null>"}");
            AddEventToHash(message);
        }

        public override void Start()
        {
            base.Start();
            lastEventReceivedTicks = DateTime.UtcNow.Ticks;
            // TODO: Handle async properly and cleanup
            // TODO: Make wait configurable?
            if (!client.ConnectAsync().Wait(3000))
            {
                throw new ConnectionFailureException();
            }
            // Connect a TCP socket at the address
            Task.Run(() => StartListening(client, true));
        }

        public override List<JObject> ReadEvents(int ticksSinceLoad)
        {
            if (IsTimedOut)
            {
                Log($"Server heartbeat timeout after {HEARTBEAT_TIMEOUT_MS}ms");
                Close();
                return new List<JObject>();
            }
            return base.ReadEvents(ticksSinceLoad);
        }


        public override void Close()
        {
            base.Close();
            client.Close();
        }
    }
}
