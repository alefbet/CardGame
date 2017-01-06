using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Content;
using MonoGame.Ruge.Glide;
using System.Threading.Tasks;
using Microsoft.Xna.Framework.Input.Touch;
using System.Diagnostics;


namespace WordGame
{
    public class MainScreen
    {
               
        ContentManager Content;
        SpriteBatch spriteBatch;
        MainGame mainGame;

        
                

        Texture2D solitaire, timedSolitaire, multiplayer, settings, sound, soundOff, info;
        Rectangle solitaireRect, timedSolitaireRect, multiplayerRect, settingsRect, soundRect, infoRect;



        private Tweener tween = new Tweener();

        public MainScreen(ContentManager Content, SpriteBatch spriteBatch, MainGame game)
        {
            this.Content = Content;
            this.spriteBatch = spriteBatch;
            this.mainGame = game;
            LoadContent();
        }

        public void LoadContent()
        {
            solitaire = Content.Load<Texture2D>("mainscreen/solitaire");
            solitaireRect = new Rectangle(100, 300, 500, 500);
            

            timedSolitaire = Content.Load<Texture2D>("mainscreen/solitaire");
            timedSolitaireRect = new Rectangle(740, 300, 500, 500);
            

            multiplayer = Content.Load<Texture2D>("mainscreen/multiplayer");
            multiplayerRect = new Rectangle(1340,300, 500, 500);
            

            sound = Content.Load<Texture2D>("mainscreen/speaker");
            soundOff = Content.Load<Texture2D>("mainscreen/speaker-off");
            soundRect = new Rectangle(800, 900, 24, 24);


            


            //Tween.TweenerImpl.SetLerper<Vector2Lerper>(typeof(Vector2));
            //tween.Tween(alefRect, new { Position = new Vector2(1920 / 2 - 100, 800) }, 5, 5);
            //tween.Tween(betRect, new { Position = new Vector2(1920 / 2 - 100, 800) }, 5, 10);
            //tween.Tween(softwareRect, new { Position = new Vector2(1920 / 2 - 100, 800) }, 3, 15).OnComplete(splashScreenEnd); ;            
        }



        public void Update(GameTime gameTime)
        {
            while (TouchPanel.IsGestureAvailable)
            {
                var gesture = TouchPanel.ReadGesture();

                var point = gesture.Position.ToPoint();

                point = mainGame.viewPort.PointToScreen(point);
                Debug.WriteLine("Gesture:" + gesture.GestureType + ":" + point.ToString());

                switch (gesture.GestureType)
                {

                    case GestureType.Tap:
                        if (solitaireRect.Contains(point))
                            mainGame.currentAppState = MainGame.AppState.SolitaireGame;
                        else if (timedSolitaireRect.Contains(point))
                            mainGame.currentAppState = MainGame.AppState.TimedSolitaireGame;
                        else if (multiplayerRect.Contains(point))
                            mainGame.currentAppState = MainGame.AppState.MultiPlayerGame;
                            
                        break;
                }
            }
        }

        public void Draw(GameTime time)
        {
            spriteBatch.Draw(solitaire, solitaireRect, Color.White);
            spriteBatch.Draw(timedSolitaire, timedSolitaireRect, Color.White);
            spriteBatch.Draw(multiplayer, multiplayerRect, Color.White);
            spriteBatch.Draw(sound, soundRect, Color.White);


        }

        
    }
}
