﻿using SC2APIProtocol;
using System.Collections.Generic;
using SC2Sharp.Agents;
using SC2Sharp.Util;

namespace SC2Sharp.Micro
{
    public class CycloneController : CustomController
    {
        private Dictionary<ulong, int> LockOnFrame = new Dictionary<ulong, int>();

        public override bool DetermineAction(Agent agent, Point2D target)
        {
            if (agent.Unit.UnitType != UnitTypes.CYCLONE)
                return false;

            if (agent.Unit.Orders != null && agent.Unit.Orders.Count >= 2 && (!LockOnFrame.ContainsKey(agent.Unit.Tag) || Bot.Main.Frame - LockOnFrame[agent.Unit.Tag] >= 22.4 * 15))
            {
                if (!LockOnFrame.ContainsKey(agent.Unit.Tag))
                    LockOnFrame.Add(agent.Unit.Tag, Bot.Main.Frame);
                else
                    LockOnFrame[agent.Unit.Tag] = Bot.Main.Frame;
            }

            if (LockOnFrame.ContainsKey(agent.Unit.Tag) && Bot.Main.Frame - LockOnFrame[agent.Unit.Tag] < 22.4 * 16)
                Bot.Main.DrawSphere(agent.Unit.Pos);

            if (LockOnFrame.ContainsKey(agent.Unit.Tag) && Bot.Main.Frame - LockOnFrame[agent.Unit.Tag] < 11)
                return true;
            bool lockedOn = LockOnFrame.ContainsKey(agent.Unit.Tag) && Bot.Main.Frame - LockOnFrame[agent.Unit.Tag] < 22.4 * 16;

            if (!lockedOn)
                return false;



            foreach (Unit enemy in Bot.Main.Enemies())
            {
                if (!UnitTypes.CanAttackGround(enemy.UnitType))
                    continue;
                if (agent.DistanceSq(enemy) <= 12 * 12)
                {
                    agent.Order(Abilities.MOVE, SC2Util.To2D(Bot.Main.MapAnalyzer.StartLocation));
                    return true;
                }
            }
            
            return false;
        }
    }
}
