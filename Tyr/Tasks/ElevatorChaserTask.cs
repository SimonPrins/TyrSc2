﻿using SC2APIProtocol;
using System.Collections.Generic;
using SC2Sharp.Agents;
using SC2Sharp.Util;

namespace SC2Sharp.Tasks
{
    class ElevatorChaserTask : Task
    {
        private List<Point2D> Targets;
        int Cur;

        public ElevatorChaserTask() : base(6)
        { }

        public override bool DoWant(Agent agent)
        {
            return agent.Unit.UnitType == UnitTypes.VOID_RAY && units.Count == 0;
        }

        public override List<UnitDescriptor> GetDescriptors()
        {
            List<UnitDescriptor> result = new List<UnitDescriptor>();
            result.Add(new UnitDescriptor() { Count = 1, UnitTypes = new HashSet<uint>() { UnitTypes.VOID_RAY } });
            return result;
        }

        public override bool IsNeeded()
        {
            return true;
        }

        public override void OnFrame(Bot bot)
        {
            if (Targets == null)
            {
                Targets = new List<Point2D>();
                Targets.Add(SC2Util.Point(bot.GameInfo.StartRaw.PlayableArea.P0.X, bot.GameInfo.StartRaw.PlayableArea.P0.Y));
                Targets.Add(SC2Util.Point(bot.GameInfo.StartRaw.PlayableArea.P1.X, bot.GameInfo.StartRaw.PlayableArea.P0.Y));
                Targets.Add(SC2Util.Point(bot.GameInfo.StartRaw.PlayableArea.P1.X, bot.GameInfo.StartRaw.PlayableArea.P1.Y));
                Targets.Add(SC2Util.Point(bot.GameInfo.StartRaw.PlayableArea.P0.X, bot.GameInfo.StartRaw.PlayableArea.P1.Y));
            }

            foreach (Agent agent in units)
            {
                if (SC2Util.DistanceSq(agent.Unit.Pos, Targets[Cur]) <= 6 * 6)
                    Cur = (Cur + 1) % 4;
                agent.Order(Abilities.ATTACK, Targets[Cur]);
            }
        }
    }
}
