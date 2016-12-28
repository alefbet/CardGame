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
   
    
    public class CompletedWord
    {
        public string word;
        public int score;
        public int bonus;

        public CompletedWord(string word, int score, int bonus)
        {
            this.word = word;
            this.score = score;
            this.bonus = bonus;
        }
    }

    public class SolitaireGame
    {
        ContentManager Content;
        SpriteBatch spriteBatch;
        MainGame mainGame;

        private Table table;

        protected const int ON_TOP = 1000;

        const int CARD_WIDTH = 210;
        const int CARD_HEIGHT = 252;
        const int stackOffsetHorizontal = 50;
        const int stackOffsetVertical = 20;
        const int numStacks = 8;

        const int kickSound = 0;
        const int playSound = 2;
        const int restackSound = 3;

        public bool quitGame = false;

        Texture2D slotTex, noCardsTex, cardBack, debug, wordTex, addWordTex, gameOverTex, exitTex, clearTex, wrongWordTex, gameOverPlayAgainTex;
        SpriteFont font, gameOverFont;
        Color debugColor, addWordColor;
        Rectangle debugRect, scoreRect, scoreLabelRect, addWordRect, clearRect, gameOverRect;
        Rectangle gameOverPlayAgainRect, exitRect;

        MoveImage curWordScoreImage = null;
        
                

        DragonDrop<IDragonDropItem> dragonDrop;
        private List<SoundEffect> soundFX;

        public Deck drawPile { get; set; }
        public Slot drawSlot { get; set; }

        public Stack currentWordStack { get; set; }
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

        public SolitaireGame(ContentManager Content, SpriteBatch spriteBatch, MainGame game)
        {
            this.Content = Content;
            this.spriteBatch = spriteBatch;
            this.mainGame = game;
            LoadContent();
        }

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

            wordTex = Content.Load<Texture2D>("gameplay/playingArea");
            addWordTex = Content.Load<Texture2D>("gameplay/addWord");
            wrongWordTex = Content.Load<Texture2D>("gameplay/wrongWord");
            clearTex = Content.Load<Texture2D>("gameplay/clear");
            exitTex = Content.Load<Texture2D>("gameplay/exit");
            exitRect = new Rectangle(1980 - exitTex.Width, 0, 100, 100);

            gameOverTex = Content.Load<Texture2D>("gameplay/GameOver");
            gameOverRect = new Rectangle(0,0,1980,1020);
            gameOverPlayAgainTex = Content.Load<Texture2D>("gameplay/playAgain");
            gameOverPlayAgainRect = new Rectangle(990 - gameOverPlayAgainTex.Width / 2, 700, gameOverPlayAgainTex.Width, gameOverPlayAgainTex.Height);

            font = Content.Load<SpriteFont>("gameplay/Cutive");
            gameOverFont = Content.Load<SpriteFont>("gameplay/gameOverFont");


            dragonDrop = new DragonDrop<IDragonDropItem>(mainGame, mainGame.viewPort);
            table = new Table(spriteBatch, dragonDrop, cardBack, slotTex, 50, 20);

            drawPile = new Deck(table, DeckType.word, cardBack, slotTex, spriteBatch, stackOffsetHorizontal, stackOffsetVertical) { type = StackType.deck };
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
            table.AddStack(drawPile);


            scoreLabelRect = new Rectangle(2*x + slotTex.Width, y+80, slotTex.Width, 80);
            scoreRect = new Rectangle(2*x + slotTex.Width, y+125, slotTex.Width, 80);

            var wordSlot = new Slot(wordTex, spriteBatch)
            {
                Position = new Vector2(x + x * 2 + slotTex.Width * 2, y),
                name = "Word"
            };

            currentWordStack = table.AddStack(wordSlot, StackType.hand, StackMethod.horizontal);


            clearRect = new Rectangle(wordSlot.Border.Right - clearTex.Width, wordSlot.Border.Top, clearTex.Width, clearTex.Height);

            addWordRect = new Rectangle((int)wordSlot.Position.X + wordTex.Width + x, y + (int) ((wordTex.Height - addWordTex.Height) * 0.5), addWordTex.Width, addWordTex.Height);


            y += slotTex.Height + y;
                       
            for (int i = 0; i < numStacks; i++)
            {

                var newSlot = new Slot(slotTex, spriteBatch)
                {
                    Position = new Vector2(x + x * i + slotTex.Width * i, y),
                    name = "Stack " + i
                };

                stackPositions.Add(newSlot.Position);
                var newStack = table.AddStack(newSlot, StackType.stack, StackMethod.vertical);

                // add crunch for these stacks
                newStack.crunchItems = 24;

                
            }
            
        }

        

        public void SetTable()
        {

            foreach (var stack in table.stacks)
            {
                stack.Clear();
            }

            drawPile.freshDeck();
            drawPile.shuffle();
            drawPile.UpdatePositions();
            InvalidWords = 0;
            quitGame = false;

            completedWords = new List<CompletedWord>();

            foreach (var card in drawPile.cards)
            {
                dragonDrop.Add(card);

                card.Selected += OnCardSelected;
                card.Collusion += OnCollusion;
                //card.Save += Save;
                card.stack = drawPile;

                var location = "deck/" + card.rank;
                card.SetTexture(Content.Load<Texture2D>(location));
            }



            drawSlot.stack = drawPile;


            int x = 10;
            int y = 10;
            y += slotTex.Height + y;

            for (var i = 0; i < numStacks; i++)
            {

                //var newX = x + x * i + slotTex.Width * i;
                
                var pos = stackPositions[i];
                var moveCard = drawPile.drawCard();
                //moveCard.Position = new Vector2(pos.X, 0 - moveCard.Texture.Height);
                moveCard.Position = drawSlot.Position;

                if (i == 0)
                {
                    moveCard.isFaceUp = true;
                    tween.Tween(moveCard, new { Position = pos }, 7, 40)
                        .OnComplete(afterTween)
                        .Ease(Ease.CubeOut);
                }
                else
                {

                    var delay = 3f + i * 2.5f;

                    tween.Tween(moveCard, new { Position = pos }, 5, delay)
                        .Ease(Ease.CubeOut)
                        .OnComplete(afterTween);
                }

                moveCard.snapPosition = pos;
                moveCard.IsDraggable = false;

                table.stacks[i + 2].addCard(moveCard);

                for (var j = 1; j < i + 1; j++)
                {

                    moveCard = drawPile.drawCard();
                    
                    moveCard.snapPosition = new Vector2(pos.X, pos.Y + stackOffsetVertical * j);
                    //moveCard.Position = new Vector2(pos.X, 0 - moveCard.Texture.Height);
                    moveCard.Position = drawSlot.Position;
                    if (j == i)
                    {

                        tween.Tween(moveCard, new { Position = moveCard.snapPosition }, 7, 40)
                            .OnBegin(() => setFaceUp(moveCard))
                            .OnComplete(afterTween)
                            .Ease(Ease.CubeOut);
                    }
                    else
                    {

                        var delay = 3f + i * 2.5f + j * 2.5f;

                        tween.Tween(moveCard, new { Position = moveCard.snapPosition }, 5, delay)
                            .Ease(Ease.CubeOut)
                            .OnComplete(afterTween);
                    }

                    moveCard.IsDraggable = false;
                    table.stacks[i + 2].addCard(moveCard);
                }
            }


            var restackAnimation = drawPile.topCard();
            restackAnimation.Position += new Vector2(stackOffsetHorizontal * 2, 0);
            restackAnimation.isSnapAnimating = true;
            restackAnimation.snapTime = 4.5f;
            if (!muteSound) soundFX[playSound].Play();




            isSetup = true;

        }

        private void setFaceUp(Card c)
        {
            c.isFaceUp = true;
        }

        private void addAndSetFaceUp(Card c, Stack s)
        {
            if (s.Count > 0)
                s.topCard().isFaceUp = false;
            s.addCard(c, true);            
            c.isFaceUp = true;            
        }

        private void afterTween()
        {

            animationCount--;
            // todo cleanup this sound stuff

            if (!muteSound) soundFX[playSound].Play();
        }

        private void afterTween(Card card)
        {
            card.flipCard();
            card.snapPosition = card.Position;
            card.IsDraggable = true;
            card.snapTime = .7f;
            //discardPile.addCard(card);
            animationCount--;
            if (!muteSound) soundFX[playSound].Play();
        }

        private void afterTween(Card card, Card destinationCard)
        {

            card.ZIndex -= ON_TOP;
            card.isSnapAnimating = false;
            card.SetParent(destinationCard);
            if (!muteSound) soundFX[playSound].Play(.3f, 0, 0);
            animationCount = 0;
        }

        private void afterTween(Card card, Stack stack)
        {
            card.ZIndex -= ON_TOP;
            card.isSnapAnimating = false;
            card.allowsSelection = true;    
            if (stack.Count == 0)
                card.MoveToEmptyStack(stack);
            else
                card.SetParent(stack.cards[stack.Count - 1]);
            if (!muteSound) soundFX[playSound].Play(.3f, 0, 0);
            animationCount = 0;
        }

        private void afterTweenPlayCardToEmptyStack(Card card, Stack stack)
        {
            afterTween(card, stack);
            if (card.previousStack.Count > 0)
                card.previousStack.topCard().isFaceUp = true;
        }

        private void afterTweenReturn(Card card, Stack stack, Stack currentWordStack)
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
            throw new NotImplementedException();
        }

        private void OnCardSelected(object sender, EventArgs e)
        {
            throw new NotImplementedException();
        }

        
        
        //private const double DELAY = 500;

        private bool dblTap = false;
        public void Update(GameTime gameTime)
        {

            


#if false
            debugRect = new Rectangle(310, 140, debug.Width, debug.Height);
            debugColor = Color.White;
            if (debugRect.Contains(point))
            {
                debugColor = Color.Aqua;
                if (click) foreach (var stack in table.stacks) stack.debug();

                if (click) Debug.WriteLine("Total Score:" + TotalScore);
            }
#endif
            addWordColor = (currentWordStack.Count > 1) ? Color.White : Color.DarkGray;        

            while (TouchPanel.IsGestureAvailable)
            {
                var gesture = TouchPanel.ReadGesture();
                
                var point = gesture.Position.ToPoint();

                point = mainGame.viewPort.PointToScreen(point);
                Debug.WriteLine("Gesture:" + gesture.GestureType + ":" + point.ToString());

                switch (gesture.GestureType)
                {
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
                            if (addWordRect.Contains(point) && currentWordStack.Count > 1)
                            {
                                PlayAddWord();
                                break;
                            }

                            if (drawSlot.Contains(point.ToVector2()) && haveMoreCards)
                            {
                                DealMoreCards();
                                break;
                            }

                            if (clearRect.Contains(point))
                            {
                                ClearCards();
                            }

                            // check if it was on one of the table stacks
                            foreach (var stack in table.stacks)
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
                                                    PlayTopCard(topCard);
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
                        }
                        break;
                    case GestureType.DoubleTap:
                        if (!isGameOver)
                        {
                            var emptyStack = GetEmptyStack();
                            if (emptyStack != null)
                            {
                                foreach (var stack in table.stacks)
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
            table.Update(gameTime);
        }

        private Stack GetEmptyStack()
        {
            Stack ret = null;
            foreach (var stack in table.stacks)
            {
                if ((stack.type == StackType.stack) && stack.Count == 0)
                    return stack;
            }

            return ret;
        }

        private void DealMoreCards()
        {
            
            int delayForClear = (currentWordStack.Count > 0) ? 5 : 0;
            ClearCards();

            for (int i = 0; i < numStacks; i++)
            {
                var moveCard = drawPile.drawCard();
                var stack = table.stacks[i + 2];


                moveCard.snapPosition = new Vector2(stack.slot.Position.X, stack.slot.Position.Y + (stackOffsetVertical * (stack.Count+1)));
                // stack.addCard(moveCard, true);
                moveCard.Position = drawSlot.Position;
                tween.Tween(moveCard, new { Position = moveCard.snapPosition }, 7, delayForClear)
                    .OnBegin(() => addAndSetFaceUp(moveCard, stack))
                    .OnComplete(afterTween)
                    .Ease(Ease.CubeOut);
                    
            }        
        }

        private void PlayAddWord()
        {
            int cardCount = 0;
            bool checkWord = mainGame.validWords.IsWordValid(CurrentWord);

            if (checkWord)
            {
                foreach (var card in currentWordStack.cards)
                {
                    if (!isAnimating)
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
                foreach (var stack in table.stacks)
                {
                    if (stack.Count > 0)
                    {
                        var topCard = stack.topCard();

                        if (stack.type == StackType.stack && !(topCard.isFaceUp))
                        {
                            topCard.isFaceUp = true;
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
                .OnComplete(() => afterTweenReturn(card, stack, currentWordStack));
            
        }

        private void PlayTopCard(Card topCard)
        {
            Vector2 pos = currentWordStack.slot.Position;
            pos.X += currentWordStack.Count * stackOffsetHorizontal;
            
            if (true)
            {
                animationCount++;
                topCard.ZIndex += ON_TOP;

                topCard.previousStack = topCard.stack;
                topCard.allowsSelection = false;
                tween.Tween(topCard, new { Position = pos }, 3)
                    .Ease(Ease.CubeInOut)
                    .OnComplete(() => afterTween(topCard, currentWordStack));
            }        
        }

        private void PlayCardToEmptyStack(Card topCard, Stack emptyStack)
        {
            Vector2 pos = emptyStack.slot.Position;            

            if (true)
            {
                animationCount++;
                topCard.ZIndex += ON_TOP;

                topCard.previousStack = topCard.stack;
                topCard.allowsSelection = false;
                tween.Tween(topCard, new { Position = pos }, 3)
                    .Ease(Ease.CubeInOut)
                    .OnComplete(() => afterTweenPlayCardToEmptyStack(topCard, emptyStack));
            }
        }

        public void Draw(GameTime gameTime)
        {
            foreach (var stack in table.stacks)
            {
                var slot = stack.slot;
                //var textWidth = debugFont.MeasureString(slot.name);
                //var textPos = new Vector2(slot.Position.X + slot.Texture.Width / 2 - textWidth.X / 2, slot.Position.Y - 16);
                //    spriteBatch.DrawString(debugFont, slot.name, textPos, Color.Black);
            }
#if false
            spriteBatch.Draw(debug, debugRect, debugColor);
#endif
            
            table.Draw(gameTime);
            spriteBatch.Draw(addWordTex, addWordRect, addWordColor);

            Util.DrawString(spriteBatch, font, "Score", scoreLabelRect, Util.Alignment.Center, Color.White);
            Util.DrawString(spriteBatch, font, TotalScore.ToString(), scoreRect, Util.Alignment.Center, Color.White);

            if (CurrentWord.Length > 0)
            {               
                spriteBatch.Draw(clearTex, clearRect, Color.White);
            }


            for (var i = 0; i < InvalidWords; i++)
                spriteBatch.Draw(wrongWordTex, new Rectangle(addWordRect.Right + ((i + 1) * 10) + i * wrongWordTex.Width, addWordRect.Center.Y - (int) (wrongWordTex.Height /2), wrongWordTex.Width, wrongWordTex.Height), Color.White);

            if (!haveMoreCards)
                spriteBatch.Draw(noCardsTex, drawSlot.Border, Color.White);

            if (curWordScoreImage != null)
            {
                string word = mainGame.validWords.IsWordValid(CurrentWord) ? "+ " : "- ";
                word += CurrentWordScore.ToString() + " " + CurrentWord;
                
                spriteBatch.DrawString(font, word, curWordScoreImage.Position, CurrentWordScore > 0 ? Color.LightGreen : Color.Red);
                
                if (CurrentWordBonus > 0)
                {
                    var pos = curWordScoreImage.Position;
                    pos.Y += 40;
                    spriteBatch.DrawString(font, "+ " + CurrentWordBonus.ToString() + " length bonus", pos, Color.LightGreen);
                }
            }
            
            if (isGameOver)
            {
                spriteBatch.Draw(mainGame.dimScreen, new Rectangle(0, 0, 1980, 1020), Color.White * 0.8f);
                spriteBatch.Draw(gameOverTex, gameOverRect, Color.White);

                var gameOverTextRect = new Rectangle(0,  175, mainGame.MID_WIDTH - 20, 150);
                var gameOverScoresTextRect = new Rectangle(mainGame.MID_WIDTH + 20, 175, 600, 150);

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
                
            }

            spriteBatch.Draw(exitTex, exitRect, Color.White);

            //if (table.gameState == GameState.won) confetti.Draw(spriteBatch);

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
