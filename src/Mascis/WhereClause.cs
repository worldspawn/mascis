using System;
using System.Linq.Expressions;

namespace Mascis
{
    public class WhereClause
    {
        public WhereClause(Expression<Func<bool>> @where)
        {
            Where = @where;
        }

        public Expression<Func<bool>> Where { get; }
    }
}