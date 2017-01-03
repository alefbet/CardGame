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
        public Stack Hand;
        public List<String> WordsInHand;
        public List<int> Scores;
        public string Avatar;
        public Color AvatarColor;
        public MoveImage scoreImageAnimate;

        public Player()
        {            
        }
    }

    public class MultiPlayerGame : Table
    {
        public ContentManager Content;        
        public MainGame mainGame;

        //private Table table;

        public int CurrentRound = 3;
        const int MaxRound = 10;

        public List<Player> Players;
        public string CurrentPlayer;

        const int CARD_WIDTH = 210;
        const int CARD_HEIGHT = 252;
        public static new int stackOffsetHorizontal = 50;
        public static new int stackOffsetVertical = 50;
        const int numWordSpaces = 5;
        
        const int kickSound = 0;
        const int playSound = 2;
        const int restackSound = 3;

        public bool quitGame = false;

        Texture2D noCardsTex, playingAreaTex, addWordTex, gameOverTex, exitTex, clearTex, wrongWordTex, gameOverPlayAgainTex;
        Texture2D wordDropSpaceTex;
        Texture2D avatarTex;
        Texture2D scoreBackgroundTex;
        SpriteFont font, gameOverFont;
        Color addWordColor;
        Rectangle otherPlayersRect;
        Rectangle scoreRect, scoreLabelRect, addWordRect, clearRect, gameOverRect;
        Rectangle gameOverPlayAgainRect, exitRect;

        MoveImage curWordScoreImage = null;
        

        public MultiPlayerGame(SpriteBatch spriteBatch, DragonDrop<IDragonDropItem> dragonDrop,  int stackOffsetH, int stackOffsetV, ContentManager Content, MainGame game) : base(spriteBatch, dragonDrop, stackOffsetH, stackOffsetV)
        {
            this.spriteBatch = spriteBatch;
            this.dragonDrop = dragonDrop;
            this.Content = Content;
            this.mainGame = game;
            LoadContent();
            
        }


        
        private List<SoundEffect> soundFX;

        public Deck drawPile { get; set; }
        public Slot drawSlot { get; set; }

        public Slot discardSlot { get; set; }        

        public Stack cardsInHand { get; set; }
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

            avatarTex = Content.Load<Texture2D>("gameplay/Avatar");
            otherPlayersRect = new Rectangle(1980 - (avatarTex.Width + 10) * 3, 20+ playingAreaTex.Height - avatarTex.Height, (avatarTex.Width + 10) * 3, avatarTex.Height);

            gameOverTex = Content.Load<Texture2D>("gameplay/GameOver");
            gameOverRect = new Rectangle(0,0,1980,1020);
            gameOverPlayAgainTex = Content.Load<Texture2D>("gameplay/playAgain");
            gameOverPlayAgainRect = new Rectangle(990 - gameOverPlayAgainTex.Width / 2, 700, gameOverPlayAgainTex.Width, gameOverPlayAgainTex.Height);

            font = Content.Load<SpriteFont>("gameplay/Cutive");
            gameOverFont = Content.Load<SpriteFont>("gameplay/gameOverFont");


            
            
            drawPile = new Deck(this, DeckType.word, cardBack, slotTex, spriteBatch, stackOffsetHorizontal, stackOffsetVertical) { type = StackType.deck };
            drawPile.freshDeck();

            
            
            InitializeTable();
            
            Tween.TweenerImpl.SetLerper<Vector2Lerper>(typeof(Vector2));
            SetTable();
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
            

            var cardsInHandSlot = new Slot(playingAreaTex, spriteBatch)
            {
                Position = new Vector2((x + slotTex.Width) * 2, y),
                name = "Word"

            };            

            cardsInHand = AddStack(cardsInHandSlot, StackType.hand, StackMethod.horizontal);

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
            mainGame.online.UserJoined += UserJoined;
            mainGame.online.Connect();
            
            //Players.Add(new Player() { Name = "Player 2", AvatarColor = Color.Red, Hand = new Stack(this, avatarTex, avatarTex, spriteBatch, 0, 0) { type = StackType.undefined } });
            //Players.Add(new Player() { Name = mainGame.online.CurrentUser });            
            //Players.Add(new Player() { Name = "Player 3", AvatarColor = Color.Green, Hand = new Stack(this, avatarTex, avatarTex, spriteBatch, 0, 0) { type = StackType.undefined } });
            //Players.Add(new Player() { Name = "Player 4", AvatarColor = Color.Blue, Hand = new Stack(this, avatarTex, avatarTex, spriteBatch, 0, 0) { type = StackType.undefined } });

            foreach (var stack in stacks)
            {
                stack.Clear();
            }

            
            drawPile.freshDeck();
            drawPile.shuffle(1);
            CurrentRound = 3;            
            SetTable();
            SetupNewRound();
        }

        void UserJoined(object sender, string user)
        {
            
        }

        public void SetupNewRound()
        {
            var xBufferForDragDrop = 20;




            
            var delay = 0;            
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
                        
                        pos.X += (p++ * (10 + avatarTex.Width) + 6f);

                    }

                    var scale = isCurrentUser ? 1f :  ((float) avatarTex.Width) / ((float) slotTex.Height);
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
            quitGame = false;

            completedWords = new List<CompletedWord>();            

            foreach (var card in drawPile.cards)
            {
                dragonDrop.Add(card);

                //card.Selected += OnCardSelected;
                card.Collusion += OnCollusion;
                //card.Save += Save;
                card.stack = drawPile;

                var location = "deck/" + card.rank;
                card.SetTexture(Content.Load<Texture2D>(location));
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

       

  
        private bool dblTap = false;

        public new void Update(GameTime gameTime)
        {
            if (isGameOver)
                ProcessEndOfGame();                                

            while (TouchPanel.IsGestureAvailable)
            {
                var gesture = TouchPanel.ReadGesture();
                
                var point = gesture.Position.ToPoint();

                point = mainGame.viewPort.PointToScreen(point);
                Debug.WriteLine("Gesture:" + gesture.GestureType + ":" + point.ToString());

                switch (gesture.GestureType)
                {
                    case GestureType.FreeDrag:
                    case GestureType.DragComplete:
                        var sel = (dragonDrop.selectedItem == null) ? "None" : ((Card) dragonDrop.selectedItem).ToString();
                        Debug.WriteLine("Gesture:" + gesture.GestureType + ":" + point.ToString() + ": Delta : " + gesture.Delta.ToPoint().ToString() + " : SelectedItem :" + sel);
                        ProcessFreeDrag(point, gesture, gameTime);
                        break;

                    case GestureType.Tap:
                        if (isGameOver)
                        {
                            if (exitRect.Contains(point))
                                mainGame.Exit();

                            if (gameOverPlayAgainRect.Contains(point))
                                SetTable();

                            Debug.Print(point.X + "," + point.Y);
                        }
                        else
                        {
                            if (exitRect.Contains(point)) { 
                                quitGame = true;
                                break;
                            }
                            // check if addWord is clicks                        
                            if (addWordRect.Contains(point) && cardsInHand.Count > 1)
                            {
                                //PlayAddWord();
                                break;
                            }

                            if (drawSlot.Contains(point.ToVector2()))
                            {
                                SelectFromDeck();
                                break;
                            }

                            
                           
                        }
                        break;
                    case GestureType.DoubleTap:
                        if (!isGameOver)
                        {
                            
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
            base.Draw(gameTime);                                    

            var p = 0;
            for (var player = 0; player < Players.Count; player++)            
            {
                if (Players[player].Name != mainGame.online.CurrentUser)
                {
                    var pos = otherPlayersRect;
                    pos.X += p++ * (avatarTex.Width + 10);
                    pos.Width = avatarTex.Width;
                    spriteBatch.Draw(avatarTex, pos, Players[player].AvatarColor);
                }
            }


            if (isGameOver)
            {
                spriteBatch.Draw(mainGame.dimScreen, new Rectangle(0, 0, 1980, 1020), Color.White * 0.8f);
                spriteBatch.Draw(gameOverTex, gameOverRect, Color.White);

                var gameOverTextRect = new Rectangle(0,  175, mainGame.MID_WIDTH - 20, 150);
                var gameOverScoresTextRect = new Rectangle(mainGame.MID_WIDTH + 20, 175, 600, 150);

                //string gameOverScoresText = completedWords.Count.ToString();

                //string gameOverBestWord = "";
                //if (completedWords.Count > 0)
                //{
                //    var bw = BestWord;
                //    gameOverBestWord = bw.word + " " + "(" + bw.score + ")";                    
                //}
                

                //var gameOverBonusText = "TBD :-)";
                               
                //Util.DrawString(spriteBatch, gameOverFont, "Words Found:", gameOverTextRect, Util.Alignment.Right, Color.White);
                //Util.DrawString(spriteBatch, gameOverFont, completedWords.Count.ToString(), gameOverScoresTextRect, Util.Alignment.Left, Color.White);
                //gameOverTextRect.Y += 100;
                //gameOverScoresTextRect.Y += 100;
                //Util.DrawString(spriteBatch, gameOverFont, "Best Word:", gameOverTextRect, Util.Alignment.Right, Color.White);
                //Util.DrawString(spriteBatch, gameOverFont, gameOverBestWord, gameOverScoresTextRect, Util.Alignment.Left, Color.White);
                //gameOverTextRect.Y += 100;
                //gameOverScoresTextRect.Y += 100;
                //Util.DrawString(spriteBatch, gameOverFont, "Bonuses:", gameOverTextRect, Util.Alignment.Right, Color.White);
                //Util.DrawString(spriteBatch, gameOverFont, gameOverBonusText, gameOverScoresTextRect, Util.Alignment.Left, Color.White);
                //gameOverTextRect.Y += 100;
                //gameOverScoresTextRect.Y += 100;
                //Util.DrawString(spriteBatch, gameOverFont, "Penalties:", gameOverTextRect, Util.Alignment.Right, Color.White);

                //if (InvalidWords == 0)
                //{
                //    Util.DrawString(spriteBatch, gameOverFont, "None", gameOverScoresTextRect, Util.Alignment.Left, Color.White);                    
                //}
                //else
                //{
                //    for (var i = 0; i < InvalidWords; i++)
                //        spriteBatch.Draw(wrongWordTex, new Rectangle(gameOverScoresTextRect.Left + (i * wrongWordTex.Width), gameOverScoresTextRect.Top, 60, 60), Color.White);
                //}
                
                //gameOverTextRect.Y += 100;
                //gameOverScoresTextRect.Y += 100;
                //Util.DrawString(spriteBatch, gameOverFont, "Total Score:", gameOverTextRect, Util.Alignment.Right, Color.White);
                //Util.DrawString(spriteBatch, gameOverFont, TotalScore.ToString(), gameOverScoresTextRect, Util.Alignment.Left, Color.LightGreen);
                                
                spriteBatch.Draw(gameOverPlayAgainTex, gameOverPlayAgainRect, Color.White);
                
            }

            spriteBatch.Draw(exitTex, exitRect, Color.White);

            //if (table.gameState == GameState.won) confetti.Draw(spriteBatch);

        }

        
    }
}
