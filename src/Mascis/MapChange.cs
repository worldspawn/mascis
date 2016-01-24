namespace Mascis
{
    public class MapChange
    {
        private readonly MapMapping _mapping;
        public object OriginalValue { get; }
        public object NewValue { get; set; }

        public MapChange(MapMapping mapping, object originalValue, object newValue)
        {
            _mapping = mapping;
            OriginalValue = originalValue;
            NewValue = newValue;
        }
    }
}