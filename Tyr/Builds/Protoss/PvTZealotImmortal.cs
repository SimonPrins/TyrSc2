using SC2APIProtocol;
using System;
using System.Collections.Generic;
using SC2Sharp.Agents;
using SC2Sharp.Builds.BuildLists;
using SC2Sharp.Managers;
using SC2Sharp.Micro;
using SC2Sharp.Tasks;

namespace SC2Sharp.Builds.Protoss
{
    public class PvTZealotImmortal : Build
    {
        private Point2D OverrideDefenseTarget;

        public override string Name()
        {
            return "PvTZealotImmortal";
        }

        public override void InitializeTasks()
        {
            base.InitializeTasks();
            DefenseTask.Enable();
            TimingAttackTask.Enable();
            if (Bot.Main.TargetManager.PotentialEnemyStartLocations.Count > 1)
                WorkerScoutTask.Enable();
            ArmyObserverTask.Enable();
            if (Bot.Main.BaseManager.Pocket != null)
                ScoutProxyTask.Enable(Bot.Main.BaseManager.Pocket.BaseLocation.Pos);
            ArchonMergeTask.Enable();
        }

        public override void OnStart(Bot bot)
        {
            OverrideDefenseTarget = bot.MapAnalyzer.Walk(NaturalDefensePos, bot.MapAnalyzer.EnemyDistances, 15);

            MicroControllers.Add(new GravitonBeamController() { LiftMarauders = true });
            MicroControllers.Add(new FearEnemyController(UnitTypes.PHOENIX, new HashSet<uint>() { UnitTypes.MISSILE_TURRET }, 11));
            MicroControllers.Add(new FearEnemyController(UnitTypes.PHOENIX, new HashSet<uint>() { UnitTypes.MARINE}, 8));
            MicroControllers.Add(new AttackEnemyController(UnitTypes.PHOENIX, new HashSet<uint> { UnitTypes.BANSHEE, UnitTypes.LIBERATOR_AG, UnitTypes.LIBERATOR }, 20, true));
            MicroControllers.Add(new SoftLeashController(new HashSet<uint> { UnitTypes.ZEALOT, UnitTypes.STALKER }, new HashSet<uint>() { UnitTypes.IMMORTAL, UnitTypes.ARCHON }, 6));
            MicroControllers.Add(new StalkerController());
            MicroControllers.Add(new StutterController());
            MicroControllers.Add(new HTController());
            MicroControllers.Add(new ColloxenController());
            MicroControllers.Add(new SoftLeashController(UnitTypes.PHOENIX, UnitTypes.IMMORTAL, 5));

            Set += ProtossBuildUtil.Pylons();
            Set += ExpandBuildings();
            Set += Units();
            Set += MainBuild();
        }

        private BuildList Units()
        {
            BuildList result = new BuildList();

            result.Train(UnitTypes.PROBE, 19);
            result.Train(UnitTypes.PROBE, 40, () => Count(UnitTypes.NEXUS) >= 2);
            result.Train(UnitTypes.PROBE, 60, () => Count(UnitTypes.NEXUS) >= 3);
            result.Train(UnitTypes.PROBE, 80, () => Count(UnitTypes.NEXUS) >= 4);
            result.If(() => Count(UnitTypes.ZEALOT) < 5 || Count(UnitTypes.IMMORTAL) < 2 || Count(UnitTypes.NEXUS) >= 2);
            result.Upgrade(UpgradeType.Charge);
            result.Upgrade(UpgradeType.WarpGate);
            result.Train(UnitTypes.IMMORTAL, 3);
            result.Train(UnitTypes.OBSERVER, 1);
            result.Train(UnitTypes.COLOSUS, 3, () => TotalEnemyCount(UnitTypes.VIKING_FIGHTER) + TotalEnemyCount(UnitTypes.LIBERATOR)  + TotalEnemyCount(UnitTypes.LIBERATOR_AG) == 0);
            result.Train(UnitTypes.IMMORTAL, 20);
            result.Train(UnitTypes.STALKER, 1, () => Completed(UnitTypes.IMMORTAL) == 0);
            result.If(() => Count(UnitTypes.STALKER) + Count(UnitTypes.IMMORTAL) > 0);
            result.Train(UnitTypes.PHOENIX, 5);
            result.Train(UnitTypes.ZEALOT, 10);
            //result.Train(UnitTypes.STALKER, 20, () => TotalEnemyCount(UnitTypes.LIBERATOR) + TotalEnemyCount(UnitTypes.LIBERATOR_AG) > 0);
            result.Train(UnitTypes.ZEALOT, 20);
            result.Train(UnitTypes.ZEALOT, 25, () => TotalEnemyCount(UnitTypes.VIKING_FIGHTER) == 0);
            result.Train(UnitTypes.STALKER, 10);
            result.Train(UnitTypes.ZEALOT);

            return result;
        }

        private BuildList ExpandBuildings()
        {
            BuildList result = new BuildList();

            result.If(() => Count(UnitTypes.NEXUS) >= 3);
            foreach (Base b in Bot.Main.BaseManager.Bases)
            {
                if (b == Main)
                    continue;
                result.Building(UnitTypes.PYLON, b, () => b.ResourceCenter != null && b.ResourceCenter.Unit.BuildProgress >= 0.95);
                result.Building(UnitTypes.GATEWAY, b, 2, () => b.ResourceCenter != null && b.ResourceCenter.Unit.BuildProgress >= 0.95 && Completed(b, UnitTypes.PYLON) >= 1 && Minerals() >= 350);
                result.Building(UnitTypes.PYLON, b, () => b.ResourceCenter != null && b.ResourceCenter.Unit.BuildProgress >= 0.95 && Count(b, UnitTypes.GATEWAY) >= 2 && Minerals() >= 700);
            }

            return result;
        }

        private BuildList MainBuild()
        {
            BuildList result = new BuildList();

            result.Building(UnitTypes.NEXUS);
            result.Building(UnitTypes.PYLON);
            result.Building(UnitTypes.GATEWAY, Main);
            result.Building(UnitTypes.NEXUS);
            result.Building(UnitTypes.CYBERNETICS_CORE);
            result.Building(UnitTypes.ASSIMILATOR, 2);
            result.Building(UnitTypes.ROBOTICS_FACILITY);
            result.Building(UnitTypes.NEXUS);
            result.Building(UnitTypes.PYLON, Natural);
            result.Building(UnitTypes.PYLON);
            result.Building(UnitTypes.TWILIGHT_COUNSEL);
            result.Building(UnitTypes.ROBOTICS_BAY);
            result.Building(UnitTypes.FORGE);
            result.Upgrade(UpgradeType.ProtossGroundWeapons);
            result.Building(UnitTypes.ASSIMILATOR, 2);
            result.Building(UnitTypes.ROBOTICS_FACILITY);
            result.Upgrade(UpgradeType.ExtendedThermalLance);
            result.Building(UnitTypes.STARGATE);
            result.Building(UnitTypes.FORGE);
            result.Upgrade(UpgradeType.ProtossGroundArmor);
            result.Building(UnitTypes.ASSIMILATOR);
            //result.Building(UnitTypes.STARGATE, () => TotalEnemyCount(UnitTypes.LIBERATOR) + TotalEnemyCount(UnitTypes.LIBERATOR_AG) > 0 || Count(UnitTypes.COLOSUS) > 0);
            result.If(() => TimingAttackTask.Task.AttackSent);
            result.Building(UnitTypes.NEXUS);
            result.Building(UnitTypes.ROBOTICS_FACILITY);
            result.Building(UnitTypes.GATEWAY, Main, 2);
            result.Building(UnitTypes.ASSIMILATOR);

            return result;
        }

        public override void OnFrame(Bot bot)
        {
            if (Completed(UnitTypes.COLOSUS) >= 3)
                TimingAttackTask.Task.RequiredSize = 20;
            else
                TimingAttackTask.Task.RequiredSize = 40;

            TimingAttackTask.Task.RetreatSize = 6;
            
            if (Count(UnitTypes.NEXUS) >= 3)
                IdleTask.Task.OverrideTarget = OverrideDefenseTarget;
            else
                IdleTask.Task.OverrideTarget = null;

            DefenseTask.GroundDefenseTask.ExpandDefenseRadius = 25;
            DefenseTask.GroundDefenseTask.MainDefenseRadius = 30;
            DefenseTask.GroundDefenseTask.MaxDefenseRadius = 120;
            DefenseTask.GroundDefenseTask.DrawDefenderRadius = 80;
        }
    }
}
