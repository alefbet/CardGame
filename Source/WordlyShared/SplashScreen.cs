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

namespace WordGame
{
    public class SplashScreen
    {

        ContentManager Content;
        SpriteBatch spriteBatch;
          
        public event EventHandler SplashScreenEnded;

        Texture2D alef, bet, software, logo;
        MoveImage alefRect, betRect, softwareRect;
        Rectangle logoRect;
        MainGame mainGame;



        private Tweener tween = new Tweener();

        public SplashScreen(ContentManager Content, SpriteBatch spriteBatch, MainGame game)
        {
            this.Content = Content;
            this.mainGame = game;
            this.spriteBatch = spriteBatch;
            LoadContent();
        }

        public void LoadContent()
        {
            alef = Content.Load<Texture2D>("splashscreen/alef");
            alefRect = new MoveImage(-200, 800);
            

            bet = Content.Load<Texture2D>("splashscreen/bet");
            betRect = new MoveImage(1920,800);
            

            software = Content.Load<Texture2D>("splashscreen/software");
            softwareRect = new MoveImage(1920/2-100, 1080);

            logo = Content.Load<Texture2D>("mainscreen/wordily");
            logoRect = new Rectangle(mainGame.MID_WIDTH -logo.Width/ 2, 20, logo.Width, logo.Height);

            Tween.TweenerImpl.SetLerper<Vector2Lerper>(typeof(Vector2));
            tween.Tween(alefRect, new { Position = new Vector2(1920 / 2 - 100, 800) }, 5, 5);
            tween.Tween(betRect, new { Position = new Vector2(1920 / 2 - 100, 800) }, 5, 10);
            tween.Tween(softwareRect, new { Position = new Vector2(1920 / 2 - 100, 800) }, 3, 15).OnComplete(splashScreenEnd); ;            
        }

        private async void splashScreenEnd()
        {
            await Task.Delay(TimeSpan.FromSeconds(1));
            SplashScreenEnded(this, null);
        }

        public void Update(GameTime gameTime)
        {
            tween.Update(float.Parse(gameTime.ElapsedGameTime.Seconds + "." + gameTime.ElapsedGameTime.Milliseconds));
        }

        public void Draw(GameTime time)
        {
            spriteBatch.Draw(logo, logoRect, Color.White);
            spriteBatch.Draw(alef, alefRect.Position, Color.White);
            spriteBatch.Draw(bet, betRect.Position, Color.White);
            spriteBatch.Draw(software, softwareRect.Position, Color.White);

        }

        
    }
}
