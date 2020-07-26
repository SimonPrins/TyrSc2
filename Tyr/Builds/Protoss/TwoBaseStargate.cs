using SC2Sharp.Agents;
using SC2Sharp.Builds.BuildLists;
using SC2Sharp.Micro;
using SC2Sharp.Tasks;

namespace SC2Sharp.Builds.Protoss
{
    public class TwoBaseStargate : Build
    {
        private TimingAttackTask attackTask = new TimingAttackTask() { RequiredSize = 35 };
        private bool Attacking = false;
        public bool UseStalkers = false;
        private bool HarassDone = false;

        public override string Name()
        {
            return "TwoBaseStargate";
        }

        public override void OnStart(Bot bot)
        {
            bot.TaskManager.Add(new DefenseTask());
            bot.TaskManager.Add(attackTask);
            bot.TaskManager.Add(new WorkerScoutTask());
            bot.TaskManager.Add(new ObserverScoutTask());
            bot.TaskManager.Add(new OracleHarassTask());
            if (bot.BaseManager.Pocket != null)
                bot.TaskManager.Add(new ScoutProxyTask(bot.BaseManager.Pocket.BaseLocation.Pos));
            MicroControllers.Add(new StutterController());
            MicroControllers.Add(new HTController());

            Set += ProtossBuildUtil.Pylons();
            Set += Nexii();
            Set += MainBuild();
        }

        private BuildList Nexii()
        {
            BuildList result = new BuildList();

            result.Building(UnitTypes.NEXUS, 2);
            result.If(() => { return Attacking; });
            result.Building(UnitTypes.NEXUS);

            return result;
        }

        private BuildList MainBuild()
        {
            BuildList result = new BuildList();

            result.Building(UnitTypes.PYLON);
            result.Building(UnitTypes.GATEWAY);
            result.Building(UnitTypes.ASSIMILATOR);
            result.Building(UnitTypes.PYLON, Natural);
            result.Building(UnitTypes.CYBERNETICS_CORE);
            result.Building(UnitTypes.PYLON);
            result.Building(UnitTypes.PYLON, Natural);
            result.Building(UnitTypes.STARGATE);
            result.Building(UnitTypes.STARGATE);
            result.Building(UnitTypes.PYLON, 2);
            result.Building(UnitTypes.PYLON, Natural);
            result.Building(UnitTypes.ASSIMILATOR, 3);
            result.Building(UnitTypes.GATEWAY, 3);
            result.Building(UnitTypes.PYLON, Natural);
            result.Building(UnitTypes.TWILIGHT_COUNSEL);
            result.Building(UnitTypes.PYLON, Natural);

            return result;
        }

        public override void OnFrame(Bot bot)
        {
            if (Count(UnitTypes.ZEALOT) + Count(UnitTypes.ADEPT) + Count(UnitTypes.STALKER) + Count(UnitTypes.VOID_RAY) >= attackTask.RequiredSize)
                Attacking = true;

            if (StrategyAnalysis.CannonRush.Get().Detected)
                attackTask.RequiredSize = 5;

            if (Count(UnitTypes.ORACLE) >= 2)
                HarassDone = true;
        }

        public override void Produce(Bot bot, Agent agent)
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
                if (Attacking && Count(UnitTypes.NEXUS) < 3)
                    return;
                if (!UseStalkers)
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
                        && Gas() <= 200
                        && (Completed(UnitTypes.CYBERNETICS_CORE) == 0 || Count(UnitTypes.ZEALOT) <= Count(UnitTypes.STALKER)))
                        agent.Order(916);
                    else if (Completed(UnitTypes.CYBERNETICS_CORE) > 0
                        && Minerals() >= 125
                        && Gas() >= 50)
                        agent.Order(917);
                }
            }
            else if (agent.Unit.UnitType == UnitTypes.STARGATE)
            {
                if (!HarassDone
                    && Count(UnitTypes.ORACLE) < 2)
                {
                    if (Minerals() >= 150
                    && Gas() >= 150)
                        agent.Order(954);
                }
                else if (Minerals() >= 250
                    && Gas() >= 150
                    && FoodUsed() + 4 <= 200)
                    agent.Order(950);
            }
            else if (agent.Unit.UnitType == UnitTypes.CYBERNETICS_CORE)
            {
                if (Count(UnitTypes.VOID_RAY) >= 1)
                {
                    if (!bot.Observation.Observation.RawData.Player.UpgradeIds.Contains(78))
                        agent.Order(1562);
                    else if (!bot.Observation.Observation.RawData.Player.UpgradeIds.Contains(81))
                        agent.Order(1565);
                }
            }
            else if (agent.Unit.UnitType == UnitTypes.TEMPLAR_ARCHIVE)
            {
                if (!Bot.Main.Observation.Observation.RawData.Player.UpgradeIds.Contains(52)
                    && Minerals() >= 200
                    && Gas() >= 200)
                    agent.Order(1126);
            }
            else if (agent.Unit.UnitType == UnitTypes.TWILIGHT_COUNSEL)
            {
                if (!Bot.Main.Observation.Observation.RawData.Player.UpgradeIds.Contains(130)
                    && Minerals() >= 100
                    && Gas() >= 100)
                    agent.Order(1594);
                else if (!Bot.Main.Observation.Observation.RawData.Player.UpgradeIds.Contains(86)
                     && Minerals() >= 100
                     && Gas() >= 100)
                    agent.Order(1592);
            }
        }
    }
}
