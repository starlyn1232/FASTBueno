using System.Threading;

namespace FASTBueno.Utilities
{
    public class Generic
    {
        // Wait using milliseconds
        public static void Wait(int milliSeconds)
        {
            Thread.Sleep(milliSeconds);
        }
    }
}
