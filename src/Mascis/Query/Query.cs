using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using Mascis.Configuration;

namespace Mascis.Query
{
    public class Projection<T, TEntity>
    {
        internal readonly Query<TEntity> Query;
        internal readonly Expression<Func<T>> Expression;

        public Projection(Query<TEntity> query, Expression<Func<T>> expression)
        {
            Query = query;
            Expression = expression;
        }
    }

    public class Query<TEntity>
    {
        private int _tableCounter;

        protected Query(EntityMapping @from, IMascisSession session)
        {
            Session = session;
            _tableCounter = 0;
            FromTable = new QueryTable<TEntity>("t" + _tableCounter++, @from, session);
        }

        public IMascisSession Session { get; }

        public QueryTable<TEntity> FromTable { get; set; }

        public Projection<T, TEntity> Project<T>(Expression<Func<T>> expression)
        {
            return new Projection<T, TEntity>(this, expression);
        }

        public static Query<TEntity> From(IMascisSession session)
        {
            var q = new Query<TEntity>(session.Factory.Mappings.MappingsByType[typeof (TEntity)], session);

            return q;
        }

        public Query<TEntity> Join<T>(QueryTable<T> queryTable, Expression<Func<bool>> on)
        {
            FromTable.Join(queryTable, on);
            return this;
        }

        private void Join<T>(QueryTable<T> queryTable, Expression<Func<T, bool>> @on)
        {
            FromTable.Join(queryTable, on);
        }

        public Query<TEntity> Where(Expression<Func<bool>> where)
        {
            FromTable.Where(where);
            return this;
        }

        public QueryTable<T> CreateTable<T>()
        {
            var em = Session.Factory.Mappings.MappingsByType[typeof (T)];
            return new QueryTable<T>("t" + _tableCounter++, em, Session);
        }

        public QueryTable<T> CreateTable<T>(Expression<Func<T, bool>> on)
        {
            var qt = CreateTable<T>();
            Join(qt, @on);
            return qt;
        }
    }
}