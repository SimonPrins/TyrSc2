using System.Collections.Generic;
using SC2APIProtocol;
using SC2Sharp.Agents;
using SC2Sharp.Util;

namespace SC2Sharp.Tasks
{
    class KillPylonTask : Task
    {
        public static KillPylonTask Task = new KillPylonTask();
        private Unit KillPylon = null;
        private Unit KillProbe = null;
        private int PylonUpdateFrame = 0;
        private bool CannonFinished = false;

        public KillPylonTask() : base(10)
        { }

        public static void Enable()
        {
            Enable(Task);
        }

        public override bool DoWant(Agent agent)
        {
            return agent.Unit.UnitType == UnitTypes.PROBE && units.Count < 8;
        }

        public override List<UnitDescriptor> GetDescriptors()
        {
            UpdateAttackers();
            int desiredWorkers = 8 - Units.Count;
            List<UnitDescriptor> result = new List<UnitDescriptor>();
            if (desiredWorkers > 0 && !CannonFinished)
                result.Add(new UnitDescriptor() { Pos = Bot.Main.TargetManager.AttackTarget, Count = desiredWorkers, UnitTypes = UnitTypes.WorkerTypes });
            return result;
        }

        public override bool IsNeeded()
        {
            UpdateAttackers();
            return KillPylon != null;
        }

        private void UpdateAttackers()
        {
            if (Bot.Main.Frame == PylonUpdateFrame)
                return;
            Point2D main = Bot.Main.BaseManager.Main.BaseLocation.Pos;
            Point2D natural = Bot.Main.BaseManager.Natural.BaseLocation.Pos;
            PylonUpdateFrame = Bot.Main.Frame;

            if (KillPylon != null)
            {
                bool pylonRemains = false;
                foreach (Unit enemy in Bot.Main.Enemies())
                {
                    if (enemy.Tag != KillPylon.Tag)
                        continue;
                    KillPylon = enemy;
                    pylonRemains = true;
                    break;
                }
                if (!pylonRemains)
                    KillPylon = null;
            }
            if (KillPylon == null)
            {
                foreach (Unit enemy in Bot.Main.Enemies())
                {
                    if (enemy.UnitType != UnitTypes.PYLON)
                        continue;
                    if (SC2Util.DistanceSq(enemy.Pos, main) > 30 * 30
                        && SC2Util.DistanceSq(enemy.Pos, natural) > 25 * 25)
                        continue;
                    KillPylon = enemy;
                    break;
                }

            }
            if (KillProbe != null)
            {
                bool probeRemains = false;
                foreach (Unit enemy in Bot.Main.Enemies())
                {
                    if (enemy.Tag != KillProbe.Tag)
                        continue;
                    if (SC2Util.DistanceSq(enemy.Pos, main) > 40 * 40
                        && SC2Util.DistanceSq(enemy.Pos, natural) > 35 * 35)
                        break;
                    KillProbe = enemy;
                    probeRemains = true;
                    break;
                }
                if (!probeRemains)
                    KillProbe = null;
            }
            if (KillProbe == null)
            {
                foreach (Unit enemy in Bot.Main.Enemies())
                {
                    if (enemy.UnitType != UnitTypes.PROBE)
                        continue;
                    if (SC2Util.DistanceSq(enemy.Pos, main) > 30 * 30
                        && SC2Util.DistanceSq(enemy.Pos, natural) > 25 * 25)
                        continue;
                    KillProbe = enemy;
                    break;
                }

            }
            foreach (Unit enemy in Bot.Main.Enemies())
            {
                if (enemy.UnitType != UnitTypes.PHOTON_CANNON)
                    continue;
                if (enemy.BuildProgress < 0.9)
                    continue;
                if (SC2Util.DistanceSq(enemy.Pos, main) > 30 * 30
                    && SC2Util.DistanceSq(enemy.Pos, natural) > 25 * 25)
                    continue;
                CannonFinished = true;
                break;
            }
        }

        public override void OnFrame(Bot bot)
        {
            UpdateAttackers();
            if (CannonFinished
                || KillPylon == null)
            {
                Clear();
                return;
            }

            int probeAttackers = 0;
            foreach (Agent agent in Units)
            {
                if (probeAttackers < 2
                    && KillProbe != null)
                {
                    probeAttackers++;
                    agent.Order(Abilities.ATTACK, KillProbe.Tag);
                    continue;
                }
                agent.Order(Abilities.ATTACK, KillPylon.Tag);
            }
        }
    }
}
