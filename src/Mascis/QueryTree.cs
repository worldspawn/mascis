using System.Collections.Generic;

namespace Mascis
{
    public static class QueryTree
    {
        public class ConstantExpression: Expression
        {
            public object Value { get; set; }
        }

        public class SelectExpression : Expression
        {
            public Expression From { get; set; }
            public IList<AliasedExpression> Values { get; set; } 
            public IList<JoinExpression> Join { get; } = new List<JoinExpression>();
            public IList<Expression> Where { get; } = new List<Expression>();
        }

        public class FromExpression: Expression
        {
            public TableExpression Table { get; set; }
        }

        public class JoinExpression: Expression
        {
            public Expression Table { get; set; }
            public Expression On { get; set; }
        }

        public class TableExpression: Expression
        {
            public string Table { get; set; }
            public string TableAlias { get; set; } 
        }

        //public class Where
        //{
        //    public Expression Clause { get; set; }
        //}

        public class BinaryExpression : Expression
        {
            public BooleanOperator Operator { get; set; }
            public Expression Left { get; set; }
            public Expression Right { get; set; }
        }

        public class ColumnExpression : Expression
        {
            public string Column { get; set; } 
            public string TableAlias { get; set; }
        }

        public class FunctionExpression : Expression
        {
            public string Name { get; set; }
            public Expression[] Arguments { get; set; }
        }

        public class AliasReferenceExpression : Expression
        {
            public string Alias { get; set; }
            public string TableAlias { get; set; }
        }

        public class AliasedExpression : Expression
        {
            public string Alias { get; set; }
            public Expression Expression { get; set; }
        }

        public enum BooleanOperator
        {
            Unknown,
            And,
            Or,
            Equal,
            GreaterThan,
            GreaterThanOrEqualTo,
            LessThan,
            LessThanOrEqualTo,
            Add
        }

        public class Expression
        {
            
        }
    }
}