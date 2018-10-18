using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using SC2API_CSharp;
using SC2APIProtocol;
using Tyr.Agents;
using Tyr.BuildingPlacement;
using Tyr.Builds;
using Tyr.Builds.Protoss;
using Tyr.Builds.Zerg;
using Tyr.Managers;
using Tyr.MapAnalysis;
using Tyr.Micro;

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

        List<Action> actions = new List<Action>();

        public BuildingPlacer buildingPlacer;
        public int Frame { get; internal set; }
        public MapAnalyzer MapAnalyzer { get; internal set; } = new MapAnalyzer();
        public EnemyStrategyAnalyzer EnemyStrategyAnalyzer = new EnemyStrategyAnalyzer();
        public PreviousEnemyStrategies PreviousEnemyStrategies = new PreviousEnemyStrategies();

        public int ReservedMinerals;
        public int ReservedGas;

        public MicroController MicroController = new MicroController();

        private bool Surrendered = false;
        private int SurrenderedFrame = 0;

        // Managers
        public List<Manager> Managers = new List<Manager>();
        public UnitManager UnitManager = new UnitManager();
        public EnemyManager EnemyManager = new EnemyManager();
        public TargetManager TargetManager = new TargetManager();
        public TaskManager TaskManager = new TaskManager();
        public EffectManager EffectManager = new EffectManager();

        public BaseManager BaseManager = new BaseManager();
        public NexusAbilityManager NexusAbilityManager = new NexusAbilityManager();


        public static Tyr Bot { get; internal set; }

        public Build Build { get; internal set; }

        public bool Monday { get; set; }
        private bool Day9Sent = false;
        private List<Unit> EnemyBases = new List<Unit>();

        private bool loggedError = false;

        private long totalExecutionTime;
        private long maxExecutionTime;

        private string ResultsFile;
        public Build FixedBuild;
        public static bool AllowWritingFiles = true;

        private Request DrawRequest;

        public static bool Debug = false;

        private int TextLine = 0;

        public Tyr()
        {
            buildingPlacer = new BuildingPlacer(this);
            Bot = this;
        }

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
                        Register("result " + EnemyRace + " " + Build.Name() + " Defeat");
                    }
                }

                if (Surrendered && Frame - SurrenderedFrame >= 118)
                    GameConnection.RequestLeaveGame().Wait();

                TrySenDay9();
            }
            catch (System.Exception e)
            {
                if (!loggedError && AllowWritingFiles)
                {
                    File.AppendAllLines(Directory.GetCurrentDirectory() + "/Data/Tyr/Tyr.log", new string[] { "Error occured: " + e.ToString() });
                    loggedError = true;
                }
                System.Console.WriteLine("Exception in OnFrame: " + e.ToString());
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
        public void DrawLine(Point p1, Point p2)
        {
            if (Debug)
            {
                InitializeDebugCommand();
                DrawRequest.Debug.Debug[0].Draw.Lines.Add(new DebugLine() { Color = new Color() { R = 255, G = 0, B = 0 }, Line = new Line() { P0 = p1, P1 = p2} });
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
                    System.Console.WriteLine("New action performed:" + Observation.Actions[0].ActionRaw.UnitCommand);
                    System.Console.WriteLine("Ability ID: " + Observation.Actions[0].ActionRaw.UnitCommand.AbilityId);
                    if (Observation.Actions[0].ActionRaw.UnitCommand.TargetWorldSpacePos != null)
                    {
                        Point2D pos = Observation.Actions[0].ActionRaw.UnitCommand.TargetWorldSpacePos;
                        System.Console.WriteLine("Position: " + pos);
                    }
                    else if (Observation.Actions[0].ActionRaw.UnitCommand.TargetUnitTag > 0)
                    {
                        System.Console.WriteLine("TargetUnit: " + Observation.Actions[0].ActionRaw.UnitCommand.TargetUnitTag);
                        foreach (Unit unit in Observation.Observation.RawData.Units)
                            if (unit.Tag == Observation.Actions[0].ActionRaw.UnitCommand.TargetUnitTag)
                            {
                                System.Console.WriteLine("TargetUnitType: " + unit.UnitType);
                                break;
                            }
                    }
                    foreach (Unit unit in Observation.Observation.RawData.Units)
                        if (unit.Tag == Observation.Actions[0].ActionRaw.UnitCommand.UnitTags[0])
                            System.Console.WriteLine("By unit of type: " + unit.UnitType);
                    System.Console.WriteLine();
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
            
            MyRace = GameInfo.PlayerInfo[(int)Observation.Observation.PlayerCommon.PlayerId - 1].RaceActual;
            EnemyRace = GameInfo.PlayerInfo[2 - (int)Observation.Observation.PlayerCommon.PlayerId].RaceRequested;
            System.Console.WriteLine("MyRace: " + MyRace);

            if (AllowWritingFiles)
            {
                File.AppendAllLines(Directory.GetCurrentDirectory() + "/Data/Tyr/Tyr.log", new string[] { "Game started on map: " + GameInfo.MapName });
                File.AppendAllLines(Directory.GetCurrentDirectory() + "/Data/Tyr/Tyr.log", new string[] { "Enemy race: " + EnemyRace });
            }

            if (opponentID == null)
                ResultsFile = Directory.GetCurrentDirectory() + "/Data/Tyr/" + EnemyRace + ".txt";
            else
                ResultsFile = Directory.GetCurrentDirectory() + "/Data/Tyr/" + opponentID + ".txt";

            if (AllowWritingFiles && !File.Exists(ResultsFile))
                File.Create(ResultsFile).Close();

            MapAnalyzer.Analyze(this);
            TargetManager.OnStart(this);
            BaseManager.OnStart(this);

            Build = DetermineBuild();
            Build.InitializeTasks();
            Build.OnStart(this);

            Register("started " + EnemyRace + " " + Build.Name());


            Managers.Add(UnitManager);
            Managers.Add(EnemyManager);
            Managers.Add(BaseManager);
            Managers.Add(TargetManager);
            Managers.Add(TaskManager);
            Managers.Add(EffectManager);

            if (GameInfo.PlayerInfo[(int)Observation.Observation.PlayerCommon.PlayerId - 1].RaceActual == Race.Protoss)
                Managers.Add(NexusAbilityManager);
        }

        private Build DetermineBuild()
        {
            if (FixedBuild != null)
                return FixedBuild;

            string[] lines = File.ReadAllLines(ResultsFile);
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
                } else if (line.StartsWith("started"))
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
            else
                options = null;


            Build preffered = null;
            int losses = int.MaxValue;
            foreach (Build option in options)
            {
                if (!defeats.ContainsKey(option.Name()))
                    defeats.Add(option.Name(), 0);
                if (!games.ContainsKey(option.Name()))
                    games.Add(option.Name(), 0);

                System.Console.WriteLine(option.Name() + " wins: " + (games[option.Name()] - defeats[option.Name()]));
                System.Console.WriteLine(option.Name() + " defeats: " + defeats[option.Name()]);

                int newLosses = defeats[option.Name()] - (games[option.Name()] - defeats[option.Name()]) / 4;

                if (newLosses < losses)
                {
                    losses = newLosses;
                    preffered = option;
                }
            }
            return preffered;
        }

        public List<Build> ZergBuilds()
        {
            List<Build> options = new List<Build>();

            if (EnemyRace == Race.Protoss)
            {
                options.Add(new RushDefense());
                options.Add(new TurtleLords());
            }
            else if (EnemyRace == Race.Terran)
            {
                options.Add(new RushDefense());
                options.Add(new HydraLurker());
            }
            else if (EnemyRace == Race.Zerg)
            {
                options.Add(new MassZergling());
                options.Add(new RoachRavager());
            }
            else
            {
                options.Add(new RushDefense());
                options.Add(new MassZergling());
                options.Add(new HydraLurker());
                options.Add(new RoachRavager());
                options.Add(new TurtleLords());
            }

            return options;
        }

        public List<Build> ProtossBuilds()
        {
            List<Build> options = new List<Build>();
            if (EnemyRace == Race.Terran)
                options.Add(new NinjaTurtleCarrier() { BuildCarriers = true, RequiredSize = 12 });
            else if (EnemyRace == Race.Zerg)
                options.Add(new MassVoidray() { BuildCarriers = true, RequiredSize = 10 });
            else
            {
                options.Add(new MassVoidray() { BuildCarriers = true, RequiredSize = 8 });
                if (PreviousEnemyStrategies.SkyToss)
                    options.Add(new OneBaseStalker() { RequiredSize = 10 });
            }
            /*
            if (EnemyRace == Race.Protoss)
            {
                options.Add(new TwoBaseRobo());
                if (TargetManager.PotentialEnemyStartLocations.Count == 1)
                    options.Add(new WorkerRush());
                options.Add(new OneBaseStalker());
            }
            else if (EnemyRace == Race.Terran)
            {
                options.Add(new TwoBaseRoboPvT());
                if (TargetManager.PotentialEnemyStartLocations.Count == 1)
                    options.Add(new WorkerRush());
                if (Bot.PreviousEnemyStrategies.FourRax && !PreviousEnemyStrategies.ReaperRush)
                    options.Add(new NinjaTurtles());
                if (!Bot.PreviousEnemyStrategies.FourRax && !PreviousEnemyStrategies.ReaperRush)
                    options.Add(new ZealotRush() { RequiredSize = 20 });
                if (!PreviousEnemyStrategies.ReaperRush)
                    options.Add(new MassVoidray());
            }
            else if (EnemyRace == Race.Zerg)
            {
                options.Add(new OneBaseStalker() { ProxyPylon = true });
                options.Add(new TwoBaseRobo());
                if (TargetManager.PotentialEnemyStartLocations.Count == 1)
                    options.Add(new WorkerRush());
                if (!PreviousEnemyStrategies.MassRoach || PreviousEnemyStrategies.MassHydra)
                    options.Add(new TwoBaseAdept());
            }
            else
            {
                options.Add(new TwoBaseRobo());
                if (Bot.TargetManager.PotentialEnemyStartLocations.Count == 1)
                    options.Add(new WorkerRush());
                options.Add(new NinjaTurtles());
                options.Add(new OneBaseStalker());
            }
            */

            return options;
        }

        public List<Unit> Enemies()
        {
            return EnemyManager.GetEnemies();
        }

        public void OnEnd(ResponseObservation observation, Result result)
        {
            Register("Result: " + result);
            Register("Average ms per frame: " + totalExecutionTime / Frame + " Max ms per frame: " + maxExecutionTime);
        }


        public int Minerals()
        {
            return (int)Observation.Observation.PlayerCommon.Minerals - ReservedMinerals;
        }

        public int Gas()
        {
            return (int)Observation.Observation.PlayerCommon.Vespene - ReservedGas;
        }

        public void Register(string line)
        {
            if (AllowWritingFiles)
                File.AppendAllLines(ResultsFile, new string[] { line });
        }
    }
}
