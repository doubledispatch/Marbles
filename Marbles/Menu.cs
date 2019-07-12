using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using OpenTK;
using Inp = OpenTK.Input;

namespace Marbles
{
    class Menu
    {
        public static Game Main { get; set; }
        private static Menu _current = null;
        public string Caption { get; set; }
        public int SelectedIndex { get; private set; } = -1;
        public ImmutableArray<MenuButton> Buttons { get; private set; } = ImmutableArray<MenuButton>.Empty;
        public Menu ParentMenu { get; set; }
        public Action<Menu> OnVisit { get; }
        public Action<Menu> OnLeave { get; }
        public const int MAX_BUTTONS_IN_COLUMN = 15;

        public static Menu Current
        {
            get => _current;
            set
            {
                if (_current != null)
                {
                    _current.SelectedIndex = -1;
                    _current.OnLeave?.Invoke(_current);
                }
                _current = value;
                if (_current != null && _current.OnVisit != null)
                    _current.OnVisit(_current);
            }
        }

        public override string ToString()
        {
 	        return Caption;
        }

        public Menu(string caption, Menu parentMenu, Action<Menu> onVisit = null, Action<Menu> onLeave = null)
        {
            Caption = caption;
            ParentMenu = parentMenu;
            OnVisit = onVisit;
            OnLeave = onLeave;
        }

        public MenuButton SelectedButton
        {
            get => SelectedIndex < 0 ? null : Buttons[SelectedIndex];
            set => SelectedIndex = value == null ? -1 : Buttons.IndexOf(value);
        }

        public void AddButton(MenuButton button, int index = -1)
        {
            button.ContainingMenu = this;
            Buttons = index < 0 ? Buttons.Add(button) : Buttons.Insert(index, button);
        }

        public void Update(Vector2 mousePosInNDC, Inp.MouseState newMouseState, Inp.MouseState oldMouseState, Inp.KeyboardState newKeyboardState, Inp.KeyboardState oldKeyboardState)
        {
            if (Main.KeyWasPressedOnce(Inp.Key.Escape))
            {
                Current = ParentMenu == Current ? null : ParentMenu;
                return;
            }
            foreach (MenuButton t in Buttons)
                t.Update(mousePosInNDC, newMouseState, oldMouseState, newKeyboardState, oldKeyboardState);
        }

        public void UpdateSpacing(float maxWidth = -1f)
        {
            float longestWidth = 0f;
            const float HEIGHT = MenuButton.HEIGHT + MenuButton.VERTICAL_DISPLACEMENT;

            int columnCount = (Buttons.Length + MAX_BUTTONS_IN_COLUMN - 1) / MAX_BUTTONS_IN_COLUMN;
            int rowCount = (Buttons.Length + columnCount - 1) / columnCount;

            float startY = (rowCount - 1) * HEIGHT / 2f + MenuButton.HEIGHT / 2f;
            int index = 0;

            foreach (MenuButton t in Buttons)
            {
                float width = Main.MeasureString(t.Text, MenuButton.HEIGHT * MenuButton.TEXT_SCALE_FOR_HEIGHT).X + 0.01f;
                if (t.BoundBool != null)
                    width += MenuButton.HEIGHT;
                if (maxWidth >= 0f && width > maxWidth)
                    width = maxWidth;
                if (width > longestWidth)
                    longestWidth = width;

                int x = index / rowCount;
                int y = index % rowCount;
                t.Column = x;
                t.PositionY = startY - y * HEIGHT;
                index++;
            }
            foreach (MenuButton t in Buttons)
                t.Width = longestWidth + MenuButton.EXTRA_WIDTH;
        }

        public void Draw(int uModelLocation, Vector2 mousePosInNDC)
        {
            if (Caption != null)
                Main.DrawString(uModelLocation, Caption, Alignment.BottomLeft, Buttons[0].PositionX(), Buttons[0].PositionY + MenuButton.VERTICAL_DISPLACEMENT, 0.08f);
            foreach (MenuButton t in Buttons)
                t.Draw(uModelLocation, mousePosInNDC);
        }
    }
}
