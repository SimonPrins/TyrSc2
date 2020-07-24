using SC2APIProtocol;
using System.Collections.Generic;
using Tyr.Agents;
using Tyr.Util;

namespace Tyr.Tasks
{
    public class SiegeBelowRampTask : Task
    {
        public static SiegeBelowRampTask Task = new SiegeBelowRampTask();
        private Point2D Natural;
        private Point2D NaturalDefensePos;

        public SiegeBelowRampTask() : base(9)
        {}

        public static void Enable()
        {
            Enable(Task);
        }

        public override bool DoWant(Agent agent)
        {
            return true;
        }

        public override bool IsNeeded()
        {
            return true;
        }

        public override List<UnitDescriptor> GetDescriptors()
        {
            if (Natural == null)
                Natural = Bot.Bot.BaseManager.Natural.BaseLocation.Pos;
            if (NaturalDefensePos == null)
                NaturalDefensePos = Bot.Bot.BaseManager.NaturalDefensePos;
            List<UnitDescriptor> result = new List<UnitDescriptor>();
            if (units.Count == 0)
                result.Add(new UnitDescriptor() { Pos = Natural, Count = 1, UnitTypes = new HashSet<uint>() { UnitTypes.SIEGE_TANK, UnitTypes.SIEGE_TANK_SIEGED } });
            return result;
        }

        public override void OnFrame(Bot tyr)
        {
            if (Stopped)
            {
                Clear();
                return;
            }
            if (Natural == null)
                Natural = Bot.Bot.BaseManager.Natural.BaseLocation.Pos;
            if (NaturalDefensePos == null)
                NaturalDefensePos = Bot.Bot.BaseManager.NaturalDefensePos;
            

            foreach (Agent agent in units)
            {
                if (agent.Unit.UnitType == UnitTypes.SIEGE_TANK_SIEGED
                    && (agent.DistanceSq(Natural) >= 8 * 8 || agent.DistanceSq(NaturalDefensePos) <= 10 * 10))
                    agent.Order(Abilities.UNSIEGE);
                else if (agent.DistanceSq(Natural) >= 8 * 8)
                    agent.Order(Abilities.MOVE, Natural);
                else if (agent.DistanceSq(NaturalDefensePos) <= 10 * 10)
                {
                    PotentialHelper potential = new PotentialHelper(agent.Unit.Pos);
                    potential.Magnitude = 1;
                    potential.From(NaturalDefensePos, 2);
                    potential.To(Natural);
                    agent.Order(Abilities.MOVE, potential.Get());
                }
                else if (agent.Unit.UnitType == UnitTypes.SIEGE_TANK)
                    agent.Order(Abilities.SIEGE);
            }
        }
    }
}
