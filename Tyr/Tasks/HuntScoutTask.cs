using SC2APIProtocol;
using System;
using System.Collections.Generic;
using SC2Sharp.Agents;
using SC2Sharp.Managers;
using SC2Sharp.MapAnalysis;
using SC2Sharp.Util;

namespace SC2Sharp.Tasks
{
    class HuntScoutTask : Task
    {
        public static HuntScoutTask Task = new HuntScoutTask();
        public int StartFrame = (int)(20 * 22.4);
        public bool Done;
        private Point2D Enemy;
        private int EnemyFrame = -200;
        List<Point2D> ScoutBases;

        public HuntScoutTask() : base(8)
        { }

        public static void Enable()
        {
            Task.Stopped = false;
            Bot.Main.TaskManager.Add(Task);
        }

        public override bool DoWant(Agent agent)
        {
            if (BuildingType.BuildingAbilities.Contains((int)agent.CurrentAbility()))
                return false;
            return agent.IsWorker && units.Count == 0 && !Done;
        }

        public override List<UnitDescriptor> GetDescriptors()
        {
            List<UnitDescriptor> result = new List<UnitDescriptor>();
            if (Units.Count == 0)
                result.Add(new UnitDescriptor() { Pos = Bot.Main.TargetManager.AttackTarget, Count = 1, UnitTypes = UnitTypes.WorkerTypes });
            return result;
        }

        public override bool IsNeeded()
        {
            return Bot.Main.Frame >= StartFrame && !Done;
        }

        public override void OnFrame(Bot bot)
        {
            foreach (RecentlyDeceased deceased in bot.EnemyManager.RecentlyDeceased)
                if (deceased.UnitType == UnitTypes.PROBE)
                    Done = true;

            if (Done)
            {
                Clear();
                return;
            }

            Unit probe = null;
            float dist = 10000;
            foreach (Unit enemy in bot.Enemies())
            {
                if (!UnitTypes.WorkerTypes.Contains(enemy.UnitType))
                    continue;
                float newDist = SC2Util.DistanceSq(enemy.Pos, bot.MapAnalyzer.StartLocation);
                if (newDist > dist)
                    continue;
                dist = newDist;
                probe = enemy;
                EnemyFrame = bot.Frame;
                if (Units.Count > 0)
                    Enemy = new PotentialHelper(probe.Pos, 2).From(Units[0].Unit.Pos).Get();
                else
                    Enemy = SC2Util.To2D(probe.Pos);
            }

            foreach (Agent agent in units)
            {
                float agentDist = agent.DistanceSq(bot.MapAnalyzer.StartLocation);
                if (agentDist >= 80 * 80 && ScoutBases == null)
                {
                    ScoutBases = new List<Point2D>();
                    foreach (Base b in bot.BaseManager.Bases)
                    {
                        float mainDist = SC2Util.DistanceSq(b.BaseLocation.Pos, bot.MapAnalyzer.StartLocation);
                        if (mainDist >= 60 * 60 || mainDist <= 8 * 8)
                            continue;
                        ScoutBases.Add(b.BaseLocation.Pos);
                    }
                    ScoutBases.Sort((Point2D a, Point2D b) => bot.MapAnalyzer.MainDistances[(int)a.X, (int)a.Y] - bot.MapAnalyzer.MainDistances[(int)b.X, (int)b.Y]);
                }

                if (probe != null)
                    agent.Order(Abilities.ATTACK, probe.Tag);
                else if (bot.Frame - EnemyFrame <= 45 && Enemy != null)
                    agent.Order(Abilities.MOVE, Enemy);
                else if (ScoutBases != null)
                {
                    if (agent.DistanceSq(ScoutBases[0]) <= 4 * 4)
                    {
                        ScoutBases.RemoveAt(0);
                        if (ScoutBases.Count == 0)
                        {
                            Done = true;
                            Clear();
                            return;
                        }
                    }
                    agent.Order(Abilities.MOVE, ScoutBases[0]);
                }
                else
                    agent.Order(Abilities.MOVE, bot.TargetManager.AttackTarget);
            }
        }
    }
}
