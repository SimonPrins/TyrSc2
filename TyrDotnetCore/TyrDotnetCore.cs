using SC2Sharp;

namespace TyrDotnetCore
{
    class TyrDotNetCore
    {
        static void Main(string[] args)
        {
            Program.MyRace = SC2APIProtocol.Race.Terran;
            Program.Run(args);
        }
    }
}
