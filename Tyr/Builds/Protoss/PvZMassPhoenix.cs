using SC2APIProtocol;
using System;
using Tyr.Agents;
using Tyr.Builds.BuildLists;
using Tyr.Managers;
using Tyr.Micro;
using Tyr.Tasks;

namespace Tyr.Builds.Protoss
{
    public class PvZMassPhoenix : Build
    {
        private Point2D OverrideDefenseTarget;
        private StutterController StutterController = new StutterController();
        private Point2D OverrideMainDefenseTarget;
        

        public override string Name()
        {
            return "PvZMassPhoenix";
        }

        public override void InitializeTasks()
        {
            base.InitializeTasks();
            DefenseTask.Enable();
            TimingAttackTask.Enable();
            if (Tyr.Bot.TargetManager.PotentialEnemyStartLocations.Count > 1)
                WorkerScoutTask.Enable();
            ArmyObserverTask.Enable();
            if (Tyr.Bot.BaseManager.Pocket != null)
                ScoutProxyTask.Enable(Tyr.Bot.BaseManager.Pocket.BaseLocation.Pos);
            ArchonMergeTask.Enable();
            WorkerRushDefenseTask.Enable();
            PhoenixHarassTask.Enable();
        }

        public override void OnStart(Tyr tyr)
        {
            OverrideDefenseTarget = tyr.MapAnalyzer.Walk(NaturalDefensePos, tyr.MapAnalyzer.EnemyDistances, 15);
            OverrideMainDefenseTarget = new PotentialHelper(tyr.MapAnalyzer.GetMainRamp(), 6).
                To(tyr.MapAnalyzer.StartLocation)
                .Get();
            
            MicroControllers.Add(new GravitonBeamController() { Delay = 22.4f * 4 });
            MicroControllers.Add(new FearMinesController());
            MicroControllers.Add(new StalkerController());
            MicroControllers.Add(new SentryController());
            MicroControllers.Add(new DisruptorController());
            MicroControllers.Add(StutterController);
            MicroControllers.Add(new HTController());
            MicroControllers.Add(new ColloxenController());
            MicroControllers.Add(new TempestController());
            MicroControllers.Add(new AdvanceController());

            Set += ProtossBuildUtil.Pylons(() => Completed(UnitTypes.PYLON) > 0 && Count(UnitTypes.CYBERNETICS_CORE) > 0);
            Set += ExpandBuildings();
            Set += Units();
            Set += MainBuild();
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

        private BuildList Units()
        {
            BuildList result = new BuildList();

            result.Train(UnitTypes.PHOENIX, 20);
            result.If(() => Count(UnitTypes.NEXUS) >= 2 && Count(UnitTypes.STARGATE) >= 1);
            result.Train(UnitTypes.ZEALOT, 1);
            result.If(() => Count(UnitTypes.PHOENIX) >= 3);
            result.Train(UnitTypes.ZEALOT, 2);
            result.Train(UnitTypes.STALKER, () => Gas() >= 150 && Minerals() >= 250);
            result.Train(UnitTypes.ZEALOT, () => Minerals() >= 200);

            return result;
        }

        private BuildList MainBuild()
        {
            BuildList result = new BuildList();

            result.Building(UnitTypes.NEXUS);
            result.Building(UnitTypes.PYLON);
            result.Building(UnitTypes.GATEWAY, Main, () => Completed(UnitTypes.PYLON) > 0);
            result.Building(UnitTypes.ASSIMILATOR);
            result.Building(UnitTypes.CYBERNETICS_CORE);
            result.Building(UnitTypes.NEXUS);
            result.Building(UnitTypes.ASSIMILATOR);
            result.Building(UnitTypes.STARGATE);
            result.Upgrade(UpgradeType.WarpGate, () => Count(UnitTypes.PHOENIX) >= 5);
            result.Building(UnitTypes.STARGATE, () => Count(UnitTypes.PHOENIX) > 0);
            result.Building(UnitTypes.ASSIMILATOR, 2, () => Count(UnitTypes.PHOENIX) > 2);
            result.If(() => Count(UnitTypes.PHOENIX) >= 20);
            result.Building(UnitTypes.NEXUS);
            result.Building(UnitTypes.GATEWAY, Main, 2, () => Minerals() >= 250);

            return result;
        }
        
        public override void OnFrame(Tyr tyr)
        {
            if (Tyr.Bot.Frame == (int)(45 * 22.4))
                tyr.Chat("This build was requested by Infy!");
            foreach (Agent agent in tyr.UnitManager.Agents.Values)
            {
                if (tyr.Frame % 224 != 0)
                    break;
                if (agent.Unit.UnitType != UnitTypes.GATEWAY)
                    continue;

                if (Count(UnitTypes.NEXUS) < 2 && TimingAttackTask.Task.Units.Count == 0)
                    agent.Order(Abilities.MOVE, Main.BaseLocation.Pos);
                else
                    agent.Order(Abilities.MOVE, tyr.TargetManager.PotentialEnemyStartLocations[0]);
            }

            BalanceGas();


            tyr.TargetManager.TargetCannons = true;


            if (WorkerScoutTask.Task.BaseCircled())
            {
                WorkerScoutTask.Task.Stopped = true;
                WorkerScoutTask.Task.Clear();
            }


            WorkerScoutTask.Task.StartFrame = 600;
            ObserverScoutTask.Task.Priority = 6;

            tyr.NexusAbilityManager.Stopped = Count(UnitTypes.STALKER) == 0 && tyr.Frame >= 120 * 22.4;
            tyr.NexusAbilityManager.PriotitizedAbilities.Add(917);
            
            TimingAttackTask.Task.RequiredSize = 25;

            TimingAttackTask.Task.Stopped = Completed(UnitTypes.STALKER) + Completed(UnitTypes.ZEALOT) < 5;

            if (ForwardProbeTask.Task.Stopped)
                ForwardProbeTask.Task.Clear();


            if (Count(UnitTypes.NEXUS) <= 1)
                IdleTask.Task.OverrideTarget = OverrideMainDefenseTarget;
            else if (Count(UnitTypes.NEXUS) >= 3)
                IdleTask.Task.OverrideTarget = OverrideDefenseTarget;
            else
                IdleTask.Task.OverrideTarget = null;

            DefenseTask.GroundDefenseTask.ExpandDefenseRadius = 30;
            DefenseTask.GroundDefenseTask.MainDefenseRadius = 30;
            DefenseTask.GroundDefenseTask.MaxDefenseRadius = 120;
            DefenseTask.GroundDefenseTask.DrawDefenderRadius = 80;

            DefenseTask.AirDefenseTask.ExpandDefenseRadius = 30;
            DefenseTask.AirDefenseTask.MainDefenseRadius = 30;
            DefenseTask.AirDefenseTask.MaxDefenseRadius = 120;
            DefenseTask.AirDefenseTask.DrawDefenderRadius = 80;

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
        }
    }
}
