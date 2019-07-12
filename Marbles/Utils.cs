using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

namespace Marbles
{
    static class Utils
    {
        public static Vector3 MousePickRay(int mouseX, int mouseY, Rectangle clientRectangle, ref Matrix4 projection, ref Matrix4 view)
        {
            Vector2 mouseInNDC = MousePositionInNDC(mouseX, mouseY, clientRectangle);
            Vector4 source4D = new Vector4(mouseInNDC.X, mouseInNDC.Y, -1f, 1f);
            Vector4 target4D = new Vector4(mouseInNDC.X, mouseInNDC.Y, 1f, 1f);

            Matrix4 invProjection = projection.Inverted();
            source4D = Vector4.Transform(source4D, invProjection);
            target4D = Vector4.Transform(target4D, invProjection);

            Vector3 source = new Vector3(source4D.Xyz);
            if (source4D.W > 0.0001f || source4D.W < -0.0001f)
                source /= source4D.W;
            Vector3 target = new Vector3(target4D.Xyz);
            if (target4D.W > 0.0001f || target4D.W < -0.0001f)
                target /= target4D.W;

            Matrix4 invView = view.Inverted();
            source = Vector3.Transform(source, invView);
            target = Vector3.Transform(target, invView);

            // Inverted model matrix (not included since I perform the line intersection in world space)

            return (target - source).Normalized();
        }

        public static Vector2 MousePositionInNDC(int mouseX, int mouseY, Rectangle clientRectangle)
        { // Normalised device coordinates
            return new Vector2(2f * mouseX / (float)clientRectangle.Width - 1f,
                               -(2f * mouseY / (float)clientRectangle.Height - 1f));
        }

        public static float ComponentSum(Vector3 v)
        {
            return v.X + v.Y + v.Z;
        }

        public static bool LineIntersectsSphere(Vector3 lineOrigin, Vector3 lineDirection, Vector3 spherePosition, float sphereRadius)
        {
            float a = ComponentSum(lineDirection * lineDirection);
            float b = ComponentSum(lineDirection * 2f * (lineOrigin - spherePosition));
            float c = ComponentSum(spherePosition * spherePosition) + ComponentSum(lineOrigin * lineOrigin) - 2f * ComponentSum(lineOrigin * spherePosition) - sphereRadius * sphereRadius;
            float d = b * b - 4f * a * c;
            return d > 0f;
        }

        public static float Distance(Vector3 v1, Vector3 v2)
        {
            return (float)Math.Sqrt(Math.Pow((v1.X - v2.X), 2) + Math.Pow((v1.Y - v2.Y), 2) + Math.Pow((v1.Z - v2.Z), 2));
        }

        public static bool TargetIsBehind(Vector3 source, Vector3 target, Vector3 viewDirection)
        {
            return Vector3.Dot(target - source, viewDirection) > 0f;
        }

        public static Vector2 AngleToVector(float angle)
        {
            return new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle));
        }

        public static float VectorToAngle(Vector2 v)
        {
            return (float)Math.Atan2(v.Y, v.X);
        }

        public static Color4 InterpolateColour(Color4 zeroColour, Color4 oneColour, float interpolation)
        {
            return new Color4(zeroColour.R + interpolation * (oneColour.R - zeroColour.R),
                              zeroColour.G + interpolation * (oneColour.G - zeroColour.G),
                              zeroColour.B + interpolation * (oneColour.B - zeroColour.B),
                              zeroColour.A + interpolation * (oneColour.A - zeroColour.A));
        }

        public static Vector3 Vector3Abs(Vector3 v)
        {
            return new Vector3(Math.Abs(v.X), Math.Abs(v.Y), Math.Abs(v.Z));
        }

        public static void GenerateSphere(float radius, int rings, int sectors, bool includeTexCoords, out float[] vertices, out int[] indices)
        { // Position, tex coords
            float rStep = 1f / (rings - 1);
            float sStep = 1f / (sectors - 1);
            vertices = new float[rings * sectors * (includeTexCoords ? 5 : 3)];
            int v = 0;

            for (int r = 0; r < rings; r++)
            {
                for (int s = 0; s < sectors; s++)
                {
                    float x = (float)Math.Cos(MathHelper.TwoPi * s * sStep) * (float)Math.Sin(MathHelper.Pi * r * rStep);
                    float y = (float)Math.Sin(MathHelper.TwoPi * s * sStep) * (float)Math.Sin(MathHelper.Pi * r * rStep);
                    float z = (float)Math.Sin(-MathHelper.PiOver2 + MathHelper.Pi * r * rStep);
                    
                    vertices[v++] = x * radius;
                    vertices[v++] = y * radius;
                    vertices[v++] = z * radius;

                    if (includeTexCoords)
                    {
                        vertices[v++] = s * sStep;
                        vertices[v++] = (rings - 1 - r) * rStep;
                    }
                }
            }

            indices = new int[rings * sectors * 4];
            int i = 0;
            for (int r = 0; r < rings; r++)
            {
                for (int s = 0; s < sectors; s++)
                {
                    indices[i++] = r * sectors + s;
                    indices[i++] = r * sectors + s + 1;
                    indices[i++] = (r + 1) * sectors + s + 1;
                    indices[i++] = (r + 1) * sectors + s;
                }
            }
        }

        public static void GenerateRing(float radius, float thickness, int sectors, bool includeTexCoords, out float[] vertices, out int[] indices)
        {
            sectors++;
            float step = 1f / (sectors - 1);
            vertices = new float[2 * sectors * (includeTexCoords ? 5 : 3)];
            int j = 0;

            for (int i = 0; i < sectors; i++)
            {
                float x = (float)Math.Cos(MathHelper.TwoPi * i * step);
                float y = (float)Math.Sin(MathHelper.TwoPi * i * step);

                // Outside edge
                vertices[j++] = x * radius;
                vertices[j++] = y * radius;
                vertices[j++] = 0f;

                if (includeTexCoords)
                {
                    vertices[j++] = i * step;
                    vertices[j++] = 1f;
                }

                // Inside edge
                vertices[j++] = x * (radius - thickness);
                vertices[j++] = y * (radius - thickness);
                vertices[j++] = 0f;

                if (includeTexCoords)
                {
                    vertices[j++] = i * step;
                    vertices[j++] = 0f;
                }
            }

            indices = new int[sectors * 4];
            j = 0;
            for (int i = 0; i < sectors; i++)
            {
                //indices[j++] = i;
                indices[j++] = 2 * i;
                indices[j++] = 2 * i + 1;
                indices[j++] = 2 * i + 3;
                indices[j++] = 2 * i + 2;
            }
        }

        public static void GenerateCylinder(float radius, float height, int sectors, out float[] vertices, out int[] indices)
        {
            sectors++;
            float step = 1f / (sectors - 1);
            vertices = new float[2 * sectors * 3];
            int j = 0;

            for (int i = 0; i < sectors; i++)
            {
                float x = radius * (float)Math.Cos(MathHelper.TwoPi * i * step);
                float y = radius * (float)Math.Sin(MathHelper.TwoPi * i * step);

                // Top
                vertices[j++] = x;
                vertices[j++] = y;
                vertices[j++] = height / 2f;

                // Bottom
                vertices[j++] = x;
                vertices[j++] = y;
                vertices[j++] = -height / 2f;
            }

            sectors--;
            indices = new int[sectors * 4];
            j = 0;
            for (int i = 0; i < sectors; i++)
            {
                //indices[j++] = i;
                indices[j++] = 2 * i;
                indices[j++] = 2 * i + 1;
                indices[j++] = 2 * i + 3;
                indices[j++] = 2 * i + 2;
            }
        }



        public static float Sin(float f) => (float)Math.Sin(f);

        public static float Cos(float f) => (float)Math.Cos(f);

        public static Vector3 FromYawPitch(float yaw, float pitch) =>
            new Vector3(
                Cos(pitch) * Sin(yaw),
                Cos(pitch) * Cos(yaw),
                Sin(pitch)
            ).Normalized();

        public static (float Yaw, float Pitch) GetYawPitch(Vector3 v) => ((float)Math.Asin(v.Z), (float)Math.Atan2(v.X, v.Y));

        public static float GetYaw(Vector3 v) => (float)Math.Atan2(v.X, v.Y);

        public static float GetPitch(Vector3 v) => (float)Math.Asin(v.Z);

        public static Vector3 RandomVector3(Random random) =>
            new Vector3((float)random.NextDouble(), (float)random.NextDouble(), (float)random.NextDouble());

        public static string ReadSaveFileLineSegment(string lineToRead)
        {
            int startIndex = lineToRead.Substring(1).IndexOf(' ') + 2;
            return lineToRead.Substring(startIndex, lineToRead.IndexOf(';') - startIndex);
        }
    }
}
