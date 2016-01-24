using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Mascis
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

        public TEntity Ex { get; }

        public QueryTable(string alias, EntityMapping mapping)
        {
            Alias = alias;
            Mapping = mapping;
            _fieldCounter = 0;

        }

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

        public QueryMap Map(Expression<Func<object>> expression)
        {
            var qm = new QueryMap(expression, "f" + _fieldCounter++, this);
            Maps.Add(qm);
            return qm;
        }
    }
}