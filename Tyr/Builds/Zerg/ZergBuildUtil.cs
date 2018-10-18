using SC2APIProtocol;
using Tyr.Agents;
using Tyr.Builds.BuildLists;

namespace Tyr.Builds.Zerg
{
    public class ZergBuildUtil
    {
        private static bool SmellCheese = false;
        private static RushDefense RushDefense = new RushDefense();

        public static BuildList Overlords()
        {
            BuildList result = new BuildList();
            result.If(() => { return Tyr.Bot.UnitManager.Count(UnitTypes.SPAWNING_POOL) > 0 && Build.FoodUsed() >= Build.ExpectedAvailableFood() - 2; });
            result.Morph(UnitTypes.OVERLORD, 25);
            return result;
        }

        public static Build GetDefenseBuild()
        {
            if (!SmellCheese)
            {
                if (Tyr.Bot.EnemyRace == Race.Terran)
                {
                    if (Tyr.Bot.EnemyStrategyAnalyzer.FourRaxDetected
                        || (Tyr.Bot.Frame >= 22.4 * 85 && !Tyr.Bot.EnemyStrategyAnalyzer.NoProxyTerranConfirmed && Tyr.Bot.TargetManager.PotentialEnemyStartLocations.Count == 1)
                        || Tyr.Bot.EnemyStrategyAnalyzer.ReaperRushDetected)
                    {
                        RushDefense.OnStart(Tyr.Bot);
                        SmellCheese = true;
                    }
                }
                else if (Tyr.Bot.EnemyRace == Race.Protoss)
                {
                    if ((Tyr.Bot.Frame >= 22.4 * 60 * 1.5
                        && !Tyr.Bot.EnemyStrategyAnalyzer.NoProxyGatewayConfirmed)
                        || (Tyr.Bot.Frame < 22.4 * 60 * 1.5 && Tyr.Bot.EnemyStrategyAnalyzer.ThreeGateDetected))
                    {
                        RushDefense.OnStart(Tyr.Bot);
                        SmellCheese = true;
                    }
                }
            }

            if (Tyr.Bot.EnemyStrategyAnalyzer.WorkerRushDetected)
                SmellCheese = true;


            if (SmellCheese)
                return RushDefense;
            else
                return null;
        }
    }
}
