using System;
using System.Linq.Expressions;

namespace Mascis
{
    public class QueryJoin
    {
        
        public QueryTable QueryTable { get; }
        public Expression<Func<bool>> On { get; }
        public QueryJoin(Expression<Func<bool>> on, QueryTable queryTable)
        {
            QueryTable = queryTable;
            On = @on;

        }
    }
}