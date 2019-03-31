using SC2APIProtocol;
using System;
using Tyr.Agents;
using Tyr.Builds.BuildLists;
using Tyr.Managers;
using Tyr.Micro;
using Tyr.Tasks;

namespace Tyr.Builds.Protoss
{
    public class TwoBaseZealotImmortal : Build
    {
        private Point2D OverrideDefenseTarget;
        private TimingAttackTask attackTask = new TimingAttackTask() { RequiredSize = 20 };
        private TimedObserverTask TimedObserverTask = new TimedObserverTask();
        private WorkerScoutTask WorkerScoutTask = new WorkerScoutTask() { StartFrame = 600 };

        public override string Name()
        {
            return "TwoBaseZealotImmortal";
        }

        public override void OnStart(Tyr tyr)
        {
            DefenseTask.Enable();
            tyr.TaskManager.Add(attackTask);
            tyr.TaskManager.Add(WorkerScoutTask);
            tyr.TaskManager.Add(new ObserverScoutTask());
            tyr.TaskManager.Add(new AdeptScoutTask());
            if (tyr.BaseManager.Pocket != null)
                tyr.TaskManager.Add(new ScoutProxyTask(tyr.BaseManager.Pocket.BaseLocation.Pos));
            ArchonMergeTask.Enable();

            OverrideDefenseTarget = tyr.MapAnalyzer.Walk(NaturalDefensePos, tyr.MapAnalyzer.EnemyDistances, 15);

            MicroControllers.Add(new StalkerController());
            MicroControllers.Add(new StutterController());
            MicroControllers.Add(new HTController());
            MicroControllers.Add(new ColloxenController());

            Set += ProtossBuildUtil.Pylons();
            Set += EmergencyGateways();
            Set += ExpandBuildings();
            Set += Nexii();
            Set += MainBuild();
        }

        private BuildList Nexii()
        {
            BuildList result = new BuildList();

            result.If(() => { return Tyr.Bot.EnemyRace != Race.Terran || Count(UnitTypes.GATEWAY) >= 2; });
            if (Tyr.Bot.EnemyRace == Race.Zerg)
                result.If(() => { return !Tyr.Bot.EnemyStrategyAnalyzer.EarlyPool || Tyr.Bot.EnemyStrategyAnalyzer.Expanded || Completed(UnitTypes.ZEALOT) >= 2; });
            result.Building(UnitTypes.NEXUS, 2);
            result.If(() => { return attackTask.AttackSent; });
            result.Building(UnitTypes.NEXUS);

            return result;
        }

        private BuildList ExpandBuildings()
        {
            BuildList result = new BuildList();

            result.If(() => { return !Tyr.Bot.EnemyStrategyAnalyzer.EarlyPool; });
            foreach (Base b in Tyr.Bot.BaseManager.Bases)
            {
                if (b == Main)
                    continue;
                result.Building(UnitTypes.PYLON, b, () => b.ResourceCenter != null && b.ResourceCenter.Unit.BuildProgress >= 0.95);
                result.Building(UnitTypes.GATEWAY, b, 2, () => b.ResourceCenter != null && b.ResourceCenter.Unit.BuildProgress >= 0.95 && Completed(b, UnitTypes.PYLON) >= 1 && Minerals() >= 350);
                result.Building(UnitTypes.PYLON, b, () => b.ResourceCenter != null && b.ResourceCenter.Unit.BuildProgress >= 0.95 && Count(b, UnitTypes.GATEWAY) >= 2 && Minerals() >= 700);
            }

            return result;
        }

        private BuildList EmergencyGateways()
        {
            BuildList result = new BuildList();

            result.If(() => { return Tyr.Bot.EnemyStrategyAnalyzer.EarlyPool && !Tyr.Bot.EnemyStrategyAnalyzer.Expanded; });
            result.Building(UnitTypes.PYLON);
            result.Building(UnitTypes.GATEWAY, 2);

            return result;
        }

        private BuildList MainBuild()
        {
            BuildList result = new BuildList();

            result.Building(UnitTypes.NEXUS);
            result.Building(UnitTypes.PYLON);
            result.Building(UnitTypes.GATEWAY, Main);
            if (Tyr.Bot.EnemyRace != Race.Terran)
                result.If(() => { return !Tyr.Bot.EnemyStrategyAnalyzer.EarlyPool || Tyr.Bot.EnemyStrategyAnalyzer.Expanded || Completed(UnitTypes.ZEALOT) >= 2; });
            result.Building(UnitTypes.NEXUS);
            result.Building(UnitTypes.ASSIMILATOR);
            result.Building(UnitTypes.CYBERNETICS_CORE);
            result.Building(UnitTypes.PYLON, Natural);
            result.Building(UnitTypes.ROBOTICS_FACILITY);
            result.Building(UnitTypes.PYLON);
            result.Building(UnitTypes.TWILIGHT_COUNSEL);
            result.Building(UnitTypes.ASSIMILATOR, 2);
            result.Building(UnitTypes.FORGE);
            result.Upgrade(UpgradeType.ProtossGroundWeapons);
            result.Building(UnitTypes.ASSIMILATOR);
            result.If(() => Count(UnitTypes.IMMORTAL) > 0);
            result.Building(UnitTypes.ROBOTICS_FACILITY);
            result.Building(UnitTypes.FORGE);
            result.Upgrade(UpgradeType.ProtossGroundArmor);

            return result;
        }

        public override void OnFrame(Tyr tyr)
        {
            if (tyr.EnemyStrategyAnalyzer.CannonRushDetected)
                attackTask.RequiredSize = 5;
            else if (Completed(UnitTypes.IMMORTAL) >= 4)
                attackTask.RequiredSize = 15;
            else
                attackTask.RequiredSize = 20;

            attackTask.RetreatSize = Tyr.Bot.EnemyRace == Race.Terran ? 0 : 6;

            attackTask.DefendOtherAgents = false;
            
            if (Count(UnitTypes.NEXUS) >= 3)
                IdleTask.Task.OverrideTarget = OverrideDefenseTarget;
            else
                IdleTask.Task.OverrideTarget = null;

            DefenseTask.GroundDefenseTask.ExpandDefenseRadius = 25;
            DefenseTask.GroundDefenseTask.MainDefenseRadius = 30;
            DefenseTask.GroundDefenseTask.MaxDefenseRadius = 120;
            DefenseTask.GroundDefenseTask.DrawDefenderRadius = 80;

            if (Tyr.Bot.EnemyStrategyAnalyzer.EarlyPool && !Tyr.Bot.EnemyStrategyAnalyzer.Expanded && Completed(UnitTypes.ZEALOT) < 2)
            {
                foreach (Agent agent in tyr.UnitManager.Agents.Values)
                    if (agent.Unit.UnitType == UnitTypes.NEXUS
                        && agent.Unit.BuildProgress < 0.99)
                        agent.Order(Abilities.CANCEL);
            }
        }

        public override void Produce(Tyr tyr, Agent agent)
        {
            if (Count(UnitTypes.PROBE) >= 24
                && Count(UnitTypes.NEXUS) < 2
                && Minerals() < 450)
                return;
            if (agent.Unit.UnitType == UnitTypes.NEXUS
                && Minerals() >= 50
                && Count(UnitTypes.PROBE) < Math.Min(70, 20 * Completed(UnitTypes.NEXUS))
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
                if (attackTask.AttackSent && Count(UnitTypes.NEXUS) < 3)
                    return;
                if (Tyr.Bot.EnemyStrategyAnalyzer.EarlyPool && !Tyr.Bot.EnemyStrategyAnalyzer.Expanded)
                {
                    if (Minerals() >= 100
                        && (Completed(UnitTypes.CYBERNETICS_CORE) == 0 || Count(UnitTypes.ZEALOT) <= Math.Max(2, Count(UnitTypes.ADEPT))))
                        agent.Order(916);
                    else if (Completed(UnitTypes.CYBERNETICS_CORE) > 0
                        && Minerals() >= 100
                        && Gas() >= 25)
                        agent.Order(922);
                }
                else
                {
                    if (Completed(UnitTypes.CYBERNETICS_CORE) > 0
                           && Minerals() >= 125
                           && Gas() >= 50
                           && tyr.EnemyRace == Race.Terran
                           && Count(UnitTypes.STALKER) == 0)
                        agent.Order(917);
                    else if (Minerals() >= 450
                        && Gas() < 100
                        && Count(UnitTypes.ZEALOT) < 20
                        && Completed(UnitTypes.ROBOTICS_FACILITY) > 0)
                        agent.Order(916);
                    else if (Minerals() >= 50
                        && Gas() >= 150
                        && Completed(UnitTypes.TEMPLAR_ARCHIVE) > 0)
                        agent.Order(919);
                    else if (Minerals() >= 350
                        && Count(UnitTypes.ZEALOT) < 20)
                        agent.Order(916);
                    else if (Minerals() >= 100
                        && Count(UnitTypes.ZEALOT) < 10)
                        agent.Order(916);
                    else if (Minerals() >= 450  )
                        agent.Order(916);
                }
            }
            else if (agent.Unit.UnitType == UnitTypes.ROBOTICS_FACILITY)
            {
                if (attackTask.AttackSent && Count(UnitTypes.NEXUS) < 3)
                    return;
                if (Count(UnitTypes.OBSERVER) == 0
                    && Minerals() >= 25
                    && Gas() >= 75)
                {
                    agent.Order(977);
                }
                else if (Minerals() >= 250
                    && Gas() >= 100)
                {
                    agent.Order(979);
                }
            }
            else if (agent.Unit.UnitType == UnitTypes.TEMPLAR_ARCHIVE)
            {
                /*
                if (!Tyr.Bot.Observation.Observation.RawData.Player.UpgradeIds.Contains(52)
                    && Minerals() >= 200
                    && Gas() >= 200)
                    agent.Order(1126);
                    */
            }
            else if (agent.Unit.UnitType == UnitTypes.TWILIGHT_COUNSEL)
            {
                    if (!Tyr.Bot.Observation.Observation.RawData.Player.UpgradeIds.Contains(130)
                        && Minerals() >= 100
                        && Gas() >= 100
                        && Completed(UnitTypes.ADEPT) > 0)
                        agent.Order(1594);
                    else if (!Tyr.Bot.Observation.Observation.RawData.Player.UpgradeIds.Contains(86)
                         && Minerals() >= 100
                         && Gas() >= 100)
                        agent.Order(1592);
            }
        }
    }
}
