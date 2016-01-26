using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Castle.Core.Internal;

namespace Mascis
{
    public class QueryParser
    {
        private readonly MascisSession _session;
        private readonly ExpressionParser _binaryParser;

        public QueryParser(MascisSession session)
        {
            _session = session;
            _binaryParser = new ExpressionParser(_session);
        }

        public QueryTree.SelectExpression Parse(QueryTable queryTable)
        {
            var isSubQuery = queryTable.Maps.Count > 0;
            var ex = new QueryTree.SelectExpression
            {
                Values = queryTable.Maps.Select(x => new QueryTree.AliasedExpression
                {
                    Alias = x.Alias,
                    Expression = _binaryParser.Parse(x.Expression) 
                }).ToList(),
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
                        Alias = new QueryTree.TableAliasExpression { Alias = j.QueryTable.Alias },
                        On = _binaryParser.Parse(j.On),
                        Table = Parse(j.QueryTable)
                    });
                }
                else
                {
                    ex.Join.Add(new QueryTree.JoinExpression
                    {
                        Alias = new QueryTree.TableAliasExpression {  Alias = j.QueryTable.Alias },
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
            parameters.ForEach(x=>x.ParameterName = "p" + parameterCount++);

            return new ParsedQuery
            {
                Expression = ex,
                Parameters = parameters
            };
        }
    }

    public class ParsedQuery
    {
        public QueryTree.SelectExpression Expression { get; set; }
        public IEnumerable<QueryTree.ConstantExpression> Parameters { get; set; }
    }
}