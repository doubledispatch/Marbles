using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Drawing;
using System.IO;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using Inp = OpenTK.Input;

namespace Marbles
{
    class Marble
    {
        public static ImmutableDictionary<CelestialBodyType, CelestialBody> CelestialBodies { get; private set; }

        public CelestialBodyType Type { get; set; }
        public int PositionSlot { get; set; }
        public float Yaw { get; private set; }
        public bool RotatingClockwise { get; private set; } = true;
        public bool Alive { get; set; } = true;
        public bool AnimatingAsInitialRemoval { get; set; }

        public int AnimateFromSlot { get; set; } = -1;
        public int AnimationTick { get; set; }

        public const int MAX_ANIMATION_TICKS = 60;
        public const int MAX_DEATH_ANIMATION_TICKS = 90;
        public static Model Model { get; set; }
        public static Model ModelRing { get; set; }
        public static Model ModelCylinder { get; set; }
        public const int STARTING_COUNT = 37;
        public const int CENTRE_SLOT = 18;
        public static int? HighlightedSlot { get; set; }
        public static Marble Selected { get; set; }

        public const float CLIPPING_DISTANCE = 0.1f;
        public static float SelectionAnimationTick { get; set; }
        public const float MAX_SELECTION_ANIMATION_TICK = 60;

        public static void InitialiseCelestialBodies(string texturesDirectory)
        {
            CelestialBodies = new Dictionary<CelestialBodyType, CelestialBody>
            {
                [CelestialBodyType.Mercury] = new CelestialBody("Mercury", new Texture(texturesDirectory + "Mercury.jpg")),
                [CelestialBodyType.Venus] = new CelestialBody("Venus", new Texture(texturesDirectory + "Venus.jpg")),
                [CelestialBodyType.Earth] = new CelestialBody("Earth", new Texture(texturesDirectory + "Earth.jpg"), new Texture(texturesDirectory + "EarthSpecular.png", TextureType.Specular), null, -1, 5.5f),
                [CelestialBodyType.Mars] = new CelestialBody("Mars", new Texture(texturesDirectory + "Mars.jpg")),
                [CelestialBodyType.Jupiter] = new CelestialBody("Jupiter", new Texture(texturesDirectory + "Jupiter.jpg")),
                [CelestialBodyType.Saturn] = new CelestialBody("Saturn", new Texture(texturesDirectory + "Saturn.jpg"), null, new Texture(texturesDirectory + "SaturnRing.png")),
                [CelestialBodyType.Uranus] = new CelestialBody("Uranus", new Texture(texturesDirectory + "Uranus.jpg"), null, new Texture(texturesDirectory + "UranusRing.png")),
                [CelestialBodyType.Neptune] = new CelestialBody("Neptune", new Texture(texturesDirectory + "Neptune.jpg")),
                [CelestialBodyType.Pluto] = new CelestialBody("Pluto", new Texture(texturesDirectory + "Pluto.jpg")),
                [CelestialBodyType.Moon] = new CelestialBody("Moon", new Texture(texturesDirectory + "Moon.jpg")),
                [CelestialBodyType.Ceres] = new CelestialBody("Ceres", new Texture(texturesDirectory + "Ceres.jpg")),
                [CelestialBodyType.Makemake] = new CelestialBody("Makemake", new Texture(texturesDirectory + "Makemake.jpg")),
                [CelestialBodyType.Callisto] = new CelestialBody("Callisto", new Texture(texturesDirectory + "Callisto.jpg")),
                [CelestialBodyType.RedDwarf] = new CelestialBody("Red Dwarf", new Texture(texturesDirectory + "RedDwarf.jpg"), null, null, 1),
                [CelestialBodyType.OrangeDwarf] = new CelestialBody("Orange Dwarf", new Texture(texturesDirectory + "OrangeDwarf.jpg"), null, null, 2),
                [CelestialBodyType.BlueDwarf] = new CelestialBody("Blue Dwarf", new Texture(texturesDirectory + "BlueDwarf.jpg"), null, null, 3),
            }.ToImmutableDictionary();
        }

        public static bool MouseIsOver(Vector3 spherePosition, float sphereRadius, Vector3 cameraPosition, Vector3 mousePickRay, ref float distance, float closestDistance = float.MaxValue)
        {
            return Utils.LineIntersectsSphere(cameraPosition, mousePickRay, spherePosition, sphereRadius) && Utils.TargetIsBehind(cameraPosition, spherePosition, mousePickRay) && (distance = Utils.Distance(cameraPosition, spherePosition)) < closestDistance && distance > sphereRadius;
        }

        static bool LightingEnabledForMarble(int slot)
        {
            return Selected == null || Selected.PositionSlot != slot || HighlightedSlot != slot;
        }

        public void UpdateLight(int shaderID)
        {
            int starIndex = CelestialBodies[Type].StarIndex;

            if (starIndex >= 0 && (Alive || AnimateFromSlot >= 0) && LightingEnabledForMarble(PositionSlot))
            {
                Game.SetLightEnabled(shaderID, starIndex, true);
                Game.SetLightPosition(shaderID, starIndex, Position());
            }
        }

        public void Initialise(int slot)
        {
            Alive = true;
            Yaw = (float)Game.Rand.NextDouble() * MathHelper.TwoPi;
            RotatingClockwise = Game.Rand.Next(2) == 0;
            AnimateFromSlot = -1;
            PositionSlot = slot;
        }

        public static bool UpdateAllSlots(int shaderID, int mouseX, int mouseY, ref Inp.KeyboardState newKeyboardState, ref Inp.MouseState newMouseState, ref Inp.MouseState oldMouseState, MarbleSelection marbleSelection, Rectangle clientRectangle, Camera camera, ref Matrix4 projection, ref Matrix4 view, List<Marble> marbles, List<MarbleMove> moves, ref int undoLevel, bool spinningMarbles, bool animateMoves, ref Marble lastMarbleRemovedForWin)
        {
            if (Game.FlashTicksAreAscending)
            {
                Game.FlashTicks++;
                if (Game.FlashTicks > Game.MAX_FLASH_TICKS)
                {
                    Game.FlashTicks -= 2;
                    Game.FlashTicksAreAscending = false;
                }
            }
            else
            {
                Game.FlashTicks--;
                if (Game.FlashTicks < 0)
                {
                    Game.FlashTicks = 1;
                    Game.FlashTicksAreAscending = true;
                }
            }
            if (Selected != null)
            {
                SelectionAnimationTick++;
                if (SelectionAnimationTick > MAX_SELECTION_ANIMATION_TICK)
                    SelectionAnimationTick = 0;
            }
            Marble oldSelected = Selected;

            for (int i = 1; i < Game.LIGHT_COUNT; i++)
                Game.SetLightEnabled(shaderID, i, false);

            HighlightedSlot = null;

            if (marbleSelection == MarbleSelection.None || (marbleSelection == MarbleSelection.Mouse && (mouseX < 0 || mouseY < 0 || mouseX > clientRectangle.Width || mouseY > clientRectangle.Height)))
            {
                foreach (Marble m in marbles)
                    m.Update(shaderID, spinningMarbles);
                return false;
            }

            Marble highlightedMarble = null;
            float closestMarbleDistance = float.MaxValue;
            float distance = float.NaN;
            //Vector2 mousePosNDC = MousePositionInNDC();
            //float rotY = (float)Math.Atan(-mousePosNDC.Y * Math.Tan(VerticalFieldOfView / 2f));
            //float rotX = -(float)Math.Atan(mousePosNDC.X * Math.Tan(HorizontalFieldOfView(VerticalFieldOfView) / 2f));
            //float rot = (float)Math.Atan(mousePosNDC.Length * (float)Math.Tan(VerticalFieldOfView / 2f));
            //float rotY = (float)Math.Atan(-mousePosNDC.Y * (float)Math.Tan(VerticalFieldOfView / 2f));
            //float rotX = -(float)Math.Atan(mousePosNDC.X * (float)Math.Tan(HorizontalFieldOfView(VerticalFieldOfView) / 2f));
            //Matrix4 xRotation = Matrix4.CreateFromAxisAngle(CameraUp(), rotX);
            //Vector3 ray = Vector3.Transform(CameraForward(), xRotation);
            //ray = Vector3.Transform(ray, Matrix4.CreateFromAxisAngle(CameraRight(), rotY));
            //Vector3 ray = Vector3.TransformPerspective(CameraForward(), Matrix4.CreateFromAxisAngle(CameraUp(), rotX) * Matrix4.CreateFromAxisAngle(CameraRight(), rotY));

            Vector3 ray;
            if (marbleSelection == MarbleSelection.Mouse)
                ray = Utils.MousePickRay(mouseX, mouseY, clientRectangle, ref projection, ref view);
            else
                ray = camera.Forward;
            bool pickHad = false;

            //Vector3 ray = Vector3.Transform(CameraForward(), Matrix4.CreateRotationZ(HorizontalFieldOfView(VerticalFieldOfView) / 2f));
            //Vector3 ray = Vector3.Transform(CameraForward(), Matrix4.CreateRotationZ(MousePositionInNDC().X * -(float)Math.Tan(HorizontalFieldOfView(VerticalFieldOfView) / VerticalFieldOfView / MathHelper.PiOver2)));

            List<int> emptySlots = new List<int>();
            for (int i = 0; i < STARTING_COUNT; i++)
                emptySlots.Add(i);

            Vector3 raySource = marbleSelection == MarbleSelection.Mouse ? camera.Position : camera.FocusPoint;

            foreach (Marble m in marbles)
            {
                if (!m.Alive)
                    continue;
                emptySlots.Remove(m.PositionSlot);
                Vector3 marblePos = m.Position();
                if (MouseIsOver(marblePos, 1f, raySource, ray, ref distance, closestMarbleDistance))
                {
                    pickHad = true;
                    highlightedMarble = m;
                    HighlightedSlot = m.PositionSlot;
                    closestMarbleDistance = distance;
                }
            }
            if (Selected != null)
            {
                emptySlots.RemoveAll(i => !SlotIsValid(i, marbles));
                foreach (int i in emptySlots)
                {
                    if (MouseIsOver(Position(i), 0.375f, raySource, ray, ref distance, closestMarbleDistance))
                    {
                        pickHad = true;
                        highlightedMarble = null;
                        HighlightedSlot = i;
                        closestMarbleDistance = distance;
                    }
                }
                if (!pickHad)
                {
                    foreach (int i in emptySlots)
                    {
                        if (MouseIsOver(Position(i), 1f, raySource, ray, ref distance, closestMarbleDistance))
                        {
                            //pickHad = true;
                            HighlightedSlot = i;
                            closestMarbleDistance = distance;
                        }
                    }
                }
            }

            bool moveMade = false;

            if (newMouseState.LeftButton == Inp.ButtonState.Pressed && oldMouseState.LeftButton == Inp.ButtonState.Released)
            {
                if (HighlightedSlot != null)
                {
                    bool shift = newKeyboardState.IsKeyDown(Inp.Key.ShiftLeft) || newKeyboardState.IsKeyDown(Inp.Key.ShiftRight);
                    //marbles.Remove(GetMarbleInSlot(HighlightedSlot.Value, marbles));
                    Marble marbleToBeRemoved;
                    if (!shift && marbles.Count(m => m.Alive) == STARTING_COUNT)
                    {
                        if (animateMoves)
                        {
                            highlightedMarble.AnimateFromSlot = highlightedMarble.PositionSlot;
                            highlightedMarble.AnimationTick = MAX_ANIMATION_TICKS;
                            highlightedMarble.AnimatingAsInitialRemoval = true;
                        }
                        highlightedMarble.Alive = false;
                        if (undoLevel > 0)
                        {
                            moves.RemoveRange(moves.Count - undoLevel, undoLevel);
                            undoLevel = 0;
                        }
                        moves.Add(new MarbleMove(highlightedMarble.PositionSlot, -1, -1, highlightedMarble.Type));
                    }
                    else if (SlotIsValid(HighlightedSlot.Value, marbles, out marbleToBeRemoved))
                    { // Remove a marble
                        if (undoLevel > 0)
                        {
                            moves.RemoveRange(moves.Count - undoLevel, undoLevel);
                            undoLevel = 0;
                        }
                        moves.Add(new MarbleMove(Selected.PositionSlot, marbleToBeRemoved.PositionSlot, HighlightedSlot.Value, marbleToBeRemoved.Type));

                        if (animateMoves)
                        {
                            Selected.AnimateFromSlot = Selected.PositionSlot;
                            Selected.AnimationTick = 0;
                            marbleToBeRemoved.AnimateFromSlot = marbleToBeRemoved.PositionSlot;
                            marbleToBeRemoved.AnimationTick = 0;
                        }
                        Selected.PositionSlot = HighlightedSlot.Value;
                        marbleToBeRemoved.Alive = false;

                        moveMade = true;
                        //if (!Selected.CanMove(marbles))
                        Selected = null;

                        if (marbles.Count(m => m.Alive) == 1)
                            lastMarbleRemovedForWin = marbleToBeRemoved;
                    }
                    else if (shift)
                        camera.FocusPoint = highlightedMarble.Position();
                    else if (highlightedMarble == Selected)
                        Selected = null;
                    else
                        Selected = highlightedMarble;
                }
                else
                    Selected = null;
            }
            foreach (Marble m in marbles)
                m.Update(shaderID, spinningMarbles);

            if (Selected != oldSelected)
                SelectionAnimationTick = 0;
            return moveMade;
        }

        public static void DrawAllSlots(int uModelLocation, int shaderID, List<Marble> marbles, bool highlightMarbles, Camera camera, float defaultSpecularPower)
        {
            List<int> emptySlots = new List<int>();
            for (int i = 0; i < STARTING_COUNT; i++)
                emptySlots.Add(i);

            foreach (Marble m in marbles)
            {
                m.Draw(uModelLocation, shaderID, highlightMarbles, camera, defaultSpecularPower);
                if (m.Alive)
                    emptySlots.Remove(m.PositionSlot);
            }
            var transparentObjects = new List<TransparentObject>();
            foreach (int i in emptySlots)
                transparentObjects.Add(new TransparentObject(TransparentObjectType.EmptySlot, i));


            Game.SetLightingEnabled(shaderID, true);
            Vector3 oldAmbient = MaterialProperties.CurrentAmbient;
            Vector3 oldDiffuse = MaterialProperties.CurrentDiffuse;
            //MaterialProperties.SetAmbient(shaderID, new Vector3(1.375f));
            //MaterialProperties.SetDiffuse(shaderID, Vector3.Zero);
            MaterialProperties.Set(shaderID, new Vector3(1f), Vector3.Zero, Vector3.Zero, 1f);
            //Game.SetDrawColour(shaderID, new Color4(1f, 1f, 1f, 0.3f));

            foreach (Marble m in marbles)
            {
                if ((m.Alive || m.AnimateFromSlot >= 0) && CelestialBodies[m.Type].RingTexture != null)
                    transparentObjects.Add(new TransparentObject(TransparentObjectType.Ring, m.PositionSlot, m));
            }

            if (transparentObjects.Any())
            {
                transparentObjects = transparentObjects.OrderByDescending(o =>
                {
                    Vector3 position;
                    switch (o.Type)
                    {
                        case TransparentObjectType.EmptySlot: position = Position(o.Slot); break;
                        case TransparentObjectType.Ring: position = o.MarbleObject.Position(); break;
                        default: throw new Exception();
                    }
                    return Utils.Distance(camera.Position, position);
                }).ToList();
                foreach (TransparentObject o in transparentObjects)
                {
                    switch (o.Type)
                    {
                        case TransparentObjectType.EmptySlot:
                            GL.Enable(EnableCap.CullFace);
                            Game.SetLightingEnabled(shaderID, false);
                            Game.SetTexturesEnabled(shaderID, false);
                            Color4 colour;
                            if (SlotIsValid(o.Slot, marbles))
                            {
                                if (HighlightedSlot == o.Slot)
                                    colour = new Color4(1f, 0.75f, 0f, 0.75f); // Yellow/orange
                                else
                                    colour = new Color4(1f, 0f, 0f, 0.625f); // Red
                            }
                            else
                                colour = new Color4(1f, 1f, 1f, 0.375f); // Light grey
                            Game.SetDrawColour(shaderID, colour);

                            Vector3 position = Position(o.Slot);
                            if (Utils.Distance(camera.Position, position) > 0.375f + CLIPPING_DISTANCE)
                                DrawStatic(uModelLocation, shaderID, o.Slot, Matrix4.CreateScale(0.375f) * Matrix4.CreateTranslation(position), true, highlightMarbles);
                            break;

                        case TransparentObjectType.Ring:
                            GL.Disable(EnableCap.CullFace);
                            Game.SetDrawColour(shaderID, Color4.White);
                            Game.SetLightingEnabled(shaderID, true);
                            Game.SetTexturesEnabled(shaderID, true);
                            o.MarbleObject.DrawRing(uModelLocation, shaderID);
                            break;

                        default: throw new Exception();
                    }
                }
                Game.SetDrawColour(shaderID, Color4.White);
                Game.SetLightingEnabled(shaderID, true);
                Game.SetTexturesEnabled(shaderID, true);
                GL.Enable(EnableCap.CullFace);
            }

            //Game.SetDrawColour(shaderID, Color4.White);
            MaterialProperties.SetAmbient(shaderID, oldAmbient);
            MaterialProperties.SetDiffuse(shaderID, oldDiffuse);
            //Model.Bind(shaderID);
        }

        void Update(int shaderID, bool spin)
        {
            UpdateLight(shaderID);

            if (AnimateFromSlot >= 0)
            {
                AnimationTick++;
                if (AnimationTick > MAX_ANIMATION_TICKS + (Alive ? 0 : MAX_DEATH_ANIMATION_TICKS))
                    AnimateFromSlot = -1;
            }
            if (AnimateFromSlot < 0)
                AnimatingAsInitialRemoval = false;

            if (!Alive)
                return;

            if (spin)
            {
                if (RotatingClockwise)
                    Yaw -= 0.01f;
                else
                    Yaw += 0.01f;
            }
        }

        Matrix4 Rotation()
        {
            Matrix4 rotation = Matrix4.CreateRotationZ(Yaw);
            if (Alive && AnimationTick >= 0)
            {
                OrthogonalDirection direction = GetOrthogonalDirection(PositionSlot, AnimateFromSlot);
                float angle = MathHelper.TwoPi * AnimationTick / MAX_ANIMATION_TICKS;
                switch (direction)
                {
                    case OrthogonalDirection.Left: rotation *= Matrix4.CreateRotationX(-angle); break;
                    case OrthogonalDirection.Right: rotation *= Matrix4.CreateRotationX(angle); break;
                    case OrthogonalDirection.Down: rotation *= Matrix4.CreateRotationY(-angle); break;
                    case OrthogonalDirection.Up: rotation *= Matrix4.CreateRotationY(angle); break;
                }
            }
            return rotation;
        }

        public static void SetDrawProperties(int shaderID, CelestialBodyType celestialBody, float defaultSpecularPower = -1f)
        {
            CelestialBody body = CelestialBodies[celestialBody];
            if (body.TextureMap != null)
                body.TextureMap.Bind();
            if (body.SpecularMap != null)
            {
                Game.SetSpecularMapEnabled(shaderID, true);
                body.SpecularMap.Bind();
            }
            else
                Game.SetSpecularMapEnabled(shaderID, false);

            if (body.SpecularPower >= 0f)
            {
                //MaterialProperties.SetSpecular(shaderID, new Vector3(body.SpecularReflectivity));
                MaterialProperties.SetShininess(shaderID, body.SpecularPower);
            }
            else
                MaterialProperties.SetShininess(shaderID, defaultSpecularPower);
        }

        void Draw(int uModelLocation, int shaderID, bool highlightMarbles, Camera camera, float defaultSpecularPower)
        {
            if ((!Alive && AnimateFromSlot < 0) || Utils.Distance(camera.Position, Position()) <= 1f + CLIPPING_DISTANCE)
                return;

            SetDrawProperties(shaderID, Type, defaultSpecularPower);
            //MaterialProperties.SetSpecular(shaderID, new Vector3(Menu.Main.RandomFloat(), Menu.Main.RandomFloat(), Menu.Main.RandomFloat()));
            //MaterialProperties.SetShininess(shaderID, Menu.Main.RandomFloat() * 128f);
            DrawStatic(uModelLocation, shaderID, PositionSlot, Rotation() * Matrix4.CreateTranslation(Position()), false, highlightMarbles, CelestialBodies[Type].StarIndex);
        }

        void DrawRing(int uModelLocation, int shaderID)
        {
            if ((!Alive && AnimateFromSlot < 0) || CelestialBodies[Type].RingTexture == null)
                return;

            CelestialBodies[Type].RingTexture.Bind();
            Matrix4 model = Rotation() * Matrix4.CreateScale(2f) * Matrix4.CreateTranslation(Position());
            GL.UniformMatrix4(uModelLocation, true, ref model);
            ModelRing.Draw(shaderID);
        }

        static void DrawStatic(int uModelLocation, int shaderID, int slot, Matrix4 model, bool emptySlot, bool highlightMarbles, int starIndex = -1)
        {
            bool selected = Selected != null && Selected.PositionSlot == slot;
            bool highlighted = HighlightedSlot == slot;
            //int uHighlightLocation = -1;
            int uPosterisationLocation = -1;
            bool highlight = highlightMarbles && !emptySlot && highlighted && !selected;
            if (highlight)
            {
                uPosterisationLocation = GL.GetUniformLocation(shaderID, "uPosterisation");
                GL.Uniform1(uPosterisationLocation, 1f);

                //uHighlightLocation = GL.GetUniformLocation(shaderID, "uHighlight");
                //GL.Uniform1(uHighlightLocation, 1f);// 0.375f + 0.625f * Game.FlashTicks / Game.MAX_FLASH_TICKS);
            }

            GL.UniformMatrix4(uModelLocation, true, ref model);
            bool useLighting = LightingEnabledForMarble(slot) && !emptySlot && starIndex < 0;
            bool lightingWasEnabled = Game.GetLightingEnabled();
            Game.SetLightingEnabled(shaderID, useLighting);

            
            Model.Draw(shaderID);
            Game.SetLightingEnabled(shaderID, lightingWasEnabled);
            if (highlight)
                GL.Uniform1(uPosterisationLocation, 0f);
        }

        public static int? Row(int slot)
        {
            if (slot < 0)
                return null;
            if (slot < 3)
                return 0;
            if (slot < 8)
                return 1;
            if (slot < 15)
                return 2;
            if (slot < 22)
                return 3;
            if (slot < 29)
                return 4;
            if (slot < 34)
                return 5;
            if (slot < 37)
                return 6;
            return null;
        }

        public int Row()
        {
            return Row(PositionSlot).Value;
        }

        public static int? Column(int slot)
        {
            switch (Row(slot))
            {
                case 0: return slot + 2;
                case 1: return slot - 2;
                case 2: return slot - 8;
                case 3: return slot - 15;
                case 4: return slot - 22;
                case 5: return slot - 28;
                case 6: return slot - 32;
                default: return null;
            }
        }

        public int Column()
        {
            return Column(PositionSlot).Value;
        }

        public static int? NextSlotRight(int slot, bool wrap = false)
        {
            switch (slot)
            {
                case 2: return wrap ? (int?)0 : null;
                case 7: return wrap ? (int?)3 : null;
                case 14: return wrap ? (int?)8 : null;
                case 21: return wrap ? (int?)15 : null;
                case 28: return wrap ? (int?)22 : null;
                case 33: return wrap ? (int?)29 : null;
                case 36: return wrap ? (int?)34 : null;
                default:
                    return slot + 1;
            }
        }

        public int? NextSlotRight(bool wrap = false)
        {
            return NextSlotRight(PositionSlot, wrap);
        }

        public static int? NextSlotLeft(int slot, bool wrap = false)
        {
            switch (slot)
            {
                case 0: return wrap ? (int?)2 : null;
                case 3: return wrap ? (int?)7 : null;
                case 8: return wrap ? (int?)14 : null;
                case 15: return wrap ? (int?)21 : null;
                case 22: return wrap ? (int?)28 : null;
                case 29: return wrap ? (int?)33 : null;
                case 34: return wrap ? (int?)36 : null;
                default:
                    return slot - 1;
            }
        }

        public int? NextSlotLeft(bool wrap = false)
        {
            return NextSlotLeft(PositionSlot, wrap);
        }

        public static int? NextSlotDown(int slot, bool wrap = false)
        {
            switch (slot)
            {
                case 22: return wrap ? (int?)8 : null;
                case 29: return wrap ? (int?)3 : null;
                case 34: return wrap ? (int?)0 : null; //
                case 35: return wrap ? (int?)1 : null; //
                case 36: return wrap ? (int?)2 : null; //
                case 33: return wrap ? (int?)7 : null;
                case 28: return wrap ? (int?)14 : null;
            }
            switch (Row(slot))
            {
                case 0: return slot + 4;
                case 1: return slot + 6;
                case 2: return slot + 7;
                case 3: return slot + 7;
                case 4: return slot + 6;
                case 5: return slot + 4;
                default: return null;
            }
        }

        public int? NextSlotDown(bool wrap = false)
        {
            return NextSlotDown(PositionSlot, wrap);
        }

        public static int? NextSlotUp(int slot, bool wrap = false)
        {
            switch (slot)
            {
                case 8: return wrap ? (int?)22 : null;
                case 3: return wrap ? (int?)29 : null;
                case 0: return wrap ? (int?)34 : null; //
                case 1: return wrap ? (int?)35 : null; //
                case 2: return wrap ? (int?)36 : null; //
                case 7: return wrap ? (int?)33 : null;
                case 14: return wrap ? (int?)28 : null;
            }
            switch (Row(slot))
            {
                case 1: return slot - 4;
                case 2: return slot - 6;
                case 3: return slot - 7;
                case 4: return slot - 7;
                case 5: return slot - 6;
                case 6: return slot - 4;
                default: return null;
            }
        }

        public int? NextSlotUp(bool wrap = false)
        {
            return NextSlotUp(PositionSlot, wrap);
        }

        public static Vector3 Position(int slot)
        {
            Vector3 result = new Vector3(4f * (Row(slot).Value - 3), 4f * (Column(slot).Value - 3), 0f);
            //result.Z = result.X * result.Y * Game.FlashTicks / 1024f;
            return result;
        }

        public Vector3 Position()
        {
            const float DROP_DOWN_DEAD = 2f;
            Vector3 position = Position(PositionSlot);

            if (AnimateFromSlot >= 0)
            {
                if (Alive)
                {
                    Vector3 oldPosition = Position(AnimateFromSlot);
                    Vector3 displacement = position - oldPosition;
                    float angle = MathHelper.Pi * AnimationTick / MAX_ANIMATION_TICKS - MathHelper.PiOver2;
                    float horizInterp = ((float)Math.Sin(angle) + 1f) / 2f;
                    position = oldPosition + new Vector3(displacement.X * horizInterp, displacement.Y * horizInterp, displacement.Length * (float)Math.Cos(angle) / 2f);
                }
                else if (AnimationTick <= MAX_ANIMATION_TICKS)
                {
                    position -= Vector3.UnitZ * DROP_DOWN_DEAD * (float)Math.Pow((float)AnimationTick / MAX_ANIMATION_TICKS, 1 / 3f);
                }
                else
                {
                    if (!AnimatingAsInitialRemoval)
                        position.Z -= DROP_DOWN_DEAD;
                    Vector3 displacement = Game.SunPosition - position;
                    position += displacement * (AnimationTick - MAX_ANIMATION_TICKS) / MAX_DEATH_ANIMATION_TICKS;
                }
            }
            if (Selected == this)
                position.Z += 0.9f * (float)Math.Sin(MathHelper.TwoPi * SelectionAnimationTick / MAX_SELECTION_ANIMATION_TICK);
            return position;
        }

        public static Marble GetMarbleInSlot(int slot, List<Marble> marbles)
        {
            if (slot < 0 || slot >= STARTING_COUNT)
                return null;
            foreach (Marble m in marbles)
            {
                if (m.PositionSlot == slot && m.Alive)
                    return m;
            }
            return null;
        }

        public static bool? SlotContainsMarble(int slot, List<Marble> marbles, out Marble marble)
        {
            if (slot < 0 || slot >= STARTING_COUNT)
            {
                marble = null;
                return null;
            }
            foreach (Marble m in marbles)
            {
                if (m.PositionSlot == slot && m.Alive)
                {
                    marble = m;
                    return true;
                }
            }
            marble = null;
            return false;
        }

        public static bool? SlotContainsMarble(int slot, List<Marble> marbles)
        {
            if (slot < 0 || slot >= STARTING_COUNT)
                return null;
            foreach (Marble m in marbles)
            {
                if (m.PositionSlot == slot && m.Alive)
                    return true;
            }
            return false;
        }

        public static bool SlotIsValid(int slot, List<Marble> marbles, out Marble marbleToBeRemoved)
        {
            marbleToBeRemoved = null;
            if (Selected == null || SlotContainsMarble(slot, marbles) != false)
                return false;

            int nextSlot = NextSlotRight(slot) ?? -1;
            if ((marbleToBeRemoved = GetMarbleInSlot(nextSlot, marbles)) != null && Selected == GetMarbleInSlot(NextSlotRight(nextSlot) ?? -1, marbles))
                return true;
            nextSlot = NextSlotUp(slot) ?? -1;
            if ((marbleToBeRemoved = GetMarbleInSlot(nextSlot, marbles)) != null && Selected == GetMarbleInSlot(NextSlotUp(nextSlot) ?? -1, marbles))
                return true;
            nextSlot = NextSlotLeft(slot) ?? -1;
            if ((marbleToBeRemoved = GetMarbleInSlot(nextSlot, marbles)) != null && Selected == GetMarbleInSlot(NextSlotLeft(nextSlot) ?? -1, marbles))
                return true;
            nextSlot = NextSlotDown(slot) ?? -1;
            if ((marbleToBeRemoved = GetMarbleInSlot(nextSlot, marbles)) != null && Selected == GetMarbleInSlot(NextSlotDown(nextSlot) ?? -1, marbles))
                return true;
            return false;
        }

        public static bool SlotIsValid(int slot, List<Marble> marbles) => SlotIsValid(slot, marbles, out Marble placeholder);

        public static OrthogonalDirection GetOrthogonalDirection(int sourceSlot, int targetSlot)
        {
            if (Row(sourceSlot) == Row(targetSlot))
            {
                if (targetSlot < sourceSlot)
                    return OrthogonalDirection.Left;
                else if (targetSlot > sourceSlot)
                    return OrthogonalDirection.Right;
                else
                    return OrthogonalDirection.None;
            }
            else if (Column(sourceSlot) == Column(targetSlot))
            {
                if (targetSlot < sourceSlot)
                    return OrthogonalDirection.Up;
                else if (targetSlot > sourceSlot)
                    return OrthogonalDirection.Down;
                else
                    return OrthogonalDirection.None;
            }
            else
                return OrthogonalDirection.None;
        }

        public bool CanMove(List<Marble> marbles)
        {
            if (!Alive)
                return false;
            int slot = NextSlotRight() ?? -1;
            if (SlotContainsMarble(slot, marbles) == true && SlotContainsMarble(NextSlotRight(slot) ?? -1, marbles) == false)
                return true;
            slot = NextSlotUp() ?? -1;
            if (SlotContainsMarble(slot, marbles) == true && SlotContainsMarble(NextSlotUp(slot) ?? -1, marbles) == false)
                return true;
            slot = NextSlotLeft() ?? -1;
            if (SlotContainsMarble(slot, marbles) == true && SlotContainsMarble(NextSlotLeft(slot) ?? -1, marbles) == false)
                return true;
            slot = NextSlotDown() ?? -1;
            if (SlotContainsMarble(slot, marbles) == true && SlotContainsMarble(NextSlotDown(slot) ?? -1, marbles) == false)
                return true;
            return false;
        }

        public static bool GameIsOver(List<Marble> marbles)
        {
            if (marbles.Count(m => m.Alive) == STARTING_COUNT)
                return false;

            foreach (Marble m in marbles)
            {
                if (m.CanMove(marbles))
                    return false;
            }
            return true;
        }

        public void Save(StreamWriter sw)
        {
            if (!Alive)
                return;

            sw.WriteLine($"Marble: Slot {PositionSlot}; Body {Type};{(CelestialBodies[Type].StarIndex < 0 ? "" : $" Light {CelestialBodies[Type].StarIndex};")}");
        }

        public override string ToString() => $"{(Alive ? "" : "DEAD ")}{CelestialBodies[Type].StarIndex} {Type} {PositionSlot}";

        public static Marble Load(string line)
        {
            var marble = new Marble();
            line = line.Substring(line.IndexOf(':') + 1);

            while (line.Length > 0)
            {
                if (line.StartsWith(" Slot "))
                {
                    int.TryParse(Utils.ReadSaveFileLineSegment(line), out int slot);
                    marble.PositionSlot = slot;
                }
                else if (line.StartsWith(" Body "))
                {
                    Enum.TryParse(Utils.ReadSaveFileLineSegment(line), out CelestialBodyType type);
                    marble.Type = type;
                }
                //else if (line.StartsWith(" Light "))
                //    int.TryParse(ReadSaveFileLineSegment(line), out marble.StarIndex);
                // If the semicolon is the last one in the string, then an empty one is naturally returned (at which point the loop terminates because we are finished reading all the elements on this line)

                line = line.Substring(line.IndexOf(';') + 1);
            }
            marble.Yaw = (float)Game.Rand.NextDouble();
            marble.RotatingClockwise = Game.Rand.Next(2) == 0;
            return marble;
        }
    }
}
