using Tyr.Agents;
using Tyr.Builds.BuildLists;
using Tyr.Tasks;
using Tyr.Util;

namespace Tyr.Builds.Protoss
{
    public class ZealotRush : Build
    {
        public bool ProxyPylon = false;
        private bool PylonPlaced = false;
        private PlacePylonTask PlacePylonTask = new PlacePylonTask();
        public int RequiredSize = 8;


        public override string Name()
        {
            return "ZealotRush";
        }

        public override void OnStart(Tyr tyr)
        {
            tyr.TaskManager.Add(new DefenseTask());
            tyr.TaskManager.Add(new TimingAttackTask() { RequiredSize = RequiredSize });
            tyr.TaskManager.Add(new WorkerScoutTask());
            if (ProxyPylon)
                tyr.TaskManager.Add(PlacePylonTask);

            Set += ProtossBuildUtil.Pylons();
            Set += BuildGateways();
        }

        private BuildList BuildGateways()
        {
            BuildList result = new BuildList();
            result.Building(UnitTypes.GATEWAY, 4);
            result.If(() => { return Count(UnitTypes.ZEALOT) >= 8; });
            result.Building(UnitTypes.GATEWAY, 2);
            return result;
        }

        public override void OnFrame(Tyr tyr)
        {
            if (!PylonPlaced)
                foreach (Agent agent in tyr.UnitManager.Agents.Values)
                    if (agent.Unit.UnitType == UnitTypes.PYLON && SC2Util.DistanceSq(agent.Unit.Pos, tyr.MapAnalyzer.StartLocation) >= 40 * 40)
                    {
                        PylonPlaced = true;
                        PlacePylonTask.Clear();
                        PlacePylonTask.Stopped = true;
                    }
        }

        public override void Produce(Tyr tyr, Agent agent)
        {
            if (agent.Unit.UnitType == UnitTypes.NEXUS
                && Minerals() >= 50
                && Count(UnitTypes.PROBE) < 16)
                agent.Order(1006);
            if (agent.Unit.UnitType == UnitTypes.GATEWAY && Minerals() >= 100
                && (Minerals() >= 150 || PylonPlaced || !ProxyPylon))
                agent.Order(916);
        }
    }
}
