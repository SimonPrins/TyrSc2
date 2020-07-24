using SC2APIProtocol;
using Tyr.Agents;
using Tyr.Builds.BuildLists;
using Tyr.Micro;
using Tyr.Tasks;
using Tyr.Util;

namespace Tyr.Builds.Protoss
{
    public class DefensiveZealotRush : Build
    {
        private Point2D DefensePoint;
        private bool Expand = false;
        public override string Name()
        {
            return "DefensiveZealotRush";
        }

        public override void InitializeTasks()
        {
            base.InitializeTasks();
            DefenseTask.Enable();
            TimingAttackTask.Enable();
            if (Bot.Bot.TargetManager.PotentialEnemyStartLocations.Count > 1)
                WorkerScoutTask.Enable();
            WorkerRushDefenseTask.Enable();
            ArmyObserverTask.Enable();
        }

        public override void OnStart(Bot tyr)
        {
            MicroControllers.Add(new FleeCyclonesController());

            DefensePoint = new PotentialHelper(MainDefensePos, 5).To(tyr.MapAnalyzer.GetMainRamp()).Get();

            Set += ProtossBuildUtil.Pylons();
            Set += Units();
            Set += BuildGateways();
        }

        private BuildList Units()
        {
            BuildList result = new BuildList();
            result.If(() => Count(UnitTypes.NEXUS) >= 2 || !Expand);
            result.Train(UnitTypes.PROBE, 16);
            result.Train(UnitTypes.PROBE, 20, () => Count(UnitTypes.ASSIMILATOR) > 0);
            result.Train(UnitTypes.PROBE, 30, () => Count(UnitTypes.NEXUS) >= 2);
            result.Upgrade(UpgradeType.Charge);
            result.Upgrade(UpgradeType.WarpGate);
            result.Train(UnitTypes.OBSERVER, 1);
            result.Train(UnitTypes.IMMORTAL);
            result.Train(UnitTypes.ZEALOT, 10, () => Bot.Bot.Frame < 22.4 * 130 || Minerals() >= 250);
            result.Train(UnitTypes.STALKER, () => Gas() >= 150 || Count(UnitTypes.ROBOTICS_FACILITY) > 0);
            return result;
        }

        private BuildList BuildGateways()
        {
            BuildList result = new BuildList();
            result.Building(UnitTypes.NEXUS);
            result.Building(UnitTypes.NEXUS, () => Expand);
            result.Building(UnitTypes.GATEWAY, 2);
            result.Building(UnitTypes.GATEWAY, 1, () => Count(UnitTypes.ZEALOT) > 0);
            //result.Building(UnitTypes.GATEWAY, 1, () => Count(UnitTypes.ZEALOT) > 4);

            result.Building(UnitTypes.PYLON, Main, DefensePoint, () => Bot.Bot.Frame >= 22.4 * 100);
            result.Building(UnitTypes.ASSIMILATOR, () => Bot.Bot.Frame >= 22.4 * 130);
            result.Building(UnitTypes.FORGE, () => Bot.Bot.Frame >= 22.4 * 130);
            result.Upgrade(UpgradeType.ProtossGroundWeapons);
            result.Upgrade(UpgradeType.ProtossGroundArmor);
            result.Building(UnitTypes.CYBERNETICS_CORE, () => Bot.Bot.Frame >= 22.4 * 130);
            result.Building(UnitTypes.PHOTON_CANNON, Main, DefensePoint, 5, () => Bot.Bot.Frame >= 22.4 * 130);
            result.Building(UnitTypes.ROBOTICS_FACILITY, () => Bot.Bot.Frame >= 22.4 * 130);
            result.Building(UnitTypes.SHIELD_BATTERY, Main, DefensePoint, 2, () => Bot.Bot.Frame >= 22.4 * 130);
            result.Building(UnitTypes.ASSIMILATOR, () => Bot.Bot.Frame >= 22.4 * 130);
            result.Building(UnitTypes.TWILIGHT_COUNSEL, () => Bot.Bot.Frame >= 22.4 * 130);
            result.Building(UnitTypes.GATEWAY, 1, () => Count(UnitTypes.NEXUS) >= 2);
            return result;
        }

        public override void OnFrame(Bot tyr)
        {
            TimingAttackTask.Task.RequiredSize = 30;

            //tyr.buildingPlacer.BuildCompact = true;

            DefenseTask.GroundDefenseTask.MainDefenseRadius = 20;
            Expand = tyr.Frame >= 22.4 * 60 * 9 && Completed(UnitTypes.OBSERVER) > 0;
        }
    }
}
