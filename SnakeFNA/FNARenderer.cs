using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SnakeCore;
using System.Drawing;

namespace SnakeFNA
{
    internal class FNARenderer : IRenderer<Texture2D>
    {
        private bool _beginCalled;
        private readonly GraphicsDevice _graphicsDevice;
        private readonly BasicEffect _effect;
        private SpriteBatch _spriteBatch;
        
        // Camera properties implementing ICamera
        public System.Numerics.Vector2 Position { get; set; }
        public float Rotation { get; set; }
        public float Zoom { get; set; } = 0.78f;
        
        private class FontAtlas
        {
            public Texture2D Texture;
            public int AtlasWidth;
            public int AtlasHeight;
            public StbTrueTypeSharp.StbTrueType.stbtt_packedchar[] Chars;
            public float FontSize;
        }
        private FontAtlas _fontAtlas;
        private void EnsureFontAtlas()
        {
            if (_fontAtlas != null)
                return;


            // Font settings
            float fontSize = 12f;
            int firstChar = 32;
            int numChars = 95; // ASCII 32-126
            int atlasWidth = 512;
            int atlasHeight = 64;

            var chars = new StbTrueTypeSharp.StbTrueType.stbtt_packedchar[numChars];
            var atlasBitmap = new byte[atlasWidth * atlasHeight];

            unsafe
            {
                fixed (byte* fontPtr = SnakeCore.Resource.font)
                fixed (byte* atlasPtr = atlasBitmap)
                fixed (StbTrueTypeSharp.StbTrueType.stbtt_packedchar* charsPtr = chars)
                {
                    StbTrueTypeSharp.StbTrueType.stbtt_pack_context context = new();
                    StbTrueTypeSharp.StbTrueType.stbtt_PackBegin(context, atlasPtr, atlasWidth, atlasHeight, 0, 1, null);
                    StbTrueTypeSharp.StbTrueType.stbtt_PackFontRange(context, fontPtr, 0, fontSize, firstChar, numChars, charsPtr);
                    StbTrueTypeSharp.StbTrueType.stbtt_PackEnd(context);
                }
            }

            // Convert 8-bit alpha to RGBA
            byte[] rgba = new byte[atlasWidth * atlasHeight * 4];
            for (int i = 0; i < atlasWidth * atlasHeight; i++)
            {
                byte a = atlasBitmap[i];
                rgba[i * 4 + 0] = 255;
                rgba[i * 4 + 1] = 255;
                rgba[i * 4 + 2] = 255;
                rgba[i * 4 + 3] = a;
            }

            var tex = ((IRenderer<Texture2D>)this).CreateImage(atlasWidth, atlasHeight, rgba);
            _fontAtlas = new FontAtlas
            {
                Texture = tex,
                AtlasWidth = atlasWidth,
                AtlasHeight = atlasHeight,
                Chars = chars,
                FontSize = fontSize
            };
        }

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
            if (_beginCalled)
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
                           Matrix.CreateScale(Zoom) *                                  // Apply zoom
                           Matrix.CreateRotationZ(Rotation) *                         // Apply rotation
                           Matrix.CreateTranslation(screenCenter.X, screenCenter.Y, 0) *     // Move back
                           Matrix.CreateTranslation(Position.X, Position.Y, 0); // Apply camera position

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
            var destinationRectangle = new Microsoft.Xna.Framework.Rectangle((int)position.X, (int)position.Y, (int)size.X, (int)size.Y);
            var xnaSourceRectangle = new Microsoft.Xna.Framework.Rectangle(sourceRectangle.X, sourceRectangle.Y, sourceRectangle.Width, sourceRectangle.Height);
            var xnaColor = Microsoft.Xna.Framework.Color.FromNonPremultiplied(color.R, color.G, color.B, color.A);
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
            Position = position;
            Rotation = rotation;
            Zoom = zoom;
        }

        public System.Numerics.Vector2 ScreenToWorld(System.Numerics.Vector2 screenPosition)
        {
            var viewport = _graphicsDevice.Viewport;
            var screenCenter = new Vector2(viewport.Width / 2f, viewport.Height / 2f);

            var matrix = Matrix.CreateTranslation(-screenCenter.X, -screenCenter.Y, 0) *
                        Matrix.CreateScale(Zoom) *
                        Matrix.CreateRotationZ(Rotation) *
                        Matrix.CreateTranslation(screenCenter.X, screenCenter.Y, 0) *
                        Matrix.CreateTranslation(Position.X, Position.Y, 0);

            matrix = Matrix.Invert(matrix);

            var pos = Vector2.Transform(new Vector2(screenPosition.X, screenPosition.Y), matrix);
            return new System.Numerics.Vector2(pos.X, pos.Y);
        }

        public System.Numerics.Vector2 WorldToScreen(System.Numerics.Vector2 worldPosition)
        {
            var viewport = _graphicsDevice.Viewport;
            var screenCenter = new Vector2(viewport.Width / 2f, viewport.Height / 2f);

            var matrix = Matrix.CreateTranslation(-screenCenter.X, -screenCenter.Y, 0) *
                        Matrix.CreateScale(Zoom) *
                        Matrix.CreateRotationZ(Rotation) *
                        Matrix.CreateTranslation(screenCenter.X, screenCenter.Y, 0) *
                        Matrix.CreateTranslation(Position.X, Position.Y, 0);

            var pos = Vector2.Transform(new Vector2(worldPosition.X, worldPosition.Y), matrix);
            return new System.Numerics.Vector2(pos.X, pos.Y);
        }

        public void DrawText(string text, System.Numerics.Vector2 position)
        {
            EnsureFontAtlas();
            if (_fontAtlas == null) return;

            float x = position.X;
            float y = position.Y;
            int firstChar = 32;
            var color = System.Drawing.Color.White;

            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (c < firstChar || c >= firstChar + _fontAtlas.Chars.Length)
                {
                    x += _fontAtlas.FontSize / 2; // skip unknown chars
                    continue;
                }
                var ch = _fontAtlas.Chars[c - firstChar];

                // Skip drawing for space (ASCII 32)
                if (c == (char)32)
                {
                    x += ch.xadvance;
                    continue;
                }

                float x0 = x + ch.xoff;
                float y0 = y + ch.yoff;
                float x1 = x0 + (ch.x1 - ch.x0);
                float y1 = y0 + (ch.y1 - ch.y0);

                var flippedY = _fontAtlas.AtlasHeight - (int)ch.y1;
                var srcRect = new System.Drawing.Rectangle((int)ch.x0, flippedY, (int)(ch.x1 - ch.x0), (int)(ch.y1 - ch.y0));
                var destPos = new System.Numerics.Vector2(x0, y0);
                var size = new System.Numerics.Vector2(x1 - x0, y1 - y0);
                DrawImage(_fontAtlas.Texture, destPos, size, 0, System.Numerics.Vector2.Zero, srcRect, color);
                x += ch.xadvance;
            }
        }
    }
}
