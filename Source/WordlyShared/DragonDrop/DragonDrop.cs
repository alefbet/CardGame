/* Attribution (a) 2016 The Ruge Project (http://ruge.metasmug.com/) 
 * Unlicensed under NWO-CS (see UNLICENSE)
 */

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Input.Touch;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
//using System.Data;
using System.Linq;
using MonoGame.Ruge.ViewportAdapters;
using System.Diagnostics;

namespace MonoGame.Ruge.DragonDrop {

    public class DragonDrop<T> : DrawableGameComponent where T : IDragonDropItem {

        MouseState oldMouse, currentMouse;        

        public readonly ViewportAdapter viewport;

        public T selectedItem;
        public List<T> dragItems;
        public List<T> mouseItems;


        /// <summary>
        /// Constructor. Uses MonoGame.Extended ViewportAdapter
        /// </summary>
        /// <param name="game"></param>
        /// <param name="sb"></param>
        /// <param name="vp"></param>
        public DragonDrop(Game game, ViewportAdapter vp) : base(game) {
            viewport = vp;
            selectedItem = default(T);
            dragItems = new List<T>();
            mouseItems = new List<T>();
        }

        public void Add(T item) {
            dragItems.Add(item);
        }
        public void Remove(T item, GameTime gameTime) { dragItems.Remove(item); item.Update(gameTime); }

        public void Clear() {
            selectedItem = default(T);
            dragItems.Clear();
        }

        private bool click => currentMouse.LeftButton == ButtonState.Pressed && oldMouse.LeftButton == ButtonState.Released;
        private bool unClick => currentMouse.LeftButton == ButtonState.Released && oldMouse.LeftButton == ButtonState.Pressed;
        private bool drag => currentMouse.LeftButton == ButtonState.Pressed;

        private Vector2 CurrentMouse {
            get {

                var point = viewport.PointToScreen(currentMouse.X, currentMouse.Y);

                return new Vector2(point.X, point.Y);

            }
        }

        public void debug()
        {
            Debug.WriteLine("--------DragDropOrder-----");
            

            var items = dragItems.OrderBy(z => z.ZIndex).ToList();
            foreach (var item in items)
            {
                var type = item.GetType();
                string str = "type " + type.Name;
                switch (type.Name)
                {
                    case "Card":
                        var card = (CardEngine.Card) (object) item;
                        var strFaceUp = (card.isFaceUp ? "face up" : "face down");
                        str += " z" + card.ZIndex.ToString("00") + ": " + card.rank + " of " + card.suit + " (" + strFaceUp + ")";
                        str += " pos: (" + card.Position.X + ", " + card.Position.Y + ")";
                        break;
                    case "Slot":
                        var slot  = (CardEngine.Slot)(object) item;                                                
                        str += " name: " + slot.name + " pos: (" + slot.Position.X + ", " + slot.Position.Y + ")";

                        break;
               }
                Debug.WriteLine(str);
                

            }

            
                
                
        }

        public Vector2 OldMouse {
            get {

                var point = viewport.PointToScreen(oldMouse.X, oldMouse.Y);

                return new Vector2(point.X, point.Y);

            }
        }




        public Vector2 Movement => CurrentMouse - OldMouse;


        public T GetCollusionItem(Vector2 point) {

            var items = dragItems.OrderByDescending(z => z.ZIndex).ToList();
            foreach (var item in items) {

                if (item.Contains(point) && !Equals(selectedItem, item)) return item;

            }
         
            // if it doesn't contain the current mouse, run again to see if it intersects
            foreach (var item in items) {

                if (item.Border.Intersects(selectedItem.Border) && !Equals(selectedItem, item)) return item;

            }
            return default(T);

        }

        private T GetMouseHoverItem() {

            var items = dragItems.OrderByDescending(z => z.ZIndex).ToList();

            foreach (var item in items) {

                if (item.Contains(CurrentMouse)) return item;

            }

            return default(T);

        }        


        public override void Update(GameTime gameTime) {


            currentMouse = Mouse.GetState();


            if (selectedItem != null) {

                if (selectedItem.IsSelected) {

                    if (drag) {
                        selectedItem.Position += Movement;
                        selectedItem.Update(gameTime);
                    }
                    else if (unClick) {

                        var collusionItem = GetCollusionItem(CurrentMouse);

                        if (collusionItem != null) {
                            selectedItem.OnCollusion(collusionItem, CurrentMouse.ToPoint());
                            collusionItem.Update(gameTime);
                        }

                        selectedItem.OnDeselected();
                        selectedItem.Update(gameTime);
                        selectedItem = default(T);

                    }
                }

            }


            foreach (var item in dragItems) {
                item.IsMouseOver = false;
                item.Update(gameTime);
            }

            var hoverItem = GetMouseHoverItem();

            if (hoverItem != null) {

                hoverItem.IsMouseOver = true;

                if (hoverItem.IsDraggable && click) {
                    selectedItem = hoverItem;
                    selectedItem.OnSelected();
                }

                hoverItem.Update(gameTime);

            }


            oldMouse = currentMouse;

        }

    }
}
