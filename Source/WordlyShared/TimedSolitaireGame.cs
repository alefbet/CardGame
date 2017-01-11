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
    
    

    public class TimedSolitaireGame : Table
    {
        public ContentManager Content;        
        public MainGame mainGame;
        
        //private Table table;

       

        

        const int CARD_WIDTH = 210;
        const int CARD_HEIGHT = 252;
        public static new int stackOffsetHorizontal = 70;
        public static new int stackOffsetVertical = 50;
        const int numStacks = 8;
        const int numCardsInStack = 6;
        const float cardAnimationTime = 1.2f;


        const int kickSound = 0;
        const int playSound = 2;
        const int restackSound = 3;

        int numJokersUsed = 0;

        public bool quitGame = false;

        Texture2D noCardsTex, wordTex, addWordTex, gameOverTex, exitTex, clearTex, wrongWordTex, gameOverPlayAgainTex;
        Texture2D jokerTex, startGameTex, timerTex;        
        SpriteFont font, gameOverFont;
        Color addWordColor;
        Rectangle scoreRect, scoreLabelRect, addWordRect, clearRect, gameOverRect, startGameRect;
        Rectangle timerRect, timerImageRect;
        Rectangle gameOverPlayAgainRect, exitRect;

        Pie2D TimerDisplay;

        MoveImage curWordScoreImage = null;

        bool postedGameScores;

        public enum GameState { NewGame, AnimatingStart, Playing, Paused, GameOver};

        public GameState gameState = GameState.NewGame;


        public TimedSolitaireGame(SpriteBatch spriteBatch,  int stackOffsetH, int stackOffsetV, ContentManager Content, MainGame game) : base(spriteBatch, game, game.viewPort, stackOffsetH, stackOffsetV)
        {
            this.spriteBatch = spriteBatch;            
            this.Content = Content;
            this.mainGame = game;
            LoadContent();
            
        }


        
        private List<SoundEffect> soundFX;

        public Deck drawPile { get; set; }        

        public Stack currentWordStack { get; set; }

        public Stack jokerDeck { get; set; }

        public Slot jokerSlot { get; set; }

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
        
        public bool haveMoreCards
        {
            get
            {
                return (drawPile.Count >= numStacks);
            }
        }

        public bool haveMoreMoves
        {
            get
            {
                if (haveMoreCards)
                    return true;

                //to do -- figure out if any words are available
                return false;                
            }
        }

        public bool isGameOver
        {
            get
            {
                return !((InvalidWords < 3) && haveMoreMoves && !quitGame);
            }
        }
        

        public string CurrentWord { get
            {
                string w = "";
                foreach (Card c in currentWordStack.cards)
                    w += c.wordValue;
                return w;
            }
        }            
        public int CurrentWordScore
        {
            get
            {
                int s = 0;
                foreach (Card c in currentWordStack.cards)
                    s += c.wordPoints;
                return s;
            }
        }

        public int CurrentWordBonus
        {
            get
            {
                if (!mainGame.validWords.IsWordValid(CurrentWord))
                    return 0;

                int s = CurrentWord.Length;
                return (s > 3 ? (s - 3) * 5 : 0);
            }
        }

        public bool isSetup;
        private bool isSnapAnimating;
        public bool isAnimating
        {
            get
            {
                if (!isSnapAnimating) return animationCount > 0;
                else return true;
            }
        }
        private int animationCount = 28;

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
            jokerTex = Content.Load<Texture2D>("deck/JOKER_2_0");



            soundFX = new List<SoundEffect> {
                Content.Load<SoundEffect>("audio/card-kick"),
                Content.Load<SoundEffect>("audio/card-parent"),
                Content.Load<SoundEffect>("audio/card-play"),
                Content.Load<SoundEffect>("audio/card-restack"),
                Content.Load<SoundEffect>("audio/card-undo"),
                Content.Load<SoundEffect>("audio/card-bounce")
            };

            timerTex = Content.Load<Texture2D>("gameplay/timer");
            wordTex = Content.Load<Texture2D>("gameplay/playingArea");
            addWordTex = Content.Load<Texture2D>("gameplay/addWord");
            wrongWordTex = Content.Load<Texture2D>("gameplay/wrongWord");
            clearTex = Content.Load<Texture2D>("gameplay/clear");
            exitTex = Content.Load<Texture2D>("gameplay/exit");            
            exitRect = new Rectangle(1980 - 100, 0, 100, 100);

            startGameTex = Content.Load<Texture2D>("gameplay/StartGame");
            startGameRect = new Rectangle(MainGame.MID_WIDTH - startGameTex.Width / 2, 650, startGameTex.Width, startGameTex.Height);

            gameOverTex = Content.Load<Texture2D>("gameplay/GameOver");
            gameOverRect = new Rectangle(0,0,1980,1020);
            gameOverPlayAgainTex = Content.Load<Texture2D>("gameplay/playAgain");
            gameOverPlayAgainRect = new Rectangle(990 - gameOverPlayAgainTex.Width / 2, 700, gameOverPlayAgainTex.Width, gameOverPlayAgainTex.Height);

            font = Content.Load<SpriteFont>("gameplay/Cutive");
            gameOverFont = Content.Load<SpriteFont>("gameplay/gameOverFont");


            
            


            drawPile = new Deck(this, DeckType.word, cardBack , mainGame.dimScreen, spriteBatch, stackOffsetHorizontal, stackOffsetVertical) { type = StackType.deck };
                        
            
            
            InitializeTable();
            
            Tween.TweenerImpl.SetLerper<Vector2Lerper>(typeof(Vector2));
            
        }
     


        private void InitializeTable()
        {
                       
           

            int x = 20;
            int y = 20;
            drawPile.freshDeck();

            scoreLabelRect = new Rectangle(2 * x + slotTex.Width, y + 40, slotTex.Width, 80);
            scoreRect = new Rectangle(2 * x + slotTex.Width, y + 85, slotTex.Width, 80);
            timerImageRect = new Rectangle(4 * x + slotTex.Width, y + 180, timerTex.Width, timerTex.Height);
            timerRect = new Rectangle(timerImageRect.Right, timerImageRect.Bottom - 30, slotTex.Width - timerImageRect.Width - 2*x, 40);

            var wordSlot = new Slot(wordTex, spriteBatch)
            {
                Position = new Vector2(x + x * 2 + slotTex.Width * 2, y),
                name = "Word"

            };            

            currentWordStack = AddStack(wordSlot, StackType.hand, StackMethod.draggable);


            clearRect = new Rectangle(wordSlot.Border.Right - clearTex.Width, wordSlot.Border.Top, clearTex.Width, clearTex.Height);

            addWordRect = new Rectangle((int)wordSlot.Position.X + wordTex.Width + x, y + (int) ((wordTex.Height - addWordTex.Height) * 0.5), addWordTex.Width, addWordTex.Height);

            jokerSlot = new Slot(jokerTex, spriteBatch)
            {
                Position = new Vector2(0, -1 * CARD_HEIGHT),
                name = "Joker"
            };
            jokerDeck = AddStack(jokerSlot, StackType.deck, StackMethod.horizontal);

            var drawSlot = new Slot(slotTex, spriteBatch)
            {
                Position = new Vector2(0, -1 * CARD_HEIGHT),
                name = "Draw"
            };


            drawPile.slot = drawSlot;;

            y += slotTex.Height + y;
                       
            for (int i = 0; i < numStacks; i++)
            {

                var newSlot = new Slot(slotTex, spriteBatch)
                {
                    Position = new Vector2(x + x * i + slotTex.Width * i, y),
                    name = "Stack " + i
                };

                stackPositions.Add(newSlot.Position);
                var newStack = AddStack(newSlot, StackType.stack, StackMethod.vertical);

                // add crunch for these stacks
                newStack.crunchItems = 24;                
            }

            TimerDisplay = new Pie2D(mainGame, timerTex.Width/2 - 4, 0, 8, false, new Color(Color.DarkGray, 0.5f));
            TimerDisplay.Position = new Vector2(timerImageRect.Center.X, timerImageRect.Bottom - timerImageRect.Width / 2);
            TimerDisplay.Rotation = MathHelper.ToRadians(-90);
            TimerDisplay.LoadContent();
        }

        

        public void PlayNewGame()
        {

            foreach (var stack in stacks)
            {
                stack.Clear();
            }

            drawPile.freshDeck();
            drawPile.shuffle();
            drawPile.UpdatePositions();

            numJokersUsed = 0;            
            completedWords = new List<CompletedWord>();
            postedGameScores = false;
            
            int y = 10;
            y += slotTex.Height + y;
            float delay = 0;
            
            for (var i = 0; i < numStacks; i++)
            {

                //var newX = x + x * i + slotTex.Width * i;

                var pos = stackPositions[i];

                                   
                for (var j = 0; j < numCardsInStack; j++)
                {

                    var moveCard = drawPile.drawCard();
                    while (moveCard.wordPoints == 0)
                        moveCard = drawPile.drawCard();

                    dragonDrop.Add(moveCard);
                    moveCard.Collusion += OnCollusion;                    

                    var location = "deck/" + moveCard.rank;
                    moveCard.SetTexture(Content.Load<Texture2D>(location));
                    moveCard.SelectedOverlayTexture = cardSelectedTex;


                    moveCard.snapPosition = new Vector2(pos.X, pos.Y + stackOffsetVertical * j);                                        
                    moveCard.IsDraggable = false;
                    moveCard.isFaceUp = false;
                    moveCard.ZIndex = j;
                    delay =  (j * numStacks * cardAnimationTime) + (i* cardAnimationTime);

                    if (j == (numCardsInStack -1))
                    {
                        
                        tween.Tween(moveCard, new { Position = moveCard.snapPosition }, cardAnimationTime, delay)
                            .OnBegin(() => setFaceUpAndDraggable(moveCard))
                            .OnComplete(afterAnimate)
                            .Ease(Ease.CubeOut);
                    }
                    else
                    {
                        
                        tween.Tween(moveCard, new { Position = moveCard.snapPosition }, cardAnimationTime, delay)
                            .Ease(Ease.CubeOut)
                            .OnComplete(afterAnimate);
                    }
                    
                    stacks[i+2].addCard(moveCard);
                    
                }

                       

            }

            drawPile.cards.Clear();

            for (int c = 0; c < 20; c++)
            {
                var card = new Card("JOKER", jokerTex, spriteBatch) { isFaceUp = true, Position = jokerSlot.Position };

                jokerDeck.addCard(card);

                dragonDrop.Add(card);
                card.Collusion += OnCollusion;

                var location = "deck/" + card.rank;
                card.SetTexture(Content.Load<Texture2D>(location));
                card.SelectedOverlayTexture = cardSelectedTex;
            }

            tween.Tween(jokerSlot, new { Position = new Vector2(20,20) }, 2.5f, delay+ cardAnimationTime)
                            .Ease(Ease.CubeOut)
                            .OnComplete(DoneAnimatingCards);

            if (!muteSound) soundFX[playSound].Play();

            gameState = GameState.AnimatingStart;
            TotalGameTime = new TimeSpan(0, 2, 00);
            GameTimer = new TimeSpan(0, 2, 0);

            debug();
        }

        private void DoneAnimatingCards()
        {
            gameState = GameState.Playing;
            
        }

        private void setFaceUpAndDraggable(Card c)
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

        private void afterAnimate()
        {

            animationCount--;
            // todo cleanup this sound stuff

            if (!muteSound) soundFX[playSound].Play();
        }


        private void afterAnimateCardToAnotherCard(Card card, Card destinationCard)
        {

            card.ZIndex -= ON_TOP;
            card.isSnapAnimating = false;
            destinationCard.stack.addCard(card);
            //card.SetParent(destinationCard);
            if (!muteSound) soundFX[playSound].Play(.3f, 0, 0);
            animationCount = 0;
        }

        private void afterAnimateCardToPlayWord(Card card, Stack stack)
        {
            card.ZIndex -= ON_TOP;
            card.isSnapAnimating = false;
            card.IsSelected = false;
            card.allowsSelection = true;
            cardsAnimatingToWord--;
            if (stack.Count == 0)
                card.MoveToEmptyStack(stack);
            else
                //card.SetParent(stack.cards[stack.Count - 1]);
                stack.addCard(card);
            if (!muteSound) soundFX[playSound].Play(.3f, 0, 0);
            animationCount--;
        }

        private void afterAnimatePlayCardToEmptyStack(Card card, Stack stack)
        {
            afterAnimateCardToPlayWord(card, stack);
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

                if (destination.stack == currentWordStack && card.stack != currentWordStack)                
                    PlayCardToEndOfCurrentWord(card);                
                else if(destination.stack.type == StackType.play)
                    PlayCardAfterCard(card, destination);
            }
            else if (type == typeof(Slot))
            {
                var slot = (Slot)e.item;
                Debug.WriteLine("OnCollusion: " + card.suit.ToString() + " : " + " Slot " + slot.stack.name);
                if (e.item == currentWordStack.slot)
                    PlayCardToEndOfCurrentWord(card);
                else if (slot.stack.Count == 0)
                {
                    var allowPlay = true;
                    foreach (var c in currentWordStack.cards)
                        if (c.previousStack == slot.stack)
                            allowPlay = false;

                    if (allowPlay)
                        PlayCardToEmptyStack(card, slot.stack);
                }
            }

        }



        TimeSpan GameTimer;
        TimeSpan TotalGameTime;
        bool isTimerRunning;

        void UpdateTimer (GameTime gameTime)
        {
            switch (gameState)
            {
                case GameState.Paused:
                    isTimerRunning = false;
                    break;
                case GameState.GameOver:
                    isTimerRunning = false;
                    break;
                case GameState.Playing:
                    isTimerRunning = true;
                    GameTimer = GameTimer.Subtract(gameTime.ElapsedGameTime);
                    float percentDown = 1f - ((float) GameTimer.TotalMilliseconds / (float) TotalGameTime.TotalMilliseconds);
                    TimerDisplay.Angle = MathHelper.ToRadians(percentDown * 360);
                    if (GameTimer.CompareTo(TimeSpan.Zero) < 0)
                    {
                        gameState = GameState.GameOver;
                    }
                    break;

            }
        }



        private bool dblTap = false;

        public new void Update(GameTime gameTime)
        {
            
            if (gameState == GameState.GameOver)
                ProcessEndOfGame();

            UpdateTimer(gameTime);

            addWordColor = (currentWordStack.Count > 1) ? Color.White : Color.DarkGray;        

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

                        switch (gameState)
                        {
                            case GameState.Playing:
                                var sel = (dragonDrop.selectedItem == null) ? "None" : ((Card)dragonDrop.selectedItem).ToString();
                                Debug.WriteLine("Gesture:" + gesture.GestureType + ":" + point.ToString() + ": Delta : " + gesture.Delta.ToPoint().ToString() + " : SelectedItem :" + sel);
                                ProcessDrag(point, gesture, gameTime);
                                break;
                        }
                        break;

                    case GestureType.Tap:
                        switch (gameState)
                        {
                            case GameState.NewGame:
                                if (exitRect.Contains(point))
                                    mainGame.currentAppState = MainGame.AppState.MainScreen;

                                if (startGameRect.Contains(point))
                                {
                                    PlayNewGame();
                                }
                                break;
                            case GameState.Playing:
                                if (exitRect.Contains(point))
                                    gameState = GameState.Paused;


                                if (addWordRect.Contains(point) && currentWordStack.Count > 1)
                                {
                                    PlayAddWord();
                                    break;
                                }

                                if (clearRect.Contains(point))
                                {
                                    ClearCards();
                                }

                                if (jokerSlot.Border.Contains(point))
                                {
                                    PlayJokerToEndOfWord();
                                }

                                // check if it was on one of the table stacks
                                foreach (var stack in stacks)
                                {
                                    if (stack.Count > 0)
                                    {
                                        if (stack.type == StackType.stack)
                                        {
                                            var topCard = stack.topCard();

                                            if (topCard.Border.Contains(point) && topCard.Child == null && topCard.isFaceUp && topCard.allowsSelection)
                                            {

                                                Task.Delay(100).ContinueWith((args) =>
                                                {
                                                    if (!dblTap)
                                                    {
                                                        // DO stuff on Tap                
                                                        Debug.WriteLine("play-card: " + topCard.wordValue + " return-stack: " + topCard.stack.name);
                                                        PlayCardToEndOfCurrentWord(topCard);
                                                    }
                                                });

                                                break;
                                            }
                                        }
                                    }
                                }

                                //check if it was from the playing word stack

                                var cardIndex = currentWordStack.Count;

                                while (cardIndex-- > 0)
                                {
                                    var topCard = currentWordStack.cards[cardIndex];

                                    if (topCard.Border.Contains(point) && topCard.isFaceUp)
                                    {
                                        Debug.WriteLine("return-card: " + topCard.wordValue + " return-stack: " + topCard.previousStack.name);
                                        ReturnCard(topCard, topCard.previousStack, cardIndex);
                                        break;
                                    }

                                }
                                break;
                            case GameState.Paused:
                                if (exitRect.Contains(point))
                                    gameState = GameState.GameOver;

                                if (startGameRect.Contains(point))
                                {
                                    gameState = GameState.Playing;
                                }
                                break;
                            case GameState.GameOver:
                            if (exitRect.Contains(point))
                                mainGame.currentAppState = MainGame.AppState.MainScreen;

                            if (gameOverPlayAgainRect.Contains(point))
                                PlayNewGame();

                                break;
                        }
                                                                                                                              
                        break;
                    case GestureType.DoubleTap:
                        if (!isGameOver)
                        {
                            var emptyStack = GetEmptyStack();
                            if (emptyStack != null)
                            {
                                foreach (var stack in stacks)
                                {
                                    if (stack.Count > 0)
                                    {
                                        if (stack.type == StackType.stack)
                                        {
                                            var topCard = stack.topCard();

                                            if (topCard.Border.Contains(point) && topCard.Child == null && topCard.isFaceUp && topCard.allowsSelection)
                                            {
                                                dblTap = true;
                                                Debug.WriteLine("play-card: " + topCard.wordValue + " orignal-stack: " + topCard.stack.name + " move-stack: " + emptyStack.name);

                                                PlayCardToEmptyStack(topCard, emptyStack);
                                                Task.Delay(100).ContinueWith((args) => { dblTap = false; });
                                                break;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        break;
                }
            }
            
            tween.Update(float.Parse(gameTime.ElapsedGameTime.Seconds + "." + gameTime.ElapsedGameTime.Milliseconds));
            base.Update(gameTime);

        }

        private void ProcessEndOfGame()
        {
            if (!postedGameScores)
            {
                mainGame.online.SubmitScore("Timed Hard", TotalScore);                
                postedGameScores = true;
            }
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

        private void PlayJokerToEndOfWord()
        {
            var topCard = jokerDeck.topCard();
            Debug.WriteLine("play-card: " + topCard.wordValue + " return-stack: " + topCard.stack.name);
            PlayCardToEndOfCurrentWord(topCard);
        }

        private void PlayAddWord()
        {
            int cardCount = 0;
            bool checkWord = mainGame.validWords.IsWordValid(CurrentWord);

            if (checkWord)
            {
                foreach (var card in currentWordStack.cards)
                {                    
                    animationCount++;
                    card.ZIndex += ON_TOP;

                    card.previousStack = null;
                    var pos = card.Position;
                    pos.Y = 0 - slotTex.Height;
                    tween.Tween(card, new { Position = pos }, 5)
                        .Ease(Ease.CubeInOut)
                        .OnComplete(() => afterScoreCardAnimate(card));
                    
                }
            }            

            curWordScoreImage = new MoveImage(addWordRect.Center.X, addWordRect.Center.Y);
            animationCount++;

            int scoreMultiplier = checkWord ? 1 : -1;
            
            Vector2 posScore = new Vector2(scoreRect.Location.X, scoreRect.Location.Y);
            tween.Tween(curWordScoreImage, new { Position = posScore }, 10)
                        .Ease(Ease.CubeInOut)
                        .OnComplete(() => afterScoreAnimate(CurrentWordScore*scoreMultiplier, checkWord));

        }

        private void afterScoreAnimate(int currentWordScore, bool isValidWord)
        {
            curWordScoreImage = null;
            animationCount--;

            bool bonus = (CurrentWord.Length > 3);            
            var completed = new CompletedWord(CurrentWord, currentWordScore, CurrentWordBonus);

            completedWords.Add(completed);

            if (isValidWord)
            {
                currentWordStack.Clear();
                foreach (var stack in stacks)
                {
                    if (stack.Count > 0)
                    {
                        var topCard = stack.topCard();

                        if (stack.type == StackType.stack && !(topCard.isFaceUp))
                        {
                            topCard.isFaceUp = true;
                            topCard.IsDraggable = true;
                        }
                    }
                }
            }
            else
            {
                InvalidWords += 1;
            }
                       

            //throw new NotImplementedException();
        }

        private void afterScoreCardAnimate(Card card)
        {
            animationCount--;
            //throw new NotImplementedException();
        }



        private void ClearCards()
        {
            if (!isAnimating)
            {
                for (var i = 0; i < currentWordStack.Count; i++)
                {
                    var card = currentWordStack.cards[i];                    
                    var stack = card.previousStack;

                    ReturnCard(card, stack, i);
                }
            }
        }


        private void ReturnCard(Card card, Stack stack, int cardIndex)
        {
            //currentWordStack.debug();
            

            if (cardIndex > 0)
                currentWordStack.cards[cardIndex - 1].Child = card.Child;
            card.Child = null;
                        
            animationCount++;
            card.ZIndex += ON_TOP;

            Vector2 pos = stack.slot.Position;
            pos.Y += stack.Count * stackOffsetVertical;            

            card.previousStack = null;
            tween.Tween(card, new { Position = pos }, 3)
                .Ease(Ease.CubeInOut)
                .OnComplete(() => afterAnimateCardReturnFromWord(card, stack, currentWordStack));
            
        }

        private void PlayCardAfterCard(Card card, Card destination)
        {
            if (destination.stack == currentWordStack)
            {
                currentWordStack.insertCardAfter(card, destination, true);
                //card.SetParent(destination);
                
            }
        }

        int cardsAnimatingToWord = 0;
        private void PlayCardToEndOfCurrentWord(Card topCard)
        {

            Vector2 pos = currentWordStack.slot.Position;
            pos.X += (currentWordStack.Count + cardsAnimatingToWord++) * stackOffsetHorizontal;
            
            
            animationCount++;
            topCard.ZIndex += ON_TOP;

            topCard.previousStack = topCard.stack;
            topCard.allowsSelection = true;
            tween.Tween(topCard, new { Position = pos }, 3)
                .Ease(Ease.CubeInOut)
                .OnComplete(() => afterAnimateCardToPlayWord(topCard, currentWordStack));
            
        }

        private void PlayCardToEmptyStack(Card topCard, Stack emptyStack)
        {
            Vector2 pos = emptyStack.slot.Position;            

            if (true)
            {
                animationCount++;
                topCard.ZIndex += ON_TOP;

                topCard.previousStack = topCard.stack;
                topCard.allowsSelection = true;
                tween.Tween(topCard, new { Position = pos }, 3)
                    .Ease(Ease.CubeInOut)
                    .OnComplete(() => afterAnimatePlayCardToEmptyStack(topCard, emptyStack));
            }
        }

        public new void Draw(GameTime gameTime)
        {
            base.Draw(gameTime);
            spriteBatch.Draw(addWordTex, addWordRect, addWordColor);
            TimerDisplay.Draw();
            switch (gameState)
            {
                case GameState.AnimatingStart:
                    spriteBatch.Draw(cardBack, new Vector2(20, 20), Color.White);
                    goto case GameState.Playing;
                case GameState.Playing:
                    
                    Util.DrawString(spriteBatch, font, "Score", scoreLabelRect, Util.Alignment.Center, Color.White);
                    Util.DrawString(spriteBatch, font, TotalScore.ToString(), scoreRect, Util.Alignment.Center, Color.White);
                    Util.DrawString(spriteBatch, font, GameTimer.ToString(@"mm\:ss"), timerRect, Util.Alignment.Center, Color.White);
                    spriteBatch.Draw(timerTex, timerImageRect, Color.White);
                    


                    if (CurrentWord.Length > 0)
                    {
                        spriteBatch.Draw(clearTex, clearRect, Color.White);
                    }


                    if (curWordScoreImage != null)
                    {
                        string word = mainGame.validWords.IsWordValid(CurrentWord) ? "+ " : "- ";
                        word += CurrentWordScore.ToString() + " " + CurrentWord;

                        spriteBatch.DrawString(font, word, curWordScoreImage.Position, mainGame.validWords.IsWordValid(CurrentWord) ? Color.LightGreen : Color.Red);

                        if (CurrentWordBonus > 0)
                        {
                            var pos = curWordScoreImage.Position;
                            pos.Y += 40;
                            spriteBatch.DrawString(font, "+ " + CurrentWordBonus.ToString() + " length bonus", pos, Color.LightGreen);
                        }
                    }
                    break;

                case GameState.NewGame:
                    spriteBatch.Draw(mainGame.dimScreen, new Rectangle(0, 0, 1980, 1020), Color.White * 0.8f);

                    string readyToPlay1 = "Ready to play timed round?";
                    string readyToPlay2 = "2:00 to get the highest score you can.";
                    string readyToPlay3 = "Use your joker's wisely...or not at all";

                    Rectangle readyRect1 = new Rectangle(MainGame.MID_WIDTH - 200, 300, 400, 50);
                    Rectangle readyRect2 = new Rectangle(MainGame.MID_WIDTH - 200, 400, 400, 50);
                    Rectangle readyRect3 = new Rectangle(MainGame.MID_WIDTH - 200, 500, 400, 50);
                    Util.DrawString(spriteBatch, gameOverFont, readyToPlay1, readyRect1, Util.Alignment.Center, Color.White);
                    Util.DrawString(spriteBatch, gameOverFont, readyToPlay2, readyRect2, Util.Alignment.Center, Color.White);
                    Util.DrawString(spriteBatch, gameOverFont, readyToPlay3, readyRect3, Util.Alignment.Center, Color.White);

                    spriteBatch.Draw(startGameTex, startGameRect, Color.White);

                    break;

                case GameState.Paused:
                    spriteBatch.Draw(mainGame.dimScreen, new Rectangle(0, 0, 1980, 1020), Color.White * 0.8f);
                    string paused = "Game Paused";
                    Rectangle pausedRect = new Rectangle(MainGame.MID_WIDTH - 200, 400, 400, 50);

                    Util.DrawString(spriteBatch, gameOverFont, paused, pausedRect, Util.Alignment.Center, Color.White);
                    spriteBatch.Draw(startGameTex, startGameRect, Color.White);
                    break;

                case GameState.GameOver:
                    spriteBatch.Draw(mainGame.dimScreen, new Rectangle(0, 0, 1980, 1020), Color.White * 0.8f);
                    spriteBatch.Draw(gameOverTex, gameOverRect, Color.White);

                    var gameOverTextRect = new Rectangle(0, 175, MainGame.MID_WIDTH - 20, 150);
                    var gameOverScoresTextRect = new Rectangle(MainGame.MID_WIDTH + 20, 175, 600, 150);

                    string gameOverScoresText = completedWords.Count.ToString();


                    string gameOverBestWord = "";
                    if (completedWords.Count > 0)
                    {
                        var bw = BestWord;
                        gameOverBestWord = bw.word + " " + "(" + bw.score + ")";
                    }


                    var gameOverBonusText = "TBD :-)";

                    Util.DrawString(spriteBatch, gameOverFont, "Words Found:", gameOverTextRect, Util.Alignment.Right, Color.White);
                    Util.DrawString(spriteBatch, gameOverFont, completedWords.Count.ToString(), gameOverScoresTextRect, Util.Alignment.Left, Color.White);
                    gameOverTextRect.Y += 100;
                    gameOverScoresTextRect.Y += 100;
                    Util.DrawString(spriteBatch, gameOverFont, "Best Word:", gameOverTextRect, Util.Alignment.Right, Color.White);
                    Util.DrawString(spriteBatch, gameOverFont, gameOverBestWord, gameOverScoresTextRect, Util.Alignment.Left, Color.White);
                    gameOverTextRect.Y += 100;
                    gameOverScoresTextRect.Y += 100;
                    Util.DrawString(spriteBatch, gameOverFont, "Bonuses:", gameOverTextRect, Util.Alignment.Right, Color.White);
                    Util.DrawString(spriteBatch, gameOverFont, gameOverBonusText, gameOverScoresTextRect, Util.Alignment.Left, Color.White);
                    gameOverTextRect.Y += 100;
                    gameOverScoresTextRect.Y += 100;
                    Util.DrawString(spriteBatch, gameOverFont, "Penalties:", gameOverTextRect, Util.Alignment.Right, Color.White);

                    if (InvalidWords == 0)
                    {
                        Util.DrawString(spriteBatch, gameOverFont, "None", gameOverScoresTextRect, Util.Alignment.Left, Color.White);
                    }
                    else
                    {
                        for (var i = 0; i < InvalidWords; i++)
                            spriteBatch.Draw(wrongWordTex, new Rectangle(gameOverScoresTextRect.Left + (i * wrongWordTex.Width), gameOverScoresTextRect.Top, 60, 60), Color.White);
                    }

                    gameOverTextRect.Y += 100;
                    gameOverScoresTextRect.Y += 100;
                    Util.DrawString(spriteBatch, gameOverFont, "Total Score:", gameOverTextRect, Util.Alignment.Right, Color.White);
                    Util.DrawString(spriteBatch, gameOverFont, TotalScore.ToString(), gameOverScoresTextRect, Util.Alignment.Left, Color.LightGreen);

                    spriteBatch.Draw(gameOverPlayAgainTex, gameOverPlayAgainRect, Color.White);
                    break;
            }
        
            spriteBatch.Draw(exitTex, exitRect, Color.White);
            

        }

        CompletedWord BestWord
            {
                get
                {                                    
                    var max = completedWords[0];

                    foreach (var cw in completedWords)
                    {
                        if (cw.score > max.score)                    
                            max = cw;                        

                    }

                    return max;
                }   

            }

    }
}
