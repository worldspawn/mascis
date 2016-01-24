namespace Mascis
{
    public class KeyMapping
    {
        public MapMapping[] Columns { get; }

        public KeyMapping(MapMapping[] columns)
        {
            Columns = columns;
        }
    }
}