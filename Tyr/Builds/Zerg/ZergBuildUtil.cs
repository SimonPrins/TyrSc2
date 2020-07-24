using SC2APIProtocol;
using Tyr.Agents;
using Tyr.Builds.BuildLists;
using Tyr.StrategyAnalysis;

namespace Tyr.Builds.Zerg
{
    public class ZergBuildUtil
    {
        private static bool SmellCheese = false;
        private static RushDefense RushDefense = new RushDefense();

        public static BuildList Overlords()
        {
            BuildList result = new BuildList();
            result.If(() => { return Bot.Bot.UnitManager.Count(UnitTypes.SPAWNING_POOL) > 0 
                && Build.FoodUsed() >= Build.ExpectedAvailableFood() 
                    - 2 * Bot.Bot.UnitManager.Completed(UnitTypes.HATCHERY)
                    - 16 * Bot.Bot.UnitManager.Completed(UnitTypes.ULTRALISK_CAVERN)
                    - (Bot.Bot.UnitManager.Count(UnitTypes.HATCHERY) >= 4 && Bot.Bot.UnitManager.Count(UnitTypes.DRONE) >= 40 ? 8 : 0); });
            result.Morph(UnitTypes.OVERLORD, 25);
            return result;
        }

        public static Build GetDefenseBuild()
        {
            if (!SmellCheese)
            {
                if (Bot.Bot.EnemyRace == Race.Terran)
                {
                    if (FourRax.Get().Detected
                        || (Bot.Bot.Frame >= 22.4 * 85 && !Bot.Bot.EnemyStrategyAnalyzer.NoProxyTerranConfirmed && Bot.Bot.TargetManager.PotentialEnemyStartLocations.Count == 1)
                        || ReaperRush.Get().Detected)
                    {
                        RushDefense.OnStart(Bot.Bot);
                        SmellCheese = true;
                    }
                }
                else if (Bot.Bot.EnemyRace == Race.Protoss)
                {
                    if ((Bot.Bot.Frame >= 22.4 * 60 * 1.5
                        && !Bot.Bot.EnemyStrategyAnalyzer.NoProxyGatewayConfirmed)
                        || (Bot.Bot.Frame < 22.4 * 60 * 1.5 && ThreeGate.Get().Detected))
                    {
                        RushDefense.OnStart(Bot.Bot);
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
