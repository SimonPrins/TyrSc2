using System.Collections.Generic;
using SC2Sharp.Agents;
using SC2Sharp.Builds.BuildLists;
using SC2Sharp.Micro;
using SC2Sharp.Tasks;

namespace SC2Sharp.Builds.Protoss
{
    public class PvZCannonRush : Build
    {
        bool CannonCompleted = false;
        public override string Name()
        {
            return "PvZCannonRush";
        }

        public override void InitializeTasks()
        {
            base.InitializeTasks();
            DefenseTask.Enable();
            ProxyTask.Enable(new List<ProxyBuilding>() { new ProxyBuilding() { UnitType = UnitTypes.PYLON }, new ProxyBuilding() { UnitType = UnitTypes.PHOTON_CANNON } });
            ProxyTask.Task.UseEnemyNatural = true;
            TimingAttackTask.Enable();
        }

        public override void OnStart(Bot bot)
        {
            MicroControllers.Add(new StutterController());

            Set += ProtossBuildUtil.Pylons(() => Count(UnitTypes.PYLON) > 0 && CannonCompleted);
            Set += MainBuild();
        }

        private BuildList MainBuild()
        {
            BuildList result = new BuildList();

            result.Building(UnitTypes.PYLON);
            result.Building(UnitTypes.FORGE);
            result.If(() => CannonCompleted || Minerals() >= 300);
            result.Building(UnitTypes.GATEWAY);
            result.Building(UnitTypes.ASSIMILATOR);
            result.Building(UnitTypes.CYBERNETICS_CORE);
            result.Building(UnitTypes.PYLON);
            result.Building(UnitTypes.GATEWAY, 2);
            result.Building(UnitTypes.ROBOTICS_FACILITY);
            result.Building(UnitTypes.ASSIMILATOR);
            result.Upgrade(UpgradeType.WarpGate, () => Count(UnitTypes.STALKER) >= 3);
            result.Train(UnitTypes.IMMORTAL);
            result.Train(UnitTypes.STALKER);

            return result;
        }

        public override void OnFrame(Bot bot)
        {
            if (ProxyTask.Task.UnitCounts.ContainsKey(UnitTypes.PHOTON_CANNON) && ProxyTask.Task.UnitCounts[UnitTypes.PHOTON_CANNON] > 0)
            {
                CannonCompleted = true;
                ProxyTask.Task.Stopped = true;
                ProxyTask.Task.Clear();
            }
            TimingAttackTask.Task.RequiredSize = 16;
        }

        public override void Produce(Bot bot, Agent agent)
        {
            if (agent.Unit.UnitType == UnitTypes.NEXUS
                && Minerals() >= 50
                && Count(UnitTypes.PROBE) < 20
                && Count(UnitTypes.PYLON) > 0)
            {
                agent.Order(1006);
            }
        }
    }
}
