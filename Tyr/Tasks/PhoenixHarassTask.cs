using SC2APIProtocol;
using System.Collections.Generic;
using SC2Sharp.Agents;
using SC2Sharp.Managers;
using SC2Sharp.Util;

namespace SC2Sharp.Tasks
{
    class PhoenixHarassTask : Task
    {
        public static PhoenixHarassTask Task = new PhoenixHarassTask();

        public int RequiredSize { get; set; } = 7;

        Point2D Target = null;

        private List<GravitonTarget> GravitonTargets = new List<GravitonTarget>();

        private List<Point2D> TargetLocations = new List<Point2D>();

        private Point2D Center;
        

        public static void Enable()
        {
            Enable(Task);
        }

        public PhoenixHarassTask() : base(8)
        { }

        public override bool DoWant(Agent agent)
        {
            return agent.Unit.UnitType == UnitTypes.PHOENIX;
        }

        public override bool IsNeeded()
        {
            return Bot.Main.UnitManager.Completed(UnitTypes.PHOENIX) >= RequiredSize;
        }

        public override void OnFrame(Bot bot)
        {

            Center = new Point2D();
            foreach (Agent agent in Units)
            {
                Center.X += agent.Unit.Pos.X / Units.Count;
                Center.Y += agent.Unit.Pos.Y / Units.Count;
            }
            DetermineTarget();

            CleanGravitonTargets();

            int lifts = 0;
            foreach (Agent agent in Units)
                lifts += (int)(agent.Unit.Energy / 50);

            DetermineGravitonTargets(lifts);


            foreach (Agent agent in units)
            {
                bool lift = false;
                foreach (GravitonTarget gravitonTarget in GravitonTargets)
                {
                    if (gravitonTarget.PhoenixTag == agent.Unit.Tag)
                    {
                        agent.Order(173, gravitonTarget.TargetTag);
                        lift = true;
                        break;
                    }
                }
                if (lift)
                    continue;


                bool attackLifted = false;
                foreach (GravitonTarget gravitonTarget in GravitonTargets)
                {
                    Point2D targetLocation = gravitonTarget.GetTargetLocation();
                    if (agent.DistanceSq(targetLocation) <= 15 * 15)
                    {
                        if (agent.DistanceSq(targetLocation) > 3 * 3)
                            agent.Order(Abilities.MOVE, targetLocation);
                        attackLifted = true;
                        break;
                    }
                }
                if (attackLifted)
                    continue;

                float dist = 15 * 15;
                Unit attackTarget = null;
                foreach (Unit enemy in bot.Enemies())
                {
                    if (!enemy.IsFlying)
                        continue;
                    float newDist = agent.DistanceSq(enemy);
                    if (newDist >= dist)
                        continue;
                    if (SC2Util.DistanceSq(enemy.Pos, Center) >= 20 * 20)
                        continue;
                    dist = newDist;
                    attackTarget = enemy;
                }
                if (attackTarget != null)
                {
                    if (agent.DistanceSq(attackTarget) > 3 * 3)
                        agent.Order(Abilities.MOVE, SC2Util.To2D(attackTarget.Pos));
                    continue;
                }
                if (bot.Frame % 5 == 0)
                    agent.Order(Abilities.MOVE, Target);
            }
        }

        public void DetermineTarget()
        {
            if (Target == null)
                Target = Bot.Main.TargetManager.PotentialEnemyStartLocations[0];

            bool phoenixClose = false;
            foreach (Agent agent in Units)
            {
                if (agent.DistanceSq(Target) <= 3 * 3)
                {
                    phoenixClose = true;
                    break;
                }
            }
            if (!phoenixClose)
                return;
            if (TargetLocations.Count == 0)
            {
                foreach (Base b in Bot.Main.BaseManager.Bases)
                {
                    if (SC2Util.DistanceSq(b.BaseLocation.Pos, Bot.Main.TargetManager.PotentialEnemyStartLocations[0]) >= 50 * 50)
                        continue;

                    TargetLocations.Add(b.BaseLocation.Pos);
                }
            }

            float dist = 1000000;
            Point2D picked = null;
            foreach (Agent agent in Units)
            {
                foreach (Point2D targetLocation in TargetLocations)
                {
                    float newDist = agent.DistanceSq(targetLocation);
                    if (agent.DistanceSq(targetLocation) > dist)
                        continue;
                    dist = newDist;
                    picked = targetLocation;
                }
            }
            if (picked != null)
            {
                Target = picked;
                TargetLocations.Remove(picked);
            }
        }

        public void CleanGravitonTargets()
        {
            for (int i = GravitonTargets.Count - 1; i >= 0; i--)
                if (GravitonTargets[i].Done())
                    GravitonTargets.RemoveAt(i);
        }

        public void DetermineGravitonTargets(int lifts)
        {
            if (lifts == 0)
                return;
            int allowedLifts = (Units.Count >= 10 ? 2 : 1) - GravitonTargets.Count;
            if (allowedLifts == 0)
                return;

            foreach (Agent agent in Units)
            {
                if (agent.Unit.Energy < 50)
                    continue;
                bool alreadyLifting = false;
                foreach (GravitonTarget target in GravitonTargets)
                    if (target.PhoenixTag == agent.Unit.Tag)
                    {
                        alreadyLifting = true;
                        break;
                    }
                if (alreadyLifting)
                    continue;

                Unit liftTarget = null;
                foreach (Unit enemy in Bot.Main.Enemies())
                {
                    if (enemy.UnitType != UnitTypes.QUEEN
                        && enemy.UnitType != UnitTypes.HYDRALISK)
                        continue;
                    if (SC2Util.DistanceSq(enemy.Pos, Center) >= 20 * 20)
                        continue;
                    if (agent.DistanceSq(enemy) >= 15 * 15)
                        continue;
                    bool alreadyLifted = false;
                    foreach (GravitonTarget target in GravitonTargets)
                        if (target.TargetTag == enemy.Tag)
                        {
                            alreadyLifted = true;
                            break;
                        }
                    if (alreadyLifted)
                        continue;
                    liftTarget = enemy;
                }
                if (liftTarget != null)
                {
                    GravitonTargets.Add(new GravitonTarget() { PhoenixTag = agent.Unit.Tag, TargetTag = liftTarget.Tag, StartFrame = Bot.Main.Frame });
                    allowedLifts--;
                    lifts--;
                    if (allowedLifts == 0)
                        return;
                    continue;
                }
                if (lifts < 5)
                    return;
                foreach (Unit enemy in Bot.Main.Enemies())
                {
                    if (enemy.UnitType != UnitTypes.ROACH
                        && enemy.UnitType != UnitTypes.RAVAGER)
                        continue;
                    if (agent.DistanceSq(enemy) >= 12 * 12)
                        continue;
                    if (SC2Util.DistanceSq(enemy.Pos, Center) >= 20 * 20)
                        continue;
                    bool alreadyLifted = false;
                    foreach (GravitonTarget target in GravitonTargets)
                        if (target.TargetTag == enemy.Tag)
                        {
                            alreadyLifted = true;
                            break;
                        }
                    if (alreadyLifted)
                        continue;
                    liftTarget = enemy;
                }
                if (liftTarget != null)
                {
                    GravitonTargets.Add(new GravitonTarget() { PhoenixTag = agent.Unit.Tag, TargetTag = liftTarget.Tag, StartFrame = Bot.Main.Frame });
                    allowedLifts--;
                    lifts--;
                    if (allowedLifts == 0)
                        return;
                    continue;
                }
                foreach (Unit enemy in Bot.Main.Enemies())
                {
                    if (!UnitTypes.WorkerTypes.Contains(enemy.UnitType))
                        continue;
                    if (agent.DistanceSq(enemy) >= 12 * 12)
                        continue;
                    if (SC2Util.DistanceSq(enemy.Pos, Center) >= 20 * 20)
                        continue;
                    bool alreadyLifted = false;
                    foreach (GravitonTarget target in GravitonTargets)
                        if (target.TargetTag == enemy.Tag)
                        {
                            alreadyLifted = true;
                            break;
                        }
                    if (alreadyLifted)
                        continue;
                    liftTarget = enemy;
                }
                if (liftTarget != null)
                {
                    GravitonTargets.Add(new GravitonTarget() { PhoenixTag = agent.Unit.Tag, TargetTag = liftTarget.Tag, StartFrame = Bot.Main.Frame });
                    allowedLifts--;
                    lifts--;
                    if (allowedLifts == 0)
                        return;
                    continue;
                }
            }
        }
    }

    class GravitonTarget
    {
        public ulong PhoenixTag;
        public ulong TargetTag;
        public int StartFrame;

        public bool Done()
        {
            if (Bot.Main.Frame - StartFrame >= 22.4 * 7)
                return true;
            if (!Bot.Main.UnitManager.Agents.ContainsKey(PhoenixTag))
                return true;
            if (Bot.Main.EnemyManager.LastSeenFrame.ContainsKey(TargetTag)
                && Bot.Main.Frame - Bot.Main.EnemyManager.LastSeenFrame[TargetTag] >= 1)
                return true;

            foreach (Unit enemy in Bot.Main.Enemies())
                if (enemy.Tag == TargetTag)
                    return false;
            return true;
        }

        public Point2D GetTargetLocation()
        {
            if (Bot.Main.EnemyManager.LastSeen.ContainsKey(TargetTag))
                return SC2Util.To2D(Bot.Main.EnemyManager.LastSeen[TargetTag].Pos);
            return SC2Util.To2D(Bot.Main.UnitManager.Agents[PhoenixTag].Unit.Pos);
        }
    }

}
