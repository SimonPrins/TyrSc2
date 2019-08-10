using Tyr;

namespace TyrDotnetCore
{
    class TyrDotNetCore
    {
        static void Main(string[] args)
        {
            Program.MyRace = SC2APIProtocol.Race.Protoss;
            Program.Run(args);
        }
    }
}
