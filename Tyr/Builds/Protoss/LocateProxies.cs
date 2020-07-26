using SC2APIProtocol;
using System.Collections.Generic;
using SC2Sharp.Agents;
using SC2Sharp.Managers;
using SC2Sharp.Util;

namespace SC2Sharp.Builds.Protoss
{
    public class LocateProxies : Build
    {
        private List<Point2D> ScoutLocations = new List<Point2D>();
        public override string Name()
        {
            return "LocateProxies";
        }

        public override void InitializeTasks()
        {
        }

        public override void OnStart(Bot bot)
        {
            DetermineProxyLocations();
        }
        private void DetermineProxyLocations()
        {

            string[] scoutingLocations = Util.FileUtil.ReadScoutLocationFile();
            HashSet<string> existingLocations = new HashSet<string>();

            string mapName = Bot.Main.GameInfo.MapName;
            string mapStartString = mapName + "(" + Bot.Main.MapAnalyzer.StartLocation.X + ", " + Bot.Main.MapAnalyzer.StartLocation.Y + "):";
            foreach (string line in scoutingLocations)
            {
                if (!line.StartsWith(mapName))
                    continue;
                existingLocations.Add(line);
            }

            /*
            foreach (string line in debugLines)
            {
                if (!line.StartsWith(mapName))
                    continue;
                string position = line.Substring(line.LastIndexOf("("));
                position = position.Replace(")", "").Replace("(", "");
                string[] pos = position.Split(',');
                Point2D point = new Point2D() { X = float.Parse(pos[0]), Y = float.Parse(pos[1]) };
                if (line.StartsWith(mapStartString))
                    fromCurrentStart.Add(point);
                else
                    fromOtherStart.Add(point);
                DrawMap(mapName + " from current", fromCurrentStart);
                DrawMap(mapName + " from other", fromOtherStart);

                Point2D basePos = null;
                dist = 1000000;
                foreach (Base b in Tyr.Bot.BaseManager.Bases)
                {
                    float newDist = SC2Util.DistanceSq(b.BaseLocation.Pos, point);
                    if (newDist > dist)
                        continue;
                    dist = newDist;
                    basePos = b.BaseLocation.Pos;
                }
                string locationString = line.Substring(0, line.LastIndexOf("(")) + "(" + basePos.X + "," + basePos.Y + ")";
                if (!existingLocations.Contains(locationString))
                {
                    existingLocations.Add(locationString);
                    FileUtil.WriteScoutLocation(locationString);
                }
            }
            */

            foreach (string line in scoutingLocations)
            {
                if (!line.StartsWith(mapStartString))
                    continue;

                string position = line.Substring(line.LastIndexOf("("));
                position = position.Replace(")", "").Replace("(", "");
                string[] pos = position.Split(',');
                Point2D point = new Point2D() { X = float.Parse(pos[0]), Y = float.Parse(pos[1]) };
                ScoutLocations.Add(point);
                DebugUtil.WriteLine("Found scout location: " + point);
            }
        }

        public override void OnFrame(Bot bot)
        {
            if (bot.Frame >= 22.4 * 100)
                bot.GameConnection.RequestLeaveGame().Wait();

            if (bot.Frame == (int)(22.4 * 40))
            {
                List<Base> bases = new List<Base>();
                foreach (Base b in bot.BaseManager.Bases)
                {
                    if (b == Natural || b == Main)
                        continue;
                    bases.Add(b);
                }

                bases.Sort((a, b) => System.Math.Sign(SC2Util.DistanceSq(a.BaseLocation.Pos, Main.BaseLocation.Pos) - SC2Util.DistanceSq(b.BaseLocation.Pos, Main.BaseLocation.Pos)));
                int basePos = 0;
                foreach (Agent agent in bot.Units())
                {
                    if (agent.Unit.UnitType != UnitTypes.PROBE)
                        continue;

                    agent.Order(Abilities.MOVE, bases[basePos].BaseLocation.Pos);

                    basePos++;
                    if (basePos >= bases.Count)
                        break;
                }
            }

            foreach (Unit enemy in bot.Enemies())
            {
                if (enemy.UnitType != UnitTypes.BARRACKS)
                    continue;
                if (enemy.IsFlying)
                    continue;
                if (Util.SC2Util.DistanceSq(enemy.Pos, bot.TargetManager.PotentialEnemyStartLocations[0]) <= 40 * 40)
                    continue;

                float dist = 1000000;
                Point2D pos = null;
                foreach (Base b in bot.BaseManager.Bases)
                {
                    float newDist = SC2Util.DistanceSq(b.BaseLocation.Pos, enemy.Pos);
                    if (newDist >= dist)
                        continue;
                    dist = newDist;
                    pos = b.BaseLocation.Pos;
                }
                if (pos != null)
                {
                    bool alreadyRegisterd = false;
                    foreach (Point2D scoutLocation in ScoutLocations )
                    {
                        if (SC2Util.DistanceSq(scoutLocation, pos) <= 4 * 4)
                        {
                            alreadyRegisterd = true;
                            break;
                        }
                    }
                    if (!alreadyRegisterd)
                    {
                        string mapStartString = bot.GameInfo.MapName + "(" + Bot.Main.MapAnalyzer.StartLocation.X + ", " + Bot.Main.MapAnalyzer.StartLocation.Y + "):";
                        string locationString = mapStartString + "(" + pos.X + "," + pos.Y + ")";
                        ScoutLocations.Add(pos);
                        FileUtil.WriteScoutLocation(locationString);
                    }
                    bot.GameConnection.RequestLeaveGame().Wait();
                }
            }
        }
    }
}
