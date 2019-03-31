using SC2APIProtocol;
using Tyr.Agents;
using Tyr.Managers;

namespace Tyr.Tasks
{
    class QueenDefenseTask : Task
    {
        public static QueenDefenseTask Task = new QueenDefenseTask();

        public QueenDefenseTask() : base(2)
        { }

        public static void Enable()
        {
            Task.Stopped = false;
            Tyr.Bot.TaskManager.Add(Task);
        }

        public override bool DoWant(Agent agent)
        {
            return agent.Unit.UnitType == UnitTypes.QUEEN;
        }

        public override bool IsNeeded()
        {
            return true;
        }

        public override void OnFrame(Tyr tyr)
        {
            int bases = 0;
            foreach (Base b in tyr.BaseManager.Bases)
                if (b.ResourceCenter != null)
                    bases++;

            Point2D target;
            Base defendBase = null;
            if (bases >= 2)
            {
                target = tyr.BaseManager.NaturalDefensePos;
                defendBase = tyr.BaseManager.Natural;
            }
            else
            {
                target = tyr.BaseManager.MainDefensePos;
                defendBase = tyr.BaseManager.Main;
            }
            
            PotentialHelper potential = new PotentialHelper(target);
            potential.Magnitude = 2;
            potential.To(defendBase.BaseLocation.Pos);
            target = potential.Get();

            foreach (Agent queen in units)
            {
                if (queen.Unit.Energy >= 50)
                {
                    Agent transfuseTarget = null;
                    foreach (Agent agent in tyr.UnitManager.Agents.Values)
                    {
                        if (agent.Unit.HealthMax - agent.Unit.Health >= 125
                            && agent.Unit.Tag != queen.Unit.Tag
                            && queen.DistanceSq(agent) <= 8 * 8)
                        {
                            transfuseTarget = agent;
                            break;
                        }
                    }
                    if (transfuseTarget != null)
                    {
                        queen.Order(Abilities.TRANSFUSE, transfuseTarget.Unit.Tag);
                        continue;
                    }
                }

                if (queen.DistanceSq(target) >= 3 * 3)
                    queen.Order(Abilities.MOVE, target);
            }
        }
    }
}
