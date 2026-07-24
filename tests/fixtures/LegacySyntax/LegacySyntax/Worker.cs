using System.Threading;

namespace LegacySyntax
{
    public class Worker
    {
        private Thread _thread;

        public void Stop()
        {
            _thread.Abort(); // MIG4008 — Thread.Abort
        }
    }
}
