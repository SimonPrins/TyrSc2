using SC2APIProtocol;
using SC2Sharp.Agents;
using SC2Sharp.Builds.BuildLists;
using SC2Sharp.StrategyAnalysis;

namespace SC2Sharp.Builds.Zerg
{
    public class ZergBuildUtil
    {
        private static bool SmellCheese = false;
        private static RushDefense RushDefense = new RushDefense();

        public static BuildList Overlords()
        {
            BuildList result = new BuildList();
            result.If(() => { return Bot.Main.UnitManager.Count(UnitTypes.SPAWNING_POOL) > 0 
                && Build.FoodUsed() >= Build.ExpectedAvailableFood() 
                    - 2 * Bot.Main.UnitManager.Completed(UnitTypes.HATCHERY)
                    - 16 * Bot.Main.UnitManager.Completed(UnitTypes.ULTRALISK_CAVERN)
                    - (Bot.Main.UnitManager.Count(UnitTypes.HATCHERY) >= 4 && Bot.Main.UnitManager.Count(UnitTypes.DRONE) >= 40 ? 8 : 0); });
            result.Morph(UnitTypes.OVERLORD, 25);
            return result;
        }

        public static Build GetDefenseBuild()
        {
            if (!SmellCheese)
            {
                if (Bot.Main.EnemyRace == Race.Terran)
                {
                    if (FourRax.Get().Detected
                        || (Bot.Main.Frame >= 22.4 * 85 && !Bot.Main.EnemyStrategyAnalyzer.NoProxyTerranConfirmed && Bot.Main.TargetManager.PotentialEnemyStartLocations.Count == 1)
                        || ReaperRush.Get().Detected)
                    {
                        RushDefense.OnStart(Bot.Main);
                        SmellCheese = true;
                    }
                }
                else if (Bot.Main.EnemyRace == Race.Protoss)
                {
                    if ((Bot.Main.Frame >= 22.4 * 60 * 1.5
                        && !Bot.Main.EnemyStrategyAnalyzer.NoProxyGatewayConfirmed)
                        || (Bot.Main.Frame < 22.4 * 60 * 1.5 && ThreeGate.Get().Detected))
                    {
                        RushDefense.OnStart(Bot.Main);
                        SmellCheese = true;
                    }
                }
            }

            if (StrategyAnalysis.WorkerRush.Get().Detected)
                SmellCheese = true;


            if (SmellCheese)
                return RushDefense;
            else
                return null;
        }
    }
}
