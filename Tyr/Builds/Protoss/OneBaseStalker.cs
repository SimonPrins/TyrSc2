using SC2APIProtocol;
using System.Collections.Generic;
using SC2Sharp.Agents;
using SC2Sharp.Builds.BuildLists;
using SC2Sharp.Managers;
using SC2Sharp.MapAnalysis;
using SC2Sharp.Micro;
using SC2Sharp.Tasks;
using SC2Sharp.Util;

namespace SC2Sharp.Builds.Protoss
{
    public class OneBaseStalker : Build
    {
        public int RequiredSize = 10;
        public bool ProxyPylon = false;
        public bool HuntProxy = false;
        private bool PylonPlaced = false;
        public bool HallucinationScout = false;
        private bool PhoenixScoutSent = false;
        private bool MainScouted = false;
        private bool ThirdScouted = false;
        private Point2D EnemyNatural = null;
        private Point2D EnemyThird = null;

        private WallInCreator WallIn = new WallInCreator();

        public bool VoidrayTransition = false;
        private KillTargetController KillCycloneController = new KillTargetController(UnitTypes.CYCLONE);
        private KillTargetController KillBansheeController = new KillTargetController(UnitTypes.BANSHEE);

        private List<Point2D> ScoutLocations = new List<Point2D>();

        public override string Name()
        {
            return "OneBaseStalker";
        }

        public override void InitializeTasks()
        {
            base.InitializeTasks();
            DefenseTask.Enable();
            TimingAttackTask.Enable();
            if (Bot.Main.TargetManager.PotentialEnemyStartLocations.Count > 1)
                WorkerScoutTask.Enable();
            if (Bot.Main.BaseManager.Pocket != null)
                ScoutProxyTask.Enable(Bot.Main.BaseManager.Pocket.BaseLocation.Pos);
            if (ProxyPylon && !PylonPlaced)
                PlacePylonTask.Enable();
            ScoutTask.Enable();
            if (HuntProxy)
                HuntProxyTask.Enable();

            HuntProxyTask.Task.CloseBasesFirst = true;
            HuntProxyTask.Task.AddMidwayPoint = false;
            HuntProxyTask.Task.StartFrame = (int)(22.4 * 15);
        }

        public override void OnStart(Bot bot)
        {
            MicroControllers.Add(new FleeCyclonesController());
            MicroControllers.Add(new BlinkForwardController());
            MicroControllers.Add(new SentryController());
            MicroControllers.Add(new DodgeBallController());
            MicroControllers.Add(new TargetUnguardedBuildingsController());
            MicroControllers.Add(new AdvanceController());
            MicroControllers.Add(new StutterController());
            MicroControllers.Add(new KillTargetController(UnitTypes.SCV) { MaxDist = 4 });
            MicroControllers.Add(KillCycloneController);
            MicroControllers.Add(KillBansheeController);
            MicroControllers.Add(new KillTargetController(UnitTypes.SCV, true));

            foreach (WorkerDefenseTask task in WorkerDefenseTask.Tasks)
                task.OnlyDefendInsideMain = true;

            bot.TargetManager.PrefferDistant = false;


            if (Bot.Main.EnemyRace == Race.Terran)
            {
                WallIn.CreateReaperWall(new List<uint> { UnitTypes.GATEWAY, UnitTypes.PYLON, UnitTypes.CYBERNETICS_CORE });
                WallIn.ReserveSpace();
            }

            Set += ProtossBuildUtil.Pylons(() => Count(UnitTypes.PYLON) > 0 && Count(UnitTypes.CYBERNETICS_CORE) > 0);
            Set += Units();
            Set += MainBuildList();

            try
            {
                string[] scoutingLocations = Util.FileUtil.ReadScoutLocationFile();
                HashSet<string> existingLocations = new HashSet<string>();

                string[] debugLines = Util.FileUtil.ReadDebugFile();
                List<Point2D> fromCurrentStart = new List<Point2D>();
                List<Point2D> fromOtherStart = new List<Point2D>();
                string mapName = bot.GameInfo.MapName;
                string mapStartString = mapName + "(" + bot.MapAnalyzer.StartLocation.X + ", " + bot.MapAnalyzer.StartLocation.Y + "):";

                foreach (string line in scoutingLocations)
                {
                    if (!line.StartsWith(mapName))
                        continue;
                    existingLocations.Add(line);
                }

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

                    Point2D basePos = null;
                    float dist = 1000000;
                    foreach (Base b in Bot.Main.BaseManager.Bases)
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
                HuntProxyTask.Task.ScoutBases = ScoutLocations;
            }
            catch (System.Exception e)
            {
                Util.DebugUtil.WriteLine("Exception when generating map image: " + e.Message);
            }
        }
        private BuildList Units()
        {
            BuildList result = new BuildList();

            result.Train(UnitTypes.PROBE, 20);
            result.Train(UnitTypes.PROBE, 32, () => Completed(UnitTypes.NEXUS) >= 2);
            result.Train(UnitTypes.STALKER, 3);
            result.Upgrade(UpgradeType.Blink);
            result.If(() => UpgradeType.LookUp[UpgradeType.Blink].Started() || Completed(UnitTypes.TWILIGHT_COUNSEL) == 0);
            result.If(() => Bot.Main.BaseManager.Main.BaseLocation.MineralFields.Count >= 8 || Count(UnitTypes.NEXUS) >= 2);
            result.Train(UnitTypes.VOID_RAY);
            result.Upgrade(UpgradeType.WarpGate);
            result.If(() => Count(UnitTypes.STARGATE) > 0 || TotalEnemyCount(UnitTypes.ROBOTICS_FACILITY) == 0 || !VoidrayTransition);
            result.Train(UnitTypes.STALKER, 8);
            result.Train(UnitTypes.SENTRY, 1, () => HallucinationScout && !PhoenixScoutSent);
            result.Train(UnitTypes.STALKER);

            return result;
        }

        private BuildList MainBuildList()
        {
            BuildList result = new BuildList();

            result.Building(UnitTypes.NEXUS);
            if (WallIn.Wall.Count >= 3)
            {
                result.Building(UnitTypes.PYLON, Main, WallIn.Wall[1].Pos, true);
                result.Building(UnitTypes.GATEWAY, Main, WallIn.Wall[0].Pos, true);
            }
            else
            {
                result.Building(UnitTypes.PYLON, Main);
                result.If(() => Completed(UnitTypes.PYLON) > 0);
                result.Building(UnitTypes.GATEWAY, Main);
            }
            result.Building(UnitTypes.ASSIMILATOR);
            if (WallIn.Wall.Count >= 3)
                result.Building(UnitTypes.CYBERNETICS_CORE, Main, WallIn.Wall[2].Pos, true);
            else
                result.Building(UnitTypes.CYBERNETICS_CORE, Main);
            result.Building(UnitTypes.GATEWAY);
            result.Building(UnitTypes.STARGATE, () => TotalEnemyCount(UnitTypes.ROBOTICS_FACILITY) > 0 && VoidrayTransition);
            result.Building(UnitTypes.ASSIMILATOR);
            result.Building(UnitTypes.TWILIGHT_COUNSEL);
            result.Building(UnitTypes.GATEWAY, () => Count(UnitTypes.STALKER) >= 2);
            result.Building(UnitTypes.GATEWAY, () => Minerals() >= 250 && Count(UnitTypes.STALKER) >= 8 && UpgradeType.LookUp[UpgradeType.Blink].Started());
            result.If(() => Bot.Main.BaseManager.Main.BaseLocation.MineralFields.Count < 8);
            result.Building(UnitTypes.NEXUS);
            result.Building(UnitTypes.PYLON, Natural);
            result.Building(UnitTypes.ASSIMILATOR, 2, () => Minerals() >= 400);

            return result;
        }

        public override void OnFrame(Bot bot)
        {
            TimingAttackTask.Task.DefendOtherAgents = false;
            TimingAttackTask.Task.RequiredSize = RequiredSize;
            TimingAttackTask.Task.RetreatSize = 6;

            DefenseTask.GroundDefenseTask.MainDefenseRadius = 20;

            if (!PylonPlaced)
                foreach (Agent agent in bot.UnitManager.Agents.Values)
                    if (agent.Unit.UnitType == UnitTypes.PYLON && SC2Util.DistanceSq(agent.Unit.Pos, bot.MapAnalyzer.StartLocation) >= 40 * 40)
                    {
                        PylonPlaced = true;
                        PlacePylonTask.Task.Clear();
                        PlacePylonTask.Task.Stopped = true;
                    }

            Point2D enemyRamp = bot.MapAnalyzer.GetEnemyRamp();
            int rampDepots = 0;
            foreach (Unit enemy in bot.Enemies())
            {
                if (enemy.UnitType != UnitTypes.SUPPLY_DEPOT)
                    continue;
                if (enemy.DisplayType != DisplayType.Visible)
                    continue;
                if (SC2Util.DistanceSq(enemyRamp, enemy.Pos) >= 4 * 4)
                    continue;
                rampDepots++;
            }
            if (rampDepots >= 3)
            {
                KillBansheeController.Stopped = true;
                KillCycloneController.Stopped = true;
            }

            foreach (Agent agent in bot.UnitManager.Agents.Values)
            {
                if (bot.Frame % 224 != 0)
                    break;
                if (agent.Unit.UnitType != UnitTypes.GATEWAY)
                    continue;

                if (Count(UnitTypes.NEXUS) < 2 && TimingAttackTask.Task.Units.Count == 0)
                    agent.Order(Abilities.MOVE, Main.BaseLocation.Pos);
                else
                    agent.Order(Abilities.MOVE, bot.TargetManager.PotentialEnemyStartLocations[0]);
            }

            bot.NexusAbilityManager.Stopped = Count(UnitTypes.STALKER) == 0;
            bot.NexusAbilityManager.PriotitizedAbilities.Add(TrainingType.LookUp[UnitTypes.STALKER].Ability);

            foreach (Agent agent in bot.Units())
            {
                if (agent.Unit.UnitType != UnitTypes.PHOENIX)
                    continue;
                if (EnemyThird != null && agent.DistanceSq(EnemyThird) <= 8 * 8)
                    ThirdScouted = true;
                if (agent.DistanceSq(bot.TargetManager.PotentialEnemyStartLocations[0]) <= 8 * 8)
                    MainScouted = true;
            }
            ScoutTask.Task.ScoutType = UnitTypes.PHOENIX;
            if (EnemyNatural == null)
                EnemyNatural = bot.MapAnalyzer.GetEnemyNatural().Pos;
            if (EnemyThird == null)
                EnemyThird = bot.MapAnalyzer.GetEnemyThird().Pos;
            if (!ThirdScouted)
                ScoutTask.Task.Target = EnemyThird;
            else if (!MainScouted)
                ScoutTask.Task.Target = bot.TargetManager.PotentialEnemyStartLocations[0];
            else
            ScoutTask.Task.Target = EnemyNatural;

            if (Count(UnitTypes.PHOENIX) > 0)
                PhoenixScoutSent = true;

            if (!PhoenixScoutSent)
            {
                foreach (Agent agent in bot.Units())
                {
                    if (agent.Unit.UnitType != UnitTypes.SENTRY)
                        continue;
                    if (agent.Unit.Energy < 75)
                        continue;
                    // Hallucinate scouting phoenix.
                    agent.Order(154);
                }
            }
        }
    }
}
