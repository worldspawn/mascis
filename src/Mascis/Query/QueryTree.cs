using System.Collections.Generic;
using System.Reflection;
using Mascis.Configuration;

namespace Mascis.Query
{
    public static class QueryTree
    {
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
            Add,
            Subtract,
            Multiply,
            Divide
        }

        public class ConstantExpression : Expression
        {
            public object Value { get; set; }
            public string ParameterName { get; set; }
        }

        public class SelectExpression : Expression
        {
            public Expression From { get; set; }
            public IList<AliasedExpression> Values { get; set; }
            public IList<JoinExpression> Join { get; } = new List<JoinExpression>();
            public IList<Expression> Where { get; } = new List<Expression>();
        }

        public class ProjectionAliasesExpression : Expression
        {
            public IList<ProjectionConstructorArgumentExpression> ConstructorArguments { get; set; }
            public IList<ProjectionMemberAssignmentExpression> MemberAssignments { get; set; }
            public ConstructorInfo Constructor { get; set; }
        }

        public class ProjectionConstructorArgumentExpression : Expression
        {
            public int Index { get; set; }
            public AliasedExpression Expression { get; set; }
        }

        public class ProjectionMemberAssignmentExpression : Expression
        {
            public MemberInfo Member { get; set; }
            public AliasedExpression Expression { get; set; }
        }

        public class FromExpression : Expression
        {
            public TableExpression Table { get; set; }
        }

        public class JoinExpression : Expression
        {
            public Expression Table { get; set; }
            public Expression On { get; set; }
            public TableAliasExpression Alias { get; set; }
        }

        public class TableAliasExpression : Expression
        {
            public string Alias { get; set; }
        }

        public class UnAliasedTableExpression : Expression
        {
            public string Table { get; set; }
        }

        public class TableExpression : Expression
        {
            public string Table { get; set; }
            public string TableAlias { get; set; }
        }

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

        public class UnAliasedColumnExpression : Expression
        {
            public string Column { get; set; }
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
            public MapMapping MapMapping { get; set; }
        }

        public abstract class Expression
        {
        }

        public class ValueGroupExpression : Expression
        {
            public IList<Expression> Values { get; set; }
        }

        public class InsertExpression : Expression
        {
            public UnAliasedTableExpression Into { get; set; }

            public IList<UnAliasedColumnExpression> Columns { get; set; }
            public Expression From { get; set; }
        }

        public class UpdateExpression : Expression
        {
            public UnAliasedTableExpression Update { get; set; }
            public Dictionary<UnAliasedColumnExpression, ConstantExpression> Set { get; set; }
            public IList<BinaryExpression> Where { get; set; }
        }
    }
}