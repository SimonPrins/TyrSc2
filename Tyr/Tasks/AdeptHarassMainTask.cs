using SC2APIProtocol;
using System;
using System.Collections.Generic;
using SC2Sharp.Agents;
using SC2Sharp.Managers;
using SC2Sharp.Util;

namespace SC2Sharp.Tasks
{
    class AdeptHarassMainTask : Task
    {
        public static AdeptHarassMainTask Task = new AdeptHarassMainTask();
        public bool Sent = false;

        public int RequiredSize = 2;
        private State CurrentState = State.GroupUp;

        enum State
        {
            Attack, GroupUp
        }
        
        public static void Enable()
        {
            Enable(Task);
        }

        public AdeptHarassMainTask() : base(15)
        { }

        public override bool DoWant(Agent agent)
        {
            return agent.Unit.UnitType == UnitTypes.ADEPT && Units.Count < RequiredSize;
        }

        public override bool IsNeeded()
        {
            return Bot.Main.Build.Completed(UnitTypes.ADEPT) >= RequiredSize;
        }

        public override List<UnitDescriptor> GetDescriptors()
        {
            List<UnitDescriptor> result = new List<UnitDescriptor>();
            int required = RequiredSize - Units.Count;
            if (required > 0 && !Sent)
                result.Add(new UnitDescriptor() { Count = required, UnitTypes = new HashSet<uint>() { UnitTypes.ADEPT } });
            return result;
        }

        public override void OnFrame(Bot bot)
        {
            bot.DrawText("Adept harass count: " + units.Count);
            if (Units.Count >= RequiredSize)
                Sent = true;

            if (Units.Count >= 2)
            {
                float dist = 0;
                foreach (Agent agent1 in Units)
                    foreach (Agent agent2 in Units)
                        dist = Math.Max(dist, agent1.DistanceSq(agent2));
                if (dist >= 6 * 6)
                    CurrentState = State.GroupUp;
                if (dist <= 2 * 2)
                    CurrentState = State.Attack;
            }

            Base enemyMain = null;
            foreach (Base b in bot.BaseManager.Bases)
            {
                if (SC2Util.DistanceSq(b.BaseLocation.Pos, bot.TargetManager.PotentialEnemyStartLocations[0]) <= 2 * 2)
                {
                    enemyMain = b;
                    break;
                }
            }

            Point2D targetLocation = new PotentialHelper(enemyMain.BaseLocation.Pos, 8).To(enemyMain.MineralLinePos).Get();

            if (CurrentState == State.Attack
                || Units.Count < 2)
            {
                foreach (Agent agent in units)
                {
                    if (Bot.Main.Frame % 48 == 0)
                        agent.Order(2544, targetLocation);
                    bot.MicroController.Attack(agent, targetLocation);
                }
            } else if (CurrentState == State.GroupUp)
            {
                Point2D center = new Point2D();
                foreach (Agent agent in Units)
                {
                    center.X += agent.Unit.Pos.X / Units.Count;
                    center.Y += agent.Unit.Pos.Y / Units.Count;
                }
                foreach (Agent agent in units)
                    bot.MicroController.Attack(agent, center);
            }

        }
    }
}
