using System.Messaging; // MIG3009 — MSMQ

namespace LegacySyntax
{
    public class MessageQueueClient
    {
        public void Send(string path, object body) => new MessageQueue(path).Send(body);
    }
}
