using System;
using System.ServiceModel;

namespace LegacySyntax
{
    // MIG3004 — server-side WCF via ServiceHost.
    public class WcfHost
    {
        public void Open()
        {
            var host = new ServiceHost(typeof(WcfHost));
            host.Open();
        }
    }
}
