using System;
using System.Linq.Expressions;

namespace Mascis.Query
{
    public class QueryJoin
    {
        public QueryJoin(Expression<Func<bool>> on, QueryTable queryTable)
        {
            QueryTable = queryTable;
            On = @on;
        }

        public QueryTable QueryTable { get; }
        public Expression<Func<bool>> On { get; }
    }
}