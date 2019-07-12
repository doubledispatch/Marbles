using System;

namespace Marbles
{
    readonly struct MarbleMove
    {
        public int FromSlot { get; }
        public int PassOverSlot { get; }
        public int ToSlot { get; }
        public CelestialBodyType PassOverMarble { get; }

        public MarbleMove(int fromSlot, int passOverSlot, int toSlot, CelestialBodyType passOverMarble)
        {
            FromSlot = fromSlot;
            PassOverSlot = passOverSlot;
            ToSlot = toSlot;
            PassOverMarble = passOverMarble;
        }

        public override string ToString() => $"From {FromSlot}; Passing {PassOverSlot}; To {ToSlot}; Marble {PassOverMarble};";

        public static MarbleMove Load(string line)
        {
            int from = 0;
            int passing = 0;
            int to = 0;
            CelestialBodyType type = 0;
            line = line.Substring(line.IndexOf(':') + 1);

            while (line.Length > 0)
            {
                if (line.StartsWith(" From "))
                    int.TryParse(Utils.ReadSaveFileLineSegment(line), out from);
                else if (line.StartsWith(" Passing "))
                    int.TryParse(Utils.ReadSaveFileLineSegment(line), out passing);
                else if (line.StartsWith(" To "))
                    int.TryParse(Utils.ReadSaveFileLineSegment(line), out to);
                else if (line.StartsWith(" Marble "))
                    Enum.TryParse(Utils.ReadSaveFileLineSegment(line), out type);
                //else if (line.StartsWith(" Light "))
                //    int.TryParse(ReadSaveFileLineSegment(line), out move.PassOverMarbleStarIndex);

                line = line.Substring(line.IndexOf(';') + 1);
            }
            return new MarbleMove(from, passing, to, type);
        }
    }
}
