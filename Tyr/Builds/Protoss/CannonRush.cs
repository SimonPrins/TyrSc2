using Tyr.Agents;
using Tyr.Builds.BuildLists;
using Tyr.Tasks;

namespace Tyr.Builds.Protoss
{
    public class CannonRush : Build
    {
        public override string Name()
        {
            return "CannonRush";
        }

        public override void InitializeTasks()
        {
            base.InitializeTasks();
            DefenseTask.Enable();
            CannonRushTask.Enable();
            TimingAttackTask.Enable();
        }

        public override void OnStart(Tyr tyr)
        {
            Set += ProtossBuildUtil.Pylons(() => Count(UnitTypes.PYLON) > 0 && (Count(UnitTypes.PHOTON_CANNON) >= 2 || Tyr.Bot.Frame >= 22.4 * 60 * 2));
            Set += MainBuild();
        }

        private BuildList MainBuild()
        {
            BuildList result = new BuildList();

            result.Building(UnitTypes.PYLON);
            result.Building(UnitTypes.FORGE);
            result.Building(UnitTypes.GATEWAY, 3, () => Minerals() >= 750 - Count(UnitTypes.PHOTON_CANNON) || CannonRushTask.Task.Units.Count == 0);
            result.Train(UnitTypes.ZEALOT);

            return result;
        }

        public override void OnFrame(Tyr tyr)
        {
            TimingAttackTask.Task.RequiredSize = 6;

            if (Tyr.Bot.Frame >= 22.4 * 30)
                CannonRushTask.Task.Stopped = true;
        }

        public override void Produce(Tyr tyr, Agent agent)
        {
            if (agent.Unit.UnitType == UnitTypes.NEXUS
                && Minerals() >= 50
                && Count(UnitTypes.PROBE) < 18
                && Count(UnitTypes.PYLON) > 0)
            {
                agent.Order(1006);
            }
        }
    }
}
