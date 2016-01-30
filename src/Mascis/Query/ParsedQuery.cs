using System.Collections.Generic;

namespace Mascis.Query
{
    public class ParsedQuery
    {
        public QueryTree.Expression Expression { get; set; }
        public IEnumerable<QueryTree.ConstantExpression> Parameters { get; set; }
    }
}