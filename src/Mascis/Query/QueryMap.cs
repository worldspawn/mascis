using System;
using System.Linq.Expressions;

namespace Mascis.Query
{
    public class QueryMap
    {
        public QueryMap(Expression<Func<object>> expression, string alias, QueryTable queryTable)
        {
            Table = queryTable;
            if (alias == null) throw new ArgumentNullException(nameof(alias));
            Expression = expression;
            Alias = alias;
        }

        public QueryTable Table { get; }
        public Expression<Func<object>> Expression { get; }
        public string Alias { get; }

        public T Value<T>()
        {
            throw new NotImplementedException();
        }
    }
}