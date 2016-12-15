using System;
using System.Collections.Generic;
using System.Linq;
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
            GroupBys = new List<QueryMap>();
        }

        public string Alias { get; set; }
        public EntityMapping Mapping { get; set; }

        public List<QueryMap> Maps { get; }
        public List<QueryJoin> Joins { get; }
        public List<WhereClause> Wheres { get; }
        public List<QueryMap> GroupBys { get; } 
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

        public QueryTable<TEntity> Join<T>(QueryTable<T> queryTable, Expression<Func<TEntity, T, bool>> on)
        {
            var sourceConstant = Expression.Constant(Ex);
            var targetConstant = Expression.Constant(queryTable.Ex);
            var ev = new ParameterToConstantExpressionVisitor<Func<bool>>(sourceConstant, targetConstant);
            var convertedOn = ev.VisitAndConvert(on);
            Joins.Add(new QueryJoin(convertedOn, queryTable));
            return this;
        }

        public QueryTable<TEntity> GroupBy(params Expression<Func<object>>[] groupBy)
        {
            GroupBys.AddRange(groupBy.Select(x => new QueryMap(x, "f" + _fieldCounter++, this)));
            return this;
        }

        public QueryMap<T> GroupBy<T>(Expression<Func<T>> groupBy)
        {
            var map = new QueryMap<T>(groupBy, $"f{_fieldCounter++}", this);
            GroupBys.Add(map);

            return map;
        }

        public QueryMap Map(Expression<Func<object>> expression)
        {
            var qm = new QueryMap(expression, "f" + _fieldCounter++, this);
            Maps.Add(qm);
            return qm;
        }

        public class ParameterToConstantExpressionVisitor<TOutput> : ExpressionVisitor
        {
            private readonly ConstantExpression[] _expression;

            public ParameterToConstantExpressionVisitor(params ConstantExpression[] expression)
            {
                _expression = expression;
            }

            internal Expression<TOutput> VisitAndConvert<T>(Expression<T> root)
            {
                return (Expression<TOutput>) VisitLambda(root);
            }

            protected override Expression VisitParameter(ParameterExpression node)
            {
                var constant = _expression.First(x => x.Type.BaseType == node.Type);//base type because these are always proxies
                return constant;
            }

            protected override Expression VisitLambda<T>(Expression<T> node)
            {
                return Expression.Lambda<TOutput>(Visit(node.Body));
            }
        }
    }
}