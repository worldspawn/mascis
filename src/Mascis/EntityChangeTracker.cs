using System.Collections.Generic;

namespace Mascis
{
    public class EntityChangeTracker
    {
        private readonly object _entity;
        private readonly Dictionary<MapMapping, MapChange> _changes;

        public EntityChangeTracker(object entity)
        {
            _entity = entity;
            _changes = new Dictionary<MapMapping, MapChange>();
        }

        public bool HasChangeFor(MapMapping mapping)
        {
            return _changes.ContainsKey(mapping);
        }

        public void MapChanged(MapMapping mapping, object originalValue, object newValue)
        {
            if (originalValue == newValue)
            {
                return;
            }

            if (_changes.ContainsKey(mapping))
            {
                _changes[mapping].NewValue = newValue;
            }
            else
            {
                _changes.Add(mapping, new MapChange(mapping, originalValue, newValue));
            }
        }
    }
}