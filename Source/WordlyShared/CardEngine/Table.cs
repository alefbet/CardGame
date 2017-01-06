/* Attribution (a) 2016 The Ruge Project (http://ruge.metasmug.com/) 
 * Unlicensed under NWO-CS (see UNLICENSE)
 */

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input.Touch;
using MonoGame.Ruge.DragonDrop;
using System.Diagnostics;

namespace MonoGame.Ruge.CardEngine {

    public class Table {

        // Z-Index constants
        protected const int ON_TOP = 1000;
        
        protected int stackOffsetHorizontal, stackOffsetVertical;
        public Texture2D cardBack, slotTex, cardSelectedTex;
        protected SpriteBatch spriteBatch;
        protected DragonDrop<IDragonDropItem> dragonDrop;
        
        public List<Stack> stacks = new List<Stack>();
                
        public Table(SpriteBatch spriteBatch, DragonDrop<IDragonDropItem> dragonDrop, int stackOffsetH, int stackOffsetV, Texture2D cardBack = null, Texture2D slotTex = null) {
            this.spriteBatch = spriteBatch;
            this.dragonDrop = dragonDrop;
            stackOffsetHorizontal = stackOffsetH;
            stackOffsetVertical = stackOffsetV;
            this.cardBack = cardBack;
            this.slotTex = slotTex;
        }


        public Stack AddStack(Slot slot, StackType type = StackType.undefined, StackMethod stackMethod = StackMethod.normal) {

            var stack = new Stack(this, cardBack, slotTex, spriteBatch, stackOffsetHorizontal, stackOffsetVertical) {
                slot = slot,
                method = stackMethod,
                type = type
            };

            slot.stack = stack;

            stacks.Add(stack);

            dragonDrop.Add(slot);

            return stack;

        }

        public void AddStack(Stack stack) {

            foreach (var card in stack.cards) card.stack = stack;

            stack.UpdatePositions();
            stacks.Add(stack);
            
        }
        

        /// <summary>
        /// I recommend only using this for debugging purposes, this is not an ideal way to do things
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public Slot GetSlotByName(string name) {

            Slot slot = null;

            foreach (var stack in stacks) {

                if (stack.name == name) slot = stack.slot;

            }

            return slot;

        }


        /// <summary>
        /// I recommend only using this for debugging purposes, this is not an ideal way to do things
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public Stack GetStackByName(string name) {

            Stack foundStack = null;

            foreach (var stack in stacks) {

                if (stack.name == name) foundStack = stack;

            }

            return foundStack;

        }


        /// <summary>
        /// override this to set up your table
        /// </summary>
        public void SetTable() { }

        /// <summary>
        /// override this to clear the table
        /// </summary>
        public void Clear() { }
        

        public void Update(GameTime gameTime) {
            foreach (var stack in stacks) stack.Update(gameTime);

            // fixes the z-ordering stuff
            var items = dragonDrop.dragItems.OrderBy(z => z.ZIndex).ToList();
            foreach (var item in items) {
                var type = item.GetType();
                if (type == typeof(Card)) item.Update(gameTime);
            }
        }

        public void Draw(GameTime gameTime)
        {

            foreach (var stack in stacks) stack.Draw(gameTime);

            // fixes the z-ordering stuff
            var items = dragonDrop.dragItems.OrderBy(z => z.ZIndex).ToList();
            foreach (var item in items)
            {
                var type = item.GetType();
                if (type == typeof(Card)) item.Draw(gameTime);
            }
        }


        public void debug()
        {
            foreach (var stack in stacks)
                stack.debug();
        }

        public void ProcessDrag(Point point, GestureSample gesture, GameTime gameTime)
        {

            var items = dragonDrop.dragItems.OrderBy(z => z.ZIndex).ToList();
            
            switch (gesture.GestureType)
            {
                case GestureType.FreeDrag:
                case GestureType.HorizontalDrag:
                case GestureType.VerticalDrag:
                    if (dragonDrop.selectedItem != null)
                    {
                        dragonDrop.selectedItem.ProcessDrag(point, gesture, gameTime);                        
                    }
                    else
                    {
                        foreach (var item in items)
                        {
                            var type = item.GetType();
                            if (type == typeof(Card) && item.Contains(point.ToVector2()) && item.IsDraggable)
                            {
                                item.OnSelected();
                                item.ZIndex += ON_TOP;
                                dragonDrop.selectedItem = item;
                                item.ProcessDrag(point, gesture, gameTime);
                                break;
                            }
                        }
                    }

                    break;

                case GestureType.DragComplete:                    
                    if (dragonDrop.selectedItem != null)
                    {
                        var collusionItem = dragonDrop.GetCollusionItem(point.ToVector2());

                        if (collusionItem != null)
                        {
                            dragonDrop.selectedItem.OnCollusion(collusionItem, point);
                            collusionItem.Update(gameTime);
                        }

                        
                        dragonDrop.selectedItem.OnDeselected();
                        dragonDrop.selectedItem.Update(gameTime);

                        //dragonDrop.selectedItem.ProcessFreeDrag(point, gesture, gameTime);
                        dragonDrop.selectedItem = null;
                    }                    
                    break;
            }

            
        }
    }
    

}
