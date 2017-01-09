using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace WordGame
{
    class Pie2D
    {

        private float rotation = 0.0f;
        public float Rotation
        {
            get { return rotation; }
            set { rotation = value; }
        }

        private bool isCentered = true;
        /// <summary>
        /// If true, the pie will be centered
        /// </summary>
        public bool IsCentered
        {
            get { return isCentered; }
            set { isCentered = value; }
        }

        private Vector2 position;
        public Vector2 Position
        {
            get { return position; }
            set { position = value; }
        }

        private float radius = 10.0f;
        /// <summary>
        /// Radius of the Pie, in pixels
        /// </summary>
        public float Radius
        {
            get { return radius; }
            set { radius = value; }
        }

        private float angle;
        /// <summary>
        /// Angle of the Pie
        /// </summary>
        public float Angle
        {
            get { return angle; }
            set
            {
                angle = value;
                RebuildVertices();
            }
        }

        private int nrTriangles;
        /// <summary>
        /// Controls the number of edges on the arc
        /// </summary>
        public int Tesselation
        {
            get { return nrTriangles; }
            set
            {
                nrTriangles = Math.Max(value, 1);
                RebuildVertices();
            }
        }




        Game game;
        BasicEffect be;
        Matrix projectionMatrix;
        Matrix viewMatrix;

        /// <summary>
        /// Creates a new Pie2D class
        /// </summary>
        /// <param name="game">Reference to the game class</param>
        /// <param name="radius">Radius of the Pie</param>
        /// <param name="angle">Angle of the Pie arc</param>
        /// <param name="tesselation">Nr of triangles on the arc</param>
        /// <param name="centered">Centers the Pie around the facing direction.</param>
        public Pie2D(Game game, float radius, float angle, int tesselation, bool centered)
        {
            this.game = game;
            this.radius = radius;
            this.angle = angle;
            this.nrTriangles = tesselation;
            this.isCentered = centered;
        }


        VertexPositionColor[] vertices;
        public void LoadContent()
        {
            viewMatrix = Matrix.CreateLookAt(new Vector3(0, 0, 1), Vector3.Zero, new Vector3(0, 1, 0));

            PresentationParameters pp = game.GraphicsDevice.PresentationParameters;
            projectionMatrix = Matrix.CreateOrthographicOffCenter(0, pp.BackBufferWidth, pp.BackBufferHeight, 0, 1, 50);
            be = new BasicEffect(game.GraphicsDevice);
                        
            RebuildVertices();
        }

        private void RebuildVertices()
        {
            List<VertexPositionColor> verts = new List<VertexPositionColor>();
            int max = nrTriangles;
            for (int i = 0; i < nrTriangles * 2; i++)
            {
                float ang = Lerp(0, max, 0, angle, i);
                verts.Add(new VertexPositionColor(new Vector3((float)Math.Cos(ang), (float)Math.Sin(ang), 0), Color.Red));
                verts.Add(new VertexPositionColor(Vector3.Zero, Color.Red));
            }
            verts.Add(new VertexPositionColor(new Vector3((float)Math.Cos(angle), (float)Math.Sin(angle), 0), Color.Red));
            vertices = verts.ToArray();
        }
        public static float Lerp(float x0, float x1, float y0, float y1, float x2)
        {
            return y0 * (x2 - x1) / (x0 - x1) + y1 * (x2 - x0) / (x1 - x0);
        }

        private RasterizerState rs = new RasterizerState()
                                         {
                                             CullMode = CullMode.None,
                                             FillMode = FillMode.Solid
                                         };

        public void Draw()
        {
            GraphicsDevice device = game.GraphicsDevice;

            if (isCentered)
                be.World = Matrix.CreateScale(radius) * Matrix.CreateRotationZ(rotation - angle / 2.0f) * Matrix.CreateTranslation(new Vector3(position, 0));
            else
                be.World = Matrix.CreateScale(radius) * Matrix.CreateRotationZ(rotation) * Matrix.CreateTranslation(new Vector3(position, 0));

            device.RasterizerState = rs;
            
            be.View = viewMatrix;
            be.Projection = projectionMatrix;
            be.VertexColorEnabled = true;
            be.LightingEnabled = false;
            foreach (EffectPass pass in be.CurrentTechnique.Passes)
            {
                pass.Apply();
                device.DrawUserPrimitives<VertexPositionColor>(PrimitiveType.TriangleStrip, vertices, 0, nrTriangles * 2);
            }
        }

    }
}
