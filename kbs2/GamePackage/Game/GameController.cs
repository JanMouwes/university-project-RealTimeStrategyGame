using System;
using System.Collections.Generic;
using System.Diagnostics;
using kbs2.Desktop.View.Camera;
using kbs2.GamePackage.DayCycle;
using kbs2.GamePackage.EventArgs;
using kbs2.utils;
using kbs2.World;
using kbs2.World.Cell;
using kbs2.World.Chunk;
using kbs2.World.Enums;
using kbs2.World.Structs;
using kbs2.World.TerrainDef;
using kbs2.UserInterface;
using kbs2.View.GUI.ActionBox;
using kbs2.World.World;
using kbs2.WorldEntity.Unit;
using kbs2.WorldEntity.Unit.MVC;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Linq;
using kbs2.Actions.GameActionDefs;
using kbs2.Actions.GameActionGrid;
using kbs2.Actions.GameActions;
using kbs2.Actions.GameActionSelector;
using kbs2.Actions.Interfaces;
using kbs2.Faction.FactionMVC;
using kbs2.UserInterface.GameActionGui;
using kbs2.View.GUI;
using kbs2.WorldEntity.Interfaces;
using kbs2.WorldEntity.Pathfinder;
using kbs2.WorldEntity.Structures;
using kbs2.WorldEntity.Structures.BuildingUnderConstructionMVC;
using kbs2.WorldEntity.WorldEntitySpawner;

namespace kbs2.GamePackage
{
    public delegate void GameSpeedObserver(object sender, GameSpeedEventArgs eventArgs);

    public delegate void GameStateObserver(object sender, EventArgsWithPayload<GameState> eventArgs);

    public delegate void MouseStateObserver(object sender, EventArgsWithPayload<MouseState> e);

    public delegate void OnTickHandler(object sender, OnTickEventArgs eventArgs);

    public delegate void ShaderDelegate();

    public class GameController : Game
    {
        public int Id { get; } = -1;

        public GameModel GameModel { get; set; }
        public GameView GameView { get; set; }
        public EntitySpawner Spawner;

        public IMapAction SelectedMapAction => MapActionSelector.SelectedMapAction;
        public readonly MapActionSelector MapActionSelector;

        public GameTime LastUpdateGameTime { get; private set; }

        public MouseInput MouseInput { get; set; }

        public GameActionGuiController GameActionGui { get; set; }
        public FogController FogController { get; set; }

        //    GameSpeed and its event
        private GameSpeed gameSpeed;

        public GameSpeed GameSpeed
        {
            get => gameSpeed;
            set
            {
                gameSpeed = value;
                GameSpeedChange?.Invoke(this, new GameSpeedEventArgs(gameSpeed)); //Invoke event if has subscribers
            }
        }

        public event GameSpeedObserver GameSpeedChange;

        public readonly TimeController TimeController = new TimeController();

        public Faction_Controller PlayerFaction;

        public event MouseStateObserver MouseStateChange;

        public MouseState PreviousMouseButtonsStatus { get; set; }

        public virtual event OnTickHandler onTick;

        //    GameState and its event
        private GameState gameState;

        public GameState GameState
        {
            get => gameState;
            set
            {
                gameState = value;
                GameStateChange?.Invoke(this, new EventArgsWithPayload<GameState>(gameState)); //Invoke event if has subscribers
            }
        }

        public event GameStateObserver GameStateChange;

        private readonly GraphicsDeviceManager graphicsDeviceManager;

        public CameraController Camera;

        private ShaderDelegate shader;

        #region FPS-debug-info

        private int ThisSecond;
        private int FramesThisSecond;
        private int FramesOutput;

        #endregion

        public GameController(GameSpeed gameSpeed, GameState gameState)
        {
            this.GameSpeed = gameSpeed;
            this.GameState = gameState;

            GameModel = new GameModel();

            GameStateChange += PauseGame;
            GameStateChange += UnPauseGame;

            PlayerFaction = new Faction_Controller("PlayerFaction", this);

            MapActionSelector = new MapActionSelector();

            graphicsDeviceManager = new GraphicsDeviceManager(this);


            shader = RandomPattern2;

            Content.RootDirectory = "Content";
        }

        /// <summary>
        /// Is subscribed to the GameState so this is called every time the GameState is changed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="eventArgs"></param>
        public void PauseGame(object sender, EventArgsWithPayload<GameState> eventArgs)
        {
            if (eventArgs.Value != GameState.Paused) return;
            //throw new NotImplementedException();
            Console.WriteLine("pause");
        }

        /// <summary>
        /// Is subscribed to the gamestate so this is called every time the gamestate is changed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="eventArgs"></param>
        public void UnPauseGame(object sender, EventArgsWithPayload<GameState> eventArgs)
        {
            if (eventArgs.Value != GameState.Running) return;
            //throw new NotImplementedException();
            Console.WriteLine("unpause");
        }

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize()
        {
            // Fill the Dictionairy
            TerrainDef.TerrainDictionary.Add(TerrainType.Grass, "grass");
            TerrainDef.TerrainDictionary.Add(TerrainType.Water, "Water-MiracleSea");
            TerrainDef.TerrainDictionary.Add(TerrainType.Sand, "Sand");
            TerrainDef.TerrainDictionary.Add(TerrainType.Soil, "Soil");
            TerrainDef.TerrainDictionary.Add(TerrainType.Snow, "Snow");
            TerrainDef.TerrainDictionary.Add(TerrainType.Rock, "Rock");
            TerrainDef.TerrainDictionary.Add(TerrainType.Trees, "Tree-2");

            // Generate world
            GameModel.World = WorldFactory.GetNewWorld(FastNoise.NoiseType.SimplexFractal);

            FogController = new FogController(PlayerFaction, GameModel.World);

//            onTick += FogController.Update;

            // Pathfinder 
            GameModel.pathfinder = new Pathfinder(GameModel.World);

            // Spawner
            Spawner = new EntitySpawner(this);

            GameModel.ActionBox = new ActionBoxController(new FloatCoords() {x = 50, y = 50});

            SpriteBatch spriteBatch = new SpriteBatch(GraphicsDevice);

            Camera = new CameraController(GraphicsDevice);

            GameView = new GameView(GameModel, graphicsDeviceManager, spriteBatch, Camera, GraphicsDevice, Content);

            GameActionGui = new GameActionGuiController(this);

            GameModel.MouseInput = new MouseInput(this);

            // Allows the user to resize the window
            base.Window.AllowUserResizing = true;

            // Makes the mouse visible in the window
            base.IsMouseVisible = true;

            shader();

            // Initalize game
            base.Initialize();
        }

        /// <summary>
        /// LoadContent is called once per game and is to load all the content.
        /// </summary>
        protected override void LoadContent()

        {
            //TESTCODE

            onTick += SetBuilding;
            onTick += TimeController.UpdateTime;
            onTick += GameModel.MouseInput.Selection.Update;
            GameModel.MouseInput.Selection.OnSelectionChanged += ChangeSelection;

            //TESTCODE
            DBController.OpenConnection("DefDex.db");
            UnitDef unitdef = DBController.GetUnitDef(1);
            DBController.CloseConnection();

            for (int i = 0; i < 12; i++)
            {
                FloatCoords coords = new FloatCoords() {x = i, y = 5};
                UnitController unit = UnitFactory.CreateNewUnit(unitdef, coords, GameModel.World, PlayerFaction);

                unit.LocationController.LocationModel.UnwalkableTerrain.Add(TerrainType.Water);
                Spawner.SpawnUnit(unit, (Coords) coords);
            }

            //============= More TestCode ===============

            MouseStateChange += GameModel.MouseInput.OnMouseStateChange;
            MouseStateChange += GameModel.ActionBox.OnRightClick;
            //TESTCODE
        }


        /// <summary>
        /// UnloadContent will be called once per game and is the place to unload
        /// game-specific content.
        /// </summary>
        protected override void UnloadContent()
        {
        }

        /// <summary>
        /// SaveToDB is called by the user or when the game is closed to save the game to the database
        /// </summary>
        public void SaveToDB()
        {
            GameState = GameState.Paused;

            //throw new NotImplementedException();
        }

        /// <summary>
        /// Loads chunk at mouse coordinates if not already loaded
        /// </summary>
        private void mouseChunkLoadUpdate(GameTime gameTime)
        {
            MouseState mouseState = Mouse.GetState();

            Coords windowCoords = new Coords
            {
                x = mouseState.X,
                y = mouseState.Y
            };

            FloatCoords cellCoords = (FloatCoords) WorldPositionCalculator.DrawCoordsToCellCoords(
                (Coords) WorldPositionCalculator.TransformWindowCoords(
                    windowCoords,
                    Camera.GetViewMatrix()
                ),
                GameView.TileSize
            );

            Coords chunkCoords = WorldPositionCalculator.ChunkCoordsOfCellCoords(cellCoords);


            if (ChunkExists(chunkCoords)) return;

            GameModel.World.WorldModel.ChunkGrid[chunkCoords] = WorldChunkLoader.ChunkGenerator(chunkCoords);
            shader();
        }

        /// <summary>
        /// 
        /// </summary>
        private bool ChunkExists(Coords chunkCoords) => GameModel.World.WorldModel.ChunkGrid.ContainsKey(chunkCoords) &&
                                                        GameModel.World.WorldModel.ChunkGrid[chunkCoords] != null;

        /// <summary>
        /// This function and its actions need to be refactored
        /// </summary>
        public void AddGui()
        {
            StatusBarView statusBarView = new StatusBarView(GraphicsDevice);
            LeftButtonBar leftButtonBar = new LeftButtonBar(GraphicsDevice);
            RightButtonBar rightButtonBar = new RightButtonBar(GraphicsDevice);
            BottomBarView bottomBarView = new BottomBarView(GraphicsDevice);
            MiniMapBar miniMap = new MiniMapBar(GraphicsDevice);
            GameActionGuiView actionBar = GameActionGui.View;

            GameModel.GuiItemList.Add(statusBarView);
            GameModel.GuiItemList.Add(leftButtonBar);
            GameModel.GuiItemList.Add(rightButtonBar);
            GameModel.GuiItemList.Add(bottomBarView);
            GameModel.GuiItemList.Add(miniMap);
            GameModel.GuiItemList.Add(actionBar);

            GameModel.GuiItemList.AddRange(actionBar.GetContents.Select(item => (IGuiViewImage) item));
        }


        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            // Exit game if escape is pressed
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
            {
                GameState = GameState.Paused;
//                SaveToDB();
//                Exit();
            }

            if (Keyboard.GetState().IsKeyDown(Keys.Z)) GameState = GameState.Running;

            if (gameState == GameState.Paused) return;

            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            LastUpdateGameTime = gameTime;

            // Updates camera according to the pressed buttons
            Camera.MoveCamera();

            // Clears the itemList
            GameModel.ItemList.Clear();
            GameModel.TextList.Clear();
            GameModel.GuiItemList.Clear();
            GameModel.GuiTextList.Clear();

            AddGui();

            // ============== Temp Code ===================================================================


            MouseState temp = Mouse.GetState();
            Coords tempcoords = new Coords {x = temp.X, y = temp.Y};
            Coords coords = WorldPositionCalculator.DrawCoordsToCellCoords((Coords) WorldPositionCalculator.TransformWindowCoords(tempcoords, Camera.GetViewMatrix()), GameView.TileSize);
            if (GameModel.World.GetCellFromCoords(coords) != null)
            {
                TerrainTester terrainTester = new TerrainTester(new FloatCoords() {x = 0, y = 100})
                {
                    Text = $"{coords.x},{coords.y}  {GameModel.World.GetCellFromCoords(coords).worldCellModel.Terrain.ToString()}"
                };
                terrainTester.Colour = GameModel.World.GetCellFromCoords(coords).worldCellModel.BuildingOnTop != null ? Color.Red : Color.Blue;


                GameModel.GuiTextList.Add(terrainTester);

                TerrainTester tester = new TerrainTester(new FloatCoords {x = 0, y = 120})
                {
                    Text = $"Chunk: {WorldPositionCalculator.ChunkCoordsOfCellCoords((FloatCoords) coords).x},{WorldPositionCalculator.ChunkCoordsOfCellCoords((FloatCoords) coords).y} "
                };

                GameModel.GuiTextList.Add(tester);
            }

            // Update Buildings & constructing buildings on screen
            GameModel.ItemList.AddRange(GameModel.World.WorldModel.Structures.Select(building => building.View));
            GameModel.TextList.AddRange(GameModel.World.WorldModel.UnderConstruction.Select(building => building.Counter));

            //    Update Units on screen
            GameModel.ItemList.AddRange(GameModel.World.WorldModel.Units.Select(unit => unit.View));

            if (GameModel.ActionBox.BoxModel.Show)
            {
//                GameModel.GuiItemList.Add(GameModel.ActionBox.BoxView);
                GameModel.GuiTextList.Add(GameModel.ActionBox.BoxModel.Text);
            }

            //    Calculate viewport-bounds
            Coords leftTopViewBound = (Coords) WorldPositionCalculator.WindowCoordsToCellCoords(new Coords
            {
                x = GraphicsDevice.Viewport.X,
                y = GraphicsDevice.Viewport.Y
            }, Camera.GetViewMatrix(), GameView.TileSize);
            Coords rightBottomViewBound = (Coords) WorldPositionCalculator.WindowCoordsToCellCoords(new Coords
            {
                x = GraphicsDevice.Viewport.X + GraphicsDevice.Viewport.Width,
                y = GraphicsDevice.Viewport.Y + GraphicsDevice.Viewport.Height
            }, Camera.GetViewMatrix(), GameView.TileSize);
            Rectangle viewRectangle = new Rectangle(leftTopViewBound.x, leftTopViewBound.y,
                Math.Abs(leftTopViewBound.x - rightBottomViewBound.x),
                Math.Abs(leftTopViewBound.y - rightBottomViewBound.y));

            List<WorldChunkController> chunks = (from chunk in GameModel.World.WorldModel.ChunkGrid
                let rightBottomBound = new Coords
                {
                    x = 20 + WorldChunkModel.ChunkSize,
                    y = 20
                }
                let leftTopBound = new Coords
                {
                    x = (chunk.Key.x * WorldChunkModel.ChunkSize),
                    y = (chunk.Key.y * WorldChunkModel.ChunkSize)
                }
                let chunkRectangle = new Rectangle(leftTopBound.x, leftTopBound.y,
                    Math.Abs(rightBottomBound.x),
                    Math.Abs(rightBottomBound.y)
                )
                where (chunkRectangle.Intersects(viewRectangle))
                select chunk.Value).ToList();

            foreach (WorldChunkController chunk in chunks)
            {
                GameModel.ItemList.AddRange(from WorldCellController cell in chunk.WorldChunkModel.grid where cell.worldCellView.ViewMode != ViewMode.None select cell.worldCellView);
            }

            GameModel.GuiTextList.Add(PlayerFaction.CurrencyController.View);

            // Chunks generate when hovered over
            mouseChunkLoadUpdate(gameTime);

            // fire Ontick event
            Stopwatch tick_stopwatch = new Stopwatch();
            tick_stopwatch.Start();

            OnTickEventArgs args = new OnTickEventArgs(gameTime);
            onTick?.Invoke(this, args);

            tick_stopwatch.Stop();


            //updates the viewmode for everything on screen

            // comment line bellow to turn on fog
            FogController.UpdateEverythingVisible();

            // Calls the game update

            //======= Fire MOUSESTATE ================
            MouseState currentMouseButtonsStatus = Mouse.GetState();
            if (currentMouseButtonsStatus.LeftButton != PreviousMouseButtonsStatus.LeftButton ||
                currentMouseButtonsStatus.RightButton != PreviousMouseButtonsStatus.RightButton)
            {
                MouseStateChange?.Invoke(this, new EventArgsWithPayload<MouseState>(currentMouseButtonsStatus));
                PreviousMouseButtonsStatus = currentMouseButtonsStatus;
            }

            AddShader();

            if (Keyboard.GetState().IsKeyDown(Keys.S)) SaveToDB();

            stopwatch.Stop();

            // Calls the game update
            base.Update(gameTime);

            Console.Clear();
            printStopWatchResults(tick_stopwatch, "OnTick");
            printStopWatchResults(stopwatch, "Update");
            Console.WriteLine($"OnTick's percentage: {(tick_stopwatch.Elapsed.Ticks / (float) stopwatch.Elapsed.Ticks) * 100}%");
            Console.WriteLine("frames: " + FramesOutput);
        }

        public static void printStopWatchResults(Stopwatch toPrint, string description) => Console.WriteLine($"{description} took: {toPrint.Elapsed.Ticks} ticks or {toPrint.Elapsed.Milliseconds} ms");

        private void updateFrames(GameTime gameTime)
        {
            if (ThisSecond < gameTime.TotalGameTime.Seconds)
            {
                ThisSecond = gameTime.TotalGameTime.Seconds;
                FramesOutput = FramesThisSecond;
                FramesThisSecond = 0;
            }

            FramesThisSecond++;
        }

        /// <summary>
        /// This checks if a new shader needs to be applied and applies shader to new chunks
        /// </summary>
        private void AddShader()
        {
            ShaderDelegate tempShader = null;

            if (Keyboard.GetState().IsKeyDown(Keys.R)) tempShader = RandomPattern2;
            if (Keyboard.GetState().IsKeyDown(Keys.C)) tempShader = CellChunkCheckered;
            if (Keyboard.GetState().IsKeyDown(Keys.D)) tempShader = DefaultPattern;

            if (tempShader == null) return;

            shader = tempShader;
            shader();
        }

        /// <summary>
        /// TestCode
        /// </summary>
        public void SetBuilding(object sender, OnTickEventArgs eventArgs)
        {
            void CheckKeysAndPlaceBuilding(bool isKeyPressed, int buildingId, MouseState mouseState, List<TerrainType> legalTerrainTypes)
            {
                if (!isKeyPressed) return;

                DBController.OpenConnection("DefDex.db");
                BuildingDef def = DBController.GetBuildingDef(buildingId);
                DBController.CloseConnection();

                Coords tempCoords = new Coords {x = mouseState.X, y = mouseState.Y};

                Coords coords = (Coords) WorldPositionCalculator.WindowCoordsToCellCoords(tempCoords, Camera.GetViewMatrix(), GameView.TileSize);

                List<Coords> buildingCoords = new List<Coords>();
                foreach (Coords buildingShape in def.BuildingShape) buildingCoords.Add(coords + buildingShape);

                if (!GameModel.World.AreTerrainCellsLegal(buildingCoords, legalTerrainTypes)) return;

                using (ConstructingBuildingFactory constructionFactory = new ConstructingBuildingFactory(PlayerFaction))
                {
                    ConstructingBuildingController building = constructionFactory.CreateConstructingBuildingControllerOf(def);
                    Spawner.SpawnStructure(coords, building);
                    building.ConstructionComplete += (o, args) => Spawner.ReplaceBuilding(o, args);
                }
            }

            KeyboardState keyboardState = Keyboard.GetState();

            if (keyboardState.IsKeyDown(Keys.D8))
            {
                SpawnActionDef def = SpawnActionDef.Pikachu;
                MapActionSelector.Select(new SpawnAction(def, this, PlayerFaction));
                return;
            }

            if (keyboardState.IsKeyDown(Keys.D7))
            {
                SpawnActionDef def = SpawnActionDef.Raichu;
                MapActionSelector.Select(new SpawnAction(def, this, PlayerFaction));
                return;
            }

            if (keyboardState.IsKeyDown(Keys.D1))
            {
                CheckKeysAndPlaceBuilding(keyboardState.IsKeyDown(Keys.D1), 1, Mouse.GetState(),
                    new List<TerrainType>()
                    {
                        TerrainType.Grass,
                        TerrainType.Rock,
                        TerrainType.Soil,
                        TerrainType.Default
                    }
                );
                return;
            }

            if (keyboardState.IsKeyDown(Keys.D2))
            {
                CheckKeysAndPlaceBuilding(keyboardState.IsKeyDown(Keys.D2), 2, Mouse.GetState(),
                    new List<TerrainType>()
                    {
                        TerrainType.Grass,
                        TerrainType.Rock,
                        TerrainType.Soil,
                        TerrainType.Default
                    }
                );
                return;
            }

            if (keyboardState.IsKeyDown(Keys.D3))
            {
                CheckKeysAndPlaceBuilding(keyboardState.IsKeyDown(Keys.D3), 3, Mouse.GetState(),
                    new List<TerrainType>()
                    {
                        TerrainType.Rock
                    }
                );
                return;
            }

            if (keyboardState.IsKeyDown(Keys.D4))
            {
                CheckKeysAndPlaceBuilding(keyboardState.IsKeyDown(Keys.D4), 4, Mouse.GetState(),
                    new List<TerrainType>()
                    {
                        TerrainType.Grass,
                        TerrainType.Rock,
                        TerrainType.Default
                    }
                );
                return;
            }

            if (keyboardState.IsKeyDown(Keys.D5))
            {
                CheckKeysAndPlaceBuilding(keyboardState.IsKeyDown(Keys.D5), 5, Mouse.GetState(),
                    new List<TerrainType>()
                    {
                        TerrainType.Trees
                    }
                );
                return;
            }

            if (keyboardState.IsKeyDown(Keys.D6))
            {
                CheckKeysAndPlaceBuilding(keyboardState.IsKeyDown(Keys.D6), 6, Mouse.GetState(),
                    new List<TerrainType>()
                    {
                        TerrainType.Grass,
                        TerrainType.Soil,
                        TerrainType.Sand,
                        TerrainType.Default
                    }
                );
                return;
            }
        }

        public void ChangeSelection(object sender, EventArgsWithPayload<List<IGameActionHolder>> eventArgs)
        {
            List<IGameActionHolder> gameActionHolders = eventArgs.Value;
            List<GameActionTabModel> gameActionTabModels = new List<GameActionTabModel>();
            foreach (IGameActionHolder gameActionHolder in gameActionHolders)
            {
                IGameAction[] mapActions = gameActionHolder.GameActions.ToArray();
                GameActionTabModel gameActionTabModel = new GameActionTabModel(mapActions, GameActionGui);
                gameActionTabModels.Add(gameActionTabModel);
            }

            GameActionGui.SetActions(gameActionTabModels);
        }


        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            GameView.Draw();

            // Calls the game's draw function
            base.Draw(gameTime);

            stopwatch.Stop();
            updateFrames(gameTime);
            printStopWatchResults(stopwatch, "Drawing");


//            Uncomment the line below to show all view-items in the console
//            Console.Clear();
//            Console.WriteLine(this.GameModel.AllDrawItems.GroupBy(item => item.GetType(), (typeKey, typeSource) => new KeyValuePair<string, int>(typeKey.Name, typeSource.Count())).Select((pair => $"{pair.Key}: {pair.Value}")).Aggregate((s, s1) => $"{s}\n{s1}"));
        }


        // ===========================================================================================================================
        /// <summary>
        /// Draws the chunks and cells in a Checkered pattern for easy debugging
        /// </summary>
        public void CellChunkCheckered()
        {
            foreach (var Chunk in GameModel.World.WorldModel.ChunkGrid)
            {
                foreach (var item2 in Chunk.Value.WorldChunkModel.grid)
                {
                    item2.worldCellView.Colour = Math.Abs(item2.worldCellModel.ParentChunk.ChunkCoords.x) % 2 ==
                                                 (Math.Abs(item2.worldCellModel.ParentChunk.ChunkCoords.y) % 2 == 1
                                                     ? 1
                                                     : 0)
                        ? Math.Abs(item2.worldCellModel.RealCoords.x) % 2 ==
                          (Math.Abs(item2.worldCellModel.RealCoords.y) % 2 == 1 ? 1 : 0)
                            ? Color.Gray
                            : Color.Yellow
                        : Math.Abs(item2.worldCellModel.RealCoords.x) % 2 ==
                          (Math.Abs(item2.worldCellModel.RealCoords.y) % 2 == 1 ? 1 : 0)
                            ? Color.Green
                            : Color.Sienna;
                }
            }
        }

        // Draws a random pattern on the cells
        public void DefaultPattern()
        {
            foreach (var Chunk in GameModel.World.WorldModel.ChunkGrid)
            {
                foreach (var item2 in Chunk.Value.WorldChunkModel.grid)
                {
                    item2.worldCellView.Colour = Color.White;
                }
            }
        }

        public void RandomPattern2()
        {
            Random random = new Random(WorldModel.Seed);

            foreach (var Chunk in GameModel.World.WorldModel.ChunkGrid)
            {
                foreach (var item2 in Chunk.Value.WorldChunkModel.grid)
                {
                    switch (item2.worldCellModel.Terrain)
                    {
                        case TerrainType.Grass:
                            switch (random.Next(0, 5))
                            {
                                case 0:
                                    item2.worldCellView.Colour = Color.Gray;
                                    break;
                                case 1:
                                    item2.worldCellView.Colour = Color.DarkGray;
                                    break;
                                case 2:
                                    item2.worldCellView.Colour = Color.LightGreen;
                                    break;
                                default:
                                    item2.worldCellView.Colour = Color.White;
                                    break;
                            }

                            break;
                        case TerrainType.Sand:
                            switch (random.Next(0, 2))
                            {
                                case 0:
                                    item2.worldCellView.Colour = Color.WhiteSmoke;
                                    break;
                                default:
                                    item2.worldCellView.Colour = Color.White;
                                    break;
                            }

                            break;
                        case TerrainType.Water:
                            switch (random.Next(0, 5))
                            {
                                case 0:
                                    item2.worldCellView.Colour = Color.LightBlue;
                                    break;
                                case 1:
                                    item2.worldCellView.Colour = Color.LightGray;
                                    break;
                                case 2:
                                    item2.worldCellView.Colour = Color.LightCyan;
                                    break;
                                default:
                                    item2.worldCellView.Colour = Color.White;
                                    break;
                            }

                            break;
                        case TerrainType.Rock:
                            switch (random.Next(0, 4))
                            {
                                case 0:
                                    item2.worldCellView.Colour = Color.LightGray;
                                    break;
                                case 1:
                                    item2.worldCellView.Colour = Color.DarkGray;
                                    break;
                                default:
                                    item2.worldCellView.Colour = Color.White;
                                    break;
                            }

                            break;
                        case TerrainType.Soil:
                            switch (random.Next(0, 7))
                            {
                                case 0:
                                    item2.worldCellView.Colour = Color.SaddleBrown;
                                    break;
                                case 1:
                                    item2.worldCellView.Colour = Color.RosyBrown;
                                    break;
                                default:
                                    item2.worldCellView.Colour = Color.White;
                                    break;
                            }

                            break;
                        case TerrainType.Trees:
                            switch (random.Next(0, 3))
                            {
                                case 0:
                                    item2.worldCellView.Colour = Color.DarkGreen;
                                    break;
                                case 1:
                                    item2.worldCellView.Colour = Color.Green;
                                    break;
                                default:
                                    item2.worldCellView.Colour = Color.ForestGreen;
                                    break;
                            }

                            break;
                        case TerrainType.Snow:
                            switch (random.Next(0, 3))
                            {
                                case 0:
                                    item2.worldCellView.Colour = Color.White;
                                    break;
                                default:
                                    item2.worldCellView.Colour = Color.WhiteSmoke;
                                    break;
                            }

                            break;
                        default:
                            break;
                    }
                }
            }
        }
    }
}