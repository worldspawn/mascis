using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Mascis.Configuration;
using Mascis.Query;

namespace Mascis
{
    public interface IMascisSession
    {
        MascisFactory Factory { get; }
        QueryParser Parser { get; }
        IDbConnection DbConnection { get; }
        void Dispose();
        void Attach(object obj);

        T Create<T>()
            where T : class;

        void Save(object obj);
        void SaveChanges();
        IList<TEntity> Execute<TEntity>(Query<TEntity> query);
        Query<TEntity> Query<TEntity>();
    }

    public class MascisSession : IDisposable, IMascisSession
    {
        private readonly Dictionary<object, object[]> _attachedObjects;

        public MascisSession(MascisFactory factory)
        {
            Factory = factory;

            DbConnection = new SqlConnection(factory.ConnectionString);
            Parser = new QueryParser(this);
            _attachedObjects = new Dictionary<object, object[]>();
        }

        public MascisFactory Factory { get; }
        public QueryParser Parser { get; }

        public IDbConnection DbConnection { get; } //TODO:

        public void Dispose()
        {
            if (DbConnection != null && DbConnection.State == ConnectionState.Open)
            {
                DbConnection.Close();
            }
        }

        private void Attach<T>(T entity, object[] state)
        {
            _attachedObjects.Add(entity, state);
        }

        public void Attach(object obj)
        {
            var state = CreateState(obj);
            Attach(obj, state);
        }

        private object[] CreateState(object obj)
        {
            if (obj == null) throw new ArgumentNullException(nameof(obj));
            var t = obj.GetType();
            var entityMapping = Factory.Mappings.MappingsByType[t];
            if (entityMapping == null)
            {
                throw new UnknownEntityException();
            }
            var state = new object[entityMapping.Maps.Length];
            var index = 0;
            foreach (var m in entityMapping.Maps)
            {
                state[index++] = m.Property.GetValue(obj);
            }
            return state;
        }

        public T Create<T>()
            where T : class
        {
            var t = typeof (T);
            var entityMapping = Factory.Mappings.MappingsByType[t];
            var state = new object[entityMapping.Maps.Length];
            var cstr = t.GetConstructor(new Type[0]);
            var entity = (T) cstr?.Invoke(null);
            if (entity == null)
            {
                throw new NoDefaultConstructorException();
            }
            Attach(entity, state);
            return entity;
        }

        public void Save(object obj)
        {
            if (obj == null) throw new ArgumentNullException(nameof(obj));
            var t = obj.GetType();
            var entityMapping = Factory.Mappings.MappingsByType[t];
            if (entityMapping == null)
            {
                throw new UnknownEntityException();
            }
            Attach(obj, null);
        }

        public void SaveChanges()
        {
            foreach (var e in _attachedObjects)
            {
                if (e.Value == null)
                {
                    Insert(e.Key);
                }
                else
                {
                    Update(e.Key, e.Value);
                }
            }
        }

        public IList<T> Execute<T, TEntity>(Projection<T, TEntity> projection)
        {
            var queryPlan = Parser.Parse(projection);
            var expression = queryPlan.Expression;
            if (expression == null) throw new NullReferenceException(nameof(expression));
            var processor = new MsSqlProcessor();
            var list = new List<T>();
            var cstr = queryPlan.Constructor.Constructor;

            using (var command = processor.Process(queryPlan.Expression, queryPlan.Parameters, this))
            {
                command.Connection = DbConnection;
                if (DbConnection.State != ConnectionState.Open)
                {
                    command.Connection.Open();
                }
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var args = queryPlan.Constructor.ConstructorArguments.Select(prop =>
                        {
                            var ordinal = reader.GetOrdinal(prop.Expression.Alias);
                            return reader.GetValue(ordinal);
                        }).ToArray();
                        var t = (T)cstr.Invoke(args);
                        foreach (var prop in queryPlan.Constructor.MemberAssignments)
                        {
                            var ordinal = reader.GetOrdinal(prop.Expression.Alias);
                            var value = reader.GetValue(ordinal);
                            ((PropertyInfo)prop.Member).SetValue(t, value);
                        }
                        list.Add(t);
                    }
                }
            }

            return list;
        } 

        public IList<TEntity> Execute<TEntity>(Query<TEntity> query)
        {
            var queryPlan = Parser.Parse(query);
            var expression = queryPlan.Expression as QueryTree.SelectExpression;
            if (expression == null) throw new NullReferenceException(nameof(expression));
            var processor = new MsSqlProcessor();
            var list = new List<TEntity>();
            var cstr = typeof (TEntity).GetConstructor(new Type[0]);
            if (cstr == null)
            {
                throw new NoDefaultConstructorException();
            }

            using (var command = processor.Process(queryPlan, this))
            {
                command.Connection = query.Session.DbConnection;
                if (query.Session.DbConnection.State != ConnectionState.Open)
                {
                    command.Connection.Open();
                }
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var t = (TEntity) cstr.Invoke(null);
                        foreach (var prop in expression.Values)
                        {
                            var ordinal = reader.GetOrdinal(prop.Alias);
                            var value = reader.GetValue(ordinal);
                            prop.MapMapping.Property.SetValue(t, value);
                        }
                        query.Session.Attach(t);
                        list.Add(t);
                    }
                }
            }

            return list;
        }

        private void Insert(object entity)
        {
            var queryPlan = Parser.Insert(entity);

            var processor = new MsSqlProcessor();
            using (var command = processor.Process(queryPlan, this))
            {
                command.Connection = DbConnection;
                if (DbConnection.State != ConnectionState.Open)
                {
                    command.Connection.Open();
                }
                command.ExecuteNonQuery();
            }
        }

        private void Update(object entity, object[] originalValues)
        {
            var state = CreateState(entity);
            var index = 0;
            var em = Factory.Mappings.MappingsByType[entity.GetType()];
            var update = new List<Tuple<MapMapping, object>>();
            foreach (var map in em.Maps)
            {
                if (state[index] != originalValues[index])
                {
                    update.Add(new Tuple<MapMapping, object>(map, state[index]));
                    index++;
                }
            }

            if (index == 0)
            {
                return;
            }

            var queryPlan = Parser.Update(entity, update);
            var processor = new MsSqlProcessor();
            var command = processor.Process(queryPlan, this);
            command.Connection = DbConnection;
            if (DbConnection.State != ConnectionState.Open)
            {
                command.Connection.Open();
            }
            command.ExecuteNonQuery();
        }

        public Query<TEntity> Query<TEntity>()
        {
            return Mascis.Query.Query<TEntity>.From(this);
        }
    }

    public class TargetNotWriteableException : Exception
    {
    }
}