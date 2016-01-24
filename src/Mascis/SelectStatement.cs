using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Mascis
{
    public class SelectStatement
    {
        private readonly EntityMapping _mapping;
        private readonly IEnumerable<MapMapping> _mapsToSelect;

        public SelectStatement(EntityMapping mapping, IEnumerable<MapMapping> mapsToSelect)
        {
            _mapping = mapping;
            _mapsToSelect = mapsToSelect;
        }

        public string GenerateStatement ()
        {
            var sb = new StringBuilder();
            sb.Append("SELECT ");
            sb.Append(String.Join(",", _mapsToSelect.Select(m => m.ColumnName).ToArray()));
            sb.Append(" FROM ");
            sb.Append(_mapping.TableName);

            return sb.ToString();
        }
    }
}