﻿using SC2APIProtocol;
using SC2Sharp.Agents;
using SC2Sharp.Managers;
using SC2Sharp.Util;

namespace SC2Sharp.Tasks
{
    class ClearBlockedExpandsTask : Task
    {
        public static ClearBlockedExpandsTask Task = new ClearBlockedExpandsTask();

        public Base ClearBase = null;

        public ClearBlockedExpandsTask() : base(7)
        { }

        public static void Enable()
        {
            Enable(Task);
        }

        public override bool DoWant(Agent agent)
        {
            if (Units.Count >= 4)
                return false;
            if (!agent.IsCombatUnit)
                return false;
            if (!agent.CanAttackGround())
                return false;
            return true;
        }

        public override bool IsNeeded()
        {
            foreach (Base b in Bot.Main.BaseManager.Bases)
                if (b.Blocked)
                    return true;
            Bot.Main.DrawText("All bases clear.");
            return false;
        }

        public override void OnFrame(Bot bot)
        {
            CheckBaseCleared();
            if (ClearBase == null)
            {
                foreach (Base b in Bot.Main.BaseManager.Bases)
                    if (b.Blocked)
                    {
                        ClearBase = b;
                        break;
                    }
                if (ClearBase == null)
                {
                    Clear();
                    return;
                }
            }
            Bot.Main.DrawText("Clearing base.");

            foreach (Agent agent in units)
                bot.MicroController.Attack(agent, ClearBase.BaseLocation.Pos);
        }

        private void CheckBaseCleared()
        {
            if (ClearBase == null)
                return;
            bool close = false;
            foreach (Agent agent in units)
            {
                if (agent.DistanceSq(ClearBase.BaseLocation.Pos) <= 2 * 2)
                {
                    close = true;
                    break;
                }
            }
            if (!close)
                return;
            foreach (Unit enemy in Bot.Main.Enemies())
                if (SC2Util.DistanceSq(enemy.Pos, ClearBase.BaseLocation.Pos) <= 10 * 10)
                    return;
            ClearBase = null;
        }
    }
}
