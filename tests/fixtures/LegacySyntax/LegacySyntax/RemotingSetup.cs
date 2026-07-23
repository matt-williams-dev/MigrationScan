using System.Runtime.Remoting; // MIG3005 — .NET Remoting

namespace LegacySyntax
{
    public class RemotingSetup
    {
        public void Configure()
        {
            RemotingConfiguration.Configure("app.config", false);
        }
    }
}
