using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using jcc.LexerScope;

namespace jcc.ParserScope
{
    /// <summary>
    /// Abstract node represents any parent for all expression nodes 
    /// </summary>
    /// <remarks>
    /// We need this class for automatize creating of any expression nodes,
    /// now we can use <see cref="T:jcc.ParserScope.Expression.Factory" > for those purposes
    /// </remarks>
    public abstract class Expression : Node
    {
        /// <summary>
        /// Factory for automatic creating node of <see cref="T:jcc.ParserScope.Expression" > child types.
        /// </summary> 
        /// <remarks>This class try create one of expression node, on priority descended order</remarks>
        public class Factory : NodeFactory
        {
            private static Type[] AllOperations = PriorityManager.Instance.AllTypes;

            public override Node Make (Parser parser)
            {
                return parser.AssertOneOfNodes<Expression>(AllOperations);
            }
        }                                      

        /// <summary>
        /// Accepts the visitor <see cref="jcc.ParserScope.IExpressionVisitor"/> ).
        /// </summary>
        /// <param name="visitor">The visitor.</param>
        public virtual void AcceptVisitor (IExpressionVisitor visitor)
        {
            visitor.Visit(this);
        }

    }

    /// <summary>
    /// Creates lists of node types that used in the node factories, for creating child nodes in right sequence
    /// </summary>
    /// <remarks>This class implements singleton pattern</remarks>
    internal class PriorityManager
    {
        private static PriorityManager instance = null;
        /// <summary>
        /// Gets the instance of PriorityManager singleton.
        /// </summary>
        /// <value>The instance.</value>
        public static PriorityManager Instance
        {
            get
            {
                if (instance == null)
                    instance = new PriorityManager();
                return instance;
            }
        }

        private Type[] CommonExpression = new Type[] { typeof(opAssignment) };

        private Type[] LogicalExpression = new Type[] { typeof(opAND), typeof(opOR) };

        private Type[] CompareExpression = new Type[] { typeof(opLess), typeof(opLessEqual), typeof(opGreat), typeof(opGreateEqual), typeof(opLess), typeof(opEqual), typeof(opNotEqual) };

        private Type[] AdditiveExpression = new Type[] { typeof(opAdd), typeof(opSub) };

        private Type[] MultiplicativeExpression = new Type[] { typeof(opMul), typeof(opDiv), typeof(opMod) };

        private Type[] UnaryOpsExpression = new Type[] { typeof(opPrefixIncrement), typeof(opPrefixDecrement), typeof(opUnaryMinus), typeof(opUnaryPlus), typeof(opPostfixDecrement), typeof(opPostfixIncrement), typeof(opNot) };

        private Type[] TermExpression = new Type[] { typeof(Parentheses), typeof(FunctionCall), typeof(Variable), typeof(Const), typeof(TypeCast) };


        private Type[] lastMergeResult = new Type[] { };
        private void Merge (ref Type[] array)
        {
            Type[] result = new Type[array.Length + lastMergeResult.Length];
            int index = 0;
            foreach (Type t in array)
                result[index++] = t;
            foreach (Type t in lastMergeResult)
                result[index++] = t;
            lastMergeResult = array = result;
        }

        /// <summary>
        /// Gets the list of nodes' types that can be left argument of this operation [see type parameter].
        /// </summary>
        /// <param name="type">The operation type.</param>
        /// <returns>Array of the left nodes' types</returns>
        public Type[] GetLeftArgs (Type type)
        {
            for (int i = 1; i < Levels.Length; i++)
            {
                Type[] array = Levels[i];
                if (Array.IndexOf(array, type) != -1)
                    return Levels[i - 1];
            }
            return null;
        }
        /// <summary>
        /// Gets the list of nodes' types that can be right argument of this operation [see type parameter].
        /// </summary>
        /// <param name="type">The operation type.</param>
        /// <returns>Array of the right nodes' types</returns>
        public Type[] GetRightArgs (Type type)
        {
            for (int i = 1; i < Levels.Length; i++)
            {
                Type[] array = Levels[i];
                if (Array.IndexOf(array, type) != -1)
                    return array;
            }
            return null;
        }

        /// <summary>
        /// Gets list expression nodes' types.
        /// </summary>
        /// <value>Array of types</value>
        public Type[] AllTypes
        {
            get
            {
                return Levels[Levels.Length - 1];
            }
        }

        private Type[][] Levels;
        private PriorityManager ()
        {
            Levels = new Type[][] { TermExpression, UnaryOpsExpression, MultiplicativeExpression, AdditiveExpression, CompareExpression, LogicalExpression, CommonExpression };
            for (int i = 0; i < Levels.Length; i++)
            {
                Type[] array = Levels[i];
                Merge(ref array);
                Levels[i] = array;
            }
        }
    }


    /// <summary>
    /// Represents a type cast node 
    /// </summary>
    public class TypeCast : Expression
    {
        private TypeNode castingTypeNode;
        private Expression innerExpression;

        /// <summary>
        /// Gets the node with the type name.
        /// </summary>
        /// <value>The TypeNode for which will be cast</value>
        public TypeNode CastingTypeNode
        {
            get
            {
                return castingTypeNode;
            }
        }

        /// <summary>
        /// Gets the node with expression for type casting.
        /// </summary>
        /// <value>The inner expression.</value>
        public Expression InnerExpression
        {
            get
            {
                return innerExpression;
            }
        }

        private TypeCast (TypeNode type, Expression expr, TextRegion region)
        {
            this.castingTypeNode = type;
            innerExpression = expr;
            this.region = region;
        }

        /// <summary>
        /// Factory for automatic creating instance of this node 
        /// </summary>
        public new class Factory : NodeFactory
        {
            public override Node Make (Parser parser)
            {
                TypeNode type = parser.AssertNode<TypeNode>();
                TextRegion region = type.Region;
                region = region | parser.AssertOperator(Operator.Tag.LeftPar).Region;
                Expression expr = parser.AssertNode<Expression>();
                region = region | expr.Region;
                region = region | parser.AssertOperator(Operator.Tag.RightPar).Region;
                return new TypeCast(type, expr, region);
            }
        }

        /// <summary>
        /// Toes the XML representation of node with the all its chils.
        /// </summary>
        /// <param name="doc">The parent XML document</param>
        /// <returns>The XML Node.</returns>
        public override XmlNode ToXml (XmlDocument doc)
        {
            XmlNode result = base.ToXml(doc);
            XmlNode node = doc.CreateElement("TargetType");
            node.AppendChild(castingTypeNode.ToXml(doc));
            result.AppendChild(node);
            node = doc.CreateElement("CastExpression");
            node.AppendChild(innerExpression.ToXml(doc));
            result.AppendChild(node);
            return result;
        }

        /// <summary>
        /// Accepts the visitor <see cref="jcc.ParserScope.IExpressionVisitor"/> ).
        /// </summary>
        /// <param name="visitor">The visitor.</param>
        public override void AcceptVisitor (IExpressionVisitor visitor)
        {
            visitor.Visit(this);
        }

    }

    /// <summary>
    /// Represents a variable using node
    /// </summary>
    public class Variable : Expression
    {
        IdentifierToken id;
        public string Name
        {
            get
            {
                return id.Text;
            }
        }


        private Variable (IdentifierToken id)
        {
            this.id = id;
            region = id.Region;
        }

        /// <summary>
        /// Factory for automatic creating instance of this node 
        /// </summary>
        public new class Factory : NodeFactory
        {

            public override Node Make (Parser parser)
            {
                IdentifierToken id = parser.AssertToken<IdentifierToken>();
                return new Variable(id);

            }
            public override bool CanStartsWith (Token token)
            {
                return token is IdentifierToken;
            }
        }

        public override XmlNode ToXml (XmlDocument doc)
        {
            XmlNode result = base.ToXml(doc);
            XmlAttribute attr = doc.CreateAttribute("Name");
            attr.Value = Name;
            result.Attributes.Append(attr);
            return result;
        }

        public override void AcceptVisitor (IExpressionVisitor visitor)
        {
            visitor.Visit(this);
        }
    }

    /// <summary>
    /// Represents the arithmetics parentheses node 
    /// </summary>
    public class Parentheses : Expression
    {
        private Expression innerExpression;
        public Expression InnerExpression
        {
            get 
            {
                return innerExpression;
            }
        }

        private Parentheses (Operator open, Expression inner, Operator close)
        {
            innerExpression = inner;
            inner.Parent = this;
            region = open.Region | inner.Region | close.Region;
        }

        /// <summary>
        /// Factory for automatic creating instance of this node 
        /// </summary>
        public new class Factory : NodeFactory
        {

            public override Node Make (Parser parser)
            {
                Operator open = parser.AssertOperator(Operator.Tag.LeftPar);
                Expression inner = parser.AssertNode<Expression>();
                Operator close = parser.AssertOperator(Operator.Tag.RightPar);
                return new Parentheses(open, inner, close);

            }

            public override bool CanStartsWith (Token token)
            {
                Delimiter deli = token as Delimiter;
                if (deli == null || deli.tag != Operator.Tag.LeftPar)
                    return false;
                return true;
            }
        }

        public override XmlNode ToXml (XmlDocument doc)
        {
            XmlNode result = base.ToXml(doc);
            result.AppendChild(innerExpression.ToXml(doc));
            return result;
        }

        public override void AcceptVisitor (IExpressionVisitor visitor)
        {
            visitor.Visit(this);
        }


    }

    /// <summary>
    /// Represents the function call node 
    /// </summary>
    public class FunctionCall : Expression, IEnumerable<Expression>
    {
        IdentifierToken name;
        List<Expression> args;

        public string Name
        {
            get
            {
                return name.Text;
            }
        }

        public Expression this[int index]
        {
            get
            {
                return args[index];
            }
        }

        public int Count
        {
            get
            {
                return args.Count;
            }
        }

        private FunctionCall (IdentifierToken name, List<Expression> args, TextRegion region)
        {
            this.name = name;
            this.args = args;

            foreach (Expression expr in args)
                expr.Parent = this;

            this.region = region;
        }

        public new class Factory : NodeFactory
        {
            // function_call ::= identifier '(' expression? {',' expression} ')'
            public override Node Make (Parser parser)
            {
                //name
                IdentifierToken name = parser.AssertToken<IdentifierToken>();
                TextRegion region = name.Region;
                //(
                region = region | parser.AssertOperator(Operator.Tag.LeftPar).Region;
                List<Expression> args = new List<Expression>();
                try
                {
                    while (true)
                    {
                        Expression expr = parser.AssertNode<Expression>();
                        region = region | expr.Region;
                        args.Add(expr);
                        Operator comma = parser.TryGetOperator(Operator.Tag.Comma);
                        if (comma == null)
                            break;
                        region = region | comma.Region;
                    }
                }
                catch
                {
                }
                //)
                region = region | parser.AssertOperator(Operator.Tag.RightPar).Region;
                return new FunctionCall(name, args, region);
            }

            public override bool CanStartsWith (Token token)
            {
                return token is IdentifierToken;
            }
        }

        public override XmlNode ToXml (XmlDocument doc)
        {
            XmlNode result = base.ToXml(doc);
            XmlAttribute nameAttr = doc.CreateAttribute("Name");
            nameAttr.Value = name.Text;
            result.Attributes.Append(nameAttr);
            int index = 0;
            foreach (Expression arg in args)
            {
                XmlNode argNode = doc.CreateElement("Arg" + index.ToString());
                argNode.AppendChild(arg.ToXml(doc));
                result.AppendChild(argNode);
            }
            return result;
        }

        public override void AcceptVisitor (IExpressionVisitor visitor)
        {
            visitor.Visit(this);
        }


        #region IEnumerable<Expression> Members

        public IEnumerator<Expression> GetEnumerator ()
        {
            return args.GetEnumerator();
        }

        #endregion

        #region IEnumerable Members

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator ()
        {
            return  args.GetEnumerator();
        }

        #endregion
    }




}