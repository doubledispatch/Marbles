using System;
using SD = System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using OpenTK.Graphics.OpenGL;

namespace Marbles
{
    class Texture
    {
        public static Texture CurrentlyBoundStandard { get; private set; }
        public static Texture CurrentlyBoundSpecular { get; private set; }

        public string Name { get; }
        //public Bitmap Bitmap;
        //public BitmapData BitmapData;
        public int ID { get; }
        public int Width { get; }
        public int Height { get; }
        public TextureType Type { get; }

        public Texture() { }

        public Texture(string filePath, TextureType type = TextureType.Standard)
        {
            Type = type;
            Name = Path.GetFileNameWithoutExtension(filePath);
            var bitmap = new SD.Bitmap(filePath);
            Width = bitmap.Width;
            Height = bitmap.Height;

            BitmapData bitmapData = bitmap.LockBits(
                new SD.Rectangle(0, 0, bitmap.Width,
                bitmap.Height), ImageLockMode.ReadOnly,
                SD.Imaging.PixelFormat.Format32bppRgb);

            switch (type)
            {
                case TextureType.Standard: GL.ActiveTexture(TextureUnit.Texture0); break;
                case TextureType.Specular: GL.ActiveTexture(TextureUnit.Texture1); break;
                default: throw new Exception();
            }
            GL.GenTextures(1, out int id);
            ID = id;

            GL.BindTexture(TextureTarget.Texture2D, ID);
            GL.TexImage2D(TextureTarget.Texture2D,
                0, PixelInternalFormat.Rgba, bitmapData.Width, bitmapData.Height,
                0, OpenTK.Graphics.OpenGL.PixelFormat.Bgra,
                PixelType.UnsignedByte, bitmapData.Scan0);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter,
                (int)TextureMinFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter,
                (int)TextureMagFilter.Linear);
            bitmap.UnlockBits(bitmapData);
        }

        public float AspectRatio => (float)Width / Height;

        public void Bind()
        {
            if (Type == TextureType.Standard)
            {
                if (CurrentlyBoundStandard == this)
                    return;

                GL.ActiveTexture(TextureUnit.Texture0);
                GL.BindTexture(TextureTarget.Texture2D, ID);
                //GL.BindTexture(TextureTarget.);
                CurrentlyBoundStandard = this;
            }
            else if (Type == TextureType.Specular)
            {
                if (CurrentlyBoundSpecular == this)
                    return;

                GL.ActiveTexture(TextureUnit.Texture1);
                GL.BindTexture(TextureTarget.Texture2D, ID);
                CurrentlyBoundSpecular = this;
            }
        }

        public override string ToString() => $"{Name} {Width}x{Height} {AspectRatio}";
    }
}
