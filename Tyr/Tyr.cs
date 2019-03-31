using System.Collections.Generic;
using System.Diagnostics;
using SC2API_CSharp;
using SC2APIProtocol;
using Tyr.Agents;
using Tyr.BuildingPlacement;
using Tyr.Builds;
using Tyr.Builds.Protoss;
using Tyr.Builds.Terran;
using Tyr.Builds.Zerg;
using Tyr.BuildSelection;
using Tyr.Managers;
using Tyr.MapAnalysis;
using Tyr.Micro;
using Tyr.StrategyAnalysis;
using Tyr.Util;

namespace Tyr
{
    public class Tyr : Bot
    {
        public GameConnection GameConnection;
        public ResponseData Data;
        public int NeutralPlayerId { get; } = 16;
        public Race MyRace;
        public Race EnemyRace;

        public ResponseGameInfo GameInfo;

        public ResponseObservation Observation;

        public uint PlayerId;
        public string OpponentID;

        List<Action> actions = new List<Action>();

        public BuildingPlacer buildingPlacer;
        public int Frame { get; internal set; }
        public MapAnalyzer MapAnalyzer { get; internal set; } = new MapAnalyzer();
        public EnemyStrategyAnalyzer EnemyStrategyAnalyzer = new EnemyStrategyAnalyzer();
        public PreviousEnemyStrategies PreviousEnemyStrategies = new PreviousEnemyStrategies();

        public int ReservedMinerals;
        public int ReservedGas;

        public MicroController MicroController = new MicroController();

        public bool Surrendered = false;
        public int SurrenderedFrame = 0;

        // Managers
        public List<Manager> Managers = new List<Manager>();
        public UnitManager UnitManager = new UnitManager();
        public EnemyManager EnemyManager = new EnemyManager();
        public TargetManager TargetManager = new TargetManager();
        public TaskManager TaskManager = new TaskManager();
        public EffectManager EffectManager = new EffectManager();
        public EnemyMineManager EnemyMineManager = new EnemyMineManager();

        public BaseManager BaseManager = new BaseManager();
        public NexusAbilityManager NexusAbilityManager = new NexusAbilityManager();
        public OrbitalAbilityManager OrbitalAbilityManager = new OrbitalAbilityManager();


        public static Tyr Bot { get; internal set; }

        public Build Build { get; internal set; }

        public bool Monday { get; set; }
        private bool Day9Sent = false;
        private List<Unit> EnemyBases = new List<Unit>();

        private bool loggedError = false;

        private long totalExecutionTime;
        private long maxExecutionTime;

        public Build FixedBuild;

        private Request DrawRequest;

        public static bool Debug = false;

        private int TextLine = 0;

        private long PrevTime = -1;

        List<long> Times = new List<long>();

        private BuildSelector BuildSelector = new RotateSelector();

        public Tyr()
        {
            buildingPlacer = new BuildingPlacer(this);
            Bot = this;
        }

        bool PrintedBarracks = false;

        public IEnumerable<Action> onFrame(ResponseObservation observation)
        {
            Stopwatch stopWatch = Stopwatch.StartNew();
            actions = new List<Action>();
            DrawRequest = null;
            TextLine = 0;
            


            long time1 = 0;
            long time2 = 0;
            long time3 = 0;
            try
            {
                if (observation.Observation == null)
                    return actions;

                if (Frame == 1)
                    Chat(Monday ? "Happy monday! :D" : "Good luck, have fun! :D");
                
                Observation = observation;

                ReservedMinerals = 0;
                ReservedGas = 0;

                EnemyStrategyAnalyzer.OnFrame(this);

                time1 = stopWatch.ElapsedMilliseconds;

                foreach (Manager manager in Managers)
                    manager.OnFrame(this);

                time2 = stopWatch.ElapsedMilliseconds;

                Build.OnFrameBase(this);

                time3 = stopWatch.ElapsedMilliseconds;

                UnitManager.AddActions(actions);

                if (!Surrendered)
                {
                    Surrendered = CheckSurrender();
                    if (Surrendered)
                    {
                        Chat("gg");
                        SurrenderedFrame = Frame;
                        FileUtil.Register("result " + EnemyRace + " " + Build.Name() + " Defeat");
                    }
                }

                if (Surrendered && Frame - SurrenderedFrame >= 118)
                    GameConnection.RequestLeaveGame().Wait();

                TrySenDay9();

                DrawText("Minerals: " + Minerals());
                DrawText("Gas: " + Gas());
            }
            catch (System.Exception e)
            {
                if (!loggedError)
                {
                    FileUtil.Log("Error occured: " + e.ToString());
                    loggedError = true;
                }
                DebugUtil.WriteLine("Exception in OnFrame: " + e.ToString());
            }

            Frame++;

            stopWatch.Stop();
            totalExecutionTime += stopWatch.ElapsedMilliseconds;
            maxExecutionTime = System.Math.Max(maxExecutionTime, stopWatch.ElapsedMilliseconds);
            
            DrawText("Average ms per frame: " + totalExecutionTime / Frame + " Max ms per frame: " + maxExecutionTime);
            DrawText("Managers time: " + (time2 - time1) + " Build order time: " + (time3 - time2));

            if (DrawRequest != null)
                GameConnection.SendRequest(DrawRequest).Wait();

            return actions;
        }

        public void DrawText(string text)
        {
            DrawScreen(text, 12, 0.05f, 0.1f + 0.02f * TextLine);
            TextLine++;
        }

        public void DrawScreen(string text, uint size, float x, float y)
        {
            if (Debug)
            {
                InitializeDebugCommand();
                DrawRequest.Debug.Debug[0].Draw.Text.Add(new DebugText() { Text = text, Size = size, VirtualPos = new Point() { X = x, Y = y } });
            }
        }

        public void DrawLine(Agent p1, Point p2)
        {
            DrawLine(p1.Unit.Pos, p2);
        }

        public void DrawLine(Agent p1, Point2D p2)
        {
            Point pos3 = new Point { X = p2.X, Y = p2.Y, Z = MapAnalyzer.MapHeight((int)p2.X, (int)p2.Y) };
            DrawLine(p1.Unit.Pos, pos3);
        }

        public void DrawLine(Point p1, Point p2)
        {
            if (Debug)
            {
                InitializeDebugCommand();
                DrawRequest.Debug.Debug[0].Draw.Lines.Add(new DebugLine() { Color = new Color() { R = 255, G = 0, B = 0 }, Line = new Line() { P0 = p1, P1 = p2 } });
            }
        }

        public void DrawSphere(Point2D pos)
        {
            if (Debug)
            {
                Point pos3 = new Point { X = pos.X, Y = pos.Y, Z = MapAnalyzer.MapHeight((int)pos.X, (int)pos.Y) };
                DrawSphere(pos3);
            }
        }

        public void DrawSphere(Point pos)
        {
            if (Debug)
            {
                InitializeDebugCommand();
                DrawRequest.Debug.Debug[0].Draw.Spheres.Add(new DebugSphere() { Color = new Color() { R = 255, G = 0, B = 0 }, R = 2, P = pos });
            }
        }

        public void DrawSphere(Point pos, float radius, Color color)
        {
            if (Debug)
            {
                InitializeDebugCommand();
                DrawRequest.Debug.Debug[0].Draw.Spheres.Add(new DebugSphere() { Color = color, R = radius, P = pos });
            }
        }

        private void InitializeDebugCommand()
        {
            if (DrawRequest == null)
            {
                DrawRequest = new Request();

                DrawRequest.Debug = new RequestDebug();
                DebugCommand debugCommand = new DebugCommand();
                debugCommand.Draw = new DebugDraw();
                DrawRequest.Debug.Debug.Add(debugCommand);
            }
        }

        private void TrySenDay9()
        {
            if (!Monday || Day9Sent)
                return;


            foreach (Unit unit in EnemyBases)
            {
                bool removed = false;
                if (Observation.Observation.RawData.Event != null
                    && Observation.Observation.RawData.Event.DeadUnits != null)
                    foreach (ulong tag in Observation.Observation.RawData.Event.DeadUnits)
                        if (unit.Tag == tag)
                            removed = true;

                if (!removed)
                    continue;

                Chat("Day[9] made me do it!");
                Day9Sent = true;
            }

            EnemyBases = new List<Unit>();
            foreach (Unit unit in Observation.Observation.RawData.Units)
                if (unit.Alliance == Alliance.Enemy && UnitTypes.ResourceCenters.Contains(unit.UnitType))
                    EnemyBases.Add(unit);
        }

        private bool CheckSurrender()
        {
            int buildings = 0;
            int health = 0;
            int shield = 0;
            foreach (Agent agent in UnitManager.Agents.Values)
            {
                if (agent.IsBuilding)
                {
                    buildings++;
                    health = (int)System.Math.Max(health, agent.Unit.Health);
                    shield = (int)System.Math.Max(shield, agent.Unit.Shield);
                }
            }
            if (buildings <= 1
                && shield == 0
                && health <= 150)
                return true;

            int bases = UnitManager.Count(UnitTypes.NEXUS) + UnitManager.Count(UnitTypes.COMMAND_CENTER) + UnitManager.Count(UnitTypes.COMMAND_CENTER_FLYING) + UnitManager.Count(UnitTypes.ORBITAL_COMMAND) + UnitManager.Count(UnitTypes.ORBITAL_COMMAND_FLYING) + UnitManager.Count(UnitTypes.PLANETARY_FORTRESS) + UnitManager.Count(UnitTypes.HATCHERY) + UnitManager.Count(UnitTypes.LAIR) + UnitManager.Count(UnitTypes.HIVE);
            int workers = UnitManager.Count(UnitTypes.SCV) + UnitManager.Count(UnitTypes.PROBE) + UnitManager.Count(UnitTypes.DRONE);
            int minerals = (int)Observation.Observation.PlayerCommon.Minerals - ReservedMinerals;
            if (bases > 0 && workers > 0)
                return false;

            if (workers > 0 && minerals >= 400)
                return false;

            if (bases > 0 && minerals >= 50)
                return false;

            foreach (Agent agent in UnitManager.Agents.Values)
                if (agent.IsCombatUnit)
                    return false;

            return true;
        }

        public void Chat(string message)
        {
            Action action = new Action();
            action.ActionChat = new ActionChat() { Message = message };
            actions.Add(action);
        }

        private void printActions()
        {
            if (Observation.Actions.Count > 0 && Observation.Actions[0].ActionRaw.UnitCommand != null && Observation.Actions[0].ActionRaw.UnitCommand.UnitTags.Count > 0)
            {
                try
                {
                    DebugUtil.WriteLine("New action performed:" + Observation.Actions[0].ActionRaw.UnitCommand);
                    DebugUtil.WriteLine("Ability ID: " + Observation.Actions[0].ActionRaw.UnitCommand.AbilityId);
                    if (Observation.Actions[0].ActionRaw.UnitCommand.TargetWorldSpacePos != null)
                    {
                        Point2D pos = Observation.Actions[0].ActionRaw.UnitCommand.TargetWorldSpacePos;
                        DebugUtil.WriteLine("Position: " + pos);
                    }
                    else if (Observation.Actions[0].ActionRaw.UnitCommand.TargetUnitTag > 0)
                    {
                        DebugUtil.WriteLine("TargetUnit: " + Observation.Actions[0].ActionRaw.UnitCommand.TargetUnitTag);
                        foreach (Unit unit in Observation.Observation.RawData.Units)
                            if (unit.Tag == Observation.Actions[0].ActionRaw.UnitCommand.TargetUnitTag)
                            {
                                DebugUtil.WriteLine("TargetUnitType: " + unit.UnitType);
                                break;
                            }
                    }
                    foreach (Unit unit in Observation.Observation.RawData.Units)
                        if (unit.Tag == Observation.Actions[0].ActionRaw.UnitCommand.UnitTags[0])
                            DebugUtil.WriteLine("By unit of type: " + unit.UnitType);
                    DebugUtil.WriteLine();
                }
                catch (System.Exception) { }
            }
        }

        public void OnStart(ResponseGameInfo gameInfo, ResponseData data, ResponseObservation observation, uint playerId, string opponentID)
        {
            Observation = observation;
            GameInfo = gameInfo;
            PlayerId = playerId;
            Data = data;
            UnitTypes.LoadData(data);

            OpponentID = opponentID;

            MyRace = GameInfo.PlayerInfo[(int)Observation.Observation.PlayerCommon.PlayerId - 1].RaceActual;
            EnemyRace = GameInfo.PlayerInfo[2 - (int)Observation.Observation.PlayerCommon.PlayerId].RaceRequested;
            DebugUtil.WriteLine("MyRace: " + MyRace);

            FileUtil.Log("Game started on map: " + GameInfo.MapName);
            FileUtil.Log("Enemy race: " + EnemyRace);

            MapAnalyzer.Analyze(this);
            TargetManager.OnStart(this);
            BaseManager.OnStart(this);

            Build = DetermineBuild();
            Build.InitializeTasks();
            Build.OnStart(this);

            FileUtil.Register("started " + EnemyRace + " " + Build.Name());


            Managers.Add(UnitManager);
            Managers.Add(EnemyManager);
            Managers.Add(BaseManager);
            Managers.Add(TargetManager);
            Managers.Add(TaskManager);
            Managers.Add(EffectManager);
            Managers.Add(EnemyMineManager);

            if (GameInfo.PlayerInfo[(int)Observation.Observation.PlayerCommon.PlayerId - 1].RaceActual == Race.Protoss)
                Managers.Add(NexusAbilityManager);

            if (GameInfo.PlayerInfo[(int)Observation.Observation.PlayerCommon.PlayerId - 1].RaceActual == Race.Terran)
                Managers.Add(OrbitalAbilityManager);
        }

        private Build DetermineBuild()
        {
            if (FixedBuild != null)
                return FixedBuild;


            string[] lines = FileUtil.ReadResultsFile();
            PreviousEnemyStrategies.Load(lines);
            Dictionary<string, int> defeats = new Dictionary<string, int>();
            Dictionary<string, int> games = new Dictionary<string, int>();
            foreach (string line in lines)
            {
                if (line.StartsWith("result "))
                {
                    string[] words = line.Split(' ');
                    if (words[1] != EnemyRace.ToString())
                        continue;
                    if (words[3] == "Defeat")
                    {
                        if (!defeats.ContainsKey(words[2]))
                            defeats.Add(words[2], 0);
                        defeats[words[2]]++;

                        if (!games.ContainsKey(words[2]))
                            games.Add(words[2], 1);
                        else if (games[words[2]] < defeats[words[2]])
                            games[words[2]] = defeats[words[2]];
                    }
                }
                else if (line.StartsWith("started"))
                {
                    string[] words = line.Split(' ');
                    if (words[1] != EnemyRace.ToString())
                        continue;

                    if (!games.ContainsKey(words[2]))
                        games.Add(words[2], 0);
                    games[words[2]]++;
                }
            }

            List<Build> options;

            if (MyRace == Race.Protoss)
                options = ProtossBuilds();
            else if (MyRace == Race.Zerg)
                options = ZergBuilds();
            else if (MyRace == Race.Terran)
                options = TerranBuilds();
            else
                options = null;

            return BuildSelector.Select(options, defeats, games);
        }

        public List<Build> ZergBuilds()
        {
            List<Build> options = new List<Build>();

            if (EnemyRace == Race.Protoss)
            {
                options.Add(new MassZergling() { AllowHydraTransition = true });
            }
            else if (EnemyRace == Race.Terran)
            {
                options.Add(new MassZergling() { AllowHydraTransition = true });
            }
            else if (EnemyRace == Race.Zerg)
            {
                options.Add(new OneBaseRoach());
                options.Add(new RoachRavager());
            }
            else
            {
                options.Add(new MacroHydra());
            }

            return options;
        }

        public List<Build> ProtossBuilds()
        {
            List<Build> options = new List<Build>();
            if (EnemyRace == Race.Terran)
            {
                options.Add(new PvTDisruptor());
            }
            else if (EnemyRace == Race.Zerg)
            {
                options.Add(new MacroToss());
            }
            else if (EnemyRace == Race.Protoss)
            {
                options.Add(new VoidrayPvP());
            }
            else
            {
                options.Add(new MacroPvP());
            }

            return options;
        }

        public List<Build> TerranBuilds()
        {
            List<Build> options = new List<Build>();

            if (EnemyRace == Race.Terran)
            {
                options.Add(new BunkerRush());
            }
            else if (EnemyRace == Race.Zerg)
            {
                if (!Zergling.Get().DetectedPreviously
                    || Queen.Get().DetectedPreviously 
                    || Mutalisk.Get().DetectedPreviously 
                    || Roach.Get().DetectedPreviously 
                    || Hydralisk.Get().DetectedPreviously)
                    options.Add(new BunkerRush());
                options.Add(new MechTvZ());
            }
            else if (EnemyRace == Race.Protoss)
            {
                options.Add(new BunkerRush());
            }
            else
            {
                options.Add(new TankPushProbots());
            }

            return options;
        }

        private void ProcessTournamentFile()
        {
            string[] tournamentLines = FileUtil.ReadTournamentFile();
            Dictionary<string, int> gamesPlayed = new Dictionary<string, int>();
            Dictionary<string, string> races = new Dictionary<string, string>();

            if (EnemyRace == Race.Terran || tournamentLines.Length >= 2)
                FileUtil.LogTournament("started " + OpponentID + " " + EnemyRace);

            for (int i = tournamentLines.Length - 1; i >= 0; i--)
            {
                string line = tournamentLines[i];
                if (!line.StartsWith("started"))
                    continue;
                string[] words = line.Split(' ');
                if (!races.ContainsKey(words[1]))
                    races.Add(words[1], words[2]);
                if (gamesPlayed.ContainsKey(words[1]))
                    continue;

                int count = 1;
                for (int j = i - 1; j >= 0; j--)
                {
                    string line2 = tournamentLines[i];
                    if (!line2.StartsWith("started"))
                        continue;

                    string[] words2 = line2.Split(' ');
                    if (gamesPlayed.ContainsKey(words[1]))
                        continue;

                    if (words[1] != words2[1])
                        break;
                    count++;
                }
                gamesPlayed.Add(words[1], count);
            }

            bool previousZergOpponent = false;
            foreach (string id in gamesPlayed.Keys)
            {
                if (id == OpponentID)
                    continue;
                if (races.ContainsKey(id) && races[id] != "Zerg")
                    continue;
                if (gamesPlayed[id] >= 3)
                {
                    previousZergOpponent = true;
                    break;
                }
            }

            bool previousTerranOpponent = false;
            foreach (string id in gamesPlayed.Keys)
            {
                if (id == OpponentID)
                    continue;
                if (races.ContainsKey(id) && races[id] != "Terran")
                    continue;
                if (gamesPlayed[id] >= 2)
                {
                    previousTerranOpponent = true;
                    break;
                }
            }

            int tournamentRound = 0;
            if (EnemyRace == Race.Protoss)
                tournamentRound = 3;
            else if (EnemyRace == Race.Zerg)
            {
                if (previousTerranOpponent && previousZergOpponent)
                    tournamentRound = 3;
                else
                    tournamentRound = 2;
            }
            else
            {
                if (previousTerranOpponent && previousZergOpponent)
                    tournamentRound = 3;
                else
                    tournamentRound = 1;
            }

            System.Console.WriteLine("Tournament round: " + tournamentRound);

        }

        public List<Unit> Enemies()
        {
            return EnemyManager.GetEnemies();
        }

        public void OnEnd(ResponseObservation observation, Result result)
        {
            FileUtil.Register("Result: " + result);
            if (Frame > 0)
                FileUtil.Register("Average ms per frame: " + totalExecutionTime / Frame + " Max ms per frame: " + maxExecutionTime);
        }


        public int Minerals()
        {
            return (int)Observation.Observation.PlayerCommon.Minerals - ReservedMinerals;
        }

        public int Gas()
        {
            return (int)Observation.Observation.PlayerCommon.Vespene - ReservedGas;
        }

        private void Time()
        {
            long currentTime = System.DateTime.Now.Ticks;
            if (PrevTime != -1 && Frame <= 224)
                Times.Add(currentTime - PrevTime);
            PrevTime = currentTime;
            if (Frame == 224 + 22)
                foreach (long time in Times)
                    FileUtil.Debug(time + "");
        }
    }
}
