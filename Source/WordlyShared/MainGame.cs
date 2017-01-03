using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGame.Ruge.ViewportAdapters;
using Microsoft.Xna.Framework.Input.Touch;
using MonoGame.Ruge.DragonDrop;

namespace WordGame
{
    /// <summary>
    /// This is the main type for your game.
    /// </summary>
    public class MainGame : Game
    {
        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;
        public enum AppState { SplashScreen, MainScreen, SolitaireGame, RatedSolitaireGame, MultiPlayerGame }

        AppState _currentAppState = AppState.SplashScreen;
        public AppState currentAppState
        {
            get
            {
                return _currentAppState;
            }
            set
            {
                _currentAppState = value;
                switch (_currentAppState)
                {
                    case AppState.SolitaireGame:
                        solitaireGame.SetTable();
                        break;
                    case AppState.MultiPlayerGame:
                        multiplayerGame.NewGame();
                        break;
                }
            }
        }
        
        public const int WINDOW_WIDTH = 1980;
        public const int WINDOW_HEIGHT = 1020;

        public int MID_WIDTH = WINDOW_WIDTH / 2;
        public int MID_HEIGHT = WINDOW_HEIGHT / 2;

        public BoxingViewportAdapter viewPort { get; set; }

        SplashScreen splashScreen;
        public MainScreen mainScreen;
        public SolitaireGame solitaireGame;
        public MultiPlayerGame multiplayerGame;

        public OnlineConnectivity online;

        public CheckValidWords validWords = new CheckValidWords();
        DragonDrop<IDragonDropItem> dragonDrop;
        
        Texture2D background;
        public Texture2D dimScreen;

        public MainGame()
        {
            graphics = new GraphicsDeviceManager(this);
            Window.AllowUserResizing = true;
            online = new OnlineConnectivity();

            graphics.PreferredBackBufferWidth = WINDOW_WIDTH;
            graphics.PreferredBackBufferHeight = WINDOW_HEIGHT;

            TouchPanel.EnableMouseTouchPoint = true;
            TouchPanel.EnableMouseGestures = true;
            TouchPanel.EnabledGestures = GestureType.DoubleTap | GestureType.FreeDrag | GestureType.Tap | GestureType.DragComplete;

            currentAppState = AppState.MainScreen;
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
            
            
        }

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize()
        {
            // TODO: Add your initialization logic here
            viewPort = new BoxingViewportAdapter(Window, GraphicsDevice, WINDOW_WIDTH, WINDOW_HEIGHT);
            dragonDrop = new DragonDrop<IDragonDropItem>(this, viewPort);
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
            splashScreen = new SplashScreen(Content, spriteBatch);
            splashScreen.SplashScreenEnded += SplashScreenEnded;

            mainScreen = new MainScreen(Content, spriteBatch, this);
            solitaireGame = new SolitaireGame(spriteBatch, dragonDrop, SolitaireGame.stackOffsetHorizontal, SolitaireGame.stackOffsetVertical, Content, this);
            multiplayerGame = new MultiPlayerGame(spriteBatch, dragonDrop, SolitaireGame.stackOffsetHorizontal, SolitaireGame.stackOffsetVertical, Content, this);

            background = Content.Load<Texture2D>("tiledBackground");

            dimScreen = new Texture2D(GraphicsDevice, 1, 1);
            dimScreen.SetData(new[] { Color.DarkGray });

            validWords.LoadWords();

            // TODO: use this.Content to load your game content here

        }

        private void SplashScreenEnded(object sender, EventArgs e)
        {
            this.currentAppState = AppState.SolitaireGame;
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
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            
            // TODO: Add your update logic here
            switch (this.currentAppState)
            {
                case AppState.SplashScreen:
                    splashScreen.Update(gameTime);
                    break;
                case AppState.MainScreen:
                    mainScreen.Update(gameTime);
                    break;
                case AppState.SolitaireGame:
                case AppState.RatedSolitaireGame:
                    solitaireGame.Update(gameTime);
                    break;
                case AppState.MultiPlayerGame:
                    multiplayerGame.Update(gameTime);
                    break;
            }
            base.Update(gameTime);
        }

        
        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);
            spriteBatch.Begin(transformMatrix: viewPort.GetScaleMatrix(), samplerState: SamplerState.LinearWrap);
            spriteBatch.Draw(background, new Rectangle(0, 0, WINDOW_WIDTH, WINDOW_HEIGHT), Color.White);            
            
            
            // draw background

            switch (this.currentAppState)
            {
                case AppState.SplashScreen:
                    splashScreen.Draw(gameTime);                    
                    break;

                case AppState.MainScreen:
                    mainScreen.Draw(gameTime);
                    break;

                case AppState.SolitaireGame:
                case AppState.RatedSolitaireGame:
                    solitaireGame.Draw(gameTime);
                    break;

                case AppState.MultiPlayerGame:
                    multiplayerGame.Draw(gameTime);
                    break;
            }

            spriteBatch.End();

            base.Draw(gameTime);
        }
    }
}
