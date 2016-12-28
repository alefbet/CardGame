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

        Texture2D alef, bet, software;
        MoveImage alefRect, betRect, softwareRect;

        

        private Tweener tween = new Tweener();

        public SplashScreen(ContentManager Content, SpriteBatch spriteBatch)
        {
            this.Content = Content;
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
            spriteBatch.Draw(alef, alefRect.Position, Color.White);
            spriteBatch.Draw(bet, betRect.Position, Color.White);
            spriteBatch.Draw(software, softwareRect.Position, Color.White);

        }

        
    }
}
