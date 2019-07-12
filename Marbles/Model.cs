using System;
using System.IO;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

namespace Marbles
{
    /// <summary>
    /// Model Utility reads a very simple, inefficient file format that uses 
    /// Triangles only. This is not good practice, but it does the job :)
    /// </summary>
    public class Model
    {
        public static Model CurrentlyBound { get; private set; }
        public float[] Vertices;
        public int[] Indices;
        public BeginMode IntendedDrawMode;
        public int ShaderIndex;
        public int ID;

        public Model(int shaderIndex, BeginMode intendedDrawMode)
        {
            ShaderIndex = shaderIndex;
            IntendedDrawMode = intendedDrawMode;
        }

        public void Bind(int shaderID)
        {
            if (CurrentlyBound == this)
                return;
            GL.BindVertexArray(ID);
            int uModelTypeLocation = GL.GetUniformLocation(shaderID, "uCurrentModel");
            GL.Uniform1(uModelTypeLocation, ShaderIndex);
            CurrentlyBound = this;
        }

        public static void Unbind()
        {
            GL.BindVertexArray(0);
            CurrentlyBound = null;
        }

        public void Draw(int shaderID)
        {
            Bind(shaderID);
            GL.DrawElements(IntendedDrawMode, Indices.Length, DrawElementsType.UnsignedInt, 0);
        }

        private static Model LoadFromBIN(int shaderIndex, string pModelFile)
        {
            Model model = new Model(shaderIndex, BeginMode.Triangles);
            model.IntendedDrawMode = BeginMode.Triangles;
            BinaryReader reader = new BinaryReader(new FileStream(pModelFile, FileMode.Open));

            int numberOfVertices = reader.ReadInt32();
            int floatsPerVertex = 6;

            model.Vertices = new float[numberOfVertices * floatsPerVertex];

            byte[]  byteArray = new byte[model.Vertices.Length * sizeof(float)];
            byteArray = reader.ReadBytes(byteArray.Length);

            Buffer.BlockCopy(byteArray, 0, model.Vertices, 0, byteArray.Length);

            int numberOfTriangles = reader.ReadInt32();

            model.Indices = new int[numberOfTriangles * 3];
            
            byteArray = new byte[model.Indices.Length * sizeof(int)];
            byteArray = reader.ReadBytes(model.Indices.Length * sizeof(int));
            Buffer.BlockCopy(byteArray, 0, model.Indices, 0, byteArray.Length);

            reader.Close();
            return model;
        }     
 
        private static Model LoadFromSJG(int shaderIndex, string pModelFile)
        {
            Model model = new Model(shaderIndex, BeginMode.Triangles);
            model.IntendedDrawMode = BeginMode.Triangles;
            StreamReader reader;
            reader = new StreamReader(pModelFile);
            string line = reader.ReadLine(); // vertex format
            int numberOfVertices = 0;
            int floatsPerVertex = 6;
            if (!int.TryParse(reader.ReadLine(), out numberOfVertices))
            {
                throw new Exception("Error when reading number of vertices in model file " + pModelFile);
            }

            model.Vertices = new float[numberOfVertices * floatsPerVertex];

            string[] values;
            for (int i = 0; i < model.Vertices.Length; )
            {
                line = reader.ReadLine();
                values = line.Split(',');
                foreach(string s in values)
                {
                    if (!float.TryParse(s, out model.Vertices[i]))
                    {
                        throw new Exception("Error when reading vertices in model file " + pModelFile + " " + s + " is not a valid number");
                    }
                    ++i;
                }
            }

            reader.ReadLine();
            int numberOfTriangles = 0;
            line = reader.ReadLine();
            if (!int.TryParse(line, out numberOfTriangles))
            {
                throw new Exception("Error when reading number of triangles in model file " + pModelFile);
            }

            model.Indices = new int[numberOfTriangles * 3];

            for(int i = 0; i < numberOfTriangles * 3;)
            {
                line = reader.ReadLine();
                values = line.Split(',');
                foreach(string s in values)
                {
                    if (!int.TryParse(s, out model.Indices[i]))
                    {
                        throw new Exception("Error when reading indices in model file " + pModelFile + " " + s + " is not a valid index");
                    }
                    ++i;
                }
            }

            reader.Close();
            return model;
        }

        public static Model LoadModel(int shaderIndex, string pModelFile)
        {
            string extension = pModelFile.Substring(pModelFile.IndexOf('.'));

            if (extension == ".sjg")
            {
                return LoadFromSJG(shaderIndex, pModelFile);
            }
            else if (extension == ".bin")
            {
                return LoadFromBIN(shaderIndex, pModelFile);
            }
            else
            {
                throw new Exception("Unknown file extension " + extension);
            }
        }
    }
}
