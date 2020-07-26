using SC2APIProtocol;
using System.Collections.Generic;
using SC2Sharp.Agents;
using SC2Sharp.Managers;
using SC2Sharp.MapAnalysis;
using SC2Sharp.Util;

namespace SC2Sharp.Tasks
{
    public class GasWorkerTask : Task
    {
        public static List<GasWorkerTask> Tasks = new List<GasWorkerTask>();
        private Point2D Pos;
        private Agent Gas;
        public Base Base;

        public static int WorkersPerGas = 3;


        public GasWorkerTask(Point2D pos, Base b) : base(6)
        {
            Pos = pos;
            Base = b;
        }

        public static void Enable()
        {
            if (Tasks.Count == 0)
            {
                foreach (Base b in Bot.Main.BaseManager.Bases)
                    foreach (Gas gas in b.BaseLocation.Gasses)
                        Tasks.Add(new GasWorkerTask(SC2Util.To2D(gas.Pos), b));
            }
            foreach (GasWorkerTask task in Tasks)
                Enable(task);
        }

        public override bool DoWant(Agent agent)
        {
            return agent.IsWorker && Units.Count < WorkersPerGas;
        }

        public override bool IsNeeded()
        {
            UpdateGas();
            if (Base.ResourceCenter == null || Base.ResourceCenter.Unit.BuildProgress < 0.9 || (Base.UnderAttack && Bot.Main.UnitManager.Completed(UnitTypes.ResourceCenters) >= 2))
                return false;
            return Gas != null;
        }

        public override List<UnitDescriptor> GetDescriptors()
        {
            List<UnitDescriptor> result = new List<UnitDescriptor>();
            if (units.Count < WorkersPerGas)
            {
                result.Add(new UnitDescriptor()
                {
                    Pos = Pos,
                    Count = WorkersPerGas - Units.Count,
                    UnitTypes = UnitTypes.WorkerTypes,
                    MaxDist = 40
                });
            }
            return result;
        }

        private void UpdateGas()
        {
            if (Gas != null && !Bot.Main.UnitManager.Agents.ContainsKey(Gas.Unit.Tag))
                Gas = null;

            if (Gas != null && Gas.Unit.VespeneContents == 0)
                Gas = null;

            if (Gas == null)
            {
                foreach (Agent agent in Bot.Main.UnitManager.Agents.Values)
                    if (UnitTypes.GasGeysers.Contains(agent.Unit.UnitType) 
                        && agent.Unit.BuildProgress >= 0.99
                        && agent.Unit.VespeneContents > 0
                        && agent.DistanceSq(Pos) <= 2 * 2)
                    {
                        Gas = agent;
                        break;
                    }
            }
        }

        public override void OnFrame(Bot bot)
        {
            UpdateGas();
            if (Gas == null || Gas.Unit.VespeneContents == 0)
            {
                Clear();
                return;
            }

            while (Units.Count > WorkersPerGas)
                ClearLast();

            foreach (Agent worker in Units)
                if ((worker.Unit.Orders.Count == 0 || MiningWrongGas(worker)))
                    worker.Order(Abilities.MOVE, Gas.Unit.Tag);
        }

        private bool MiningWrongGas(Agent worker)
        {
            if (Gas.Unit.Tag == worker.Unit.Orders[0].TargetUnitTag)
                return false;
            uint ability = worker.Unit.Orders[0].AbilityId;
            return ability == 298 || ability == 295 || ability == 1183;
        }
    }
}
