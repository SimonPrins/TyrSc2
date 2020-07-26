using SC2APIProtocol;
using System.Collections.Generic;
using SC2Sharp.Agents;
using SC2Sharp.Util;

namespace SC2Sharp.Tasks
{
    public class SiegeAtRampTask : Task
    {
        public static SiegeAtRampTask Task = new SiegeAtRampTask();
        private Point2D IdleLocation;

        public SiegeAtRampTask() : base(10)
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
            if (IdleLocation == null)
                IdleLocation = Bot.Main.MapAnalyzer.GetMainRamp();
            List<UnitDescriptor> result = new List<UnitDescriptor>();
            if (units.Count == 0)
                result.Add(new UnitDescriptor() { Pos = IdleLocation, Count = 1, UnitTypes = new HashSet<uint>() { UnitTypes.SIEGE_TANK, UnitTypes.SIEGE_TANK_SIEGED } });
            return result;
        }

        public override void OnFrame(Bot bot)
        {
            if (Stopped)
            {
                Clear();
                return;
            }
            if (IdleLocation == null)
                IdleLocation = bot.MapAnalyzer.GetMainRamp();

            Agent bunker = null;
            foreach (Agent agent in bot.UnitManager.Agents.Values)
                if (agent.Unit.UnitType == UnitTypes.BUNKER)
                {
                    bunker = agent;
                    break;
                }

            if (bunker == null)
                return;

            foreach (Agent agent in units)
            {
                if (agent.Unit.UnitType == UnitTypes.SIEGE_TANK_SIEGED
                    && agent.DistanceSq(bunker.Unit.Pos) >= 4 * 4)
                    agent.Order(Abilities.UNSIEGE);
                else if (agent.DistanceSq(IdleLocation) < 5 * 5)
                {
                    if (agent.DistanceSq(bot.MapAnalyzer.GetMainRamp()) < agent.DistanceSq(bunker))
                        agent.Order(Abilities.MOVE, SC2Util.To2D(bot.MapAnalyzer.StartLocation));
                    else
                    {
                        PotentialHelper potential = new PotentialHelper(agent.Unit.Pos);
                        potential.Magnitude = 1;
                        potential.From(IdleLocation, 2);
                        potential.To(bunker.Unit);
                        agent.Order(Abilities.MOVE, potential.Get());
                    }
                }
                else if (agent.DistanceSq(bunker.Unit.Pos) >= 4 * 4)
                    agent.Order(Abilities.MOVE, SC2Util.To2D(bunker.Unit.Pos));
                else if (agent.Unit.UnitType == UnitTypes.SIEGE_TANK)
                    agent.Order(Abilities.SIEGE);
            }
        }
    }
}
