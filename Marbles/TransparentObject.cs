namespace Marbles
{
    struct TransparentObject
    {
        public TransparentObjectType Type { get; }
        public Marble MarbleObject { get; }
        public int Slot { get; }

        public TransparentObject(TransparentObjectType type, int slot, Marble marble = null)
        {
            Type = type;
            Slot = slot;
            MarbleObject = marble;
        }

        public override string ToString()
        {
            return Type + ", " + Slot;
        }
    }
}
