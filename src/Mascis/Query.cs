using System;
using System.Linq.Expressions;

namespace Mascis
{
    public class Query<TEntity>
    {
        private readonly MascisSession _session;
        private int _tableCounter;

        private int _fieldCounter;

        protected Query(EntityMapping @from, MascisSession session)
        {
            _session = session;
            _tableCounter = 0;
            FromTable = new QueryTable<TEntity>("t" + _tableCounter++, @from);
            _fieldCounter = 0;
        }

        public static Query<TEntity> From(MascisSession session)
        {
            var q = new Query<TEntity>(session.Factory.Mappings.MappingsByType[typeof(TEntity)], session);
            
            return q;
        }

        public Query<TEntity> Join<T>(QueryTable<T> queryTable, Expression<Func<bool>> on)
        {
            FromTable.Join(queryTable, on);
            return this;
        }

        public Query<TEntity> Where(Expression<Func<bool>> where)
        {
            FromTable.Where(where);
            return this;
        }

        public QueryTable<T> CreateTable<T>()
        {
            var em = _session.Factory.Mappings.MappingsByType[typeof (T)];
            return new QueryTable<T>("t" + _tableCounter++, em);
        } 
        

        public QueryTable<TEntity> FromTable { get; set; }
    }
}