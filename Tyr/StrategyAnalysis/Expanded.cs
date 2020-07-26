using SC2APIProtocol;
using SC2Sharp.Agents;
using SC2Sharp.Util;

namespace SC2Sharp.StrategyAnalysis
{
    public class Expanded : Strategy
    {
        private static Strategy Singleton = new Expanded();

        public static Strategy Get()
        {
            return Singleton;
        }

        public override bool Detect()
        {
            if (Count(UnitTypes.NEXUS)
                   + Count(UnitTypes.HATCHERY)
                   + Count(UnitTypes.LAIR)
                   + Count(UnitTypes.HIVE)
                   + Count(UnitTypes.COMMAND_CENTER)
                   + Count(UnitTypes.COMMAND_CENTER_FLYING)
                   + Count(UnitTypes.ORBITAL_COMMAND)
                   + Count(UnitTypes.ORBITAL_COMMAND_FLYING)
                   + Count(UnitTypes.PLANETARY_FORTRESS) >= 2)
                return true;

            foreach (Unit enemy in Bot.Main.Enemies())
            {
                if (UnitTypes.ResourceCenters.Contains(enemy.UnitType))
                {
                    bool startingBase = false;
                    foreach (Point2D loc in Bot.Main.TargetManager.PotentialEnemyStartLocations)
                    {
                        if (SC2Util.DistanceSq(enemy.Pos, loc) <= 4)
                        {
                            startingBase = true;
                            break;
                        }
                    }
                    if (!startingBase)
                        return true;
                }
            }
            return false;
        }

        public override string Name()
        {
            return "Expanded";
        }
    }
}
