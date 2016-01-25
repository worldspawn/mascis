using System;
using System.Linq;
using System.Linq.Expressions;

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
            var ex = new QueryTree.SelectExpression
            {
                Values = queryTable.Maps.Select(x => new QueryTree.AliasedExpression
                {
                    Alias = x.Alias,
                    Expression = _binaryParser.Parse(x.Expression)
                }).ToList(),

            };

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
                        On = _binaryParser.Parse(j.On),
                        Table = Parse(j.QueryTable)
                    });
                }
                else
                {
                    ex.Join.Add(new QueryTree.JoinExpression
                    {
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

        public QueryTree.SelectExpression Parse<TEntity>(Query<TEntity> query)
        {
            var ex = Parse(query.FromTable);
            
            return ex;
        }
    }
}