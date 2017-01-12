using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Content;
using MonoGame.Ruge.Glide;
using MonoGame.Ruge.CardEngine;
using MonoGame.Ruge.DragonDrop;
using System.Linq;

using System.Threading.Tasks;
using System.Diagnostics;
using Microsoft.Xna.Framework.Input.Touch;

namespace WordGame
{
    

    public class Player
    {
        public string Name;
        public List<Card> Hand;
        public List<String> WordsInHand;
        public List<int> Scores;
        public Texture2D Avatar;
        public bool isCurrentPlayer;        
        public MoveImage scoreImageAnimate;
        public bool LeftGame;
        public Vector2 Position;        

        public Player()
        {
            Hand = new List<Card>();
        }
    }

    

    public class MultiPlayerGame : Table
    {
        public ContentManager Content;        
        public MainGame mainGame;

        public enum MultiplayerGameState { Connecting, WaitingForPlayers, WaitingForStart, Starting, CurrentPlayerTurn, OtherPlayerTurn, EndOfGame }

        public MultiplayerGameState gameState { get; set; }
        //private Table table;

        public int CurrentRound = 3;
        const int MaxRound = 10;        

        public List<Player> Players;

        
        public string CurrentPlayer;
        public string HostPlayer;

        public int PlayerIndex (string PlayerName)
        {
            return Players.FindIndex(p => p.Name == PlayerName);
        } 


        const int CARD_WIDTH = 210;
        const int CARD_HEIGHT = 252;
        const int avatarWidth = 100;
        public static new int stackOffsetHorizontal = 50;
        public static new int stackOffsetVertical = 50;
        const int numWordSpaces = 5;
        
        const int kickSound = 0;
        const int playSound = 2;
        const int restackSound = 3;

        public bool quitGame = false;

        Texture2D noCardsTex, playingAreaTex, addWordTex, gameOverTex, exitTex, wrongWordTex, gameOverPlayAgainTex;
        Texture2D wordDropSpaceTex;
        Texture2D startGameTex;        
        SpriteFont font, gameOverFont;        
        Rectangle otherPlayersRect;
        Rectangle startGameRect, gameOverRect;
        Rectangle gameOverPlayAgainRect, exitRect;

        MoveImage curWordScoreImage = null;
        

        public MultiPlayerGame(SpriteBatch spriteBatch, int stackOffsetH, int stackOffsetV, ContentManager Content, MainGame game) : base(spriteBatch, game, game.viewPort, stackOffsetH, stackOffsetV)
        {
            this.spriteBatch = spriteBatch;            
            this.Content = Content;
            this.mainGame = game;
            LoadContent();
            
        }


        
        private List<SoundEffect> soundFX;

        public Deck drawPile { get; set; }
        public Slot drawSlot { get; set; }

        public Slot discardSlot { get; set; }

        public Stack cardsInHand { get; set; }
        public Slot cardsInHandSlot { get; set; }
        
        public int TotalScore
        {
            get
            {
                int score = 0;
                foreach (var c in completedWords)
                {
                    score += c.score;
                    score += c.bonus;
                }
                return score;
            }
        }
        public int InvalidWords = 0;

        public Stack saveStack;


        public List<Vector2> stackPositions = new List<Vector2>();
        public List<CompletedWord> completedWords = new List<CompletedWord>();
                

        public bool isGameOver
        {
            get
            {
                return CurrentRound > MaxRound;
            }
        }
                                
        public bool isSetup;
        private bool isSnapAnimating;
        public bool isAnimating
        {
            get
            {
                return (animationCount > 0);
                
            }
        }
        private int animationCount = 0;

        public bool muteSound
        {
            get { return Plugin.Settings.CrossSettings.Current.GetValueOrDefault("mute", true); }
            set { Plugin.Settings.CrossSettings.Current.AddOrUpdateValue("mute", value); }
        }

        private Tweener tween = new Tweener();
                

        public void LoadContent()
        {
            slotTex = Content.Load<Texture2D>("deck/card_slot");
            noCardsTex = Content.Load<Texture2D>("deck/noCards");

            cardBack = Content.Load<Texture2D>("deck/cardBackground");
            cardSelectedTex = Content.Load<Texture2D>("deck/card_selected");

            soundFX = new List<SoundEffect> {
                Content.Load<SoundEffect>("audio/card-kick"),
                Content.Load<SoundEffect>("audio/card-parent"),
                Content.Load<SoundEffect>("audio/card-play"),
                Content.Load<SoundEffect>("audio/card-restack"),
                Content.Load<SoundEffect>("audio/card-undo"),
                Content.Load<SoundEffect>("audio/card-bounce")
            };

            

            playingAreaTex = Content.Load<Texture2D>("gameplay/playingArea");
            wordDropSpaceTex = Content.Load<Texture2D>("gameplay/wordDropSpace");
            addWordTex = Content.Load<Texture2D>("gameplay/addWord");
            wrongWordTex = Content.Load<Texture2D>("gameplay/wrongWord");

            
            exitTex = Content.Load<Texture2D>("gameplay/exit");
            exitRect = new Rectangle(1980 - 100, 0, 100, 100);
            

            gameOverTex = Content.Load<Texture2D>("gameplay/GameOver");
            gameOverRect = new Rectangle(0,0,1980,1020);
            gameOverPlayAgainTex = Content.Load<Texture2D>("gameplay/playAgain");
            gameOverPlayAgainRect = new Rectangle(MainGame.MID_WIDTH - gameOverPlayAgainTex.Width / 2, 700, gameOverPlayAgainTex.Width, gameOverPlayAgainTex.Height);

            font = Content.Load<SpriteFont>("gameplay/Cutive");
            gameOverFont = Content.Load<SpriteFont>("gameplay/gameOverFont");

            startGameTex = Content.Load<Texture2D>("gameplay/StartGame");
            startGameRect = new Rectangle(MainGame.MID_WIDTH - startGameTex.Width / 2, 650, startGameTex.Width, startGameTex.Height);
            
            
            drawPile = new Deck(this, DeckType.word, cardBack, slotTex, spriteBatch, stackOffsetHorizontal, stackOffsetVertical) { type = StackType.deck };
            drawPile.freshDeck();

            
            
            InitializeTable();
            
            Tween.TweenerImpl.SetLerper<Vector2Lerper>(typeof(Vector2));
            
        }


     


        private void InitializeTable()
        {
                       
           

            int x = 20;
            int y = 20;
            drawPile.freshDeck();

            drawSlot = new Slot(slotTex, spriteBatch)
            {
                name = "Draw",
                Position = new Vector2(x, y),
                stack = drawPile
            };



            dragonDrop.Add(drawSlot);
            drawPile.slot = drawSlot;
            AddStack(drawPile);

            discardSlot = new Slot(slotTex, spriteBatch)
            {
                name = "Discard",
                Position = new Vector2(2 * (x + slotTex.Width), y),
                stack = null
            };
            

            cardsInHandSlot = new Slot(playingAreaTex, spriteBatch)
            {
                Position = new Vector2((x + slotTex.Width) * 2, y),
                name = "Cards In Hand"

            };

            cardsInHand = AddStack(cardsInHandSlot, StackType.hand, StackMethod.draggable);

            y += slotTex.Height + 20;

            for (int i = 0; i < numWordSpaces; i++)
            {
                int row = i / 2;
                bool even = i % 2 == 0;
                

                var newSlot = new Slot(wordDropSpaceTex, spriteBatch)
                {
                    Position = new Vector2(even ? x : 2*x + wordDropSpaceTex.Width, y),
                    name = "Potential Word" + i
                };

                if (!even)
                {
                    y += slotTex.Height + 20;
                }

                stackPositions.Add(newSlot.Position);
                var newStack = AddStack(newSlot, StackType.play, StackMethod.horizontal);

                // add crunch for these stacks
                newStack.crunchItems = 24;                
            }
            
        }



        public void NewGame()
        {
            //
            Players = new List<Player>();

            gameState = MultiplayerGameState.Connecting;
            mainGame.online.Connect();
            mainGame.online.GetInitialRoomState += OnGetInitialRoomState;
            mainGame.online.UserJoined += UserJoined;
            mainGame.online.UserLeft += UserLeft;
            mainGame.online.RoomStateChanged += OnRoomStateChanged;
            mainGame.online.RecievedGameMessage += OnReceivedMove;
            

#if DEBUG

            SetupLocalGame();
#endif            
            SetTable();
        }
        void SetupLocalGame(int otherPlayers = 1)
        {
            OnRoomJoinEventArgs newRoom = new OnRoomJoinEventArgs();
            newRoom.RoomOwner = mainGame.online.CurrentUser;
            newRoom.RoomId = "localTest";
            var player2 = Guid.NewGuid().ToString();
            newRoom.Users = new string[] { mainGame.online.CurrentUser};
            OnGetInitialRoomState(this, newRoom);
            UserJoined(this, player2);
        }
            
        public void StartGame()
        {
            gameState = MultiplayerGameState.Starting;

            foreach (var stack in stacks)
            {
                stack.Clear();
            }


            
            
            Random rand = new Random();
            int randomPlayer = rand.Next(Players.Count());
            randomPlayer = 0;
            
            CurrentPlayer = Players[randomPlayer].Name;
            Players.All(p => { p.isCurrentPlayer = p.Name == Players[randomPlayer].Name; return true; });

            InitializeTable();

            SetupCardsForRound(CurrentRound, randomPlayer);

            SetTable();
            
            
            SetupNewRound();
            
        }

        private void SetupCardsForRound(int Round, int startPlayer)
        {
            drawPile.freshDeck();
            drawPile.shuffle();
            for (var c=0; c < Round; c++)
            {
                for (int p= startPlayer; p < startPlayer + Players.Count(); p++)
                {
                    Players[p % Players.Count()].Hand.Add(drawPile.drawCard());
                }
            }
        }

        private void OnRoomStateChanged(object sender, Dictionary<string, object> e)
        {
            throw new NotImplementedException();
        }

        private void OnReceivedMove(object sender, string e)
        {
            throw new NotImplementedException();
        }

        private async void OnGetInitialRoomState(object sender, OnRoomJoinEventArgs e)
        {
            if (e.Users.Count() == 1)
            {
                gameState = MultiplayerGameState.WaitingForPlayers;
            }
            else
            {
                gameState = MultiplayerGameState.WaitingForStart;
            }

            Players.Clear();
            foreach (string user in e.Users)
            {
                Debug.WriteLine("Initializing room: " + e.RoomId + "  User: " + user);
                var playerAvatar = await Util.GetWebImageAsStream("https://api.adorable.io/avatars/" + avatarWidth + "/wordly-" + user);
                Players.Add(new Player() { Name = user, Avatar = Texture2D.FromStream(mainGame.GraphicsDevice, playerAvatar) });                
            }
            HostPlayer = e.RoomOwner;

            UpdatePlayersPositions();
        }

        async void UserJoined(object sender, string user)
        {
            var playerAvatar = await Util.GetWebImageAsStream("https://api.adorable.io/avatars/" + avatarWidth + "/wordly-" + user);
            Players.Add(new Player() { Name = user, Avatar = Texture2D.FromStream(mainGame.GraphicsDevice, playerAvatar) });
            UpdatePlayersPositions();
        }

        void UpdatePlayersPositions()
        {

        }


        void UserLeft(object sender, string user)
        {

            
            Player playerObj = null;
            foreach (var p in Players)
            {
                if (p.Name == user)
                {
                    p.LeftGame = true;
                    playerObj = p;
                }
            }
            switch (gameState)
            {
                case MultiplayerGameState.Connecting:
                case MultiplayerGameState.WaitingForPlayers:
                case MultiplayerGameState.WaitingForStart:
                    if (playerObj != null)
                        Players.Remove(playerObj);
                    break;
                default:
                    if (Players.Where(p => p.LeftGame == false).Count() == 1)
                        gameState = MultiplayerGameState.EndOfGame;
                    break;
            }

            //todo -- person that leaves is current player            
        }

        public void SetupNewRound()
        {
            var xBufferForDragDrop = 20;
            
            if (CurrentPlayer == mainGame.online.CurrentUser)
            {
                gameState = MultiplayerGameState.CurrentPlayerTurn;
            }
            else
            {
                gameState = MultiplayerGameState.OtherPlayerTurn;
            }

            var delay = 0;

            otherPlayersRect = new Rectangle(1980 - (avatarWidth + 10) * 3, 20 + playingAreaTex.Height - avatarWidth, (avatarWidth + 10) * 3, avatarWidth);

            for (var c = 0; c < CurrentRound; c++)
            {
                var p = 0;
                for (var player = 0; player < Players.Count; player++)
                    {
                    var pos = cardsInHand.slot.Position;
                    var moveCard = drawPile.drawCard();
                    moveCard.Position = drawSlot.Position;

                    bool isCurrentUser = Players[player].Name == mainGame.online.CurrentUser;
                    
                    moveCard.isFaceUp = isCurrentUser;
                    moveCard.IsDraggable = isCurrentUser;

                    
                    if (isCurrentUser)
                    {
                        pos = cardsInHand.slot.Position;
                        pos.X += xBufferForDragDrop + (cardsInHand.Count * stackOffsetHorizontal);
                        cardsInHand.addCard(moveCard);
                    }
                    else
                    {                        
                        pos = otherPlayersRect.Location.ToVector2();
                        
                        pos.X += (p++ * (10 + avatarWidth) + 6f);

                    }

                    var scale = isCurrentUser ? 1f :  ((float) avatarWidth) / ((float) slotTex.Height);
                    var rotation = isCurrentUser ? 0f : 2f * Math.PI;
                    moveCard.ZIndex += ON_TOP;
                    moveCard.snapPosition = pos;

                    tween.Tween(moveCard, new { Position = pos, Rotation = rotation, Scale = scale }, 7, delay)
                        .Ease(Ease.CubeOut)
                        .OnComplete(afterAnimate);
                    Debug.WriteLine("player: " + player + " card: " + c + " delay: " + delay + " p: " + p);
                    delay += 3;
                }

            }

            int x = 10;
            int y = 10;
            y += slotTex.Height + y;



            var restackAnimation = drawPile.topCard();
            restackAnimation.Position += new Vector2(stackOffsetHorizontal * 2, 0);
            restackAnimation.isSnapAnimating = true;
            restackAnimation.snapTime = 4.5f;
            if (!muteSound) soundFX[playSound].Play();


        }

        public void SetTable()
        {

            //Load PlayerStacks

            
            drawPile.UpdatePositions();                                    
                      

            foreach (var card in drawPile.cards)
            {
                dragonDrop.Add(card);

                //card.Selected += OnCardSelected;
                card.Collusion += OnCollusion;
                //card.Save += Save;
                card.stack = drawPile;

                var location = "deck/" + card.rank;
                card.SetTexture(Content.Load<Texture2D>(location));
                card.SelectedOverlayTexture = cardSelectedTex;
            }

            foreach (var p in Players)
            {                
                if (p.Name == mainGame.online.CurrentUser)
                {
                    //p.Hand = cardsInHand;                    
                }
                else
                {
                    //var slot = new Slot(p.Avatar, spriteBatch) { IsDraggable = false, Position = new Vector2(otherPlayersRect.X, otherPlayersRect.Y) };
                    //p.Hand = AddStack(slot, StackType.undefined, StackMethod.undefined);
                }
            }

            drawSlot.stack = drawPile;
            
            isSetup = true;

        }

        private void setFaceUp(Card c)
        {
            c.isFaceUp = true;
            c.IsDraggable = true;
        }

        private void addAndSetFaceUp(Card c, Stack s)
        {
            if (s.Count > 0)
            {
                s.topCard().isFaceUp = false;
                s.topCard().IsDraggable = false;
            }
            s.addCard(c, true);            
            c.isFaceUp = true;
            c.IsDraggable = true;
        }

        private void addCardLeaveFaceDown(Card c, Stack s)
        {
            s.addCard(c, true);
            c.isFaceUp = false;
            c.IsDraggable = false;
        }

        private void afterAnimate()
        {

            animationCount--;
            // todo cleanup this sound stuff

            if (!muteSound) soundFX[playSound].Play();
        }

        //private void afterTween(Card card)
        //{
        //    card.flipCard();
        //    card.snapPosition = card.Position;
        //    card.IsDraggable = true;
        //    card.snapTime = .7f;
        //    //discardPile.addCard(card);
        //    animationCount--;
        //    if (!muteSound) soundFX[playSound].Play();
        //}

        private void afterAnimateCardToAnotherCard(Card card, Card destinationCard)
        {

            card.ZIndex -= ON_TOP;
            card.isSnapAnimating = false;
            destinationCard.stack.addCard(card);
            //card.SetParent(destinationCard);
            if (!muteSound) soundFX[playSound].Play(.3f, 0, 0);
            animationCount = 0;
        }

        private void afterAnimateCardToStack(Card card, Stack stack)
        {
            card.ZIndex -= ON_TOP;
            card.isSnapAnimating = false;
            card.allowsSelection = true;
            if (stack.Count == 0)
                card.MoveToEmptyStack(stack);
            else
                //card.SetParent(stack.cards[stack.Count - 1]);
                stack.addCard(card);
            if (!muteSound) soundFX[playSound].Play(.3f, 0, 0);
            animationCount = 0;
        }

        private void afterAnimatePlayCardToEmptyStack(Card card, Stack stack)
        {
            afterAnimateCardToStack(card, stack);
            if (card.previousStack.Count > 0)
            {
                card.previousStack.topCard().isFaceUp = true;
                card.previousStack.topCard().IsDraggable = true;
            }
        }

        private void afterAnimateCardReturnFromWord(Card card, Stack stack, Stack currentWordStack)
        {
            card.ZIndex -= ON_TOP;
            card.isSnapAnimating = false;
            stack.addCard(card);
            currentWordStack.UpdatePositions();   
            if (!muteSound) soundFX[playSound].Play(.3f, 0, 0);
            animationCount = 0;
        }

        private void OnCollusion(object sender, Card.CollusionEvent e)
        {
            var type = e.item.GetType();
            var card = (Card)sender;
            if (type == typeof(Card))
            {
                
                var destination = (Card) e.item;
                
                Debug.WriteLine("OnCollusion: " + card.suit.ToString() + " : " + " Card " + destination.suit.ToString() + ": " + destination.stack.name);

                if (destination.stack.type == StackType.play)            
                    PlayCardAfterCard(card, destination);
            }
            else if (type == typeof(Slot))
            {
                var slot = (Slot)e.item;
                Debug.WriteLine("OnCollusion: " + card.suit.ToString() + " : " + " Slot " + slot.stack.name);

                if (slot.stack.type == StackType.play)
                {
                    slot.stack.addCard(card, true);
                }

                
            }

        }



        bool drawOverlay = true;
        bool drawAvatars = false;
        string overlayString = "";
        private bool dblTap = false;

        public new void Update(GameTime gameTime)
        {
            if (isGameOver)
                ProcessEndOfGame();

            switch (gameState)
            {
                case MultiplayerGameState.Connecting:
                    drawOverlay = true;
                    overlayString = "Connecting...";
                    break;
                case MultiplayerGameState.WaitingForPlayers:
                    drawOverlay = true;
                    overlayString = "Waiting for players...";
                    
                    drawAvatars = true;
                    
                    break;
                case MultiplayerGameState.WaitingForStart:
                    drawOverlay = true;
                    drawAvatars = true;                    
                    
                    overlayString = "Waiting for game to start...";
                    
                    break;
                case MultiplayerGameState.Starting:
                    drawOverlay = true;
                    drawAvatars = true;                    
                    overlayString = "Starting \nSelecting first dealer...";
                    break;
                case MultiplayerGameState.OtherPlayerTurn:
                case MultiplayerGameState.CurrentPlayerTurn:
                    drawOverlay = false;
                    
                    break;
            }
            while (TouchPanel.IsGestureAvailable)
            {


                var gesture = TouchPanel.ReadGesture();
                
                var point = gesture.Position.ToPoint();

                point = mainGame.viewPort.PointToScreen(point);
                Debug.WriteLine("Gesture:" + gesture.GestureType + ":" + point.ToString());

                switch (gesture.GestureType)
                {                    
                    case GestureType.FreeDrag:
                    case GestureType.HorizontalDrag:
                    case GestureType.VerticalDrag:
                    case GestureType.DragComplete:
                        if (drawOverlay == false)
                        {
                            var sel = (dragonDrop.selectedItem == null) ? "None" : ((Card)dragonDrop.selectedItem).ToString();
                            Debug.WriteLine("Gesture:" + gesture.GestureType + ":" + point.ToString() + ": Delta : " + gesture.Delta.ToPoint().ToString() + " : SelectedItem :" + sel);
                            ProcessDrag(point, gesture, gameTime);
                        }
                        break;

                    case GestureType.Tap:
                        switch (gameState)
                        {
                            case MultiplayerGameState.WaitingForPlayers:
                                if (startGameRect.Contains(point))
                                {

                                    StartGame();
                                }
                                break;
                            case MultiplayerGameState.CurrentPlayerTurn:
                                if (drawSlot.Contains(point.ToVector2()))
                                {
                                    SelectFromDeck();
                                    break;
                                }
                                break;

                        }

                        if (exitRect.Contains(point))
                        {
                            mainGame.online.Disconnect();
                            mainGame.currentAppState = MainGame.AppState.MainScreen;
                        }                                                                                                                                                       
                        break;                    
                }
            }
            
            tween.Update(float.Parse(gameTime.ElapsedGameTime.Seconds + "." + gameTime.ElapsedGameTime.Milliseconds));
            base.Update(gameTime);

        }

        private void ProcessEndOfGame()
        {
            //if (!postedGameScores)
            //{
            //    mainGame.online.SubmitScore("Level 1", TotalScore, BestWord.word);
            //    postedGameScores = true;
            //}
        }

        private Stack GetEmptyStack()
        {
            Stack ret = null;
            foreach (var stack in stacks)
            {
                if ((stack.type == StackType.stack) && stack.Count == 0)
                    return stack;
            }

            return ret;
        }

        private void SelectFromDeck()
        {                                                
            var moveCard = drawPile.drawCard();
            var stack = cardsInHand;

                moveCard.snapPosition = new Vector2(stack.slot.Position.X + (stackOffsetHorizontal * (stack.Count + 1)), stack.slot.Position.Y);
                // stack.addCard(moveCard, true);
                moveCard.Position = drawSlot.Position;
                tween.Tween(moveCard, new { Position = moveCard.snapPosition }, 7, 0)
                    .OnBegin(() => addAndSetFaceUp(moveCard, stack))
                    .OnComplete(afterAnimate)
                    .Ease(Ease.CubeOut);
                                        
        }
                

        private void afterScoreCardAnimate(Card card)
        {
            animationCount--;
            //throw new NotImplementedException();
        }

 

       

        private void PlayCardAfterCard(Card card, Card destination)
        {
            if (destination.stack == cardsInHand)
            {
                cardsInHand.insertCardAfter(card, destination, true);
                //card.SetParent(destination);                
            }
        }

        private void PlayCardToEndOfCurrentWord(Card topCard)
        {
            Vector2 pos = cardsInHand.slot.Position;
            pos.X += cardsInHand.Count * stackOffsetHorizontal;
            
            if (true)
            {
                animationCount++;
                topCard.ZIndex += ON_TOP;

                topCard.previousStack = topCard.stack;
                topCard.allowsSelection = false;
                tween.Tween(topCard, new { Position = pos }, 3)
                    .Ease(Ease.CubeInOut)
                    .OnComplete(() => afterAnimateCardToStack(topCard, cardsInHand));
            }        
        }


        public new void Draw(GameTime gameTime)
        {
            // draw stuff before overlays & avatars
            switch (gameState)
            {
                case MultiplayerGameState.Connecting:
                    break;

                case MultiplayerGameState.WaitingForPlayers:
                    break;                                        
                case MultiplayerGameState.CurrentPlayerTurn:
                case MultiplayerGameState.OtherPlayerTurn:

                    base.Draw(gameTime);
                    break;

            }

            if (drawOverlay)
            {
                spriteBatch.Draw(mainGame.dimScreen, new Rectangle(0, 0, MainGame.WINDOW_WIDTH, MainGame.WINDOW_HEIGHT), Color.LightGray * 0.6f);
                Util.DrawString(spriteBatch, gameOverFont, overlayString, new Rectangle(0, 200, MainGame.WINDOW_WIDTH, 250), Util.Alignment.Center, Color.White);
            }

            var centeredAvatarRect = new Rectangle(MainGame.MID_WIDTH - ((avatarWidth + 10) * Players.Count - 1) / 2, 400, ((avatarWidth + 10) * Players.Count - 1), avatarWidth);

            switch (gameState)
            {
                case MultiplayerGameState.Connecting:
                    break;

                case MultiplayerGameState.WaitingForStart:
                    DrawAvatars(true);
                    break;

                case MultiplayerGameState.WaitingForPlayers:                           
                    DrawAvatars(true);
                    if (Players.Count() > 1)
                        spriteBatch.Draw(startGameTex, startGameRect, Color.White);
                    break;

                case MultiplayerGameState.CurrentPlayerTurn:
                case MultiplayerGameState.OtherPlayerTurn:                    
                    DrawAvatars();

                    break;

            }

            spriteBatch.Draw(exitTex, exitRect, Color.White);


        }

        public void DrawAvatars(bool centered = false)
        {
            var p = 0; 


            for (var player = 0; player < Players.Count; player++)
            {
                if (Players[player].Name != mainGame.online.CurrentUser)
                {
                    var pos = Players[player].Position;                    ;                    
                    spriteBatch.Draw(Players[player].Avatar, pos, Color.White);
                }
            }
        }
    }
}

    

