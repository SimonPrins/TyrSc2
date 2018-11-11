using Tyr.Agents;

namespace Tyr.Tasks
{
    class BunkerDefendersTask : Task
    {
        public static BunkerDefendersTask Task = new BunkerDefendersTask();
        private Agent Bunker = null;
        private int BunkerDeterminedFrame = -1;

        public static void Enable()
        {
            Task.Stopped = false;
            Tyr.Bot.TaskManager.Add(Task);
        }

        public BunkerDefendersTask() : base(10)
        { }

        public override bool DoWant(Agent agent)
        {
            if (GetBunker() == null)
                return false;
            return agent.Unit.UnitType == UnitTypes.MARINE && Units.Count < Tyr.Bot.Build.Count(UnitTypes.BUNKER) * 4;
        }

        public override bool IsNeeded()
        {
            return Tyr.Bot.Build.Completed(UnitTypes.BUNKER) > 0;
        }

        public override void OnFrame(Tyr tyr)
        {
            Tyr.Bot.DrawText("Bunker defenders count: " + Units.Count);
            GetBunker();
            if (Bunker == null)
                return;

            foreach (Agent agent in units)
                agent.Order(Abilities.MOVE, Bunker.Unit.Tag);
        }

        public Agent GetBunker()
        {
            if (Tyr.Bot.Frame == BunkerDeterminedFrame)
                return Bunker;
            BunkerDeterminedFrame = Tyr.Bot.Frame;

            Bunker = null;
            foreach (Agent bunker in Tyr.Bot.UnitManager.Agents.Values)
            {
                if (bunker.Unit.UnitType != UnitTypes.BUNKER
                    || bunker.Unit.BuildProgress < 0.90)
                    continue;
                if (bunker.Unit.Passengers == null)
                    continue;

                if (bunker.Unit.Passengers.Count >= 4)
                    continue;
                Bunker = bunker;
                break;
            }
            return Bunker;
        }
    }
}
