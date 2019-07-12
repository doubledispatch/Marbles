using System;
using System.Drawing;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using Inp = OpenTK.Input;

namespace Marbles
{
    class MaterialProperties
    {
        public static Vector3 CurrentAmbient { get; private set; }
        public static Vector3 CurrentDiffuse { get; private set; }
        public static Vector3 CurrentSpecular { get; private set; }
        public static float CurrentShininess { get; private set; }

        public Vector3 Ambient;
        public Vector3 Diffuse;
        public Vector3 Specular;
        public float Shininess;

        public MaterialProperties(Vector3 ambient, Vector3 diffuse, Vector3 specular, float shininess)
        {
            Ambient = ambient;
            Diffuse = diffuse;
            Specular = specular;
            Shininess = shininess;
        }

        public MaterialProperties(float ambientRed, float ambientGreen, float ambientBlue, float diffuseRed, float diffuseGreen, float diffuseBlue, float specularRed, float specularGreen, float specularBlue, float shininess)
        {
            Ambient = new Vector3(ambientRed, ambientGreen, ambientBlue);
            Diffuse = new Vector3(diffuseRed, diffuseGreen, diffuseBlue);
            Specular = new Vector3(specularRed, specularGreen, specularBlue);
            Shininess = shininess;
        }

        /// <summary>
        /// Sets up the fLighting.frag shader material properties for subsequent draw calls. Each vector represents the colour of the corresponding light that hits it that will be reflected.
        /// </summary>
        /// <param name="ambientReflectivity"></param>
        /// <param name="diffuseReflectivity"></param>
        /// <param name="specularReflectivity"></param>
        /// <param name="shininess"></param>
        public static void Set(int shaderID, Vector3 ambientReflectivity, Vector3 diffuseReflectivity, Vector3 specularReflectivity, float shininess)
        {
            CurrentAmbient = ambientReflectivity;
            int uAmbientReflectivityLocation = GL.GetUniformLocation(shaderID, "uMaterial.AmbientReflectivity");
            GL.Uniform3(uAmbientReflectivityLocation, ref ambientReflectivity);

            CurrentDiffuse = diffuseReflectivity;
            int uDefuseReflectivityLocation = GL.GetUniformLocation(shaderID, "uMaterial.DiffuseReflectivity");
            GL.Uniform3(uDefuseReflectivityLocation, ref diffuseReflectivity);

            CurrentSpecular = specularReflectivity;
            int uSpecularReflectivityLocation = GL.GetUniformLocation(shaderID, "uMaterial.SpecularReflectivity");
            GL.Uniform3(uSpecularReflectivityLocation, ref specularReflectivity);

            CurrentShininess = shininess;
            int uShininessLocation = GL.GetUniformLocation(shaderID, "uMaterial.Shininess");
            GL.Uniform1(uShininessLocation, shininess);
        }


        public static void SetAmbient(int shaderID, Vector3 amount)
        {
            CurrentAmbient = amount;
            int uAmbientReflectivityLocation = GL.GetUniformLocation(shaderID, "uMaterial.AmbientReflectivity");
            GL.Uniform3(uAmbientReflectivityLocation, ref amount);
        }

        public static void SetDiffuse(int shaderID, Vector3 amount)
        {
            CurrentDiffuse = amount;
            int uDefuseReflectivityLocation = GL.GetUniformLocation(shaderID, "uMaterial.DiffuseReflectivity");
            GL.Uniform3(uDefuseReflectivityLocation, ref amount);
        }

        public static void SetSpecular(int shaderID, Vector3 amount)
        {
            CurrentSpecular = amount;
            int uSpecularReflectivityLocation = GL.GetUniformLocation(shaderID, "uMaterial.SpecularReflectivity");
            GL.Uniform3(uSpecularReflectivityLocation, ref amount);
        }

        public static void SetShininess(int shaderID, float amount)
        {
            CurrentShininess = amount;
            int uShininessLocation = GL.GetUniformLocation(shaderID, "uMaterial.Shininess");
            GL.Uniform1(uShininessLocation, amount);
        }

        public void Set(int shaderID)
        {
            Set(shaderID, Ambient, Diffuse, Specular, Shininess);
        }
    }

    class Game : GameWindow
    {
        enum NextAction { Update, Render }

        /*public Game()
            : base(800, 600, GraphicsMode.Default, "OpenTK Quick Start Sample")
        {
            VSync = VSyncMode.On;
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            GL.ClearColor(0.1f, 0.2f, 0.5f, 0.0f);
            GL.Enable(EnableCap.DepthTest);
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);

            GL.Viewport(ClientRectangle.X, ClientRectangle.Y, ClientRectangle.Width, ClientRectangle.Height);

            Matrix4 projection = Matrix4.CreatePerspectiveFieldOfView((float)Math.PI / 4, Width / (float)Height, 1.0f, 64.0f);
            GL.MatrixMode(MatrixMode.Projection);
            GL.LoadMatrix(ref projection);
        }

        protected override void OnUpdateFrame(FrameEventArgs e)
        {
            base.OnUpdateFrame(e);

            if (Keyboard[Key.Escape])
                Exit();
        }

        protected override void OnRenderFrame(FrameEventArgs e)
        {
            base.OnRenderFrame(e);

            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            Matrix4 modelview = Matrix4.LookAt(Vector3.Zero, Vector3.UnitZ, Vector3.UnitY);
            GL.MatrixMode(MatrixMode.Modelview);
            GL.LoadMatrix(ref modelview);

            GL.Begin(PrimitiveType.Triangles);

            GL.Color3(1.0f, 1.0f, 0.0f); GL.Vertex3(-1.0f, -1.0f, 4.0f);
            GL.Color3(1.0f, 0.0f, 0.0f); GL.Vertex3(1.0f, -1.0f, 4.0f);
            GL.Color3(0.2f, 0.9f, 1.0f); GL.Vertex3(0.0f, 1.0f, 4.0f);

            GL.End();

            SwapBuffers();
        }*/

        // For the fragment shader
        const int MODEL_SPHERE = 0;
        const int MODEL_SQUARE = 1;
        const int MODEL_RING = 2;
        const int MODEL_CYLINDER = 3;

        public static int FlashTicks;
        public const int MAX_FLASH_TICKS = 30;
        public static bool FlashTicksAreAscending;
        const int MODEL_COUNT = 4;

        private static int[] mVBO_IDs = new int[MODEL_COUNT * 2];
        private static int[] mVAO_IDs = new int[MODEL_COUNT];
        private static ShaderUtility mShader;
        Model Sphere;
        Model Square;
        const float RING_THICKNESS = 0.4f;
        Model Ring;
        Model Cylinder;
        private static bool lightingEnabled = true; // Use SetLightingEnabled(bool)
        private static bool texturesEnabled = true;
        private static bool specularMapEnabled = false;
        List<Marble> Marbles;
        Marble lastMarbleRemovedForVictory;
        int victoryAnimationTick;
        const int MAX_SUN_DEATH_TICKS = 120;
        const int MAX_VICTORY_ANIMATION_TICKS = Marble.MAX_ANIMATION_TICKS + Marble.MAX_DEATH_ANIMATION_TICKS + MAX_SUN_DEATH_TICKS;
        const int VICTORY_CAMERA_LINGER_TICKS = 90;

        Camera _camera = new Camera();
        Camera _previousCamera;
        
        Inp.KeyboardState oldKeyboardState = Inp.Keyboard.GetState();
        Inp.MouseState oldMouseState = Inp.Mouse.GetState();
        public static readonly Random Rand = new Random();
        float SunAngle = 0f;
        //private static Dictionary<ModelType, int> ModelIDs = new Dictionary<ModelType, int>();
        private Dictionary<char, CharProperties> Chars = new Dictionary<char, CharProperties>();

        public Texture TexMilkyWay;
        public Texture TexTick;
        public Texture TexSun;
        int UndoLevel;
        string TextInput = null;
        int TextCursorIndex = 0;

        public const int LIGHT_COUNT = 4;
        private static readonly Vector3[] mLightPositions = new Vector3[LIGHT_COUNT];
        private static readonly bool[] mLightStates = new bool[LIGHT_COUNT] { false, false, false, false };

        static int MouseX;
        static int MouseY;

        const string TEXTURES_DIRECTORY = "Content\\Textures\\";
        public static Vector3 SunPosition;
        public List<MarbleMove> Moves = new List<MarbleMove>();
        char[] InvalidFileNameChars = Path.GetInvalidFileNameChars();
        WindowState windowStateBeforeFullScreen = OpenTK.WindowState.Maximized;

        RefBool FullScreenEnabled = new RefBool("FullScreen", true);
        RefBool MoveAnimationEnabled = new RefBool("MoveAnimation", true);
        RefBool SpinningMarblesEnabled = new RefBool("SpinningMarbles", true);
        RefBool SkyboxEnabled = new RefBool("SpaceBackdrop", true);
        RefBool ShowFocusPointEnabled = new RefBool("ShowFocusPoint", false);
        RefBool ShowSunEnabled = new RefBool("ShowSun", true);
        RefBool ShowControlsEnabled = new RefBool("ShowControls", true);
        RefBool PromptsEnabled = new RefBool("Prompts", true);
        RefBool FPSModeEnabled = new RefBool("FPSMode", false);
        RefFloat FieldOfView = new RefFloat("FieldOfView", MathHelper.DegreesToRadians(45f), MathHelper.DegreesToRadians(170f), MathHelper.DegreesToRadians(10f));

        const float DEFAULT_AMBIENT = 0.2f;
        const float DEFAULT_DIFFUSE = 1f;
        public RefFloat SpecularRedReflectivity = new RefFloat("RedSpecularReflectivity", 1f);
        public RefFloat SpecularGreenReflectivity = new RefFloat("GreenSpecularReflectivity", 1f);
        public RefFloat SpecularBlueReflectivity = new RefFloat("BlueSpecularReflectivity", 1f);
        public RefFloat SpecularPower = new RefFloat("SpecularPower", 60f, 100f, 1f, true);
        public RefFloat DiffuseRedReflectivity = new RefFloat("RedDiffuseReflectivity", DEFAULT_DIFFUSE);
        public RefFloat DiffuseGreenReflectivity = new RefFloat("GreenDiffuseReflectivity", DEFAULT_DIFFUSE);
        public RefFloat DiffuseBlueReflectivity = new RefFloat("BlueDiffuseReflectivity", DEFAULT_DIFFUSE);
        public RefFloat AmbientRedReflectivity = new RefFloat("RedAmbientReflectivity", DEFAULT_AMBIENT);
        public RefFloat AmbientGreenReflectivity = new RefFloat("GreenAmbientReflectivity", DEFAULT_AMBIENT);
        public RefFloat AmbientBlueReflectivity = new RefFloat("BlueAmbientReflectivity", DEFAULT_AMBIENT);
        public RefBool TexturesEnabled = new RefBool("Textures", true);
        public RefBool LightingEnabled = new RefBool("Lighting", true);
        public RefBool GreyscaleEnabled = new RefBool("Greyscale", false);
        //RefBool PosterisationEnabled = new RefBool("Posterisation", false);
        
        Menu PauseMenu;
        Menu LoadMenu;
        Menu SaveMenu;
        Menu SettingsMenu;
        Menu GraphicsMenu;
        Menu ConfirmationMenu;
        List<MenuButton> MenuButtonsWithHotkeys = new List<MenuButton>();
        List<Menu> Menus = new List<Menu>();

        string TemporaryMessageText;
        int TemporaryMessageTicks;
        Color4 TemporaryMessageColour;

        Inp.KeyboardState NewKeyboardState;
        Inp.MouseState NewMouseState;
        int TicksBeforeNextUndoRedo = 0;
        const int MAX_TICKS_BEFORE_NEXT_UNDO_REDO = 2;
        public static Matrix4 View;
        public static Matrix4 Projection;
        NextAction nextAction = NextAction.Update;
        bool moveMadeSinceSave;
        bool windowHadFocusLastTick = true;
        static bool currentlyRendering3D;

        //IntPtr hWnd;
        //WF.Control control;
        //WF.Form form;

        private void SetLightProperties(int index, float intensity, Vector3 ambient, Vector3 diffuse, Vector3 specular)
        {
            int uLightIntensityLocation = GL.GetUniformLocation(mShader.ShaderProgramID, "uLight[" + index + "].Intensity");
            GL.Uniform1(uLightIntensityLocation, intensity);

            int uAmbientLightLocation = GL.GetUniformLocation(mShader.ShaderProgramID, "uLight[" + index + "].AmbientLight");
            GL.Uniform3(uAmbientLightLocation, ambient);

            int uDiffuseLightLocation = GL.GetUniformLocation(mShader.ShaderProgramID, "uLight[" + index + "].DiffuseLight");
            GL.Uniform3(uDiffuseLightLocation, diffuse);

            int uSpecularLightLocation = GL.GetUniformLocation(mShader.ShaderProgramID, "uLight[" + index + "].SpecularLight");
            GL.Uniform3(uSpecularLightLocation, specular);
        }

        public static void SetLightEnabled(int shaderID, int index, bool enabled)
        {
            if (mLightStates[index] == enabled)
                return;
            int uLightEnabledLocation = GL.GetUniformLocation(shaderID, "uLight[" + index + "].Enabled");
            GL.Uniform1(uLightEnabledLocation, enabled ? 1 : 0);
            mLightStates[index] = enabled;
        }

        public void SetLightEnabled(int index, bool enabled)
        {
            SetLightEnabled(mShader.ShaderProgramID, index, enabled);
        }

        public void SetLightPosition(int index, Vector3 position)
        {
            SetLightPosition(mShader.ShaderProgramID, index, position);
        }

        public static void SetLightPosition(int shaderID, int index, Vector3 position)
        {
            int uLightPositionLocation = GL.GetUniformLocation(shaderID, "uLight[" + index + "].Position");
            GL.Uniform4(uLightPositionLocation, new Vector4(position, 1f));
            mLightPositions[index] = position;
        }

        /*void window_MouseMove(object sender, Inp.MouseMoveEventArgs e)
        {
            MouseX = e.X;
            MouseY = e.Y;
            //if (newKeyboardState.IsKeyUp(Inp.Key.Comma))
            //    return;

            //Console.Clear();
            //Vector2 ndc = MousePositionInNDC();
            //Console.WriteLine();
            //Console.WriteLine("Direction: " + MousePickRay());
            //Console.WriteLine("Camera Direction: " + CameraForward());




            //if (newKeyboardState.IsKeyDown(Inp.Key.Comma))
            //{
            //    Vector2 v = MousePositionInNDC();
            //    Vector4 mousePositionInClipSpace = new Vector4(v.X, v.Y, 0f, 1f);
            //    Vector4 mousePositionInViewSpace = Vector4.Transform(mousePositionInClipSpace, projection.Inverted());
            //    mousePositionInClipSpace.W = 1f;
            //    Vector4 mousePositionInWorldSpace = Vector4.Transform(mousePositionInViewSpace, view.Inverted());
            //    mousePositionInWorldSpace.W = 1f;
            //    Console.WriteLine("Mouse (NDC): " + v);
            //    Console.WriteLine("Mouse (world): " + mousePositionInWorldSpace);
            //    Console.WriteLine("Position: " + CameraPosition);
            //    Console.WriteLine("Orientation: " + CameraForward());
            //    Console.WriteLine("Ray direction: " + (mousePositionInWorldSpace.Xyz - CameraPosition).Normalized());
            //    Console.WriteLine();
            //}
        }*/

        private void SetUpModel(int mVAO_ID, Model model/*, ModelType modelType*/, int vPositionLocation, int vTexCoordLocation)
        {
            //ModelIDs[modelType] = mVAO_ID;
            int mVBO_ID = 2 * mVAO_ID;
            mVAO_IDs[mVAO_ID] = GL.GenVertexArray();
            model.ID = mVAO_IDs[mVAO_ID];
            GL.GenBuffers(mVBO_IDs.Length, mVBO_IDs);

            GL.BindVertexArray(mVAO_IDs[mVAO_ID]);
            GL.BindBuffer(BufferTarget.ArrayBuffer, mVBO_IDs[mVBO_ID]);
            GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)(model.Vertices.Length * sizeof(float)), model.Vertices, BufferUsageHint.StaticDraw);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, mVBO_IDs[mVBO_ID + 1]);
            GL.BufferData(BufferTarget.ElementArrayBuffer, (IntPtr)(model.Indices.Length * sizeof(float)), model.Indices, BufferUsageHint.StaticDraw);

            int size;
            GL.GetBufferParameter(BufferTarget.ArrayBuffer, BufferParameterName.BufferSize, out size);
            if (model.Vertices.Length * sizeof(float) != size)
            {
                throw new ApplicationException("Vertex data not loaded onto graphics card correctly");
            }

            GL.GetBufferParameter(BufferTarget.ElementArrayBuffer, BufferParameterName.BufferSize, out size);
            if (model.Indices.Length * sizeof(float) != size)
            {
                throw new ApplicationException("Index data not loaded onto graphics card correctly");
            }

            GL.EnableVertexAttribArray(vPositionLocation);
            GL.VertexAttribPointer(vPositionLocation, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), 0);
            // For when including tex coords
            /*GL.EnableVertexAttribArray(vPositionLocation);
            GL.VertexAttribPointer(vPositionLocation, 3, VertexAttribPointerType.Float, false, 5 * sizeof(float), 0);

            GL.EnableVertexAttribArray(vTexCoordLocation);
            GL.VertexAttribPointer(vTexCoordLocation, 2, VertexAttribPointerType.Float, false, 5 * sizeof(float), 3 * sizeof(float));*/


            /*if (modelType == Model.box)
            {
                GL.EnableVertexAttribArray(vTexCoordsLocation);
                GL.VertexAttribPointer(vTexCoordsLocation, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), 6 * sizeof(float));
            }*/
        }

        public void SaveSettings()
        {
            StreamWriter sw = null;
            try
            {
                sw = new StreamWriter("Settings.txt");
                sw.WriteLine("Marbles");
                sw.WriteLine();

                foreach (MenuButton t in OptionButtons)
                    sw.WriteLine(t.BoundVar.SaveName + " " + t.BoundVar.ValueString());
            }
            catch
            {
                DisplayTemporaryMessage("Failed to Save Settings", 360, Color4.Yellow);
            }
            finally
            {
                if (sw != null)
                    sw.Close();
            }
        }

        protected override void OnLoad(EventArgs e)
        {
            Menu.Main = this;
            MenuButton.Main = this;
            Title = "Marbles";
            //this.Cursor
            //hWnd = Window.Handle;
            //control = WF.Control.FromHandle(hWnd);
            //form = control.FindForm();

            // Set some GL state
            GL.ClearColor(Color4.Black);
            GL.Enable(EnableCap.DepthTest);
            GL.Enable(EnableCap.CullFace);
            GL.Enable(EnableCap.Blend);
            GL.Enable(EnableCap.Texture2D);
            GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);

            //MouseDown += window_MouseDown;
            //MouseMove += window_MouseMove;

            Sphere = new Model(MODEL_SPHERE, BeginMode.Quads);
            Utils.GenerateSphere(1f, 64, 64, false, out Sphere.Vertices, out Sphere.Indices);
            //Sphere = ModelUtility.LoadModel(@"Content/Sphere.bin");
            Marble.Model = Sphere;

            Square = new Model(MODEL_SQUARE, BeginMode.Quads);
            Square.Vertices = new float[]
            { // Position, tex coords
                -0.5f, 0.5f, 0f,// 0f, 0f,
                0.5f, 0.5f, 0f,// 1f, 0f,
                -0.5f, -0.5f, 0f,// 0f, 1f,
                0.5f, -0.5f, 0f//, 1f, 1f
            };
            Square.Indices = new int[] { 2, 3, 1, 0 };

            Ring = new Model(MODEL_RING, BeginMode.Quads);
            Utils.GenerateRing(1f, RING_THICKNESS, 64, false, out Ring.Vertices, out Ring.Indices);
            Marble.ModelRing = Ring;

            Cylinder = new Model(MODEL_CYLINDER, BeginMode.Quads);
            Utils.GenerateCylinder(1f, 1f, 64, out Cylinder.Vertices, out Cylinder.Indices);
            Marble.ModelCylinder = Cylinder;

            mShader = new ShaderUtility(@"Content/VertexShader.txt", @"Content/FragmentShader.txt");
            GL.UseProgram(mShader.ShaderProgramID);
            int uTextureSamplerLocation = GL.GetUniformLocation(mShader.ShaderProgramID, "uTextureSampler");
            GL.Uniform1(uTextureSamplerLocation, 0);
            int uSpecularSamplerLocation = GL.GetUniformLocation(mShader.ShaderProgramID, "uSpecularSampler");
            GL.Uniform1(uSpecularSamplerLocation, 1);
            int vPositionLocation = GL.GetAttribLocation(mShader.ShaderProgramID, "vPosition");
            //int vTexCoordsLocation = GL.GetAttribLocation(mShader.ShaderProgramID, "vTexCoords");

            SetUpModel(0, Sphere, vPositionLocation, -1);
            SetUpModel(1, Square,  vPositionLocation, -1);
            SetUpModel(2, Ring, vPositionLocation, -1);
            SetUpModel(3, Cylinder, vPositionLocation, -1);

            //WF.MessageBox.Show(GL.GetError().ToString());
            /*int uProjectionLocation = GL.GetUniformLocation(mShader.ShaderProgramID, "uProjection");
            Matrix4 projection = Matrix4.CreateOrthographic(10, 10, -1, 1);
            GL.UniformMatrix4(uProjectionLocation, true, ref projection);*/
            //WF.MessageBox.Show(GL.GetError().ToString());

            SetLightProperties(0, 45f, new Vector3(0.9f, 0.9f, 0.9f), new Vector3(1.0f, 1.0f, 1.0f), new Vector3(1.0f, 1.0f, 1.0f));
            SetLightProperties(1, 3.5f, new Vector3(0.4f, 0.12f, 0.12f), new Vector3(1f, 0.3f, 0.3f), new Vector3(1f, 0.3f, 0.3f));
            SetLightProperties(2, 3.5f, new Vector3(0.4f, 0.24f, 0.12f), new Vector3(1f, 0.6f, 0.3f), new Vector3(1f, 0.6f, 0.3f));
            SetLightProperties(3, 3.5f, new Vector3(0.08f, 0.28f, 0.4f), new Vector3(0.2f, 0.7f, 1f), new Vector3(0.2f, 0.7f, 1f));


            //TexSnookerBall = new Texture("Content\\13.jpg");
            Marble.InitialiseCelestialBodies(TEXTURES_DIRECTORY);
            TexMilkyWay = new Texture(TEXTURES_DIRECTORY + "MilkyWay.jpg");
            TexTick = new Texture(TEXTURES_DIRECTORY + "Tick.png");
            TexSun = new Texture(TEXTURES_DIRECTORY + "Sun.jpg");

            Marble.CelestialBodies[CelestialBodyType.Earth].SpecularMap.Bind();
            SetLightEnabled(0, true);

            Marbles = new List<Marble>(Marble.STARTING_COUNT);
            for (int i = 0; i < Marble.STARTING_COUNT; i++)
                Marbles.Add(new Marble());

            // Uncomment in order to regenerate character textures and the character widths file for a new font
            /*using (Bitmap bitmapPlaceholder = new Bitmap(1, 1, System.Drawing.Imaging.PixelFormat.Format32bppArgb))
            {
                const float FONT_SIZE = 128f;
                Font font = new Font(new FontFamily("Calibri"), FONT_SIZE);

                Graphics g = Graphics.FromImage(bitmapPlaceholder);
                g.SmoothingMode = SmoothingMode.HighQuality;
                g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;

                float height = g.MeasureString("|pI", font).Height;

                for (char c = '€'; c <= '€'; c++)
                {
                    float longerWidth = g.MeasureString(c.ToString(), font).Width;
                    float width = c == ' ' ? longerWidth * 1.25f : g.MeasureString(new string(c, 2), font).Width / 2f;
                    Chars[c] = new CharProperties(c, width / height, null);

                    using (Bitmap bitmap = new Bitmap((int)width, (int)height, System.Drawing.Imaging.PixelFormat.Format32bppArgb))
                    {
                        g = Graphics.FromImage(bitmap);
                        g.SmoothingMode = SmoothingMode.HighQuality;
                        g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
                        float drawX = c == ' ' ? width : (width - longerWidth) / 2f;
                        //for (int x = -1; x <= 1; x++)
                        //{
                        //    for (int y = -1; y <= 1; y++)
                        //        g.DrawString(c.ToString(), font, Brushes.Black, drawX + x, y);
                        //}
                        for (int i = 6; i <= 10; i++)
                            g.DrawString(c.ToString(), font, Brushes.Black, drawX + i, i);
                        g.DrawString(c.ToString(), font, Brushes.White, drawX, 0f);
                        bitmap.Save("Content\\Textures\\Chars\\" + (int)c + ".png", ImageFormat.Png);
                    }
                }
            }
            StreamWriter stream = new StreamWriter("Content\\CharWidths.txt");
            stream.WriteLine("Marbles");
            stream.WriteLine();
            foreach (KeyValuePair<char, CharProperties> c in Chars)
                stream.WriteLine(c.Key + " " + c.Value.Width);
            stream.Close();*/

            StreamReader sr = new StreamReader("Content\\CharWidths.txt");
            string line = sr.ReadLine();
            while (!string.IsNullOrWhiteSpace(line))
            {
                char c = line[0];
                Chars[c] = new CharProperties(c, float.Parse(line.Substring(2)), new Texture("Content\\Textures\\Chars\\" + (int)c + ".png"));
                line = sr.ReadLine();
            }

            PauseMenu = new Menu(null, null);
            Menus.Add(PauseMenu);
            PauseMenu.AddButton(new MenuButton("Hide UI", Color4.White, sender => Menu.Current = null));
            PauseMenu.AddButton(new MenuButton("New", Color4.White, sender =>
                Prompt("Begin a new game? Unsaved progress will be lost.", ResetGame, moveMadeSinceSave), null, null, Inp.Key.R));
            PauseMenu.AddButton(new MenuButton("Load", Color4.White, sender =>
            {
                Menu.Current = LoadMenu;
                /*WF.OpenFileDialog ofd = new WF.OpenFileDialog() { InitialDirectory = Directory.GetCurrentDirectory(), Title = "Load", DefaultExt = ".txt" };
                if (Directory.Exists("Saves"))
                    ofd.InitialDirectory += "\\Saves";
                WF.DialogResult dr = ofd.ShowDialog();
                if (dr == WF.DialogResult.OK)
                    LoadGame(ofd.FileName);*/
            }, null, null, Inp.Key.L));
            PauseMenu.AddButton(new MenuButton("Save", Color4.White, sender =>
            {
                Menu.Current = SaveMenu;
                /*WF.SaveFileDialog sfd = new WF.SaveFileDialog() { InitialDirectory = Directory.GetCurrentDirectory(), Title = "Save", DefaultExt = ".txt" };
                if (Directory.Exists("Saves"))
                    sfd.InitialDirectory += "\\Saves";
                WF.DialogResult dr = sfd.ShowDialog();
                if (dr == WF.DialogResult.OK)
                    SaveGame(sfd.FileName);*/
            }, null, null, Inp.Key.V));
            PauseMenu.AddButton(new MenuButton("Settings", Color4.White, sender => Menu.Current = Menu.Current == SettingsMenu ? null : SettingsMenu, null, null, Inp.Key.T));
            PauseMenu.AddButton(new MenuButton("Quit", Color4.White, sender => Prompt("Are you sure you wish to quit?", Exit), null, null, Inp.Key.Q));
            PauseMenu.UpdateSpacing();
            Menu.Current = PauseMenu;
            foreach (Marble m in Marbles)
                m.UpdateLight(mShader.ShaderProgramID);

            const float SAVE_LOAD_BUTTON_WIDTH = 0.5f;
            LoadMenu = new Menu("Load", PauseMenu, sender =>
            {
                LoadMenu.Buttons.RemoveRange(1, LoadMenu.Buttons.Length - 1);
                if (Directory.Exists("Saves"))
                {
                    foreach (string s in Directory.GetFiles("Saves", "*.txt"))
                    {
                        string fileName = Path.GetFileNameWithoutExtension(s);
                        LoadMenu.AddButton(new MenuButton(fileName, Color4.White, sender0 =>
                        {
                            if (LoadGame(s))
                                Menu.Current = Menu.Current.ParentMenu;
                        }, null, null, Inp.Key.Unknown, SAVE_LOAD_BUTTON_WIDTH));
                    }
                }
                LoadMenu.UpdateSpacing(SAVE_LOAD_BUTTON_WIDTH);
            });
            Menus.Add(LoadMenu);
            LoadMenu.AddButton(new MenuButton("Back", Color4.Yellow, sender => Menu.Current = Menu.Current.ParentMenu, null, null, Inp.Key.Unknown, SAVE_LOAD_BUTTON_WIDTH));

            SaveMenu = new Menu("Save", PauseMenu, sender =>
            {
                SaveMenu.Buttons.RemoveRange(2, SaveMenu.Buttons.Length - 2);
                if (Directory.Exists("Saves"))
                {
                    foreach (string s in Directory.GetFiles("Saves", "*.txt"))
                    {
                        string fileName = Path.GetFileNameWithoutExtension(s);
                        SaveMenu.AddButton(new MenuButton(fileName, Color4.White, sender0 =>
                        {
                            Prompt("Are you sure you wish to overwrite \"" + fileName + "\"?", () =>
                            {
                                if (SaveGame(s))
                                    Menu.Current = Menu.Current.ParentMenu;
                            });
                        }, null, null, Inp.Key.Unknown, SAVE_LOAD_BUTTON_WIDTH));
                    }
                }
                SaveMenu.UpdateSpacing(SAVE_LOAD_BUTTON_WIDTH);
            });
            Menus.Add(SaveMenu);
            SaveMenu.AddButton(new MenuButton("Back", Color4.Yellow, sender => Menu.Current = Menu.Current.ParentMenu, null, null, Inp.Key.Unknown, SAVE_LOAD_BUTTON_WIDTH));
            SaveMenu.AddButton(new MenuButton("New Save", Color4.Yellow, sender =>
            {
                TextCursorIndex = 0;
                TextInput = "";
            }));

            SettingsMenu = new Menu("Settings", PauseMenu, null, sender => SaveSettings());
            Menus.Add(SettingsMenu);
            SettingsMenu.AddButton(new MenuButton("Back", Color4.White, sender => Menu.Current = Menu.Current.ParentMenu));
            SettingsMenu.AddButton(new MenuButton("Material Properties", Color4.White, sender => Menu.Current = Menu.Current == GraphicsMenu ? null : GraphicsMenu, null, null, Inp.Key.M));
            SettingsMenu.AddButton(new MenuButton("Full Screen", Color4.White, sender =>
            {
                if (sender.BoundBool.Value)
                {
                    windowStateBeforeFullScreen = base.WindowState;
                    base.WindowState = OpenTK.WindowState.Fullscreen;
                }
                else
                {
                    base.WindowState = windowStateBeforeFullScreen;
                }
            }, FullScreenEnabled, "Might improve performance, although is probably unnecessary."));
            SettingsMenu.AddButton(new MenuButton("Animate Moves", Color4.White, null, MoveAnimationEnabled, "Animates the moved marble and the marble being moved over, making the former arc over the latter and then flinging the latter into the sun."));
            SettingsMenu.AddButton(new MenuButton("FPS Mode", Color4.White, null, FPSModeEnabled, "Applies only when the UI is hidden. Hides the mouse and allows the camera to move without needing to right-click, which will instead reduce sensitivity. Selection is done with a crosshair. Recommended in conjuction with first person perspective (press F).", Inp.Key.F, -1f, null, true));
            SettingsMenu.AddButton(new MenuButton("Spinning Marbles", Color4.White, null, SpinningMarblesEnabled, "Makes marbles rotate on the spot constantly, allowing you to see more of the texture from the same position."));
            SettingsMenu.AddButton(new MenuButton("Space Backdrop", Color4.White, null, SkyboxEnabled, "Draws the milky way in the infinite distance. Since this rotates with the camera, disabling this might help relieve motion sickness."));
            SettingsMenu.AddButton(new MenuButton("Show Focus Point", Color4.White, null, ShowFocusPointEnabled, "Draws a small blue sphere at the point around which the camera orients. In first person perspective, this won't be visible."));
            SettingsMenu.AddButton(new MenuButton("Show Main Sun", Color4.White, null, ShowSunEnabled, "Draws the sun, which moves in circles above the marbles. Disabling this will not disable its lighting."));
            SettingsMenu.AddButton(new MenuButton("Show Controls", Color4.White, null, ShowControlsEnabled, "Lists all of the controls in the game while the UI showing.", Inp.Key.C, -1f, null, true));
            SettingsMenu.AddButton(new MenuButton("Prompts", Color4.White, null, PromptsEnabled, "Gives a prompt whenever you try to quit, overwrite a save or begin a new game without saving."));
            SettingsMenu.AddButton(new MenuButton("Field of View: 170", Color4.White, null, FieldOfView, "The vertical angle covered by the height of the screen. The wider the field of view, the more will be visible at once and the more pronounced the perspective will be with nearby objects.", Inp.Key.Unknown, -1f, sender => "Field of View: " + (int)Math.Round(MathHelper.RadiansToDegrees(FieldOfView.ModifiedValue)) + "°"));
            SettingsMenu.AddButton(new MenuButton("Lighting", Color4.White, null, LightingEnabled, "If disabled, all marbles will be rendered under constant lighting, fully (but not overly) bright."));
            SettingsMenu.AddButton(new MenuButton("Textures", Color4.White, null, TexturesEnabled, "Whether or not texture mapping should be used for the marbles' diffuse and ambient reflection. If disabled, the marbles will be solid white."));
            SettingsMenu.AddButton(new MenuButton("Greyscale", Color4.White, null, GreyscaleEnabled, "Converts all colours to a shade of grey based upon the following perceived brightness equation: √(red² * 0.241 + green² * 0.691 + blue² * 0.068)"));
            //SettingsMenu.AddButton(new MenuButton("Posterisation", Color4.White, null, PosterisationEnabled, "Gives a stylistic effect by limiting the colours." ));
            SettingsMenu.UpdateSpacing();

            GraphicsMenu = new Menu("Material Properties", SettingsMenu, null, sender => SaveSettings());
            Menus.Add(GraphicsMenu);
            GraphicsMenu.AddButton(new MenuButton("Back", Color4.White, sender => Menu.Current = Menu.Current.ParentMenu));
            const float F = 0.625f;
            //GraphicsMenu.AddButton(new MenuButton("Default Specular", Color4.White, null, UseDefaultSpecularEnabled, "Uses the default specular properties for each marble based on the celestial body it depicts."));
            GraphicsMenu.AddButton(new MenuButton("Reset to Default", Color4.White, sender =>
            {
                SpecularRedReflectivity.Value = 1f;
                SpecularGreenReflectivity.Value = 1f;
                SpecularBlueReflectivity.Value = 1f;
                DiffuseRedReflectivity.Value = 1f;
                DiffuseGreenReflectivity.Value = 1f;
                DiffuseBlueReflectivity.Value = 1f;
                AmbientRedReflectivity.Value = 0.2f;
                AmbientGreenReflectivity.Value = 0.2f;
                AmbientBlueReflectivity.Value = 0.2f;
                SpecularPower.ModifiedValue = 60f;
            }));
            GraphicsMenu.AddButton(new MenuButton("Randomise", Color4.White, sender =>
            {
                foreach (MenuButton t in GraphicsMenu.Buttons)
                {
                    if (t.BoundFloat != null)
                        t.BoundFloat.Value = (float)Rand.NextDouble() * (t.Text.Contains("Ambient") ? 0.5f : 1f);
                }
            }));
            GraphicsMenu.AddButton(new MenuButton("Red Specular: 100%", new Color4(1f, F, F, 1f), null, SpecularRedReflectivity, null, Inp.Key.Unknown, -1f, sender => "Specular Red: " + (int)(SpecularRedReflectivity.Value * 100) + "%"));
            GraphicsMenu.AddButton(new MenuButton("Green Specular: 100%", new Color4(F, 1f, F, 1f), null, SpecularGreenReflectivity, null, Inp.Key.Unknown, -1f, sender => "Specular Green: " + (int)(SpecularGreenReflectivity.Value * 100) + "%"));
            GraphicsMenu.AddButton(new MenuButton("Blue Specular: 100%", new Color4(F, F, 1f, 1f), null, SpecularBlueReflectivity, null, Inp.Key.Unknown, -1f, sender => "Specular Blue: " + (int)(SpecularBlueReflectivity.Value * 100) + "%"));
            GraphicsMenu.AddButton(new MenuButton("Specular Power: " + SpecularPower.MaxModifiedValue.ToString("0.00"), Color4.White, null, SpecularPower, null, Inp.Key.Unknown, -1f, sender => "Specular Power: " + SpecularPower.ModifiedValue.ToString("0.00")));
            GraphicsMenu.AddButton(new MenuButton("Red Diffuse: 100%", new Color4(1f, F, F, 1f), null, DiffuseRedReflectivity, null, Inp.Key.Unknown, -1f, sender => "Diffuse Red: " + (int)(DiffuseRedReflectivity.Value * 100) + "%"));
            GraphicsMenu.AddButton(new MenuButton("Green Diffuse: 100%", new Color4(F, 1f, F, 1f), null, DiffuseGreenReflectivity, null, Inp.Key.Unknown, -1f, sender => "Diffuse Green: " + (int)(DiffuseGreenReflectivity.Value * 100) + "%"));
            GraphicsMenu.AddButton(new MenuButton("Blue Diffuse: 100%", new Color4(F, F, 1f, 1f), null, DiffuseBlueReflectivity, null, Inp.Key.Unknown, -1f, sender => "Diffuse Blue: " + (int)(DiffuseBlueReflectivity.Value * 100) + "%"));
            GraphicsMenu.AddButton(new MenuButton("Red Ambient: 100%", new Color4(1f, F, F, 1f), null, AmbientRedReflectivity, null, Inp.Key.Unknown, -1f, sender => "Ambient Red: " + (int)(AmbientRedReflectivity.Value * 100) + "%"));
            GraphicsMenu.AddButton(new MenuButton("Green Ambient: 100%", new Color4(F, 1f, F, 1f), null, AmbientGreenReflectivity, null, Inp.Key.Unknown, -1f, sender => "Ambient Green: " + (int)(AmbientGreenReflectivity.Value * 100) + "%"));
            GraphicsMenu.AddButton(new MenuButton("Blue Ambient: 100%", new Color4(F, F, 1f, 1f), null, AmbientBlueReflectivity, null, Inp.Key.Unknown, -1f, sender => "Ambient Blue: " + (int)(AmbientBlueReflectivity.Value * 100) + "%"));
            GraphicsMenu.UpdateSpacing();

            ConfirmationMenu = new Menu("", null);
            Menus.Add(ConfirmationMenu);
            ConfirmationMenu.AddButton(new MenuButton("Yes", Color4.White, null, null, null, Inp.Key.Y));
            ConfirmationMenu.AddButton(new MenuButton("No", Color4.White, sender => Menu.Current = sender.ContainingMenu.ParentMenu == sender.ContainingMenu ? null : sender.ContainingMenu.ParentMenu, null, null, Inp.Key.N));
            ConfirmationMenu.UpdateSpacing();

            foreach (Menu m in Menus)
            {
                foreach (MenuButton t in m.Buttons)
                {
                    if (t.Hotkey != Inp.Key.Unknown)
                        MenuButtonsWithHotkeys.Add(t);
                }
            }

            try
            {
                if (File.Exists("Settings.txt"))
                {
                    sr = new StreamReader("Settings.txt");
                    line = sr.ReadLine();
                    while (line != null)
                    {
                        if (line.Contains(' '))
                        {
                            int spaceIndex = line.IndexOf(' ');
                            string attribute = line.Substring(0, spaceIndex);
                            string value = line.Substring(spaceIndex + 1);
                            foreach (MenuButton t in OptionButtons)
                            {
                                if (attribute == t.BoundVar.SaveName)
                                {
                                    if (t.BoundBool != null)
                                        bool.TryParse(value, out t.BoundBool.Value);
                                    else if (t.BoundFloat != null)
                                        float.TryParse(value, out t.BoundFloat.Value);
                                    break;
                                }
                            }
                        }
                        line = sr.ReadLine();
                    }
                }
            }
            catch
            {
                DisplayTemporaryMessage("Failed to Load Settings", 480, Color4.Yellow);
            }
            finally
            {
                if (sr != null)
                    sr.Close();
            }

            base.WindowState = FullScreenEnabled.Value ? OpenTK.WindowState.Fullscreen : windowStateBeforeFullScreen;

            ResetGame();
            ResetCamera();

            //PauseMenu.Buttons[0].Position 
            //WindowState = OpenTK.WindowState.Maximized;
            base.OnLoad(e);
        }

        IEnumerable<MenuButton> OptionButtons
        {
            get
            {
                foreach (MenuButton t in SettingsMenu.Buttons)
                {
                    if (t.BoundVar != null)
                        yield return t;
                }
                foreach (MenuButton t in GraphicsMenu.Buttons)
                {
                    if (t.BoundVar != null)
                        yield return t;
                }
            }
        }

        void ResetGame()
        {
            moveMadeSinceSave = false;
            List<int> textures = new List<int>(Marble.STARTING_COUNT);
            for (int i = 0; i < 12; i++)
            {
                textures.Add(i);
                textures.Add(i);
                if (i != 7 && i != 9 && i != 10 && i != 11)
                    textures.Add(i);
            }
            textures.Add(3);
            textures.Add(4);
            textures.Add(12);
            textures.Add(13);
            textures.Add(14);
            
            for (int i = 0; i < textures.Count; i++)
                Marbles[i].Type = (CelestialBodyType)textures[i];

            for (int i = 1; i < LIGHT_COUNT; i++)
                SetLightEnabled(i, true);

            List<int> positionSlots = new List<int>(Marble.STARTING_COUNT);

            for (int i = 0; i < Marble.STARTING_COUNT; i++)
                positionSlots.Add(i);
            
            foreach (Marble m in Marbles)
            {
                m.Initialise(positionSlots[Rand.Next(positionSlots.Count)]);
                positionSlots.Remove(m.PositionSlot);
            }
            Marble.Selected = null;
            Moves.Clear();
            UndoLevel = 0;
            Title = "Marbles";
        }

        public string AddTextWrappingSeparators(float textHeight, float fitInWidth, string text, string separator = "\n")
        {
            int totalLengthOfAboveLines = 0;
            string remainder = text;
            string current = "";
            bool loop = true;

            while (loop && remainder.Length > 0)
            {
                int index = -1;
                if (remainder.Contains(' '))
                    index = remainder.IndexOf(' ');
                else
                {
                    loop = false;
                    index = remainder.Length - 1;
                }

                current += remainder.Substring(0, index);
                if (MeasureString(current, textHeight).X >= fitInWidth)
                {
                    int temp = current.LastIndexOf(' ') + 1;
                    text = text.Insert(totalLengthOfAboveLines + temp, separator);
                    totalLengthOfAboveLines += temp + separator.Length;// sCurrent.Length - nIndex;
                    current = current.Substring(temp);
                }
                //else
                current += " ";
                remainder = remainder.Substring(index + 1);
            }
            return text;
        }

        public void Prompt(string question, Action onYesChosen, bool onlyPromptIf = true)
        {
            if (Menu.Current == ConfirmationMenu)
                return;
            if (onlyPromptIf && PromptsEnabled.Value)
            {
                ConfirmationMenu.ParentMenu = Menu.Current;
                ConfirmationMenu.Caption = question;
                ConfirmationMenu.Buttons[0].OnActivate = sender =>
                {
                    Menu.Current = Menu.Current.ParentMenu;
                    onYesChosen();
                };
                Menu.Current = ConfirmationMenu;
            }
            else
                onYesChosen();
        }

        protected override void OnKeyPress(KeyPressEventArgs e)
        {
            if (!TakingTextInput())
                return;
            if (InvalidFileNameChars.Contains(e.KeyChar))
                DisplayTemporaryMessage("Invalid File Name Character", 240, Color4.White);
            else
                TextInput = TextInput.Insert(TextCursorIndex++, e.KeyChar.ToString());
        }

        protected override void OnKeyDown(Inp.KeyboardKeyEventArgs e)
        {
            if (!TakingTextInput())
                return;
            if (e.Key == Inp.Key.BackSpace && TextCursorIndex > 0)
                TextInput = TextInput.Remove(--TextCursorIndex, 1);
            else if (e.Key == Inp.Key.Delete && TextCursorIndex < TextInput.Length)
                TextInput = TextInput.Remove(TextCursorIndex, 1);
            else if (e.Key == Inp.Key.Left)
                TextCursorIndex = TextCursorIndex > 0 ? TextCursorIndex - 1 : TextInput.Length;
            else if (e.Key == Inp.Key.Right)
                TextCursorIndex = TextCursorIndex < TextInput.Length ? TextCursorIndex + 1 : 0;
        }

        bool SaveGame(string fileName)
        {
            StreamWriter sw = null;
            try
            {
                if (!Directory.Exists("Saves"))
                    Directory.CreateDirectory("Saves");
                sw = new StreamWriter(fileName);
                sw.WriteLine("Marbles");
                sw.WriteLine();
                sw.WriteLine("Undos " + UndoLevel);
                sw.WriteLine();
                foreach (Marble m in Marbles)
                    m.Save(sw);
                sw.WriteLine();
                foreach (MarbleMove v in Moves)
                    sw.WriteLine("Move: " + v);
                Title = Path.GetFileNameWithoutExtension(fileName) + " - Marbles";
                DisplayTemporaryMessage("Game Saved Successfully", 360, Color4.Lime);
                moveMadeSinceSave = false;
                return true;
            }
            catch
            {
                //WF.MessageBox.Show(fileName + "\n\n" + e, "Save Error", WF.MessageBoxButtons.OK, WF.MessageBoxIcon.Error);
                DisplayTemporaryMessage("Save Failed", 480, Color4.Yellow);
                return false;
            }
            finally
            {
                if (sw != null)
                    sw.Close();
            }
        }

        bool LoadGame(string fileName)
        {
            StreamReader sr = null;
            try
            {
                sr = new StreamReader(fileName);
                string line = sr.ReadLine();
                int newUndoLevel = 0;
                var newMarbles = new List<Marble>();
                var newMoves = new List<MarbleMove>();

                while (line != null)
                {
                    if (line.StartsWith("Undos "))
                        int.TryParse(line.Substring(line.IndexOf(' ') + 1), out newUndoLevel);
                    else if (line.StartsWith("Marble: "))
                        newMarbles.Add(Marble.Load(line));
                    else if (line.StartsWith("Move: "))
                        newMoves.Add(MarbleMove.Load(line));

                    line = sr.ReadLine();
                }

                for (int i = 1; i < LIGHT_COUNT; i++)
                    SetLightEnabled(i, false);

                for (int i = 0; i < Marbles.Count; i++)
                {
                    if (i < newMarbles.Count)
                        Marbles[i] = newMarbles[i];
                    else
                        Marbles[i].Alive = false;
                }
                UndoLevel = newUndoLevel;
                Moves = newMoves;
                Title = Path.GetFileNameWithoutExtension(fileName) + " - Marbles";
                moveMadeSinceSave = false;
                Marble.Selected = null;
                return true;
            }
            catch
            {
                DisplayTemporaryMessage("Load Failed", 420, Color4.Yellow);
                return false;
            }
            finally
            {
                sr?.Close();
            }
        }

        public void EndCutsceneMode()
        {
            lastMarbleRemovedForVictory = null;
            victoryAnimationTick = 0;
            _camera = _previousCamera;
        }

        void ResetCamera()
        {
            if (FPSModeEnabled.Value)
            {
                _camera.FocusPoint = new Vector3(0f, 22f, 10f);
                _camera.Pitch = -0.58f;
                _camera.FirstPerson = true;
            }
            else
            {
                _camera.FocusPoint = new Vector3(0f, 0f, 0f);
                _camera.Pitch = -0.8f;
                _camera.FirstPerson = false;
            }
            _camera.Yaw = MathHelper.Pi;
            _camera.Zoom = 35f;
        }

        protected override void OnUnload(EventArgs e)
        {
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);
            GL.BindVertexArray(0);
            GL.DeleteBuffers(mVBO_IDs.Length, mVBO_IDs);
            GL.DeleteVertexArray(mVAO_IDs[0]);
            GL.DeleteVertexArray(mVAO_IDs[1]);
            mShader.Delete();
            base.OnUnload(e);
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);

            /*if (mShader != null)
            {
                if (windowHeight > windowWidth)
                {
                    if (windowWidth < 1)
                        windowWidth = 1;

                    float ratio = windowHeight / windowWidth;
                    
                    GL.UniformMatrix4(uProjectionLocation, true, ref projection);
                }
                else
                {
                    if (windowHeight < 1)
                        windowHeight = 1;

                    float ratio = windowWidth / windowHeight;
                    Matrix4 projection = Matrix4.CreateOrthographic(ratio * 10, 10, -1, 1);
                    GL.UniformMatrix4(uProjectionLocation, true, ref projection);
                }
            }*/
            GL.Viewport(ClientRectangle);
        }



        public static bool GetLightingEnabled()
        {
            return lightingEnabled;
        }

        public void SetLightingEnabled(bool enabled)
        {
            SetLightingEnabled(mShader.ShaderProgramID, enabled);
        }

        public static void SetLightingEnabled(int shaderID, bool enabled)
        {
            if (!Menu.Main.LightingEnabled.Value)
                enabled = false;
            if (enabled == lightingEnabled)
                return;
            int uLightingEnabledLocation = GL.GetUniformLocation(shaderID, "uLightingEnabled");
            GL.Uniform1(uLightingEnabledLocation, enabled ? 1 : 0);
            lightingEnabled = enabled;
        }

        public static bool GetTexturesEnabled()
        {
            return texturesEnabled;
        }

        public void SetTexturesEnabled(bool enabled)
        {
            SetTexturesEnabled(mShader.ShaderProgramID, enabled);
        }

        public static void SetTexturesEnabled(int shaderID, bool enabled)
        {
            if (currentlyRendering3D && !Menu.Main.TexturesEnabled.Value)
                enabled = false;
            if (enabled == texturesEnabled)
                return;
            int uTexturesEnabledLocation = GL.GetUniformLocation(shaderID, "uTexturesEnabled");
            GL.Uniform1(uTexturesEnabledLocation, enabled ? 1 : 0);
            texturesEnabled = enabled;
        }

        public static void SetSpecularMapEnabled(int shaderID, bool enabled)
        {
            if (enabled == specularMapEnabled)
                return;
            int uSpecularMapEnabledLocation = GL.GetUniformLocation(shaderID, "uSpecularMapEnabled");
            GL.Uniform1(uSpecularMapEnabledLocation, enabled ? 1 : 0);
            specularMapEnabled = enabled;
        }

        public void SetDrawColour(Color4 colour)
        {
            SetDrawColour(mShader.ShaderProgramID, colour);
        }

        public static void SetDrawColour(int shaderID, Color4 colour)
        {
            int uColourLocation = GL.GetUniformLocation(shaderID, "uColour");
            GL.Uniform4(uColourLocation, colour);
        }

        public bool KeyWasPressedOnce(Inp.Key key)
        {
            return NewKeyboardState.IsKeyDown(key) && oldKeyboardState.IsKeyUp(key);
        }

        float HorizontalFieldOfView(float verticalFov)
        {
            return 2f * (float)Math.Atan(AspectRatio() * Math.Tan(verticalFov / 2f));
            //cotangent(fovy / 2) / aspect
            //return 1f / (float)Math.Tan(VerticalFieldOfView / 2f) / aspectRatio;
        }

        void Undo()
        {
            if (UndoLevel >= Moves.Count)
                return;

            try
            {
                UndoLevel++;
                MarbleMove move = Moves[Moves.Count - UndoLevel];
                if (Marbles.Count(m => m.Alive) == Marble.STARTING_COUNT - 1)
                {
                    Marble marble = Marbles.Find(m => !m.Alive);
                    marble.Alive = true;
                    marble.PositionSlot = move.FromSlot;
                    marble.Type = move.PassOverMarble;
                    marble.AnimateFromSlot = -1;
                }
                else
                {
                    Marble.GetMarbleInSlot(move.ToSlot, Marbles).PositionSlot = move.FromSlot;
                    Marble marble = Marbles.Find(m => !m.Alive);
                    marble.Alive = true;
                    marble.PositionSlot = move.PassOverSlot;
                    marble.Type = move.PassOverMarble;
                }
                Marble.Selected = null;
            }
            catch
            {
                UndoLevel--;
                DisplayTemporaryMessage("Undo Failed", 300, Color4.Yellow);
            }
        }

        void Redo(bool animate)
        {
            if (UndoLevel <= 0)
                return;

            try
            {
                MarbleMove move = Moves[Moves.Count - UndoLevel];

                if (Marbles.Count(m => m.Alive) == Marble.STARTING_COUNT)
                {
                    Marble marble = Marbles.Find(m => m.PositionSlot == move.FromSlot);
                    if (animate)
                    {
                        marble.AnimateFromSlot = marble.PositionSlot;
                        marble.AnimationTick = Marble.MAX_ANIMATION_TICKS;
                        marble.AnimatingAsInitialRemoval = true;
                    }
                    marble.Alive = false;
                }
                else
                {
                    if (Marble.SlotContainsMarble(move.FromSlot, Marbles, out Marble marbleToMove) != true ||
                        Marble.SlotContainsMarble(move.PassOverSlot, Marbles, out Marble marbleToRemove) != true)
                        return;

                    if (animate)
                    {
                        marbleToMove.AnimateFromSlot = marbleToMove.PositionSlot;
                        marbleToMove.AnimationTick = 0;
                        marbleToRemove.AnimateFromSlot = marbleToRemove.PositionSlot;
                        marbleToRemove.AnimationTick = 0;
                    }
                    marbleToMove.PositionSlot = move.ToSlot;
                    marbleToRemove.Alive = false;
                }
                Marble.Selected = null;
            }
            catch
            {
                DisplayTemporaryMessage("Redo Failed", 300, Color4.Yellow);
            }
            UndoLevel--;
        }

        bool TakingTextInput()
        {
            return TextInput != null && Menu.Current != ConfirmationMenu;
        }

        bool InCutsceneOrTextInputMode()
        {
            return InCutsceneMode() || TakingTextInput();
        }

        bool InCutsceneMode()
        {
            return lastMarbleRemovedForVictory != null;
        }

        //int qdp = 0;

        protected override void OnUpdateFrame(FrameEventArgs e)
        {
            /*if (qdp < 8)
            {
                qdp++;
                return;
            }
            else
                qdp = 0;*/

            if (!Focused)
            {
                windowHadFocusLastTick = false;
                return;
            }
            if (nextAction == NextAction.Render)
                return;

            try
            {
                //Console.Clear();
                //for (int i = 0; i < LIGHT_COUNT.Length; i++)
                //    Console.WriteLine(i + ". " + mLightStates[i] + (mLightStates[i] ? " " + mLightPositions[i] : ""));

                NewKeyboardState = Inp.Keyboard.GetState();
                NewMouseState = Inp.Mouse.GetState();
                bool updateMenu = Menu.Current != null && !InCutsceneOrTextInputMode();

                if (updateMenu)
                {
                    Menu.Current.Update(Utils.MousePositionInNDC(MouseX, MouseY, ClientRectangle), NewMouseState, oldMouseState, NewKeyboardState, oldKeyboardState);
                    //return;
                }
                foreach (MenuButton t in MenuButtonsWithHotkeys)
                {
                    if (t.ContainingMenu != ConfirmationMenu || Menu.Current == ConfirmationMenu)
                        t.UpdateHotkeys(NewKeyboardState, oldKeyboardState);
                }
                if (!updateMenu && KeyWasPressedOnce(Inp.Key.Escape))
                {
                    if (TakingTextInput())
                        TextInput = null;
                    else if (InCutsceneMode())
                        EndCutsceneMode();
                    else// if (Menu.Current == null)
                        Menu.Current = PauseMenu;
                    //return;
                }

                if (!InCutsceneOrTextInputMode() && NewKeyboardState.IsKeyUp(Inp.Key.LControl) && NewKeyboardState.IsKeyUp(Inp.Key.RControl))
                {
                    if (KeyWasPressedOnce(Inp.Key.R))
                        ResetCamera();
                    else if (KeyWasPressedOnce(Inp.Key.F))
                        _camera.FirstPerson = !_camera.FirstPerson;
                }
                else if (KeyWasPressedOnce(Inp.Key.Enter) || KeyWasPressedOnce(Inp.Key.KeypadEnter))
                {
                    Prompt("\"" + TextInput + "\" already exists. Do you wish to overwrite it?", () => 
                    {
                        if (SaveGame("Saves\\" + TextInput + ".txt"))
                        {
                            TextInput = null;
                            Menu.Current = Menu.Current.ParentMenu;
                        }
                    }, File.Exists("Saves\\" + TextInput + ".txt"));
                }

                //if (KeyWasPressedOnce(Inp.Key.Up))
                //    Marble.HighlightedSlot = Marble.NextSlotUp(Marble.HighlightedSlot, true) ?? Marble.CENTRE_SLOT;
                //else if (KeyWasPressedOnce(Inp.Key.Right))
                //    Marble.HighlightedSlot = Marble.NextSlotRight(Marble.HighlightedSlot, true) ?? Marble.CENTRE_SLOT;
                //else if (KeyWasPressedOnce(Inp.Key.Down))
                //    Marble.HighlightedSlot = Marble.NextSlotDown(Marble.HighlightedSlot, true) ?? Marble.CENTRE_SLOT;
                //else if (KeyWasPressedOnce(Inp.Key.Left))
                //    Marble.HighlightedSlot = Marble.NextSlotLeft(Marble.HighlightedSlot, true) ?? Marble.CENTRE_SLOT;

                /*Vector2 v = MousePositionInNDC();
                Vector4 mousePositionInClipSpace = new Vector4(v.X, v.Y, 0f, 1f);
                Vector4 mousePositionInViewSpace = Vector4.Transform(mousePositionInClipSpace, projection.Inverted());
                //mousePositionInViewSpace.W = 1f;
                Vector3 mousePositionInWorldSpace = Vector4.Transform(mousePositionInViewSpace, view.Inverted()).Xyz.Normalized();
                Console.WriteLine(mousePositionInWorldSpace);*/
                MarbleSelection marbleSelection = MarbleSelection.None;
                const Inp.Key UNDO_KEY = Inp.Key.Z;
                const Inp.Key REDO_KEY = Inp.Key.X;

                if (NewKeyboardState.IsKeyUp(Inp.Key.LControl) && NewKeyboardState.IsKeyUp(Inp.Key.RControl))
                {
                    if (!InCutsceneOrTextInputMode())
                    {
                        //FocusPoint = SunPosition;
                        bool shift = NewKeyboardState.IsKeyDown(Inp.Key.LShift) || NewKeyboardState.IsKeyDown(Inp.Key.RShift);

                        TicksBeforeNextUndoRedo--;

                        if (shift && TicksBeforeNextUndoRedo <= 0)
                        {
                            if (NewKeyboardState.IsKeyDown(UNDO_KEY))
                            {
                                Undo();
                                TicksBeforeNextUndoRedo = MAX_TICKS_BEFORE_NEXT_UNDO_REDO;
                            }
                            else if (NewKeyboardState.IsKeyDown(REDO_KEY))
                            {
                                Redo(false);
                                TicksBeforeNextUndoRedo = MAX_TICKS_BEFORE_NEXT_UNDO_REDO;
                            }
                        }
                        else if (KeyWasPressedOnce(UNDO_KEY))
                            Undo();
                        else if (KeyWasPressedOnce(REDO_KEY))
                            Redo(MoveAnimationEnabled.Value);

                        bool forward = NewKeyboardState.IsKeyDown(Inp.Key.W);
                        bool backward = NewKeyboardState.IsKeyDown(Inp.Key.S);
                        bool left = NewKeyboardState.IsKeyDown(Inp.Key.A);
                        bool right = NewKeyboardState.IsKeyDown(Inp.Key.D);
                        bool down = NewKeyboardState.IsKeyDown(Inp.Key.Q);
                        bool up = NewKeyboardState.IsKeyDown(Inp.Key.E);
                        Vector3 displacement = Vector3.Zero;

                        if (forward && !backward)
                            displacement += _camera.Forward;
                        else if (backward && !forward)
                            displacement -= _camera.Forward;
                        if (left && !right)
                            displacement -= _camera.Right;
                        else if (right && !left)
                            displacement += _camera.Right;
                        if (down && !up)
                            displacement -= _camera.Up;
                        else if (up && !down)
                            displacement += _camera.Up;
                        if (NewKeyboardState.IsKeyDown(Inp.Key.LAlt) || NewKeyboardState.IsKeyDown(Inp.Key.RAlt))
                            displacement -= Vector3.UnitZ;
                        if (NewKeyboardState.IsKeyDown(Inp.Key.Space))
                            displacement += Vector3.UnitZ;

                        if (displacement != Vector3.Zero)
                        {
                            float speed = shift ? 0.3f : 0.06f;
                            if (FPSModeEnabled.Value && Menu.Current == null)
                                speed *= 1.5f;
                            _camera.FocusPoint += displacement.Normalized() * speed;
                        }

                        if (windowHadFocusLastTick)
                            _camera.Zoom *= (float)Math.Pow(1.2f, oldMouseState.Wheel - NewMouseState.Wheel);

                        bool fpsMode = FPSModeEnabled.Value && Menu.Current == null;
                        float CAMERA_ROTATE_SPEED = fpsMode ? NewMouseState.RightButton == Inp.ButtonState.Pressed ? 0.001f : 0.0025f : 0.007f;
                        float ARROW_ROTATION_MULTIPLIER = 2.5f * (fpsMode ? 2f : 1f) * (shift ? 5f : 1f);

                        left = NewKeyboardState.IsKeyDown(Inp.Key.Left);
                        right = NewKeyboardState.IsKeyDown(Inp.Key.Right);
                        forward = NewKeyboardState.IsKeyDown(Inp.Key.C);// || (shift && NewKeyboardState.IsKeyDown(Inp.Key.Up));
                        backward = NewKeyboardState.IsKeyDown(Inp.Key.V);// || (shift && NewKeyboardState.IsKeyDown(Inp.Key.Down));
                        up = /*!shift && */NewKeyboardState.IsKeyDown(Inp.Key.Up);
                        down = /*!shift && */NewKeyboardState.IsKeyDown(Inp.Key.Down);

                        if (left && !right)
                            _camera.Yaw -= CAMERA_ROTATE_SPEED * ARROW_ROTATION_MULTIPLIER;
                        else if (right && !left)
                            _camera.Yaw += CAMERA_ROTATE_SPEED * ARROW_ROTATION_MULTIPLIER;
                        if (forward && !backward)
                            _camera.Zoom /= 1.04f;
                        else if (backward & !forward)
                            _camera.Zoom *= 1.04f;
                        if (down && !up)
                            _camera.Pitch -= CAMERA_ROTATE_SPEED * ARROW_ROTATION_MULTIPLIER;
                        else if (up && !down)
                            _camera.Pitch += CAMERA_ROTATE_SPEED * ARROW_ROTATION_MULTIPLIER;

                        if (NewMouseState.RightButton == Inp.ButtonState.Pressed || NewMouseState.MiddleButton == Inp.ButtonState.Pressed || fpsMode)
                        {
                            if (fpsMode && NewMouseState.MiddleButton == Inp.ButtonState.Released)
                                marbleSelection = MarbleSelection.Crosshair;

                            CursorVisible = false;
                            Point p = PointToScreen(new Point(MouseX, MouseY));
                            Inp.Mouse.SetPosition(p.X, p.Y);

                            if (windowHadFocusLastTick)
                            {
                                _camera.Yaw += (NewMouseState.X - oldMouseState.X) * CAMERA_ROTATE_SPEED;
                                _camera.Pitch -= (NewMouseState.Y - oldMouseState.Y) * CAMERA_ROTATE_SPEED;
                            }
                        }
                        else
                        {
                            marbleSelection = MarbleSelection.Mouse;
                            CursorVisible = true;
                        }
                        if (Menu.Current != null && Menu.Current.SelectedButton != null)
                            marbleSelection = MarbleSelection.None;

                        if (_camera.Pitch > MathHelper.PiOver2 - 0.01f)
                            _camera.Pitch = MathHelper.PiOver2 - 0.01f;
                        else if (_camera.Pitch < -MathHelper.PiOver2 + 0.01f)
                            _camera.Pitch = -MathHelper.PiOver2 + 0.01f;
                        //if (CameraPitch > MathHelper.ThreePiOver2)
                        //    CameraPitch -= MathHelper.TwoPi;
                        //else if (CameraPitch < -MathHelper.PiOver2)
                        //    CameraPitch += MathHelper.TwoPi;
                        //DisplayTemporaryMessage(CameraPitch.ToString(), 90, Color4.White);

                        if (_camera.Zoom > 750f)
                            _camera.Zoom = 750f;
                        else if (_camera.Zoom < 0.01f)
                            _camera.Zoom = 0.01f;
                        //DisplayTemporaryMessage("Zoom: " + CameraZoom, 90, Color4.White);
                    }
                    else if (KeyWasPressedOnce(UNDO_KEY))
                    {
                        EndCutsceneMode();
                        Undo();
                    }
                }

                SunAngle += 0.007f;
                int marbleCount = Marbles.Count(m => m.Alive || m.AnimateFromSlot >= 0);
                if (marbleCount <= 1f)
                {
                    SetLightProperties(0, 30f, new Vector3(0.63f, 0.81f, 0.9f), new Vector3(0.7f, 0.9f, 1.0f), new Vector3(0.7f, 0.9f, 1.0f));
                }
                else
                {
                    float redGiant = MainSunRedGiantAmount(marbleCount);
                    SetLightProperties(0, 45f, new Vector3(0.9f, 0.9f - 0.45f * redGiant, 0.9f - 0.45f * redGiant), new Vector3(1.0f, 1.0f - redGiant / 2f, 1.0f - redGiant / 2f), new Vector3(1.0f, 1.0f - redGiant / 2f, 1.0f - redGiant / 2f));
                }

                if (lastMarbleRemovedForVictory == null)
                    _previousCamera = _camera.Copy;

                if (Marble.UpdateAllSlots(mShader.ShaderProgramID, MouseX, MouseY, ref NewKeyboardState, ref NewMouseState, ref oldMouseState, marbleSelection, ClientRectangle, _camera, ref Projection, ref View, Marbles, Moves, ref UndoLevel, SpinningMarblesEnabled.Value, MoveAnimationEnabled.Value, ref lastMarbleRemovedForVictory))
                    moveMadeSinceSave = true;

                if (lastMarbleRemovedForVictory != null && !MoveAnimationEnabled.Value)
                    lastMarbleRemovedForVictory = null;

                const int MAX_CAMERA_TRANSITION_TICKS = Marble.MAX_ANIMATION_TICKS + Marble.MAX_DEATH_ANIMATION_TICKS;

                if (lastMarbleRemovedForVictory == null)
                {
                    victoryAnimationTick = 0;
                }
                else if (victoryAnimationTick > MAX_VICTORY_ANIMATION_TICKS + VICTORY_CAMERA_LINGER_TICKS)
                {
                    EndCutsceneMode();
                }
                else
                {
                    if (lastMarbleRemovedForVictory.AnimateFromSlot >= 0 && lastMarbleRemovedForVictory.AnimationTick < MAX_CAMERA_TRANSITION_TICKS)
                    {
                        _camera.FocusPoint = lastMarbleRemovedForVictory.Position();
                        _camera.Yaw = Utils.GetYaw(SunPosition - _camera.FocusPoint);
                    }
                    else
                    {
                        _camera.FocusPoint = SunPosition;
                    }
                    if (victoryAnimationTick >= MAX_CAMERA_TRANSITION_TICKS)
                    {
                        _camera.Pitch = 0f;
                        _camera.Zoom = 32f;
                    }
                    else
                    {
                        _camera.Pitch = 1.4f - 1.4f * victoryAnimationTick / MAX_CAMERA_TRANSITION_TICKS;
                        _camera.Zoom = 4f + 28f * victoryAnimationTick / MAX_CAMERA_TRANSITION_TICKS;
                    }
                    _camera.FirstPerson = false;
                    victoryAnimationTick++;
                }
            }
            finally
            {
                View = Matrix4.LookAt(_camera.Position, _camera.Position + _camera.Forward, Vector3.UnitZ);
                int uView = GL.GetUniformLocation(mShader.ShaderProgramID, "uView");
                GL.UniformMatrix4(uView, true, ref View);

                SunPosition = new Vector3((float)Math.Cos(SunAngle) * 30f, (float)Math.Sin(SunAngle) * 30f, 40f);
                
                mLightPositions[0] = SunPosition;
                
                for (int i = 0; i < LIGHT_COUNT; ++i)
                {
                    if (!mLightStates[i])
                        continue;

                    int uLightPosition = GL.GetUniformLocation(mShader.ShaderProgramID, "uLight[" + i + "].Position");
                    Vector4 lightPosition = Vector4.Transform(new Vector4(mLightPositions[i], 1f), View);
                    GL.Uniform4(uLightPosition, ref lightPosition);
                }

                //FieldOfView.Value = (float)(Marbles.Count(m => m.Alive) - 1) / (Marble.STARTING_COUNT - 1);

                base.OnUpdateFrame(e);
                nextAction = NextAction.Render;
                oldKeyboardState = NewKeyboardState;
                oldMouseState = NewMouseState;
                windowHadFocusLastTick = true;

                if (CursorVisible)
                {
                    Inp.MouseState ms = Inp.Mouse.GetCursorState();
                    Point mousePos = PointToClient(new Point(ms.X, ms.Y));
                    MouseX = mousePos.X;
                    MouseY = mousePos.Y;
                }
                if (TemporaryMessageTicks > 0)
                    TemporaryMessageTicks--;
            }
        }

        //float r = 0f;

        float MainSunRedGiantAmount(int marbleCount = -1)
        {
            if (marbleCount < 0)
                marbleCount = Marbles.Count(m => m.Alive || m.AnimateFromSlot >= 0);
            float amount = 1f - (marbleCount - 2) / 12f;
            if (amount < 0f)
                amount = 0f;
            return amount;
        }

        protected override void OnRenderFrame(FrameEventArgs e)
        {
            if (nextAction == NextAction.Update && Focused)
                return;

            base.OnRenderFrame(e);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            //int uEyePosition = GL.GetUniformLocation(mShader.ShaderProgramID, "uEyePosition");
            //Vector4 eyePosition = new Vector4(0, 0, 0, 1);// Vector4.Transform(new Vector4(0, 0.5f, 0, 1), mView);
            //GL.Uniform4(uEyePosition, ref eyePosition);

            int uEyePosition = GL.GetUniformLocation(mShader.ShaderProgramID, "uEyePosition");
            Vector4 eyePosition = Vector4.Transform(new Vector4(_camera.Position, 1f), View);
            GL.Uniform4(uEyePosition, ref eyePosition);

            int greyscaleLocation = GL.GetUniformLocation(mShader.ShaderProgramID, "uGreyscale");
            GL.Uniform1(greyscaleLocation, GreyscaleEnabled.Value ? 1 : 0);

            //Matrix4 mProjection = Matrix4.CreatePerspectiveFieldOfView(MathHelper.PiOver2, (float)ClientSize.Width / ClientSize.Height, float.Epsilon, float.MaxValue);
            //int uProjection = GL.GetUniformLocation(mShader.ShaderProgramID, "uProjection");  
            //GL.UniformMatrix4(uProjection, true, ref mProjection);

            int uModelLocation = GL.GetUniformLocation(mShader.ShaderProgramID, "uModel");
            //Matrix4 rotation = Matrix4.CreateRotationZ(0.8f);
            Matrix4 translation = Matrix4.CreateTranslation(0.5f, 0, 0);
            //GL.UniformMatrix4(uModelLocation, true, ref translation);
            const int PASS_COUNT = 1;
            currentlyRendering3D = true;
            

            int uProjectionLocation = GL.GetUniformLocation(mShader.ShaderProgramID, "uProjection");
                Projection = Matrix4.CreatePerspectiveFieldOfView(FieldOfView.ModifiedValue, AspectRatio(), 0.1f, 1000f);
            GL.UniformMatrix4(uProjectionLocation, true, ref Projection);

            SetLightingEnabled(false);
            // Skybox (i.e. empty space)
            if (SkyboxEnabled.Value)
            {
                TexMilkyWay.Bind();
                GL.Disable(EnableCap.CullFace);
                GL.Disable(EnableCap.DepthTest);
                //r -= 0.002f;
                //CameraZoom -= 0.026f;
                translation = /*Matrix4.CreateRotationZ(r) * */ Matrix4.CreateScale(16f) * Matrix4.CreateTranslation(_camera.Position);
                //VerticalFieldOfView = -r;

                /*try
                {
                    int uProjectionLocation = GL.GetUniformLocation(mShader.ShaderProgramID, "uProjection");
                    projection = Matrix4.CreatePerspectiveFieldOfView(VerticalFieldOfView, AspectRatio(), 0.1f, float.MaxValue);
                    GL.UniformMatrix4(uProjectionLocation, true, ref projection);
                }
                catch { }*/

                GL.UniformMatrix4(uModelLocation, true, ref translation);
                Sphere.Draw(mShader.ShaderProgramID);
                GL.Enable(EnableCap.DepthTest);
                GL.Enable(EnableCap.CullFace);
            }

            //SetMaterialProperties(new Vector3(0.5f, 0.5f, 0.5f), new Vector3(1f, 1f, 1f), new Vector3(0.75f, 0.75f, 0.75f), 0.080f * 128f);
            //System.Threading.Thread.Sleep(1000);
            //TexMercury.Bind();
            //SetMaterialProperties(new Vector3(0.25f, 0.20725f, 0.20725f), new Vector3(1f, 0.829f, 0.829f), new Vector3(0.296648f, 0.296648f, 0.296648f), 0.088f * 128f); // Pearl
            //MaterialProperties.Set(mShader.ShaderProgramID, new Vector3(0.025f, 0.025f, 0.025f), new Vector3(0.1f, 0.1f, 0.1f), new Vector3(0.5f, 0.5f, 0.5f), 0.040f * 128f);

            //SetMaterialProperties(new Vector3(0.1f, 0.2f, 0.3f), new Vector3(0.2f, 0.875f, 1f), new Vector3(0.6f, 0.6f, 0.6f), 0.040f * 128f);
            //SetMaterialProperties(new Vector3(2f, 2f, 2f), new Vector3(1f, 1f, 1f), new Vector3(0f, 0f, 0f), 0.040f * 128f);
            // Focus point

            if (ShowSunEnabled.Value || InCutsceneMode())
            {
                const float FINAL_SUN_RADIUS = 0.75f;
                Color4 colour;
                Texture texture = TexSun;
                //if (victoryAnimationTick > MAX_VICTORY_ANIMATION_TICKS - MAX_SUN_DEATH_TICKS)
                //    colour.R *= 1f - (float)(victoryAnimationTick + MAX_SUN_DEATH_TICKS - MAX_VICTORY_ANIMATION_TICKS) / MAX_SUN_DEATH_TICKS;

                int marbleCount = Marbles.Count(m => m.Alive || m.AnimateFromSlot >= 0);
                float radius = 2f + 0.25f * (Marble.STARTING_COUNT - marbleCount);

                if (marbleCount <= 1)
                {
                    if (victoryAnimationTick > MAX_VICTORY_ANIMATION_TICKS - MAX_SUN_DEATH_TICKS)
                    {
                        float interpolation = (float)(victoryAnimationTick + MAX_SUN_DEATH_TICKS - MAX_VICTORY_ANIMATION_TICKS) / MAX_SUN_DEATH_TICKS;
                        if (interpolation > 1f)
                            interpolation = 1f;
                        radius = FINAL_SUN_RADIUS + (1f - interpolation) * (radius - FINAL_SUN_RADIUS);
                        interpolation *= interpolation * interpolation;
                        colour = new Color4(1.5f, 1.5f * interpolation, 1.5f * interpolation, 1f);
                    }
                    else
                    {
                        radius = FINAL_SUN_RADIUS;
                        colour = new Color4(1.5f, 1.5f, 1.5f, 1f);
                    }
                    texture = Marble.CelestialBodies[CelestialBodyType.Uranus].TextureMap;
                }
                else
                {
                    float redGiantAmount = MainSunRedGiantAmount(marbleCount);
                    colour = new Color4(1f, 1f - redGiantAmount, 1f - redGiantAmount, 1f);
                    //colour.R *= 1f - (float)(victoryAnimationTick + MAX_SUN_DEATH_TICKS - MAX_VICTORY_ANIMATION_TICKS) / MAX_SUN_DEATH_TICKS;
                }

                if (Utils.Distance(SunPosition, _camera.Position) > radius * 1.5f)
                { // Sun
                    SetDrawColour(colour);
                    texture.Bind();
                    translation = Matrix4.CreateScale(radius) * Matrix4.CreateRotationZ(-2f * SunAngle) * Matrix4.CreateTranslation(SunPosition);
                    GL.UniformMatrix4(uModelLocation, true, ref translation);
                    Sphere.Draw(mShader.ShaderProgramID);
                }
                SetDrawColour(Color4.White);
            }
            MaterialProperties.SetSpecular(mShader.ShaderProgramID, new Vector3(SpecularRedReflectivity.Value, SpecularGreenReflectivity.Value, SpecularBlueReflectivity.Value));
            MaterialProperties.SetDiffuse(mShader.ShaderProgramID, new Vector3(DiffuseRedReflectivity.Value, DiffuseGreenReflectivity.Value, DiffuseBlueReflectivity.Value));
            MaterialProperties.SetAmbient(mShader.ShaderProgramID, new Vector3(AmbientRedReflectivity.Value, AmbientGreenReflectivity.Value, AmbientBlueReflectivity.Value));

            //Textures[CelestialBody.Earth].Bind();
            //SetLightingEnabled(true);
            if (ShowFocusPointEnabled.Value && !_camera.FirstPerson)
            { // Focus point
                const float SIZE = 0.125f;
                translation = Matrix4.CreateScale(SIZE) * Matrix4.CreateTranslation(_camera.FocusPoint);
                GL.UniformMatrix4(uModelLocation, true, ref translation);
                float distance = Utils.Distance(_camera.FocusPoint, _camera.Position);

                if (distance > 2f)
                {
                    SetTexturesEnabled(false);
                    SetDrawColour(new Color4(0f, 0.5f, 1f, 1f));
                    Sphere.Draw(mShader.ShaderProgramID);
                    //Sphere.Bind(mShader.ShaderProgramID);
                    SetDrawColour(Color4.White);
                    SetTexturesEnabled(true);
                }
                else if (distance > SIZE + Marble.CLIPPING_DISTANCE)
                {
                    Marble.SetDrawProperties(mShader.ShaderProgramID, CelestialBodyType.Earth);
                    SetLightingEnabled(true);
                    Sphere.Draw(mShader.ShaderProgramID);
                }
            }

            SetDrawColour(Color4.White);
            SetLightingEnabled(true);
            SetTexturesEnabled(true);
            Marble.DrawAllSlots(uModelLocation, mShader.ShaderProgramID, Marbles, CursorVisible, _camera, SpecularPower.ModifiedValue);
            SetLightingEnabled(false);

            /*SetLightingEnabled(true);
            MaterialProperties.Set(mShader.ShaderProgramID, new Vector3(0.25f), new Vector3(0.5f), new Vector3(1f), 40f);
            Marble.CelestialBodies[CelestialBodyType.Earth].TextureMap.Bind();
            BindModel(Model.Ring);
            translation = Matrix4.CreateScale(4f) * Matrix4.CreateTranslation(6f, 6f, -2f);
            GL.UniformMatrix4(uModelLocation, true, ref translation);
            GL.DrawElements(Ring.IntendedDrawMode, Ring.Indices.Length, DrawElementsType.UnsignedInt, 0);
            //translation = Matrix4.CreateRotationX(MathHelper.Pi) * Matrix4.CreateScale(4f) * Matrix4.CreateTranslation(6f, 6f, 1.5f);
            //GL.UniformMatrix4(uModelLocation, true, ref translation);
            //GL.DrawElements(Ring.IntendedDrawMode, Ring.Indices.Length, DrawElementsType.UnsignedInt, 0);
            SetLightingEnabled(false);*/

            //Square.Bind(mShader.ShaderProgramID);
            //SetLightingEnabled(false);

            // Hud
            //GL.Enable(EnableCap.AlphaTest);
            currentlyRendering3D = false;
            SetTexturesEnabled(true);
            GL.Disable(EnableCap.DepthTest);
            //GL.BindVertexArray(mVAO_IDs[ModelIDs[Model.Square]]);
            int u3DLocation = GL.GetUniformLocation(mShader.ShaderProgramID, "u3D");
            GL.Uniform1(u3DLocation, 0);
            Vector2 v = Utils.MousePositionInNDC(MouseX, MouseY, ClientRectangle);
            //float aspectRatio = (float)ClientRectangle.Width / ClientRectangle.Height;

            //v.X = 0f;
            //v.Y = 0f;
            //SetDrawColour(new Color4(1f, 1f, 1f, 0.5f));
            //DrawSprite(uModelLocation, Alignment.BottomRight, 1f, -1f, 0.5f, 0.5f);
            //DrawString(uModelLocation, "Hello World", Alignment.BottomRight, v.X, v.Y, 0.5f, Color4.White);
            //SetDrawColour(Color4.White);

            /*TexFont.Bind();
            GL.Disable(EnableCap.Blend);
            Blt(10, 40, TextureWidth, TextureHeight);
            GL.Enable(EnableCap.Blend);
            Settings.Text = "Test";
            DrawText(0, 0, Settings.Text);*/
            if (!InCutsceneMode())
            {
                int marbleCount = Marbles.Count(m => m.Alive);
                DrawString(uModelLocation, "Marbles: " + marbleCount, Alignment.BottomLeft, -0.95f, -0.95f, 0.125f);
                if (Menu.Current != null)
                    DrawString(uModelLocation, Title, Alignment.Top, 0f, 0.95f, 0.125f);

                string message = null;
                if (marbleCount >= Marble.STARTING_COUNT)
                    message = "Choose your initial empty slot.";
                else if (marbleCount <= 1)
                    message = "Victory is yours! Remember to save to replay later.";
                else if (Marble.GameIsOver(Marbles))
                    message = "Game over. Remember to save to replay later.";
                if (message != null)
                    DrawString(uModelLocation, message, Alignment.Top, 0f, Menu.Current == null ? 0.95f : 0.85f, 0.09f);
            }

            //DrawString(uModelLocation, string.Format("Pos {0}, Pitch {1}", CameraPosition, CameraPitch), Alignment.Top, 0f, Menu.Current == null ? 0.95f : 0.85f, 0.09f);

            if (TakingTextInput())
            {
                DrawString(uModelLocation, "Enter your save name (press Esc to cancel):", Alignment.Bottom, 0f, 0.05f, 0.125f, Color4.White);
                DrawString(uModelLocation, TextInput, Alignment.Top, 0f, -0.05f, 0.08f, Color4.White, -1f, TextCursorIndex);
            }
            else if (Menu.Current != null && !InCutsceneMode())
            {
                if (ShowControlsEnabled.Value)
                {
                    float x = 1f - 0.85f / AspectRatio();
                    const float Y_DELTA = -0.085f;
                    const float HEIGHT = 0.08f;
                    const int LINE_COUNT = 18;
                    const float Y_START = (LINE_COUNT - 1) * -Y_DELTA / 2f;
                    for (int i = 0; i < LINE_COUNT; i++)
                    {
                        string text;
                        Color4? colour = null;
                        switch (i)
                        {
                            case 0: text = "Esc - Menu Back & Show/Hide UI"; colour = Color4.Yellow; break;
                            case 1: text = "W - Move Foward"; colour = Color4.White; break;
                            case 2: text = "S - Move Backward"; break;
                            case 3: text = "A - Move Left"; break;
                            case 4: text = "D - Move Right"; break;
                            case 5: text = "Q - Move Down"; break;
                            case 6: text = "E - Move Up"; break;
                            case 7: text = "Alt - Move Down (World Space)"; break;
                            case 8: text = "Space - Move Up (World Space)"; break;
                            case 9: text = "C & Wheel Up - Zoom In"; break;
                            case 10: text = "V & Wheel Down - Zoom Out"; break;
                            case 11: text = "R - Reset Camera"; colour = Color4.Yellow; break;
                            case 12: text = "F - Toggle Perspective"; colour = Color4.White; break;
                            case 13: text = "Arrows & R/M Mouse - Rotate"; break;
                            case 14: text = "Z - Undo"; colour = Color4.Yellow; break;
                            case 15: text = "X - Redo"; break;
                            case 16: text = "Shift - Move/Undo/Redo Faster"; colour = Color4.White; break;
                            case 17: text = "Shift Click - Focus on Marble"; break;
                            default: throw new Exception();
                        }
                        DrawString(uModelLocation, text, Alignment.Left, x, Y_START + Y_DELTA * i, HEIGHT, colour);
                    }
                }
                Menu.Current.Draw(uModelLocation, v);
            }

            if (TemporaryMessageTicks > 0)
            {
                if (TemporaryMessageTicks >= 90)
                    DrawString(uModelLocation, TemporaryMessageText, Alignment.Bottom, 0f, -0.95f, 0.125f, TemporaryMessageColour);
                else
                    DrawString(uModelLocation, TemporaryMessageText, Alignment.Bottom, 0f, -0.95f, 0.125f, new Color4(TemporaryMessageColour.R, TemporaryMessageColour.G, TemporaryMessageColour.B, TemporaryMessageColour.A * TemporaryMessageTicks / 90f));
            }

            SetDrawColour(Color4.White);

            //Vector2 mousePos = Utils.MousePositionInNDC(MouseX, MouseY, ClientRectangle);
            //DrawString(uModelLocation, "Hello\nWorld\nTest Test Test Test Test Test\nNew Line", Alignment.Centre, mousePos.X, mousePos.Y, 0.1f);

            if (FPSModeEnabled.Value && _camera.FirstPerson && Menu.Current == null && !InCutsceneMode())
                DrawString(uModelLocation, "+", Alignment.Centre, 0f, 0f, 0.1f);
            
            //DrawString(uModelLocation, "Y" /*string.Format("Yaw: {0}, Pitch: {1}, Zoom: {2}, Focus: {3}\nPosition: {4}, Direction: {5}", CameraYaw, CameraPitch, CameraZoom, FocusPoint, CameraPosition, CameraForward())*/, Alignment.BottomRight, 1f, -0.95f, 0.075f);
            //DrawSprite(uModelLocation, Alignment.Centre, 0f, 0f, 0.6f, 0.4f);

            GL.Uniform1(u3DLocation, 1);
            SetLightingEnabled(true);
            //Model.Unbind();
            GL.Enable(EnableCap.DepthTest);
            SwapBuffers();

            nextAction = NextAction.Update;
        }

        public void DisplayTemporaryMessage(string message, int duration, Color4 colour)
        {
            TemporaryMessageText = message;
            TemporaryMessageTicks = duration;
            TemporaryMessageColour = colour;
        }

        public float AspectRatio()
        {
            return (float)ClientRectangle.Width / ClientRectangle.Height;
        }

        public void DrawSprite(int uModelLocation, Alignment alignment, float x, float y, float width, float height, Texture texture = null, Color4? colour = null)
        {
            if (colour != null)
                SetDrawColour(colour.Value);
            if (texture != null)
                texture.Bind();
            float aspectRatio = AspectRatio();
            float scaleX = width / aspectRatio / 2f;
            height /= 2f;
            switch (alignment)
            {
                case Alignment.TopLeft: x += scaleX; y -= height; break;
                case Alignment.Top: y -= height; break;
                case Alignment.TopRight: x -= scaleX; y -= height; break;
                case Alignment.Left: x += scaleX; break;
                case Alignment.Centre: break;
                case Alignment.Right: x -= scaleX; break;
                case Alignment.BottomLeft: x += scaleX; y += height; break;
                case Alignment.Bottom: y += height; break;
                case Alignment.BottomRight: x -= scaleX; y += height; break;
                default: throw new Exception();
            }
            Matrix4 uModel = Matrix4.CreateScale(scaleX * 2f, height * 2f, 1f) * Matrix4.CreateTranslation(x, y, 0f);
            GL.UniformMatrix4(uModelLocation, true, ref uModel);
            Square.Draw(mShader.ShaderProgramID);
        }

        const float CHARACTER_SPACING_ADJUSTMENT = -0.125f;

        public bool DrawString(int uModelLocation, string text, Alignment alignment, float x, float y, float height, Color4? colour = null, float maxWidth = -1f, int cursorIndex = -1)
        {
            if (colour != null)
                SetDrawColour(colour.Value);

            float aspectRatio = AspectRatio();
            Vector2 size = MeasureString(text, height);
            size.X /= aspectRatio;
            switch (alignment)
            {
                case Alignment.TopLeft: break;
                case Alignment.Top: x -= size.X / 2f; break;
                case Alignment.TopRight: x -= size.X; break;
                case Alignment.Left: y += size.Y / 2f; break;
                case Alignment.Centre: x -= size.X / 2f; y += size.Y / 2f; break;
                case Alignment.Right: x -= size.X; y += size.Y / 2f; break;
                case Alignment.BottomLeft: y += size.Y; break;
                case Alignment.Bottom: x -= size.X / 2f; y += size.Y; break;
                case Alignment.BottomRight: x -= size.X; y += size.Y; break;
                default: throw new Exception();
            }
            float totalWidth = 0f;
            float totalHeight = 0f;
            float cursorX = -1f;
            int index = 0;
            foreach (char character in text)
            {
                if (character == '\n')
                {
                    totalWidth = 0f;
                    totalHeight += height;
                    continue;
                }
                char c = Chars.ContainsKey(character) ? character : '?';
                if (maxWidth >= 0f && totalWidth + (Chars[c].Width + CHARACTER_SPACING_ADJUSTMENT) * height / aspectRatio > maxWidth)
                    return false;
                DrawSprite(uModelLocation, Alignment.TopLeft, x + totalWidth, y - totalHeight, height * Chars[c].Width, height, Chars[c].Tex);
                totalWidth += (Chars[c].Width + CHARACTER_SPACING_ADJUSTMENT) * height / aspectRatio;
                index++;
                if (index == cursorIndex)
                    cursorX = totalWidth;
            }
            if (cursorIndex == 0 && cursorX < 0f)
                cursorX = 0f;
            if (cursorX >= 0f)
            {
                SetTexturesEnabled(false);
                DrawSprite(uModelLocation, Alignment.TopLeft, x + cursorX, y, 0.008f, height, null, Utils.InterpolateColour(Color4.Red, Color4.Yellow, (float)FlashTicks / MAX_FLASH_TICKS));
                SetTexturesEnabled(true);
            }
            return true;
        }

        public Vector2 MeasureString(string text, float height)
        {
            float longestWidth = 0f;
            float width = 0f;
            float totalHeight = height;

            foreach (char c in text)
            {
                if (c == '\n')
                {
                    if (longestWidth < width)
                        longestWidth = width;
                    width = 0f;
                    totalHeight += height;
                }
                else if (Chars.ContainsKey(c))
                    width += Chars[c].Width + CHARACTER_SPACING_ADJUSTMENT;
                else
                    width += Chars['?'].Width + CHARACTER_SPACING_ADJUSTMENT;
            }
            if (longestWidth < width)
                longestWidth = width;

            return new Vector2(longestWidth * height, totalHeight);
        }

        [STAThread]
        static void Main()
        {
            using (Game game = new Game())
            {
                game.Run(60);
            }
        }
    }
}
