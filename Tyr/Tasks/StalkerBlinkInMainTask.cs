using SC2APIProtocol;
using System;
using System.Collections.Generic;
using System.Linq;
using SC2Sharp.Agents;
using SC2Sharp.MapAnalysis;
using SC2Sharp.Util;

namespace SC2Sharp.Tasks
{
    class StalkerBlinkInMainTask : Task
    {
        public static StalkerBlinkInMainTask Task = new StalkerBlinkInMainTask();
        private Agent Sentry = null;
        private Point2D EnemyThird = null;
        private Point2D LoadArea = null;
        public Point2D StagingArea = null;
        private HashSet<ulong> BlinkedStalkers = new HashSet<ulong>();
        private bool sentryInPlace = false;

        public bool Cancelled = false;

        public StalkerBlinkInMainTask() : base(5)
        { }

        public static void Enable()
        {
            Enable(Task);
        }

        public override bool DoWant(Agent agent)
        {
            return agent.Unit.UnitType == UnitTypes.STALKER || (agent.Unit.UnitType == UnitTypes.SENTRY && Sentry == null) || agent.Unit.UnitType == UnitTypes.COLOSUS;
        }

        public override List<UnitDescriptor> GetDescriptors()
        {
            List<UnitDescriptor> result = new List<UnitDescriptor>();
            if (Cancelled)
                return result;
            if (Sentry == null)
                result.Add(new UnitDescriptor(UnitTypes.SENTRY) { Pos = Bot.Main.TargetManager.AttackTarget, Count = 1});
            result.Add(new UnitDescriptor(UnitTypes.STALKER) { Pos = Bot.Main.TargetManager.AttackTarget });
            result.Add(new UnitDescriptor(UnitTypes.COLOSUS) { Pos = Bot.Main.TargetManager.AttackTarget });
            return result;
        }

        public override bool IsNeeded()
        {
            return EnemyThird != null && UpgradeType.LookUp[UpgradeType.Blink].Progress() >= 0.8;
        }

        public override void OnFrame(Bot bot)
        {
            if (Sentry != null)
            {
                bool sentryAlive = false;
                foreach (Agent agent in Units)
                {
                    if (agent.Unit.Tag == Sentry.Unit.Tag)
                    {
                        sentryAlive = true;
                        break;
                    }
                }
                if (!sentryAlive)
                    Sentry = null;
            }

            if (Cancelled)
                for (int i = Units.Count - 1; i >= 0; i--)
                {
                    if (!BlinkedStalkers.Contains(Units[i].Unit.Tag)
                        && Units[i].Unit.UnitType != UnitTypes.SENTRY)
                        ClearAt(i);
                }

            if (EnemyThird == null && bot.TargetManager.PotentialEnemyStartLocations.Count == 1)
            {
                Point2D enemyNatural = bot.MapAnalyzer.GetEnemyNatural().Pos;
                Point2D enemyMain = bot.TargetManager.PotentialEnemyStartLocations[0];
                Point2D enemyRamp = bot.MapAnalyzer.GetEnemyRamp();
                float dist = 1000000;
                foreach (BaseLocation loc in bot.MapAnalyzer.BaseLocations)
                {
                    if (SC2Util.DistanceSq(loc.Pos, enemyNatural) <= 2 * 2)
                        continue;
                    float mainDist = SC2Util.DistanceSq(loc.Pos, enemyMain);
                    if (mainDist <= 2 * 2)
                        continue;
                    if (mainDist > 50 * 50)
                        continue;
                    //float newDist = SC2Util.DistanceSq(loc.Pos, enemyRamp);
                    if (mainDist > dist)
                        continue;
                    dist = mainDist;
                    EnemyThird = loc.Pos;
                }
                PotentialHelper potential;
                dist = 25 * 25;
                for (int x = 0; x < bot.MapAnalyzer.EnemyDistances.GetLength(0); x++)
                    for (int y = 0; y < bot.MapAnalyzer.EnemyDistances.GetLength(1); y++)
                    {
                        if (bot.MapAnalyzer.EnemyDistances[x, y] > 30)
                            continue;

                        Point2D point = new Point2D() { X = x, Y = y };
                        float newDist = SC2Util.DistanceSq(point, EnemyThird);
                        if (newDist > dist)
                            continue;
                        dist = newDist;
                        
                        StagingArea = new PotentialHelper(point, 1)
                            .To(bot.TargetManager.PotentialEnemyStartLocations[0])
                            .Get();
                    }
                
                potential = new PotentialHelper(StagingArea, 7f);
                potential.From(bot.TargetManager.PotentialEnemyStartLocations[0]);
                LoadArea = potential.Get();
            }

            if (StagingArea != null)
                bot.DrawSphere(new Point() { X = StagingArea.X, Y = StagingArea.Y, Z = bot.MapAnalyzer.StartLocation.Z });

            if (units.Count == 0)
                return;

            if (Sentry == null)
            {
                foreach (Agent agent in units)
                    if (agent.Unit.UnitType == UnitTypes.SENTRY)
                    {
                        Sentry = agent;
                        break;
                    }
            }
            OrderSentry();

            bool highGroundVision = false;
            bool colosusExists = false;
            foreach (Agent agent in Units)
            {
                if (agent.Unit.UnitType != UnitTypes.COLOSUS)
                    continue;
                colosusExists = true;
                if (agent.DistanceSq(StagingArea) <= 3 * 3)
                {
                    highGroundVision = true;
                    break;
                }
            }

            foreach (Agent agent in units)
            {
                if (Sentry != null && agent.Unit.Tag == Sentry.Unit.Tag)
                    continue;
                if (agent.Unit.BuffIds.Contains(3687))
                    BlinkedStalkers.Add(agent.Unit.Tag);

                if (agent.Unit.UnitType == UnitTypes.COLOSUS)
                    agent.Order(Abilities.MOVE, StagingArea);
                else if (BlinkedStalkers.Contains(agent.Unit.Tag) || bot.Frame >= 22.4 * 60 * 7.5)
                    Attack(agent, bot.TargetManager.AttackTarget);
                else if (agent.DistanceSq(LoadArea) <= 2 * 2 && highGroundVision)
                    agent.Order(Abilities.BLINK, StagingArea);
                else if (agent.DistanceSq(EnemyThird) <= 10 * 10 && colosusExists)
                    agent.Order(Abilities.ATTACK, LoadArea);
                else
                    Attack(agent, EnemyThird);
            }
        }

        private void OrderSentry()
        {
            sentryInPlace = false;
            if (Sentry == null)
            {
                Bot.Main.DrawText("No sentry.");
                return;
            }
            float distance = Sentry.DistanceSq(EnemyThird);
            if (distance < 20 * 20)
                sentryInPlace = true;
            if (distance >= 9 * 9)
            {
                Bot.Main.DrawText("Sentry moving.");
                Sentry.Order(Abilities.MOVE, EnemyThird);
                return;
            }
            Bot.Main.DrawText("Sentry hallucinating.");
            if (Bot.Main.Frame % 5 == 0)
                Sentry.Order(148);
        }
    }
}
