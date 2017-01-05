using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
#if WINDOWS_UAP
using Windows.Web.Http;
#else
using System.Net;
#endif
using System.Text;
using System.Threading.Tasks;

namespace WordGame
{
    public class MoveImage
    {
        public Vector2 Position { get; set; }

        public MoveImage (float x, float y)
        {
            Position = new Vector2(x, y);
        }
    }

    public class Util
    {
        public enum Alignment { Center = 0, Left = 1, Right = 2, Top = 4, Bottom = 8 }

        public static void DrawString(SpriteBatch spriteBatch, SpriteFont font, string text, Rectangle bounds, Alignment align, Color color)
        {
            Vector2 size = font.MeasureString(text);
            Vector2 pos = bounds.Center.ToVector2();
            Vector2 origin = size * 0.5f;

            if (align.HasFlag(Alignment.Left))
                origin.X += bounds.Width / 2 - size.X / 2;

            if (align.HasFlag(Alignment.Right))
                origin.X -= bounds.Width / 2 - size.X / 2;

            if (align.HasFlag(Alignment.Top))
                origin.Y += bounds.Height / 2 - size.Y / 2;

            if (align.HasFlag(Alignment.Bottom))
                origin.Y -= bounds.Height / 2 - size.Y / 2;

            spriteBatch.DrawString(font, text, pos, color, 0, origin, 1, SpriteEffects.None, 0);
        }
        
        public async static Task<Stream> GetWebImageAsStream(string uri)
        {

#if WINDOWS_UAP
            HttpClient client = new HttpClient();
            Stream webStream = (Stream) await client.GetInputStreamAsync(new Uri(uri));
#else
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(uri);            
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            Stream webStream = response.GetResponseStream();
#endif
            Stream cachedStream = CacheStream(webStream);

            return cachedStream;
        }


        static Stream CacheStream(Stream stream)
        {
            Stream cachedStream = new MemoryStream();

            const int bufferSize = 1500;
            byte[]buffer = new byte[bufferSize];

            int read = 0;

            while ((read = stream.Read(buffer, 0, bufferSize)) != 0)
            {
                cachedStream.Write(buffer, 0, read);
            }

            cachedStream.Seek(0, SeekOrigin.Begin);

            return cachedStream;
        }
    }

    

public static class SpriteBatchExtensions
    {
        public static void DrawRoundedRect(this SpriteBatch spriteBatch, Rectangle destinationRectangle,
            Texture2D texture, int border, Color color)
        {
            // Top left
            spriteBatch.Draw(
                texture,
                new Rectangle(destinationRectangle.Location, new Point(border)),
                new Rectangle(0, 0, border, border),
                color);

            // Top
            spriteBatch.Draw(
                texture,
                new Rectangle(destinationRectangle.Location + new Point(border, 0),
                    new Point(destinationRectangle.Width - border * 2, border)),
                new Rectangle(border, 0, texture.Width - border * 2, border),
                color);

            // Top right
            spriteBatch.Draw(
                texture,
                new Rectangle(destinationRectangle.Location + new Point(destinationRectangle.Width - border, 0), new Point(border)),
                new Rectangle(texture.Width - border, 0, border, border),
                color);

            // Middle left
            spriteBatch.Draw(
                texture,
                new Rectangle(destinationRectangle.Location + new Point(0, border), new Point(border, destinationRectangle.Height - border * 2)),
                new Rectangle(0, border, border, texture.Height - border * 2),
                color);

            // Middle
            spriteBatch.Draw(
                texture,
                new Rectangle(destinationRectangle.Location + new Point(border), destinationRectangle.Size - new Point(border * 2)),
                new Rectangle(border, border, texture.Width - border * 2, texture.Height - border * 2),
                color);

            // Middle right
            spriteBatch.Draw(
                texture,
                new Rectangle(destinationRectangle.Location + new Point(destinationRectangle.Width - border, border),
                    new Point(border, destinationRectangle.Height - border * 2)),
                new Rectangle(texture.Width - border, border, border, texture.Height - border * 2),
                color);

            // Bottom left
            spriteBatch.Draw(
                texture,
                new Rectangle(destinationRectangle.Location + new Point(0, destinationRectangle.Height - border), new Point(border)),
                new Rectangle(0, texture.Height - border, border, border),
                color);

            // Bottom
            spriteBatch.Draw(
                texture,
                new Rectangle(destinationRectangle.Location + new Point(border, destinationRectangle.Height - border),
                    new Point(destinationRectangle.Width - border * 2, border)),
                new Rectangle(border, texture.Height - border, texture.Width - border * 2, border),
                color);

            // Bottom right
            spriteBatch.Draw(
                texture,
                new Rectangle(destinationRectangle.Location + destinationRectangle.Size - new Point(border), new Point(border)),
                new Rectangle(texture.Width - border, texture.Height - border, border, border),
                color);
        }
    }
}
