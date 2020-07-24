using SC2APIProtocol;
using System;
using System.Collections.Generic;
using Tyr.Agents;
using Tyr.Managers;
using Tyr.Util;

namespace Tyr.Tasks
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
            return Bot.Bot.Build.Completed(UnitTypes.ADEPT) >= RequiredSize;
        }

        public override List<UnitDescriptor> GetDescriptors()
        {
            List<UnitDescriptor> result = new List<UnitDescriptor>();
            int required = RequiredSize - Units.Count;
            if (required > 0 && !Sent)
                result.Add(new UnitDescriptor() { Count = required, UnitTypes = new HashSet<uint>() { UnitTypes.ADEPT } });
            return result;
        }

        public override void OnFrame(Bot tyr)
        {
            tyr.DrawText("Adept harass count: " + units.Count);
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
            foreach (Base b in tyr.BaseManager.Bases)
            {
                if (SC2Util.DistanceSq(b.BaseLocation.Pos, tyr.TargetManager.PotentialEnemyStartLocations[0]) <= 2 * 2)
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
                    if (Bot.Bot.Frame % 48 == 0)
                        agent.Order(2544, targetLocation);
                    tyr.MicroController.Attack(agent, targetLocation);
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
                    tyr.MicroController.Attack(agent, center);
            }

        }
    }
}
