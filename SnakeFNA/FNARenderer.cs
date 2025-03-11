using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SnakeCore;

namespace SnakeFNA
{
    internal class FNARenderer : IRenderer<Texture2D>
    {
        private bool _beginCalled;
        private readonly GraphicsDevice _graphicsDevice;
        private readonly BasicEffect _effect;
        private SpriteBatch _spriteBatch;
        private Vector2 _cameraPosition;
        private float _cameraRotation;
        private float _cameraZoom = 1.0f;

        public FNARenderer(GraphicsDevice graphicsDevice)
        {
            _graphicsDevice = graphicsDevice;
            _effect = new BasicEffect(graphicsDevice) 
            {
                VertexColorEnabled = true,
            };
            _spriteBatch = new SpriteBatch(graphicsDevice);
        }

        public void Begin()
        {
            if(_beginCalled)
            {
                throw new InvalidOperationException(
					"Begin has been called before calling End" +
					" after the last call to Begin." +
					" Begin cannot be called again until" +
					" End has been successfully called."
				);
            }

            _beginCalled = true;

            // Get screen center for zoom origin
            var viewport = _graphicsDevice.Viewport;
            var screenCenter = new Vector2(viewport.Width / 2f, viewport.Height / 2f);

            // Create transform matrix for camera - order matters!
            var transform = Matrix.CreateTranslation(-screenCenter.X, -screenCenter.Y, 0) *  // Move to origin
                           Matrix.CreateScale(_cameraZoom) *                                  // Apply zoom
                           Matrix.CreateRotationZ(_cameraRotation) *                         // Apply rotation
                           Matrix.CreateTranslation(screenCenter.X, screenCenter.Y, 0) *     // Move back
                           Matrix.CreateTranslation(_cameraPosition.X, _cameraPosition.Y, 0); // Apply camera position

            _spriteBatch.Begin(SpriteSortMode.Deferred,
                BlendState.AlphaBlend,
                SamplerState.LinearClamp,
                DepthStencilState.Default,
                RasterizerState.CullCounterClockwise,
                null,
                transform);
        }

        public void End()
        {
            if (!_beginCalled)
            {
                throw new InvalidOperationException(
                    "End was called, but Begin has not yet" +
                    " been called. You must call Begin" +
                    " successfully before you can call End."
                );
            }

            _beginCalled = false;

            _spriteBatch.End();
        }

        public void DrawImage(Texture2D image, System.Numerics.Vector2 position, System.Numerics.Vector2 size, float rotation, System.Numerics.Vector2 origin, System.Drawing.Rectangle sourceRectangle, System.Drawing.Color color)
        {   
            var destinationRectangle = new Rectangle((int)position.X, (int)position.Y, (int)size.X, (int)size.Y);
            var xnaSourceRectangle = new Rectangle(sourceRectangle.X, sourceRectangle.Y, sourceRectangle.Width, sourceRectangle.Height);
            var xnaColor = Color.FromNonPremultiplied(color.R, color.G, color.B, color.A);
            var xnaOrigin = new Vector2(origin.X, origin.Y);
            //xnaOrigin = Vector2.Zero;
            _spriteBatch.Draw(image, destinationRectangle, xnaSourceRectangle, xnaColor, rotation, xnaOrigin, SpriteEffects.None, 0);
        }

        public unsafe Texture2D CreateImage(int width, int height, ReadOnlySpan<byte> data)
        {
            var texture = new Texture2D(_graphicsDevice, width, height, false, SurfaceFormat.Color);
            fixed (byte* p = data) texture.SetDataPointerEXT(0, null, (nint)p, data.Length);

            return texture;
        }

        public void SetCamera(System.Numerics.Vector2 position, float rotation, float zoom)
        {
            _cameraPosition = new Vector2(position.X, position.Y);
            _cameraRotation = rotation;
            _cameraZoom = zoom;
        }

        public System.Numerics.Vector2 ScreenToWorld(System.Numerics.Vector2 screenPosition)
        {
            var viewport = _graphicsDevice.Viewport;
            var screenCenter = new Vector2(viewport.Width / 2f, viewport.Height / 2f);

            var matrix = Matrix.CreateTranslation(-screenCenter.X, -screenCenter.Y, 0) *
                        Matrix.CreateScale(_cameraZoom) *
                        Matrix.CreateRotationZ(_cameraRotation) *
                        Matrix.CreateTranslation(screenCenter.X, screenCenter.Y, 0) *
                        Matrix.CreateTranslation(_cameraPosition.X, _cameraPosition.Y, 0);
            
            matrix = Matrix.Invert(matrix);

            var pos = Vector2.Transform(new Vector2(screenPosition.X, screenPosition.Y), matrix);
            return new System.Numerics.Vector2(pos.X, pos.Y);
        }

        public System.Numerics.Vector2 WorldToScreen(System.Numerics.Vector2 worldPosition)
        {
            var viewport = _graphicsDevice.Viewport;
            var screenCenter = new Vector2(viewport.Width / 2f, viewport.Height / 2f);

            var matrix = Matrix.CreateTranslation(-screenCenter.X, -screenCenter.Y, 0) *
                        Matrix.CreateScale(_cameraZoom) *
                        Matrix.CreateRotationZ(_cameraRotation) *
                        Matrix.CreateTranslation(screenCenter.X, screenCenter.Y, 0) *
                        Matrix.CreateTranslation(_cameraPosition.X, _cameraPosition.Y, 0);

            var pos = Vector2.Transform(new Vector2(worldPosition.X, worldPosition.Y), matrix);
            return new System.Numerics.Vector2(pos.X, pos.Y);
        }
    }
}
