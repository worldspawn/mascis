namespace Mascis.Configuration
{
    public class KeyMapping
    {
        public KeyMapping(MapMapping[] columns)
        {
            Columns = columns;
        }

        public MapMapping[] Columns { get; }
    }
}