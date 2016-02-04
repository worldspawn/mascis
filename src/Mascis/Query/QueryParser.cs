using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Mascis.Configuration;

namespace Mascis.Query
{
    public class QueryParser
    {
        private readonly ExpressionParser _binaryParser;
        private readonly IMascisSession _session;

        public QueryParser(IMascisSession session)
        {
            _session = session;
            _binaryParser = new ExpressionParser(_session);
        }

        private QueryTree.InsertExpression Parse(object entity, ConstantExpressionCatcher ec)
        {
            var entityMapping = _session.Factory.Mappings.MappingsByType[entity.GetType()];
            var insert = new QueryTree.InsertExpression();
            var index = 0;
            insert.From = new QueryTree.ValueGroupExpression
            {
                Values =
                    entityMapping.Maps.Select(
                        x =>
                            (QueryTree.Expression)
                                ec.Catch(new QueryTree.ConstantExpression
                                {
                                    ParameterName = "p" + index++,
                                    Value = x.Property.GetValue(entity)
                                })).ToList()
            };
            insert.Columns =
                new List<QueryTree.UnAliasedColumnExpression>(
                    entityMapping.Maps.Select(x => new QueryTree.UnAliasedColumnExpression {Column = x.ColumnName}));
            insert.Into = new QueryTree.UnAliasedTableExpression {Table = entityMapping.TableName};

            return insert;
        }

        public ParsedQuery Insert(object entity)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));

            var ec = new ConstantExpressionCatcher();
            var ex = Parse(entity, ec);

            return new ParsedQuery
            {
                Expression = ex,
                Parameters = ec.Constants
            };
        }

        public ParsedQuery Update(object entity, List<Tuple<MapMapping, object>> updates)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));

            var ec = new ConstantExpressionCatcher();
            var ex = Parse(entity, updates, ec);

            return new ParsedQuery
            {
                Expression = ex,
                Parameters = ec.Constants
            };
        }

        private QueryTree.UpdateExpression Parse(object entity, List<Tuple<MapMapping, object>> updates,
            ConstantExpressionCatcher ec)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));
            if (updates == null) throw new ArgumentNullException(nameof(updates));
            var entityMapping = _session.Factory.Mappings.MappingsByType[entity.GetType()];
            var update = new QueryTree.UpdateExpression();
            var index = 0;
            update.Update = new QueryTree.UnAliasedTableExpression {Table = entityMapping.TableName};
            update.Set = updates.ToDictionary(
                x => new QueryTree.UnAliasedColumnExpression {Column = x.Item1.ColumnName},
                x => ec.Catch(new QueryTree.ConstantExpression {ParameterName = "p" + index++, Value = x.Item2}));

            update.Where =
                entityMapping.Key.Keys.Select(
                    x =>
                        new QueryTree.BinaryExpression
                        {
                            Left = new QueryTree.UnAliasedColumnExpression {Column = x.ColumnName},
                            Operator = QueryTree.BooleanOperator.Equal,
                            Right =
                                ec.Catch(new QueryTree.ConstantExpression
                                {
                                    ParameterName = "p" + index++,
                                    Value = x.Property.GetValue(entity)
                                })
                        }).ToList();

            return update;
        }

        public ParsedProjection Parse<T, TEntity>(Projection<T, TEntity> projection)
        {
            var select = Parse(projection.Query.FromTable, true);
            var values = _binaryParser.Parse(projection.Expression) as QueryTree.ProjectionAliasesExpression;
            if (values == null) throw new ArgumentNullException(nameof(values));
            select.Values.Clear();
            select.Values = values.ConstructorArguments.Select(x => new QueryTree.AliasedExpression
            {
                Alias = x.Expression.Alias,
                Expression = x.Expression.Expression
            })
            .Union(values.MemberAssignments.Select(x=> new QueryTree.AliasedExpression
            {
                Alias = x.Expression.Alias,
                Expression = x.Expression.Expression
            }))
            .ToList();
            
            var parameters = _binaryParser.GetConstantExpressionsAndClear();
            var parameterCount = 0;
            Array.ForEach(parameters, x => x.ParameterName = "p" + parameterCount++);

            return new ParsedProjection
            {
                Constructor = values,
                Expression = select,
                Parameters = parameters
            };
        }

        public QueryTree.SelectExpression Parse(QueryTable queryTable, bool isSubQuery = false)
        {
            var ex = new QueryTree.SelectExpression
            {
                Values = queryTable.Maps.Select(x => new QueryTree.AliasedExpression
                {
                    Alias = x.Alias,
                    Expression = _binaryParser.Parse(x.Expression)
                }).ToList()
            };

            if (!isSubQuery && ex.Values.Count == 0)
            {
                ex.Values = queryTable.Mapping.Maps.Select(x => new QueryTree.AliasedExpression
                {
                    Alias = x.ColumnName,
                    Expression = new QueryTree.ColumnExpression
                    {
                        Column = x.ColumnName,
                        TableAlias = queryTable.Alias
                    },
                    MapMapping = x
                }).ToList();
            }

            ex.From = new QueryTree.FromExpression
            {
                Table = new QueryTree.TableExpression
                {
                    TableAlias = queryTable.Alias,
                    Table = queryTable.Mapping.TableName
                }
            };

            foreach (var j in queryTable.Joins)
            {
                if (j.QueryTable.Maps.Count > 0 || j.QueryTable.Joins.Count > 0)
                {
                    ex.Join.Add(new QueryTree.JoinExpression
                    {
                        Alias = new QueryTree.TableAliasExpression {Alias = j.QueryTable.Alias},
                        On = _binaryParser.Parse(j.On),
                        Table = Parse(j.QueryTable)
                    });
                }
                else
                {
                    ex.Join.Add(new QueryTree.JoinExpression
                    {
                        Alias = new QueryTree.TableAliasExpression {Alias = j.QueryTable.Alias},
                        On = _binaryParser.Parse(j.On),
                        Table = new QueryTree.TableExpression
                        {
                            TableAlias = j.QueryTable.Alias,
                            Table = j.QueryTable.Mapping.TableName
                        }
                    });
                }


                foreach (var w in queryTable.Wheres)
                {
                    ex.Where.Add(_binaryParser.Parse(w.Where));
                }
            }

            return ex;
        }

        public ParsedQuery Parse<TEntity>(Query<TEntity> query)
        {
            var ex = Parse(query.FromTable);
            var parameters = _binaryParser.GetConstantExpressionsAndClear();
            var parameterCount = 0;
            Array.ForEach(parameters, x => x.ParameterName = "p" + parameterCount++);

            return new ParsedQuery
            {
                Expression = ex,
                Parameters = parameters
            };
        }

        public class ConstantExpressionCatcher
        {
            public ConstantExpressionCatcher()
            {
                Constants = new List<QueryTree.ConstantExpression>();
            }

            public IList<QueryTree.ConstantExpression> Constants { get; }

            public QueryTree.ConstantExpression Catch(QueryTree.ConstantExpression expression)
            {
                Constants.Add(expression);
                return expression;
            }
        }
    }

    public class ParsedProjection
    {
        public QueryTree.ProjectionAliasesExpression Constructor { get; set; }
        public QueryTree.SelectExpression Expression { get; set; }
        public QueryTree.ConstantExpression[] Parameters { get; set; }
    }
}