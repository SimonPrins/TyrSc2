using SC2APIProtocol;
using System.Collections.Generic;
using Tyr.Agents;
using Tyr.Builds.BuildLists;
using Tyr.Managers;
using Tyr.MapAnalysis;
using Tyr.Micro;
using Tyr.Tasks;
using Tyr.Util;

namespace Tyr.Builds.Protoss
{
    public class PvZMothershipSiege : Build
    {
        private StutterController StutterController = new StutterController();
        private bool BuildZealots = true;


        public override string Name()
        {
            return "PvZMothershipSiege";
        }

        public override void InitializeTasks()
        {
            base.InitializeTasks();

            DefenseTask.Enable();
            TimingAttackTask.Enable();
            //if (Tyr.Bot.TargetManager.PotentialEnemyStartLocations.Count > 1)
            WorkerScoutTask.Enable();
            if (Bot.Main.BaseManager.Pocket != null)
                ScoutProxyTask.Enable(Bot.Main.BaseManager.Pocket.BaseLocation.Pos);
            ArmyObserverTask.Enable();
            ObserverScoutTask.Enable();
            ArmyOracleTask.Enable();
            SaveWorkersTask.Enable();
            ObserverHunterTask.Enable();
            PhoenixScoutTask.Enable();
        }

        public override void OnStart(Bot tyr)
        {
            TimingAttackTask.Task.BeforeControllers.Add(new SoftLeashController(new HashSet<uint> { UnitTypes.TEMPEST, UnitTypes.CARRIER, UnitTypes.STALKER, UnitTypes.IMMORTAL, UnitTypes.ZEALOT }, UnitTypes.MOTHERSHIP, 6));
            MicroControllers.Add(new DTController());
            MicroControllers.Add(new VoidrayController());
            MicroControllers.Add(new SentryController());
            MicroControllers.Add(new TempestController());
            MicroControllers.Add(new CarrierController());
            MicroControllers.Add(new MothershipController());
            MicroControllers.Add(new DisruptorController());
            MicroControllers.Add(new DodgeBallController());
            MicroControllers.Add(new StalkerController());
            MicroControllers.Add(new GravitonBeamController());
            MicroControllers.Add(new FearEnemyController(UnitTypes.PHOENIX, UnitTypes.STALKER, 6));
            MicroControllers.Add(new AdvanceController());
            MicroControllers.Add(StutterController);

            foreach (WorkerDefenseTask task in WorkerDefenseTask.Tasks)
                task.OnlyDefendInsideMain = true;

            Set += ProtossBuildUtil.Pylons(() =>
            (Count(UnitTypes.PYLON) > 0 && Count(UnitTypes.CYBERNETICS_CORE) > 0 && Count(UnitTypes.ZEALOT) > 0) && Count(UnitTypes.PYLON) < 3 || tyr.Frame >= 22.4 * 60 * 3.5);
            Set += ExpandBuildings();
            Set += ExtraAssimilators();
            Set += Units();
            Set += MainBuildList();
        }

        private BuildList ExpandBuildings()
        {
            BuildList result = new BuildList();
            
            /*
            foreach (Base b in Tyr.Bot.BaseManager.Bases)
            {
                if (b == Main)
                    continue;
                result.Building(UnitTypes.PYLON, b, () => b.ResourceCenter != null && b.ResourceCenter.Unit.BuildProgress >= 0.95);
                result.Building(UnitTypes.GATEWAY, b, 2, () => b.ResourceCenter != null && b.ResourceCenter.Unit.BuildProgress >= 0.95 && Completed(b, UnitTypes.PYLON) >= 1 && Minerals() >= 450);
            }
            */

            return result;
        }

        private BuildList ExtraAssimilators()
        {
            BuildList result = new BuildList();

            result.If(() => Minerals() >= 600 && Completed(UnitTypes.NEXUS) >= 3 && Gas() < 100 && Bot.Main.Frame % 10 == 0);
            result.Building(UnitTypes.ASSIMILATOR, 6);
            result.If(() => Minerals() >= 800);
            result.Building(UnitTypes.ASSIMILATOR, 7);
            result.If(() => Minerals() >= 900);
            result.Building(UnitTypes.ASSIMILATOR, 8);
            result.If(() => Minerals() >= 1000);
            result.Building(UnitTypes.ASSIMILATOR, 10);

            return result;
        }

        private BuildList Units()
        {
            BuildList result = new BuildList();

            result.Train(UnitTypes.PROBE, 20);
            result.Train(UnitTypes.PROBE, 40, () => Count(UnitTypes.NEXUS) >= 2 && Count(UnitTypes.PYLON) > 2);
            result.Train(UnitTypes.PROBE, 60, () => Count(UnitTypes.NEXUS) >= 3 && Count(UnitTypes.PYLON) > 2);
            result.Train(UnitTypes.PROBE, 70, () => Count(UnitTypes.NEXUS) >= 4 && Count(UnitTypes.PYLON) > 2);
            result.Train(UnitTypes.ZEALOT, 1, () => BuildZealots);
            result.Train(UnitTypes.STALKER, 1, () => !BuildZealots);
            result.Upgrade(UpgradeType.WarpGate, () => Count(UnitTypes.IMMORTAL) > 0);
            result.Upgrade(UpgradeType.ProtossAirWeapons, () => Count(UnitTypes.TEMPEST) + Count(UnitTypes.CARRIER) > 0);
            result.Upgrade(UpgradeType.ProtossAirArmor, () => Count(UnitTypes.TEMPEST) + Count(UnitTypes.CARRIER) > 0);
            result.Upgrade(UpgradeType.ProtossGroundArmor, () => Count(UnitTypes.TEMPEST) + Count(UnitTypes.CARRIER) > 0);
            result.Upgrade(UpgradeType.ProtossGroundWeapons, () => Count(UnitTypes.TEMPEST) + Count(UnitTypes.CARRIER) > 0);
            result.Upgrade(UpgradeType.ProtossShields, () => Count(UnitTypes.TEMPEST) + Count(UnitTypes.CARRIER) > 0);
            result.Train(UnitTypes.PHOENIX, 3, () => Count(UnitTypes.FLEET_BEACON) == 0);
            result.Train(UnitTypes.MOTHERSHIP, 1);
            result.Train(UnitTypes.CARRIER, 10);
            result.If(() => Count(UnitTypes.STALKER) + Count(UnitTypes.ZEALOT) + Count(UnitTypes.PHOENIX) < 30 || Count(UnitTypes.NEXUS) >= 3);
            result.If(() => Count(UnitTypes.NEXUS) >= 2 || Count(UnitTypes.STALKER) + Count(UnitTypes.ZEALOT) < 15);
            result.If(() => Count(UnitTypes.NEXUS) >= 3 || Count(UnitTypes.STALKER) + Count(UnitTypes.ZEALOT) < 20);
            result.Train(UnitTypes.IMMORTAL, 2);
            result.Train(UnitTypes.OBSERVER, 2);
            result.Train(UnitTypes.IMMORTAL, 8);
            result.Train(UnitTypes.ZEALOT, 10, () => BuildZealots);
            result.Train(UnitTypes.STALKER, 15);

            return result;
        }

        private BuildList MainBuildList()
        {
            BuildList result = new BuildList();

            result.Building(UnitTypes.NEXUS);
            result.Building(UnitTypes.PYLON, Main);
            result.If(() => Completed(UnitTypes.PYLON) > 0);
            result.Building(UnitTypes.GATEWAY, Main);
            result.Building(UnitTypes.ASSIMILATOR);
            result.Building(UnitTypes.CYBERNETICS_CORE, Main);
            result.Building(UnitTypes.NEXUS);
            result.Building(UnitTypes.STARGATE);
            result.Building(UnitTypes.PYLON, Natural, NaturalDefensePos);
            result.Building(UnitTypes.ASSIMILATOR);
            result.Building(UnitTypes.PYLON);
            result.If(() => Count(UnitTypes.IMMORTAL) + Count(UnitTypes.STALKER) + Count(UnitTypes.ZEALOT) >= 7);
            result.Building(UnitTypes.NEXUS);
            result.Building(UnitTypes.ASSIMILATOR, () => Count(UnitTypes.FLEET_BEACON) > 0);
            result.Building(UnitTypes.ASSIMILATOR, () => Count(UnitTypes.STALKER) + Count(UnitTypes.ZEALOT) >= 12);
            result.Building(UnitTypes.ASSIMILATOR, () => Count(UnitTypes.TEMPEST) + Count(UnitTypes.CARRIER) > 0);
            result.Building(UnitTypes.GATEWAY, Natural);
            result.Building(UnitTypes.ROBOTICS_FACILITY);
            result.Upgrade(UpgradeType.Blink);
            result.Building(UnitTypes.ASSIMILATOR, 2);
            result.Building(UnitTypes.FLEET_BEACON);
            result.Building(UnitTypes.STARGATE);
            result.Building(UnitTypes.NEXUS);
            result.Building(UnitTypes.FORGE, 2);
            result.Building(UnitTypes.ASSIMILATOR);
            result.Building(UnitTypes.ASSIMILATOR);
            result.Building(UnitTypes.STARGATE);
            result.Building(UnitTypes.NEXUS);
            result.Building(UnitTypes.ASSIMILATOR, 3);
            result.Building(UnitTypes.NEXUS);
            result.Building(UnitTypes.ASSIMILATOR, 2);
            result.Building(UnitTypes.STARGATE);
            result.Building(UnitTypes.TWILIGHT_COUNSEL);
            result.Building(UnitTypes.GATEWAY, 2, () => Minerals() >= 500);
            return result;
        }

        public override void OnFrame(Bot tyr)
        {
            BalanceGas();

            if (WorkerScoutTask.Task.BaseCircled())
            {
                WorkerScoutTask.Task.Stopped = true;
                WorkerScoutTask.Task.Clear();
            }


            if (TotalEnemyCount(UnitTypes.ROACH) + TotalEnemyCount(UnitTypes.HYDRALISK) >= 5
                || TotalEnemyCount(UnitTypes.CORRUPTOR) + TotalEnemyCount(UnitTypes.MUTALISK) > 0)
                BuildZealots = false;

            tyr.NexusAbilityManager.Stopped = Completed(UnitTypes.PYLON) == 0;
            tyr.NexusAbilityManager.PriotitizedAbilities.Add(1006);


            SaveWorkersTask.Task.Stopped = tyr.Frame >= 22.4 * 60 * 7 || EnemyCount(UnitTypes.CYCLONE) == 0 || !Natural.UnderAttack;
            if (SaveWorkersTask.Task.Stopped)
                SaveWorkersTask.Task.Clear();

            DefenseTask.GroundDefenseTask.IncludePhoenixes = true;

            WorkerTask.Task.EvacuateThreatenedBases = true;
            

            TimingAttackTask.Task.DefendOtherAgents = false;

            if (Completed(UnitTypes.MOTHERSHIP) == 0 || Completed(UnitTypes.TEMPEST) + Completed(UnitTypes.CARRIER) < 6)
            {
                TimingAttackTask.Task.RetreatSize = 15;
                TimingAttackTask.Task.RequiredSize = 35;
            }
            else if (Completed(UnitTypes.MOTHERSHIP) == 0 || Completed(UnitTypes.TEMPEST) + Completed(UnitTypes.CARRIER) < 8)
            {
                TimingAttackTask.Task.RetreatSize = 15;
                TimingAttackTask.Task.RequiredSize = 35;
            }
            else
            {
                TimingAttackTask.Task.RetreatSize = 8;
                TimingAttackTask.Task.RequiredSize = 15;
            }

            DefenseTask.GroundDefenseTask.ExpandDefenseRadius = 20;
            DefenseTask.GroundDefenseTask.MainDefenseRadius = 20;
            DefenseTask.GroundDefenseTask.MaxDefenseRadius = 120;
            DefenseTask.GroundDefenseTask.DrawDefenderRadius = 80;

            DefenseTask.AirDefenseTask.ExpandDefenseRadius = 30;
            DefenseTask.AirDefenseTask.MainDefenseRadius = 30;
            DefenseTask.AirDefenseTask.MaxDefenseRadius = 120;
            DefenseTask.AirDefenseTask.DrawDefenderRadius = 80;

        }
    }
}
