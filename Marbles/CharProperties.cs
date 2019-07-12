namespace Marbles
{
    class CharProperties
    {
        public char Character { get; }
        public Texture Tex { get; }
        public float Width { get; }

        public CharProperties(char character, float width, Texture texture)
        {
            Character = character;
            Width = width;
            Tex = texture;
        }

        public override string ToString() => Character + " " + Width;
    }
}
