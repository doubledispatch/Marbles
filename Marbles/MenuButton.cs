using System;
using OpenTK;
using OpenTK.Graphics;
using Inp = OpenTK.Input;

namespace Marbles
{
    abstract class RefVar
    {
        public string SaveName;

        public abstract string ValueString();
    }

    class RefBool : RefVar
    {
        public bool Value;

        public RefBool(string saveName, bool value)
        {
            SaveName = saveName;
            Value = value;
        }

        public override string ValueString()
        {
            return Value.ToString();
        }
    }

    class RefFloat : RefVar
    {
        public float Value;
        public float MinModifiedValue;
        public float MaxModifiedValue;
        public bool QuadraticScale;

        public RefFloat(string saveName, float modifiedValue, float maxModifiedValue = 1f, float minModifiedValue = 0f, bool quadraticScale = false)
        {
            SaveName = saveName;
            MaxModifiedValue = maxModifiedValue;
            MinModifiedValue = minModifiedValue;
            QuadraticScale = quadraticScale;
            ModifiedValue = modifiedValue;
        }

        public float ModifiedValue
        {
            get
            {
                return MinModifiedValue + Value * (QuadraticScale ? Value : 1f) * (MaxModifiedValue - MinModifiedValue);
            }
            set
            {
                Value = (value - MinModifiedValue) / (MaxModifiedValue - MinModifiedValue);
                if (QuadraticScale)
                    Value = (float)Math.Sqrt(Value);
            }
        }

        public override string ValueString()
        {
            return Value.ToString();
        }
    }

    class MenuButton
    {
        public static Game Main;
        public const float HEIGHT = 0.08f;
        public const float TEXT_SCALE_FOR_HEIGHT = 1.1f;
        public const float VERTICAL_DISPLACEMENT = 0.016f;
        public const float EXTRA_WIDTH = 0.05f;
        public const float DESCRIPTION_HEIGHT = 0.075f;
        public const float DESCRIPTION_WRAP_WIDTH = 1f;

        public float PositionY;
        public float Width;
        public bool LimitTextDrawWidth;
        public Menu ContainingMenu;
        public string Text;
        public Func<MenuButton, string> VariableText = null;
        public Action<MenuButton> OnActivate;
        public bool MousePressedHereWithoutRelease = false;
        public int Column = 0;
        public Color4 TextColour;
        public RefVar BoundVar;
        public string Description;
        public Inp.Key Hotkey;
        public bool SaveSettingsOnHotkey;

        public RefBool BoundBool
        {
            get { return BoundVar as RefBool; }
            set { BoundVar = value; }
        }

        public RefFloat BoundFloat
        {
            get { return BoundVar as RefFloat; }
            set { BoundVar = value; }
        }

        public float PositionX()
        {
            float x = -0.95f;
            if (Column != 0)
                x += Column * (VERTICAL_DISPLACEMENT + Width / Main.AspectRatio());
            return x;
        }

        public MenuButton(string text, Color4 textColour, Action<MenuButton> onActivate = null, RefVar boundVar = null, string description = null, Inp.Key hotkey = Inp.Key.Unknown, float width = -1f, Func<MenuButton, string> variableText = null, bool saveSettingsOnHotkey = false)
        {
            Text = text;
            OnActivate = onActivate;
            Width = width;
            LimitTextDrawWidth = width >= 0f;
            TextColour = textColour;
            BoundVar = boundVar;
            VariableText = variableText;
            Hotkey = hotkey;
            SaveSettingsOnHotkey = saveSettingsOnHotkey;
            if (hotkey != Inp.Key.Unknown)
                description = "Ctrl + " + hotkey + (description == null ? "" : "\n" + description);
            if (description != null)
                Description = Main.AddTextWrappingSeparators(DESCRIPTION_HEIGHT, DESCRIPTION_WRAP_WIDTH, description);
        }

        public bool MouseIsOver(Vector2 mousePosInNDC)
        {
            float positionX = PositionX();
            return mousePosInNDC.X > positionX && mousePosInNDC.X < positionX + Width / Main.AspectRatio() &&
                   mousePosInNDC.Y > PositionY - HEIGHT && mousePosInNDC.Y < PositionY;
        }

        public void Update(Vector2 mousePosInNDC, Inp.MouseState newMouseState, Inp.MouseState oldMouseState, Inp.KeyboardState newKeyboardState, Inp.KeyboardState oldKeyboardState)
        {
            //bool activated = UpdateHotkeys(newKeyboardState, oldKeyboardState);

            if (MouseIsOver(mousePosInNDC))
            {
                if (newMouseState.LeftButton == Inp.ButtonState.Released)
                    ContainingMenu.SelectedButton = this;
                if (MousePressedHereWithoutRelease && newMouseState.LeftButton == Inp.ButtonState.Released)
                {
                    if (BoundBool != null)
                        BoundBool.Value = !BoundBool.Value;
                    if (OnActivate != null)
                        OnActivate(this);
                }
                if (newMouseState.LeftButton == Inp.ButtonState.Pressed && oldMouseState.LeftButton == Inp.ButtonState.Released)
                    MousePressedHereWithoutRelease = true;
            }
            else if (ContainingMenu.SelectedButton == this)
                ContainingMenu.SelectedButton = null;

            if (newMouseState.LeftButton == Inp.ButtonState.Released)
                MousePressedHereWithoutRelease = false;
            else if (MousePressedHereWithoutRelease && BoundFloat != null)
                BoundFloat.Value = (mousePosInNDC.X - PositionX()) / Width * Main.AspectRatio();

            if (BoundFloat != null)
            {
                if (BoundFloat.Value < 0f)
                    BoundFloat.Value = 0f;
                else if (BoundFloat.Value > 1f)
                    BoundFloat.Value = 1f;
            }
        }

        public bool UpdateHotkeys(Inp.KeyboardState newKeyboardState, Inp.KeyboardState oldKeyboardState)
        {
            if (Hotkey != Inp.Key.Unknown && (newKeyboardState.IsKeyDown(Inp.Key.LControl) || newKeyboardState.IsKeyDown(Inp.Key.RControl)) && newKeyboardState.IsKeyDown(Hotkey) && oldKeyboardState.IsKeyUp(Hotkey))
            {
                if (BoundBool != null)
                    BoundBool.Value = !BoundBool.Value;
                if (OnActivate != null)
                    OnActivate(this);
                Main.EndCutsceneMode();
                if (SaveSettingsOnHotkey)
                    Main.SaveSettings();
                return true;
            }
            return false;
        }

        public void Draw(int uModelLocation, Vector2 mousePosInNDC)
        {
            Texture texture = Marble.CelestialBodies[CelestialBodyType.Uranus].TextureMap;
            float alpha;
            Color4 colour;
            if (MousePressedHereWithoutRelease && MouseIsOver(mousePosInNDC))
                colour = new Color4(1.75f, 1.75f, 1.75f, alpha = 0.85f);
            else if (ContainingMenu.SelectedButton == this)
                colour = new Color4(1f, 1f, 1f, alpha = 0.85f);
            else
                colour = new Color4(0.4f, 0.4f, 0.75f, alpha = 0.6f);

            float aspectRatio = Main.AspectRatio();
            float x = PositionX();

            if (BoundBool != null)
            {
                Main.DrawSprite(uModelLocation, Alignment.TopLeft, x, PositionY, HEIGHT, HEIGHT, texture, BoundBool.Value ? new Color4(0f, 1f, 0f, alpha) : new Color4(1f, 0f, 0f, alpha));
                if (BoundBool.Value)
                    Main.DrawSprite(uModelLocation, Alignment.TopLeft, x, PositionY, HEIGHT, HEIGHT, Main.TexTick, Color4.White);
                x += HEIGHT / aspectRatio;
                Main.DrawSprite(uModelLocation, Alignment.TopLeft, x, PositionY, Width - HEIGHT, HEIGHT, texture, colour);
            }
            else if (BoundFloat != null)
            {
                float greenWidth = Width * BoundFloat.Value;
                Main.DrawSprite(uModelLocation, Alignment.TopLeft, x, PositionY, greenWidth, HEIGHT, texture, new Color4(0f, 1f, 0f, alpha));
                Main.DrawSprite(uModelLocation, Alignment.TopLeft, x + greenWidth / aspectRatio, PositionY, Width - greenWidth, HEIGHT, texture, new Color4(1f, 0f, 0f, alpha));
            }
            else
            {
                Main.DrawSprite(uModelLocation, Alignment.TopLeft, x, PositionY, Width, HEIGHT, texture, colour);
            }
            Main.SetDrawColour(TextColour);

            string text;
            if (VariableText != null)
                text = VariableText(this);
            else
                text = Text;

            if (!Main.DrawString(uModelLocation, text, Alignment.TopLeft, x + EXTRA_WIDTH / 2f / aspectRatio, PositionY, HEIGHT * TEXT_SCALE_FOR_HEIGHT, null, LimitTextDrawWidth ? (Width - EXTRA_WIDTH - (BoundVar == null ? 0 : HEIGHT)) / aspectRatio : -1f))
            {
                Main.DrawString(uModelLocation, "...", Alignment.BottomRight, x + (Width - EXTRA_WIDTH / 4f) / aspectRatio, PositionY - HEIGHT * 1.2f, HEIGHT * TEXT_SCALE_FOR_HEIGHT);
                if (ContainingMenu.SelectedButton == this)
                    Main.DrawString(uModelLocation, text, Alignment.Bottom, 0f, -0.85f, 0.08f);
            }
            if (Description != null && ContainingMenu.SelectedButton == this)
                Main.DrawString(uModelLocation, Description, Alignment.Left, PositionX() + (Width + EXTRA_WIDTH) / aspectRatio, PositionY - HEIGHT / 2f, DESCRIPTION_HEIGHT);
        }

        public override string ToString()
        {
            return Text;
        }
    }
}
