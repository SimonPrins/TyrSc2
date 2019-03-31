using SC2APIProtocol;
using System;
using System.Collections.Generic;
using Tyr.Agents;
using Tyr.Managers;
using Tyr.Util;

namespace Tyr.Tasks
{
    class CreeperLordTask : Task
    {
        public static CreeperLordTask Task = new CreeperLordTask();

        public int KeepForOverseers = 3;

        Dictionary<ulong, Base> AssignedBases = new Dictionary<ulong, Base>();

        bool printed = false;

        public CreeperLordTask() : base(7)
        { }

        public static void Enable()
        {
            Enable(Task);
        }

        public override bool DoWant(Agent agent)
        {
            return true;
        }

        public override List<UnitDescriptor> GetDescriptors()
        {
            List<UnitDescriptor> result = new List<UnitDescriptor>();
            int desired = Tyr.Bot.UnitManager.Completed(UnitTypes.OVERLORD) + Tyr.Bot.UnitManager.Completed(UnitTypes.OVERSEER) - KeepForOverseers - Units.Count;
            if (desired > 0)
                result.Add(new UnitDescriptor() { Count = desired, UnitTypes = new HashSet<uint>() { UnitTypes.OVERLORD } });
            return result;
        }

        public override bool IsNeeded()
        {
            return true;
        }

        public override void OnFrame(Tyr tyr)
        {
            HashSet<Base> alreadyAssigned = new HashSet<Base>();
            foreach (Agent agent in Units)
            {
                if (AssignedBases.ContainsKey(agent.Unit.Tag))
                {
                    if (AssignedBases[agent.Unit.Tag].Owner != -1)
                        AssignedBases.Remove(agent.Unit.Tag);
                    else
                        alreadyAssigned.Add(AssignedBases[agent.Unit.Tag]);
                }
            }

            List<Base> bases = new List<Base>();
            foreach (Base b in tyr.BaseManager.Bases)
            {
                if (!printed)
                {
                    Console.WriteLine("Checking base: " + b.BaseLocation.Pos);
                    Console.WriteLine("Is main: " + (b == tyr.BaseManager.Main));
                    Console.WriteLine("Is natural: " + (b == tyr.BaseManager.Natural));
                    Console.WriteLine("Is enemy main: " + (SC2Util.DistanceSq(b.BaseLocation.Pos, tyr.TargetManager.PotentialEnemyStartLocations[0]) < 2 * 2));
                    Console.WriteLine("Owner is neutral: " + (b.Owner == -1));
                    Console.WriteLine("Alread assigned: " + (alreadyAssigned.Contains(b)));
                }
                if (b != tyr.BaseManager.Main
                    && b != tyr.BaseManager.Natural
                    && SC2Util.DistanceSq(b.BaseLocation.Pos, tyr.TargetManager.PotentialEnemyStartLocations[0]) >= 2 * 2
                    && b.Owner == -1
                    && !alreadyAssigned.Contains(b))
                {
                    if (!printed)
                        Console.WriteLine("Adding base.");
                    bases.Add(b);
                }
                if (!printed)
                    Console.WriteLine();
            }
            printed = true;
            bases.Sort((Base a, Base b) => Math.Sign(tyr.MapAnalyzer.EnemyDistances[(int)a.BaseLocation.Pos.X, (int)a.BaseLocation.Pos.Y] - tyr.MapAnalyzer.EnemyDistances[(int)b.BaseLocation.Pos.X, (int)b.BaseLocation.Pos.Y]));
            
            int assignPos = 0;
            if (bases.Count > 0)
            {
                foreach (Agent agent in Units)
                {
                    if (AssignedBases.ContainsKey(agent.Unit.Tag))
                        continue;

                    CollectionUtil.Add(AssignedBases, agent.Unit.Tag, bases[assignPos % bases.Count]);
                    assignPos++;
                }
            }
            foreach (Agent agent in Units)
            {
                if (!AssignedBases.ContainsKey(agent.Unit.Tag))
                    continue;
                if (agent.DistanceSq(AssignedBases[agent.Unit.Tag].BaseLocation.Pos) > 2 * 2)
                    agent.Order(Abilities.MOVE, AssignedBases[agent.Unit.Tag].BaseLocation.Pos);
            }
        }
    }
}
