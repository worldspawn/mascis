using System;
using System.Linq.Expressions;
using Castle.DynamicProxy.Generators.Emitters.SimpleAST;

namespace Mascis.Query
{
    public class QueryMap
    {
        public QueryMap(Expression<Func<object>> expression, string alias, QueryTable queryTable)
        {
            Table = queryTable;
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

    public class QueryMap<T> : QueryMap
    {
        public QueryMap(Expression<Func<T>> expression, string alias, QueryTable queryTable)
            :base (ConvertExpression(expression), alias, queryTable)
        {
        }

        public T Value()
        {
            throw new NotImplementedException();
        }

        private static Expression<Func<object>>  ConvertExpression(Expression<Func<T>> expression)
        {
            //var newParameter = System.Linq.Expressions.Expression.Parameter(typeof (object));
            //var oldParameter = expression.Parameters[0];


            return System.Linq.Expressions.Expression.Lambda<Func<object>>(System.Linq.Expressions.Expression.Convert(expression.Body, typeof(object)));
        }
    }
}