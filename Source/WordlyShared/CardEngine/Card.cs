﻿/* Attribution (a) 2016 The Ruge Project (http://ruge.metasmug.com/) 
 * Unlicensed under NWO-CS (see UNLICENSE)
 */

using System;
using MonoGame.Ruge.DragonDrop;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Ruge.Glide;
using Microsoft.Xna.Framework.Input.Touch;

namespace MonoGame.Ruge.CardEngine {

    enum FlipState { none, flipIn, flipOut }

    public class Card : IDragonDropItem {        
        
        // Z-Index constants
        protected const int ON_TOP = 1000;

        // it's a tween thing
        private Tweener tween = new Tweener();
        


        private readonly SpriteBatch spriteBatch;
        public CardType cardType;

        public float Scale { get; set; }
        public float Rotation { get; set; }

        private Vector2 _position;
        public Vector2 Position {
            get { return _position; }
            set {
                
                _position = value;
                
                if (Child != null) {
                    
                    var pos = new Vector2(_position.X + stack.offset.X, _position.Y + stack.offset.Y);

                    Child.Position = pos;
                    Child.snapPosition = pos;
                    Child.ZIndex = ZIndex + 1;
                    
                }

            }
        }

        public new string ToString() { return suit.ToString(); }
        
        public Vector2 snapPosition { get; set; }
        public Card Child { get; set; } = null;

        public Rectangle Border => new Rectangle((int) Position.X, (int) Position.Y, Texture.Width, Texture.Height);

        protected Texture2D cardBack, texture;

        public Texture2D Texture => isFaceUp ? texture : cardBack;
        public Texture2D SelectedOverlayTexture;
        public void SetTexture(Texture2D newTexture) => texture = newTexture;

        public Stack stack { get; set; }
        public int ZIndex { get; set; } = 1;

        public string StackName { get { return stack.name; } }
        public Stack previousStack { get; set; }
        /*
        private FlipState flipAnimating = FlipState.none;
        private Rectangle flipRect;
        private int flipWidth;
        */

        // animation stuff
        public bool isSnapAnimating = false;
        private bool startTween = true;
        public bool snap = true;
        public float snapTime = .7f;
        
        public bool allowsSelection = true;

        // this seems hacky, need to come up with a better solution.
        public bool render = true;


        public CardColor color {
            get {

                if (cardType.deckType == DeckType.hex) {

                    switch ((HexSuit) cardType.suit) {

                        case HexSuit.beakers:
                        case HexSuit.planets:
                            return CardColor.black;
                        default:
                            return CardColor.white;
                    }
                }
                if (cardType.deckType == DeckType.playing) {

                    switch ((PlayingSuit) cardType.suit) {

                        case PlayingSuit.diamonds:
                        case PlayingSuit.hearts:
                            return CardColor.red;
                        default:
                            return CardColor.black;
                    }
                }
                if (cardType.deckType == DeckType.friendly) {

                    switch ((FriendlySuit)cardType.suit) {

                        case FriendlySuit.carrots:
                        case FriendlySuit.oranges:
                            return CardColor.orange;
                        default:
                            return CardColor.red;
                    }
                }

                return CardColor.none;
                
            }
        }

        public bool isFaceUp = false;
        public Enum suit;
        public Enum rank;
        public String wordValue;
        public int wordPoints;

        public Card(DeckType deckType, Enum suit, Enum rank, Texture2D cardBack, SpriteBatch spriteBatch) {

            cardType = new CardType(deckType) {
                suit = suit,
                rank = rank
            };

            this.suit = suit;
            this.rank = rank;

            this.spriteBatch = spriteBatch;
            this.cardBack = cardBack;
            Scale = 1f;

            Tween.TweenerImpl.SetLerper<Vector2Lerper>(typeof(Vector2));
            
        }        

        //create a letter based card
        public Card(string letter, Texture2D cardBack, SpriteBatch spriteBatch)
        {            
            foreach (WordRank c in Enum.GetValues(typeof(WordRank)))
            {
                var details = c.ToString().Split('_');
                if (details[0] == letter)
                {
                    cardType = new CardType(DeckType.word)
                    {
                        suit = c,
                        rank = c
                    };

                    this.suit = c;
                    this.rank = c;
                    this.wordPoints = int.Parse(details[2]);
                    this.wordValue = details[0];
                    if (this.wordValue == "JOKER")
                        this.wordValue = ".";

                    this.spriteBatch = spriteBatch;
                    this.cardBack = cardBack;
                    Scale = 1f;
                    
                    Tween.TweenerImpl.SetLerper<Vector2Lerper>(typeof(Vector2));
                    return;
                }
            }
            
        }

        public void flipCard() {
            isFaceUp = !isFaceUp;
            /*
            flipAnimating = FlipState.flipIn;
            flipWidth = texture.Width;
            ZIndex += ON_TOP;
            */
        }


        #region DragonDrop Stuff

        public bool IsSelected { get; set; } = false;
        
        public bool IsMouseOver { get; set; }

        public bool IsDraggable { get; set; } = false;
        public bool Contains(Vector2 pointToCheck) {
            var mouse = new Point((int)pointToCheck.X, (int)pointToCheck.Y);
            return Border.Contains(mouse);
        }

        public void ProcessDrag(Point point, GestureSample gesture, GameTime gameTime)
        {
            switch (gesture.GestureType) {
                case GestureType.FreeDrag:
                case GestureType.HorizontalDrag:
                case GestureType.VerticalDrag:
                    if (IsSelected)
                    {                        
                        Position += gesture.Delta;
                        Update(gameTime);
                    }

                    break;

                case GestureType.DragComplete:
                    Position += gesture.Delta;
                   
                    IsSelected = false;
                    
                    break;
            }
            Update(gameTime);
            
        }

       
        #endregion

        #region MonoGame


        public void Update(GameTime gameTime) {
            
            if (IsSelected) {
                var fixChild = Child;

                while (fixChild != null) {
                    fixChild.ZIndex += ON_TOP;
                    fixChild = fixChild.Child;
                }
            }
            
            if (isSnapAnimating) {

                //                isSnapAnimating = !SnapAnimation();
                if (startTween) { 
                    tween.Tween(this, new { Position = snapPosition }, snapTime)
                        .Ease(Ease.ElasticOut)
                        .OnComplete(afterTween);

                    startTween = false;
                }

            }
            /*
            if (flipAnimating == FlipState.flipIn) {
                flipWidth--;
                if (flipWidth == 0) {
                    flipAnimating = FlipState.flipOut;
                    isFaceUp = !isFaceUp;
                }
                flipRect = new Rectangle((int)Position.X, (int)Position.Y, flipWidth, texture.Height);
            }
            if (flipAnimating == FlipState.flipOut) {
                flipWidth++;
                if (flipWidth == texture.Width) {
                    flipAnimating = FlipState.none;
                    ZIndex -= ON_TOP;
                }
                flipRect = new Rectangle((int)Position.X, (int)Position.Y, flipWidth, texture.Height);
            }
            */

            tween.Update(float.Parse(gameTime.ElapsedGameTime.Seconds + "." + gameTime.ElapsedGameTime.Milliseconds));

        }

        private void afterTween() {

            ZIndex -= ON_TOP;

            if (stack.crunchStacks) stack.UpdatePositions();

            isSnapAnimating = false;
            startTween = true;

        }

        public void Draw(GameTime gameTime) {
            /*if (flipAnimating != FlipState.none) spriteBatch.Draw(Texture, flipRect, Color.White);
            else*/

            if (render)
            {                                
                spriteBatch.Draw(Texture, Position, null, Color.White, Rotation, new Vector2(0,0), Scale,SpriteEffects.None, 0 );
                if (IsSelected && isFaceUp && SelectedOverlayTexture != null)
                    spriteBatch.Draw(SelectedOverlayTexture, Position, null, Color.White, Rotation, new Vector2(0, 0), Scale, SpriteEffects.None, 0);
                

            }
        }

        #endregion

        public void MoveToEmptyStack(Stack newStack) {

            var s = new SaveEvent { stack = stack };
            Save?.Invoke(this, s);

            if (newStack.Count == 0) newStack.addCard(this, true);

        }
        



        public void SetParent(Card parent) {
            
            var s = new SaveEvent { stack = stack };
            Save?.Invoke(this, s);
            
            parent.Child = this;
            parent.stack.addCard(this, true);

        }

        public void InsertAfter(Card parent)
        {

            var s = new SaveEvent { stack = stack };
            Save?.Invoke(this, s);

            parent.Child = this;
            parent.stack.insertCardAfter(this, parent, true);

        }

        #region events

        public event EventHandler Selected;

        public void OnSelected() {

            if (IsDraggable) {
                IsSelected = true;
            }
            ZIndex += ON_TOP;

            Selected?.Invoke(this, EventArgs.Empty);
        }

        public event EventHandler Deselected;

        public void OnDeselected() {
            
            IsSelected = false;

            if (Position != snapPosition) isSnapAnimating = true;

            Deselected?.Invoke(this, EventArgs.Empty);

        }

        public event EventHandler<CollusionEvent> Collusion;

        public void OnCollusion(IDragonDropItem item, Point point) {

            var e = new CollusionEvent {item = item, point = point};

            Collusion?.Invoke(this, e);

        }

        public class CollusionEvent : EventArgs {

            public IDragonDropItem item { get; set; }
            public Point point { get; set; }

        }

        public event EventHandler<SaveEvent> Save;
        public class SaveEvent : EventArgs {
            
            public Stack stack { get; set; }

        }

#endregion

    }
}
