using SC2APIProtocol;
using System;
using SC2Sharp.Agents;
using SC2Sharp.Builds.BuildLists;
using SC2Sharp.Micro;
using SC2Sharp.StrategyAnalysis;
using SC2Sharp.Tasks;

namespace SC2Sharp.Builds.Protoss
{
    public class TwoBaseRobo : Build
    {
        private TimingAttackTask attackTask = new TimingAttackTask() { RequiredSize = 32, RetreatSize = 12 };
        private TimingAttackTask PokeTask = new TimingAttackTask() { RequiredSize = 10 };
        private TimedObserverTask TimedObserverTask = new TimedObserverTask();
        private WorkerScoutTask WorkerScoutTask = new WorkerScoutTask() { StartFrame = 600 };
        private bool Attacking = false;
        public bool UseImmortals = false;
        public bool UseStalkers = true;

        public override string Name()
        {
            return "TwoBaseRobo";
        }

        public override void OnStart(Bot bot)
        {
            bot.TaskManager.Add(new DefenseTask() { ExpandDefenseRadius = 18 });
            bot.TaskManager.Add(attackTask);
            if (bot.EnemyRace == Race.Zerg)
                bot.TaskManager.Add(PokeTask);
            bot.TaskManager.Add(WorkerScoutTask);
            bot.TaskManager.Add(new ObserverScoutTask());
            bot.TaskManager.Add(new AdeptScoutTask());
            if (bot.BaseManager.Pocket != null)
                bot.TaskManager.Add(new ScoutProxyTask(bot.BaseManager.Pocket.BaseLocation.Pos));
            MicroControllers.Add(new StalkerController());
            MicroControllers.Add(new StutterController());
            MicroControllers.Add(new HTController());
            MicroControllers.Add(new ColloxenController());

            Set += ProtossBuildUtil.Pylons();
            Set += EmergencyGateways();
            Set += Nexii();
            Set += MainBuild();
        }

        private BuildList Nexii()
        {
            BuildList result = new BuildList();

            result.If(() => { return Bot.Main.EnemyRace != Race.Terran || Count(UnitTypes.GATEWAY) >= 2; });
            if (Bot.Main.EnemyRace == Race.Zerg)
                result.If(() => { return !EarlyPool.Get().Detected || Expanded.Get().Detected || Completed(UnitTypes.ZEALOT) >= 2; });
            result.Building(UnitTypes.NEXUS, 2);
            result.If(() => { return Attacking; });
            result.Building(UnitTypes.NEXUS);

            return result;
        }

        private BuildList EmergencyGateways()
        {
            BuildList result = new BuildList();

            result.If(() => { return EarlyPool.Get().Detected && !Expanded.Get().Detected; });
            result.Building(UnitTypes.PYLON);
            result.Building(UnitTypes.GATEWAY, 2);

            return result;
        }

        private BuildList MainBuild()
        {
            BuildList result = new BuildList();

            result.Building(UnitTypes.PYLON);
            result.Building(UnitTypes.GATEWAY);
            if (Bot.Main.EnemyRace != Race.Terran)
                result.If(() => { return !EarlyPool.Get().Detected || Expanded.Get().Detected || Completed(UnitTypes.ZEALOT) >= 2; });
            result.Building(UnitTypes.ASSIMILATOR);
            result.Building(UnitTypes.PYLON, Natural, NaturalDefensePos);
            result.Building(UnitTypes.CYBERNETICS_CORE);
                result.Building(UnitTypes.ASSIMILATOR, 2);
                result.Building(UnitTypes.GATEWAY);
            result.Building(UnitTypes.PYLON);
            result.Building(UnitTypes.PYLON, Natural);
            result.Building(UnitTypes.ROBOTICS_FACILITY);
            result.Building(UnitTypes.PYLON, 2);
            result.Building(UnitTypes.ROBOTICS_BAY, () => { return !UseImmortals; });
            result.Building(UnitTypes.PYLON, Natural);
            result.Building(UnitTypes.ROBOTICS_FACILITY);
            result.Building(UnitTypes.ASSIMILATOR);
            result.Building(UnitTypes.GATEWAY, 2);
            result.Building(UnitTypes.PYLON, Natural);
            result.Building(UnitTypes.TWILIGHT_COUNSEL);
            result.Building(UnitTypes.PYLON, Natural);
            result.If(() => { return Count(UnitTypes.COLOSUS) >= 6; } );
            result.Building(UnitTypes.GATEWAY, 2);

            return result;
        }

        public override void OnFrame(Bot bot)
        {
            if (Count(UnitTypes.ZEALOT) + Count(UnitTypes.ADEPT) + Count(UnitTypes.STALKER) + Count(UnitTypes.COLOSUS) + Count(UnitTypes.IMMORTAL) >= attackTask.RequiredSize)
                Attacking = true;

            if (StrategyAnalysis.CannonRush.Get().Detected)
                attackTask.RequiredSize = 5;

            if (bot.EnemyRace == Race.Zerg && !PokeTask.Stopped)
            {
                if (bot.EnemyStrategyAnalyzer.Count(UnitTypes.ZERGLING) >= 10
                    || bot.EnemyStrategyAnalyzer.Count(UnitTypes.SPINE_CRAWLER) >= 2
                    || bot.EnemyStrategyAnalyzer.Count(UnitTypes.ROACH) >= 5)
                {
                    PokeTask.Stopped = true;
                    PokeTask.Clear();
                }
            }

            if (EarlyPool.Get().Detected && !Expanded.Get().Detected && Completed(UnitTypes.ZEALOT) < 2)
            {
                foreach (Agent agent in bot.UnitManager.Agents.Values)
                    if (agent.Unit.UnitType == UnitTypes.NEXUS
                        && agent.Unit.BuildProgress < 0.99)
                        agent.Order(Abilities.CANCEL);
            }

            if (MassHydra.Get().Detected)
            {
                UseStalkers = false;
                UseImmortals = false;
            }
            else if (MassRoach.Get().Detected)
            {
                UseStalkers = true;
                UseImmortals = true;
            }
            else if (bot.EnemyStrategyAnalyzer.EncounteredEnemies.Contains(UnitTypes.HYDRALISK) || bot.EnemyStrategyAnalyzer.EncounteredEnemies.Contains(UnitTypes.HYDRALISK_DEN))
            {
                UseStalkers = false;
                UseImmortals = false;
            }
            else if (bot.EnemyStrategyAnalyzer.EncounteredEnemies.Contains(UnitTypes.ROACH) || bot.EnemyStrategyAnalyzer.EncounteredEnemies.Contains(UnitTypes.ROACH_WARREN))
            {
                UseStalkers = true;
                UseImmortals = true;
            }
            else
            {
                UseStalkers = bot.EnemyRace == Race.Protoss || (MassRoach.Get().DetectedPreviously && !MassHydra.Get().DetectedPreviously);
                UseImmortals = (MassRoach.Get().DetectedPreviously && !MassHydra.Get().DetectedPreviously);
            }
        }

        public override void Produce(Bot bot, Agent agent)
        {
            if (Count(UnitTypes.PROBE) >= 24
                && Count(UnitTypes.NEXUS) < 2
                && Minerals() < 450)
                return;
            if (agent.Unit.UnitType == UnitTypes.NEXUS
                && Minerals() >= 50
                && Count(UnitTypes.PROBE) < 44 - Completed(UnitTypes.ASSIMILATOR)
                && (Count(UnitTypes.NEXUS) >= 2 || Count(UnitTypes.PROBE) < 18 + 2 * Completed(UnitTypes.ASSIMILATOR)))
            {
                if (Count(UnitTypes.PROBE) < 13 || Count(UnitTypes.PYLON) > 0)
                    agent.Order(1006);
            }
            else if (agent.Unit.UnitType == UnitTypes.GATEWAY)
            {
                if (Count(UnitTypes.ZEALOT) >= 6
                    && Count(UnitTypes.NEXUS) < 2
                    && Minerals() < 500)
                    return;
                if (Attacking && Count(UnitTypes.NEXUS) < 3)
                    return;
                if (EarlyPool.Get().Detected && !Expanded.Get().Detected)
                {
                    if (Minerals() >= 100
                        && (Completed(UnitTypes.CYBERNETICS_CORE) == 0 || Count(UnitTypes.ZEALOT) <= Math.Max(2, Count(UnitTypes.ADEPT))))
                        agent.Order(916);
                    else if (Completed(UnitTypes.CYBERNETICS_CORE) > 0
                        && Minerals() >= 100
                        && Gas() >= 25)
                        agent.Order(922);
                }
                else if (!UseStalkers)
                {
                    if (Minerals() >= 50
                        && Gas() >= 150
                        && Completed(UnitTypes.TEMPLAR_ARCHIVE) > 0
                        && Count(UnitTypes.HIGH_TEMPLAR) * 5 < Count(UnitTypes.STALKER) + Count(UnitTypes.ZEALOT) + Count(UnitTypes.ADEPT))
                        agent.Order(919);
                    else if (Minerals() >= 100
                        && (Completed(UnitTypes.CYBERNETICS_CORE) == 0 || Count(UnitTypes.ZEALOT) <= Count(UnitTypes.ADEPT))
                        && (Count(UnitTypes.ZEALOT) < 10 || Gas() < 25))
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
            else if (agent.Unit.UnitType == UnitTypes.ROBOTICS_FACILITY)
            {
                if (Attacking && Count(UnitTypes.NEXUS) < 3)
                    return;
                if (Count(UnitTypes.OBSERVER) == 0
                    && Minerals() >= 25
                    && Gas() >= 75)
                {
                    agent.Order(977);
                }
                else if (Completed(UnitTypes.ROBOTICS_BAY) > 0
                    && Minerals() >= 300
                    && Gas() >= 200
                    && !UseImmortals
                    && Count(UnitTypes.COLOSUS) < 7)
                {
                    agent.Order(978);
                }
                else if (Minerals() >= 250
                    && Gas() >= 100
                    && UseImmortals)
                {
                    agent.Order(979);
                }
            }
            else if (agent.Unit.UnitType == UnitTypes.ROBOTICS_BAY)
            {
                if (Minerals() >= 150
                    && Gas() >= 150
                    && !Bot.Main.Observation.Observation.RawData.Player.UpgradeIds.Contains(50)
                    && !UseImmortals)
                {
                    agent.Order(1097);
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
                if (UseStalkers)
                {
                    if (!Bot.Main.Observation.Observation.RawData.Player.UpgradeIds.Contains(87)
                         && Minerals() >= 150
                         && Gas() >= 150)
                        agent.Order(1593);
                }
                else
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
}
