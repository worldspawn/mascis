using System;
using System.Linq.Expressions;
using Mascis.Configuration;

namespace Mascis.Query
{
    public class Query<TEntity>
    {
        private int _tableCounter;

        protected Query(EntityMapping @from, MascisSession session)
        {
            Session = session;
            _tableCounter = 0;
            FromTable = new QueryTable<TEntity>("t" + _tableCounter++, @from, session);
        }

        public MascisSession Session { get; }
        
        public QueryTable<TEntity> FromTable { get; set; }

        public static Query<TEntity> From(MascisSession session)
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