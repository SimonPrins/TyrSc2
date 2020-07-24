using SC2APIProtocol;
using Tyr.Agents;
using Tyr.Builds.BuildLists;
using Tyr.Micro;
using Tyr.Tasks;

namespace Tyr.Builds.Protoss
{
    public class GatewayPush : Build
    {
        private TimingAttackTask attackTask = new TimingAttackTask() { RequiredSize = 40 };
        public override string Name()
        {
            return "GatewayPush";
        }

        public override void OnStart(Bot tyr)
        {
            tyr.TaskManager.Add(new DefenseTask());
            tyr.TaskManager.Add(attackTask);
            tyr.TaskManager.Add(new WorkerScoutTask());
            if (tyr.BaseManager.Pocket != null)
                tyr.TaskManager.Add(new ScoutProxyTask(tyr.BaseManager.Pocket.BaseLocation.Pos));
            MicroControllers.Add(new StutterController());
            MicroControllers.Add(new HTController());

            Set += ProtossBuildUtil.Pylons();
            Set += MainBuild();
        }

        private BuildList MainBuild()
        {
            BuildList result = new BuildList();

            result.Building(UnitTypes.NEXUS, 2);
            result.Building(UnitTypes.PYLON);
            result.Building(UnitTypes.GATEWAY);
            result.If(() => { return Completed(UnitTypes.GATEWAY) > 0; });
            result.Building(UnitTypes.CYBERNETICS_CORE);
            result.Building(UnitTypes.ASSIMILATOR);
            result.Building(UnitTypes.GATEWAY);
            result.Building(UnitTypes.ASSIMILATOR, 3);
            result.If(() => { return Completed(UnitTypes.CYBERNETICS_CORE) > 0; });
            result.Building(UnitTypes.TWILIGHT_COUNSEL);
            result.Building(UnitTypes.GATEWAY, 3);
            result.If(() => { return Completed(UnitTypes.TWILIGHT_COUNSEL) > 0; });
            result.Building(UnitTypes.TEMPLAR_ARCHIVE);
            result.Building(UnitTypes.GATEWAY, 3);

            return result;
        }

        public override void OnFrame(Bot tyr)
        {
            if (StrategyAnalysis.CannonRush.Get().Detected)
                attackTask.RequiredSize = 5;
        }

        public override void Produce(Bot tyr, Agent agent)
        {
            if (agent.Unit.UnitType == UnitTypes.NEXUS
                && Minerals() >= 50
                && Count(UnitTypes.PROBE) < 44 - Completed(UnitTypes.ASSIMILATOR))
            {
                if (Count(UnitTypes.PROBE) < 13 || Count(UnitTypes.PYLON) > 0)
                    agent.Order(1006);
            }
            else if (agent.Unit.UnitType == UnitTypes.GATEWAY)
            {
                if (Bot.Bot.EnemyRace == Race.Zerg)
                {
                    if (Minerals() >= 50
                        && Gas() >= 150
                        && Completed(UnitTypes.TEMPLAR_ARCHIVE) > 0
                        && Count(UnitTypes.HIGH_TEMPLAR) * 5 < Count(UnitTypes.STALKER) + Count(UnitTypes.ZEALOT) + Count(UnitTypes.ADEPT))
                        agent.Order(919);
                    else if (Minerals() >= 100
                        && (Completed(UnitTypes.CYBERNETICS_CORE) == 0 || Count(UnitTypes.ZEALOT) <= Count(UnitTypes.ADEPT)))
                        agent.Order(916);
                    else if (Completed(UnitTypes.CYBERNETICS_CORE) > 0
                        && Minerals() >= 100
                        && Gas() >= 25)
                        agent.Order(922);
                }
                else
                {
                    if (Minerals() >= 50
                        && Gas() >= 150
                        && Completed(UnitTypes.TEMPLAR_ARCHIVE) > 0
                        && Count(UnitTypes.HIGH_TEMPLAR) * 5 < Count(UnitTypes.STALKER) + Count(UnitTypes.ZEALOT) + Count(UnitTypes.ADEPT))
                        agent.Order(919);
                    else if (Minerals() >= 100
                        && (Completed(UnitTypes.CYBERNETICS_CORE) == 0 || Count(UnitTypes.ZEALOT) <= Count(UnitTypes.STALKER)))
                        agent.Order(916);
                    else if (Completed(UnitTypes.CYBERNETICS_CORE) > 0
                        && Minerals() >= 125
                        && Gas() >= 50)
                        agent.Order(917);
                }
            }
            else if (agent.Unit.UnitType == UnitTypes.TEMPLAR_ARCHIVE)
            {
                if (!Bot.Bot.Observation.Observation.RawData.Player.UpgradeIds.Contains(52)
                    && Minerals() >= 200
                    && Gas() >= 200)
                    agent.Order(1126);
            }
            else if (agent.Unit.UnitType == UnitTypes.TWILIGHT_COUNSEL)
            {
                if (!Bot.Bot.Observation.Observation.RawData.Player.UpgradeIds.Contains(130)
                    && Minerals() >= 100
                    && Gas() >= 100)
                    agent.Order(1594);
                else if (!Bot.Bot.Observation.Observation.RawData.Player.UpgradeIds.Contains(86)
                     && Minerals() >= 100
                     && Gas() >= 100)
                    agent.Order(1592);
            }
        }
    }
}
