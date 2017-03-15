using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using jcc.LexerScope;

namespace jcc.ParserScope
{
    public abstract class UnaryOperation : Expression
    {
        private Expression arg;
        private Operator op;

        public Expression Arg
        {
            get
            {
                return arg;
            }
        }

        protected UnaryOperation (Expression arg, Operator op)
        {
            this.arg = arg;
            this.op = op;
            region = arg.Region | op.Region;
            arg.Parent = this;
        }

        public override XmlNode ToXml (System.Xml.XmlDocument doc)
        {
            XmlNode result = base.ToXml(doc);

            XmlNode argNode = doc.CreateElement("Arg");
            argNode.AppendChild(arg.ToXml(doc));

            result.AppendChild(argNode);
            return result;
        }

        public new abstract class Factory : NodeFactory
        {
            protected Type[] ArgTypes;
            protected Operator.Tag Tag;

            public override Node Make (Parser parser)
            {
                Operator tok = parser.AssertOperator(Tag);
                Expression arg = parser.AssertOneOfNodes<Expression>(ArgTypes);
                return Create(arg, tok);
            }

            protected Factory (Type operation, Operator.Tag Tag)
            {
                this.Tag = Tag;
                this.ArgTypes = PriorityManager.Instance.GetRightArgs(operation);
            }

            protected abstract UnaryOperation Create (Expression arg, Operator op);
        }

        public abstract class PostfixFactory : Factory
        {
            protected PostfixFactory (Type operation, Operator.Tag Tag)
                : base(operation, Tag)
            {
                this.ArgTypes = PriorityManager.Instance.GetLeftArgs(operation);
            }

            public override Node Make (Parser parser)
            {
                Expression arg = parser.AssertOneOfNodes<Expression>(ArgTypes);
                Operator tok = parser.AssertOperator(Tag);
                return Create(arg, tok);
            }
        }

        public override void AcceptVisitor (IExpressionVisitor visitor)
        {
            visitor.Visit(this);
        }

        public virtual void AcceptVisitor (IUnaryOperationVisitor visitor)
        {
            visitor.Visit(this);
        }

    }





    public sealed class opUnaryMinus : UnaryOperation
    {
        private opUnaryMinus (Expression arg, Operator op)
            : base(arg, op)
        {
        }

        public new class Factory : UnaryOperation.Factory
        {
            public Factory ()
                : base(typeof(opUnaryMinus), Operator.Tag.Minus)
            {
            }

            protected override UnaryOperation Create (Expression arg, Operator op)
            {
                return new opUnaryMinus(arg, op);
            }
        }

        public override void AcceptVisitor (IExpressionVisitor visitor)
        {
            visitor.Visit(this);
        }
        public override void AcceptVisitor (IUnaryOperationVisitor visitor)
        {
            visitor.Visit(this);
        }

    }

    public sealed class opNot : UnaryOperation
    {
        private opNot (Expression arg, Operator op)
            : base(arg, op)
        {
        }

        public new class Factory : UnaryOperation.Factory
        {
            public Factory ()
                : base(typeof(opNot), Operator.Tag.Pling)
            {
            }

            protected override UnaryOperation Create (Expression arg, Operator op)
            {
                return new opNot(arg, op);
            }
        }

        public override void AcceptVisitor (IExpressionVisitor visitor)
        {
            visitor.Visit(this);
        }
        public override void AcceptVisitor (IUnaryOperationVisitor visitor)
        {
            visitor.Visit(this);
        }

    }

    public sealed class opUnaryPlus : UnaryOperation
    {
        private opUnaryPlus (Expression arg, Operator op)
            : base(arg, op)
        {
        }

        public new class Factory : UnaryOperation.Factory
        {
            public Factory ()
                : base(typeof(opUnaryPlus), Operator.Tag.Plus)
            {
            }

            protected override UnaryOperation Create (Expression arg, Operator op)
            {
                return new opUnaryPlus(arg, op);
            }
        }

        public override void AcceptVisitor (IExpressionVisitor visitor)
        {
            visitor.Visit(this);
        }
        public override void AcceptVisitor (IUnaryOperationVisitor visitor)
        {
            visitor.Visit(this);
        }
    }

    public sealed class opPrefixIncrement : UnaryOperation
    {
        private opPrefixIncrement (Expression arg, Operator op)
            : base(arg, op)
        {
        }

        public new class Factory : UnaryOperation.Factory
        {
            public Factory ()
                : base(typeof(opPrefixIncrement), Operator.Tag.Increment)
            {
            }

            protected override UnaryOperation Create (Expression arg, Operator op)
            {
                return new opPrefixIncrement(arg, op);
            }

            public override bool CanStartsWith (Token token)
            {
                Operator op = token as Operator;
                return op != null && op.tag == Operator.Tag.Increment;
            }
        }

        public override void AcceptVisitor (IExpressionVisitor visitor)
        {
            visitor.Visit(this);
        }
        public override void AcceptVisitor (IUnaryOperationVisitor visitor)
        {
            visitor.Visit(this);
        }
    }

    public sealed class opPrefixDecrement : UnaryOperation
    {
        private opPrefixDecrement (Expression arg, Operator op)
            : base(arg, op)
        {
        }

        public new class Factory : UnaryOperation.Factory
        {
            public Factory ()
                : base(typeof(opPrefixDecrement), Operator.Tag.Decrement)
            {
            }

            protected override UnaryOperation Create (Expression arg, Operator op)
            {
                return new opPrefixDecrement(arg, op);
            }

            public override bool CanStartsWith (Token token)
            {
                Operator op = token as Operator;
                return op != null && op.tag == Operator.Tag.Decrement;
            }
        }

        public override void AcceptVisitor (IExpressionVisitor visitor)
        {
            visitor.Visit(this);
        }
        public override void AcceptVisitor (IUnaryOperationVisitor visitor)
        {
            visitor.Visit(this);
        }
    }

    public sealed class opPostfixIncrement : UnaryOperation
    {
        private opPostfixIncrement (Expression arg, Operator op)
            : base(arg, op)
        {
        }

        public new class Factory : UnaryOperation.PostfixFactory
        {
            public Factory ()
                : base(typeof(opPostfixIncrement), Operator.Tag.Increment)
            {
            }

            protected override UnaryOperation Create (Expression arg, Operator op)
            {
                return new opPostfixIncrement(arg, op);
            }
        }

        public override void AcceptVisitor (IExpressionVisitor visitor)
        {
            visitor.Visit(this);
        }
        public override void AcceptVisitor (IUnaryOperationVisitor visitor)
        {
            visitor.Visit(this);
        }
    }

    public sealed class opPostfixDecrement : UnaryOperation
    {
        private opPostfixDecrement (Expression arg, Operator op)
            : base(arg, op)
        {
        }

        public new class Factory : UnaryOperation.PostfixFactory
        {
            public Factory ()
                : base(typeof(opPostfixDecrement), Operator.Tag.Decrement)
            {
            }

            protected override UnaryOperation Create (Expression arg, Operator op)
            {
                return new opPostfixDecrement(arg, op);
            }
        }

        public override void AcceptVisitor (IExpressionVisitor visitor)
        {
            visitor.Visit(this);
        }
        public override void AcceptVisitor (IUnaryOperationVisitor visitor)
        {
            visitor.Visit(this);
        }
    }
}