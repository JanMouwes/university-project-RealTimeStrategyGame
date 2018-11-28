using System;
using System.Collections.Generic;
using kbs2.Desktop.View.Camera;
using kbs2.Desktop.View.EventArgs;
using kbs2.Desktop.World.World;
using kbs2.GamePackage;
using kbs2.World;
using kbs2.World.Cell;
using kbs2.World.Chunk;
using kbs2.World.Structs;
using kbs2.World.TerrainDef;
using kbs2.World.World;
using kbs2.WorldEntity.Unit.MVC;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGame.Extended;
using MonoGame.Extended.Sprites;
using MonoGame.Extended.TextureAtlases;

namespace kbs2.Desktop.View.MapView
{
    public delegate void ZoomObserver(object sender, ZoomEventArgs eventArgs);

    public class MapView : Game
    {
        private GraphicsDeviceManager graphics;
        private SpriteBatch spriteBatch;
        private CameraController Camera;
        private WorldController World;
        private Selection_Controller Selection;
        

        // Calculate the size (Width) of a tile
        public int TileSize => (int) (GraphicsDevice.Viewport.Width / Camera.CameraModel.TileCount);

        // Calculates the height of a cell
        public int CellHeight => (int) (Camera.CameraModel.TileCount / GraphicsDevice.Viewport.AspectRatio);

        // Constructor
        public MapView()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
        }

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize()
        {
            // Add initialization logic here

            // initialize world
            World = WorldFactory.GetNewWorld();

            // initialize camera
            Camera = new CameraController(GraphicsDevice);

            // initialize selection
            Selection = new Selection_Controller("PurpleLine", Mouse.GetState());

            // Sets the defenition of terraintextures
            TerrainDef.TerrainDictionairy.Add(TerrainType.Sand, "grass");

            // Allows the user to resize the window
            base.Window.AllowUserResizing = true;

            // Makes the mouse visible in the window
            base.IsMouseVisible = true;

            // Initalize game
            base.Initialize();
        }

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            // Create a new SpriteBatch, which can be used to draw textures.
            spriteBatch = new SpriteBatch(GraphicsDevice);

            // TODO: use this.Content to load your game content here
        }

        /// <summary>
        /// UnloadContent will be called once per game and is the place to unload
        /// game-specific content.
        /// </summary>
        protected override void UnloadContent()
        {
            // TODO: Unload any non ContentManager content here
        }

        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            // Exit game if escape is pressed
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed ||
                Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            // Updates camera according to the pressed buttons
            Camera.MoveCamera();

            // Draws a selection box according to the selected area
            Selection.DrawSelectionBox(Mouse.GetState());

            // Calls the game update
            base.Update(gameTime);
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            // Clears the GraphicsDevice to make room for the new draw items
            GraphicsDevice.Clear(Color.Black);

            // Draws cells in that in the chunks that are in the camera's view
            DrawCells();

            // Draws the units on screen
            DrawUnits();

            // Draws the selection box when you select and drag
            DrawSelection();

            // Calls the game's draw function
            base.Draw(gameTime);
        }

        // Delegate that decides what tilecolor function should be called
        public delegate Color TileColourDelegate(WorldCellModel cell);

        // Holds the function that is called when the delegate is called
        public TileColourDelegate TileColour;

        private Color NotCheckered => Color.White;
        
        // Draws the chunks in a Checkered pattern for easy debugging
        private Color ChunkCheckered(WorldCellModel cell) =>
            Math.Abs(cell.ParentChunk.ChunkCoords.x) % 2 ==
            (Math.Abs(cell.ParentChunk.ChunkCoords.y) % 2 == 1 ? 1 : 0)
                ? Color.Gray
                : Color.Yellow;

        // Draws the cells in a Checkered pattern for easy debugging
        private Color CellCheckered(WorldCellModel cell) =>
            Math.Abs(cell.RealCoords.x) % 2 == ((Math.Abs(cell.RealCoords.y) % 2 == 1) ? 1 : 0)
                ? Color.Gray
                : Color.Yellow;

        // Draws the chunks and cells in a Checkered pattern for easy debugging
        private Color CellChunkCheckered(WorldCellModel cell) =>
            Math.Abs(cell.ParentChunk.ChunkCoords.x) % 2 ==
            (Math.Abs(cell.ParentChunk.ChunkCoords.y) % 2 == 1 ? 1 : 0)
                ? Math.Abs(cell.RealCoords.x) % 2 == ((Math.Abs(cell.RealCoords.y) % 2 == 1) ? 1 : 0)
                    ? Color.Gray
                    : Color.Yellow
                : Math.Abs(cell.RealCoords.x) % 2 == ((Math.Abs(cell.RealCoords.y) % 2 == 1) ? 1 : 0)
                    ? Color.DarkCyan
                    : Color.LightSeaGreen;

        // Draws a random pattern on the cells
        private Color RandomColour(WorldCellModel cell) =>
            new Random(cell.RealCoords.y * cell.RealCoords.x).Next(0, 2) == 1 ? Color.Gray : Color.Pink;

        //    Draw-position relative to x or y coordinate provided
        private int CellDrawInt(double realCoord) => (int) Math.Round(realCoord * TileSize);

        //    Draw-position relative to x or y coordinate provided
        private int CellDrawPosition(double realCoord) => (int) Math.Round(realCoord * TileSize);

        //    Draw-position relative to x or y coordinate provided
        private Coords CellDrawCoords(FloatCoords floatCoords) => CellDrawCoords(floatCoords.x, floatCoords.y);

        //    Draw-position relative to x or y coordinate provided
        private Coords CellDrawCoords(float x, float y) => new Coords
            {x = (int) Math.Round(x * TileSize), y = (int) Math.Round(y * TileSize)};

        // Draws the cells on the screen according to the given chunks in the camera view
        private void DrawCells()
        {
            // Start spritebatch for drawing
            spriteBatch.Begin(transformMatrix: Camera.GetViewMatrix());

            // draw each tile in the chunks in the chunkGrid
            foreach (KeyValuePair<Coords, WorldChunkController> chunkGrid in GetChunksOnScreen())
            {
                foreach (WorldCellModel cell in chunkGrid.Value.WorldChunkModel.grid)
                {
                    // Sets the x and y for the current tile
                    int y = CellDrawPosition(cell.RealCoords.y);
                    int x = CellDrawPosition(cell.RealCoords.x);

                    // Gets the texture according to the terrain type of the cell
                    Texture2D texture = this.Content.Load<Texture2D>(TerrainDef.TerrainDictionairy[cell.Terrain]);

                    // Defines the Color of the cell (for debugging)
                    Color colour = TileColour != null ? TileColour(cell) : CellChunkCheckered(cell);

                    // Draws the texture of the cell on the location coords with the size of the tile and the color
                    spriteBatch.Draw(texture, new Rectangle(x, y, TileSize, TileSize), colour);
                }
            }

            // Close cells draw batch
            spriteBatch.End();
        }

        // Calculates wich chunks are in the camera's view and returns them in a list
        public Dictionary<Coords, WorldChunkController> GetChunksOnScreen()
        {
            // This function is for testing and is still in progress
            Dictionary<Coords, WorldChunkController> chunksOnScreen = new Dictionary<Coords, WorldChunkController>();
            chunksOnScreen = World.WorldModel.ChunkGrid;
            float x = Camera.GetViewMatrix().M41;
            float y = Camera.GetViewMatrix().M42;
            Console.WriteLine($"X: {x}");
            Console.WriteLine($"Y: {y}");
            return chunksOnScreen;
        }

        // Draws the selection box arround the selected area
        public void DrawSelection()
        {
            // Begin drawing without an offset
            spriteBatch.Begin();

            DrawHorizontalLine(Selection.View.Selection.Y);
            DrawHorizontalLine(Selection.View.Selection.Y + Selection.View.Selection.Height);
            DrawVerticalLine(Selection.View.Selection.X);
            DrawVerticalLine(Selection.View.Selection.X + Selection.View.Selection.Width);

            // End drawing of the selection box
            spriteBatch.End();
        }

        // Todo: Add comments from here on down
        public void DrawHorizontalLine(int PositionY)
        {
            Texture2D texture = Content.Load<Texture2D>(Selection.View.LineTexture);
            if (Selection.View.Selection.Width > 0)
            {
                for (int i = 0; i <= Selection.View.Selection.Width - 10; i += 10)
                {
                    if (Selection.View.Selection.Width - i >= 0)
                    {
                        spriteBatch.Draw(texture, new Rectangle(Selection.View.Selection.X + i, PositionY, 10, 5),
                            Color.White);
                    }
                }
            }
            else if (Selection.View.Selection.Width < 0)
            {
                for (int i = -10; i >= Selection.View.Selection.Width; i -= 10)
                {
                    if (Selection.View.Selection.Width - i <= 0)
                    {
                        spriteBatch.Draw(texture, new Rectangle(Selection.View.Selection.X + i, PositionY, 10, 5),
                            Color.White);
                    }
                }
            }
        }

        public void DrawVerticalLine(int PositionX)
        {
            Texture2D texture = Content.Load<Texture2D>(Selection.View.LineTexture);
            if (Selection.View.Selection.Height > 0)
            {
                for (int i = -2; i <= Selection.View.Selection.Height; i += 10)
                {
                    if (Selection.View.Selection.Height - i >= 0)
                    {
                        spriteBatch.Draw(texture, new Rectangle(PositionX, Selection.View.Selection.Y + i, 10, 5),
                            new Rectangle(0, 0, texture.Width, texture.Height), Color.White, MathHelper.ToRadians(90),
                            new Vector2(0, 0), SpriteEffects.None, 0);
                    }
                }
            }

            else if (Selection.View.Selection.Height < 0)
            {
                for (int i = 0; i >= Selection.View.Selection.Height; i -= 10)
                {
                    if (Selection.View.Selection.Height - i <= 0)
                    {
                        spriteBatch.Draw(texture, new Rectangle(PositionX - 10, Selection.View.Selection.Y + i, 10, 5),
                            Color.White);
                    }
                }
            }
        }

        public void DrawUnits()
        {
            Unit_View Pichu = new Unit_View("pichu_idle", 0.3f, 0.3f);
            Unit_View Pikachu = new Unit_View("pikachu_idle", 0.4f, 0.4f);
            Unit_View Raichu = new Unit_View("raichu_idle", 0.8f, 0.8f);
            Unit_View Rayquaza = new Unit_View("rayquaza_idle", 3.5f, 3.5f);

            spriteBatch.Begin(transformMatrix: Camera.GetViewMatrix());

            Coords drawPos = CellDrawCoords(0.05f, 0.5f);
            spriteBatch.Draw(Content.Load<Texture2D>(Pichu.Draw()),
                new Rectangle(
                    (int)(drawPos.x - TileSize * Pichu.Width * .5), 
                    (int)(drawPos.y - TileSize * Pichu.Height * .5), 
                    (int) (TileSize * Pichu.Height), (int) (TileSize * Pichu.Width)), Color.White);
            spriteBatch.Draw(Content.Load<Texture2D>(Pikachu.Draw()),
                new Rectangle(
                    CellDrawPosition(2),
                    CellDrawPosition(1),
                    (int) (TileSize * Pikachu.Height), (int) (TileSize * Pikachu.Width)),
                Color.White);
            spriteBatch.Draw(Content.Load<Texture2D>(Raichu.Draw()),
                new Rectangle(
                    CellDrawPosition(3),
                    CellDrawPosition(1),
                    (int) (TileSize * Raichu.Height), (int) (TileSize * Raichu.Width)), Color.White);
            spriteBatch.Draw(Content.Load<Texture2D>(Rayquaza.Draw()),
                new Rectangle(
                    CellDrawPosition(6),
                    CellDrawPosition(1),
                    (int) (TileSize * Rayquaza.Height), (int) (TileSize * Rayquaza.Width)),
                Color.White);

            spriteBatch.End();
        }
    }
}