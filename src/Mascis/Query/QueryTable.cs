using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Mascis.Configuration;

namespace Mascis.Query
{
    public abstract class QueryTable
    {
        protected QueryTable()
        {
            Maps = new List<QueryMap>();
            Joins = new List<QueryJoin>();
            Wheres = new List<WhereClause>();
        }

        public string Alias { get; set; }
        public EntityMapping Mapping { get; set; }

        public List<QueryMap> Maps { get; }
        public List<QueryJoin> Joins { get; }
        public List<WhereClause> Wheres { get; }
    }

    public class QueryTable<TEntity> : QueryTable
    {
        private int _fieldCounter;

        public QueryTable(string alias, EntityMapping mapping, IMascisSession session)
        {
            Alias = alias;
            Mapping = mapping;
            _fieldCounter = 0;

            Ex = (TEntity) session.Factory.Generator.CreateClassProxy(typeof (TEntity), new[] {typeof (IQueryTableReference)}, new QueryTableReferenceInterceptor(this));
        }

        public TEntity Ex { get; }

        public QueryTable<TEntity> Where(Expression<Func<bool>> where)
        {
            Wheres.Add(new WhereClause(where));
            return this;
        }

        public QueryTable<TEntity> Join<T>(QueryTable<T> queryTable, Expression<Func<bool>> on)
        {
            Joins.Add(new QueryJoin(on, queryTable));
            return this;
        }

        public QueryTable<TEntity> Join<T>(QueryTable<T> queryTable, Expression<Func<T, bool>> on)
        {
            var constant = Expression.Constant(queryTable.Ex);
            var ev = new ParameterToConstantExpressionVisitor<Func<bool>>(constant);
            var convertedOn = ev.VisitAndConvert(on);
            Joins.Add(new QueryJoin(convertedOn, queryTable));
            return this;
        }

        public QueryMap Map(Expression<Func<object>> expression)
        {
            var qm = new QueryMap(expression, "f" + _fieldCounter++, this);
            Maps.Add(qm);
            return qm;
        }

        public class ParameterToConstantExpressionVisitor<TOutput> : ExpressionVisitor
        {
            private readonly ConstantExpression _expression;

            public ParameterToConstantExpressionVisitor(ConstantExpression expression)
            {
                _expression = expression;
            }

            internal Expression<TOutput> VisitAndConvert<T>(Expression<T> root)
            {
                return (Expression<TOutput>) VisitLambda(root);
            }

            protected override Expression VisitParameter(ParameterExpression node)
            {
                return _expression;
            }

            protected override Expression VisitLambda<T>(Expression<T> node)
            {
                return Expression.Lambda<TOutput>(Visit(node.Body));
            }
        }
    }
}