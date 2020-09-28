namespace QudUX.Utilities
{
    public class Coords
    {
        private readonly int x;
        public int X { get { return x; } }

        private readonly int y;
        public int Y { get { return y; } }

        public Coords(int x, int y)
        {
            this.x = x;
            this.y = y;
        }
    }
}
