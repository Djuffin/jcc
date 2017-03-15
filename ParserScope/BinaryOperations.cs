using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using jcc.LexerScope;

namespace jcc.ParserScope
{

    public abstract class BinaryOperation : Expression
    {
        private Expression arg0, arg1;
        private Operator op;

        public Expression Arg0
        {
            get
            {
                return arg0;
            }
        }
        public Expression Arg1
        {
            get
            {
                return arg1;
            }
        }

        protected BinaryOperation (Expression arg0, Operator op, Expression arg1)
        {
            this.arg0 = arg0;
            this.arg1 = arg1;
            this.op = op;
            region = arg0.Region | op.Region | arg1.Region;
            arg0.Parent = this;
            arg1.Parent = this;
        }



        public new abstract class Factory : NodeFactory
        {
            protected Type[] Arg0Types;
            protected Type[] Arg1Types;
            protected Operator.Tag Tag;

            public override Node Make (Parser parser)
            {
                Expression arg0 = parser.AssertOneOfNodes<Expression>(Arg0Types);
                Operator tok = parser.AssertOperator(Tag);
                Expression arg1 = parser.AssertOneOfNodes<Expression>(Arg1Types);
                return Create(arg0, tok, arg1);
            }

            protected Factory (Type operation, Operator.Tag Tag)
            {
                this.Tag = Tag;
                this.Arg0Types = PriorityManager.Instance.GetLeftArgs(operation);
                this.Arg1Types = PriorityManager.Instance.GetRightArgs(operation);
            }

            protected abstract BinaryOperation Create (Expression arg0, Operator op, Expression arg1);
        }


        public override XmlNode ToXml (System.Xml.XmlDocument doc)
        {
            XmlNode result = base.ToXml(doc);

            XmlNode arg0Node = doc.CreateElement("Arg0");
            arg0Node.AppendChild(arg0.ToXml(doc));
            XmlNode arg1Node = doc.CreateElement("Arg1");
            arg1Node.AppendChild(arg1.ToXml(doc));

            result.AppendChild(arg0Node);
            result.AppendChild(arg1Node);
            return result;
        }

        public override void AcceptVisitor (IExpressionVisitor visitor)
        {
            visitor.Visit(this);
        }
        public virtual void AcceptVisitor (IBinaryOperationVisitor visitor)
        {
            visitor.Visit(this);
        }
    }

    // CommonExpression 
    public sealed class opAssignment : BinaryOperation
    {
        private opAssignment (Expression arg0, Operator op, Expression arg1)
            : base(arg0, op, arg1)
        {
        }

        public new class Factory : BinaryOperation.Factory
        {
            public Factory ()
                : base(typeof(opAssignment), Operator.Tag.Assignment)
            {
            }

            protected override BinaryOperation Create (Expression arg0, Operator op, Expression arg1)
            {
                return new opAssignment(arg0, op, arg1);
            }
        }

        public override void AcceptVisitor (IExpressionVisitor visitor)
        {
            visitor.Visit(this);
        }
        public override void AcceptVisitor (IBinaryOperationVisitor visitor)
        {
            visitor.Visit(this);
        }

    }



    // LogicalExpression
    public sealed class opOR : BinaryOperation
    {
        private opOR (Expression arg0, Operator op, Expression arg1)
            : base(arg0, op, arg1)
        {
        }

        public new class Factory : BinaryOperation.Factory
        {
            public Factory ()
                : base(typeof(opOR), Operator.Tag.Or)
            {

            }

            protected override BinaryOperation Create (Expression arg0, Operator op, Expression arg1)
            {
                return new opOR(arg0, op, arg1);
            }
        }

        public override void AcceptVisitor (IExpressionVisitor visitor)
        {
            visitor.Visit(this);
        }
        public override void AcceptVisitor (IBinaryOperationVisitor visitor)
        {
            visitor.Visit(this);
        }

    }

    public sealed class opAND : BinaryOperation
    {
        private opAND (Expression arg0, Operator op, Expression arg1)
            : base(arg0, op, arg1)
        {
        }

        public new class Factory : BinaryOperation.Factory
        {
            public Factory ()
                : base(typeof(opAND), Operator.Tag.And)
            {

            }

            protected override BinaryOperation Create (Expression arg0, Operator op, Expression arg1)
            {
                return new opAND(arg0, op, arg1);
            }
        }

        public override void AcceptVisitor (IExpressionVisitor visitor)
        {
            visitor.Visit(this);
        }
        public override void AcceptVisitor (IBinaryOperationVisitor visitor)
        {
            visitor.Visit(this);
        }
    }

    // CompareExpression
    public sealed class opEqual : BinaryOperation
    {
        private opEqual (Expression arg0, Operator op, Expression arg1)
            : base(arg0, op, arg1)
        {
        }

        public new class Factory : BinaryOperation.Factory
        {
            public Factory ()
                : base(typeof(opEqual), Operator.Tag.Equal)
            {

            }

            protected override BinaryOperation Create (Expression arg0, Operator op, Expression arg1)
            {
                return new opEqual(arg0, op, arg1);
            }
        }

        public override void AcceptVisitor (IExpressionVisitor visitor)
        {
            visitor.Visit(this);
        }
        public override void AcceptVisitor (IBinaryOperationVisitor visitor)
        {
            visitor.Visit(this);
        }
    }

    public sealed class opNotEqual : BinaryOperation
    {
        private opNotEqual (Expression arg0, Operator op, Expression arg1)
            : base(arg0, op, arg1)
        {
        }

        public new class Factory : BinaryOperation.Factory
        {
            public Factory ()
                : base(typeof(opNotEqual), Operator.Tag.NotEqual)
            {

            }

            protected override BinaryOperation Create (Expression arg0, Operator op, Expression arg1)
            {
                return new opNotEqual(arg0, op, arg1);
            }
        }

        public override void AcceptVisitor (IExpressionVisitor visitor)
        {
            visitor.Visit(this);
        }
        public override void AcceptVisitor (IBinaryOperationVisitor visitor)
        {
            visitor.Visit(this);
        }
    }


    public sealed class opLess : BinaryOperation
    {
        private opLess (Expression arg0, Operator op, Expression arg1)
            : base(arg0, op, arg1)
        {
        }

        public new class Factory : BinaryOperation.Factory
        {
            public Factory ()
                : base(typeof(opLess), Operator.Tag.Less)
            {

            }

            protected override BinaryOperation Create (Expression arg0, Operator op, Expression arg1)
            {
                return new opLess(arg0, op, arg1);
            }
        }

        public override void AcceptVisitor (IExpressionVisitor visitor)
        {
            visitor.Visit(this);
        }
        public override void AcceptVisitor (IBinaryOperationVisitor visitor)
        {
            visitor.Visit(this);
        }
    }

    public sealed class opGreat : BinaryOperation
    {
        private opGreat (Expression arg0, Operator op, Expression arg1)
            : base(arg0, op, arg1)
        {
        }

        public new class Factory : BinaryOperation.Factory
        {
            public Factory ()
                : base(typeof(opGreat), Operator.Tag.Great)
            {

            }

            protected override BinaryOperation Create (Expression arg0, Operator op, Expression arg1)
            {
                return new opGreat(arg0, op, arg1);
            }
        }

        public override void AcceptVisitor (IExpressionVisitor visitor)
        {
            visitor.Visit(this);
        }
        public override void AcceptVisitor (IBinaryOperationVisitor visitor)
        {
            visitor.Visit(this);
        }
    }

    public sealed class opLessEqual : BinaryOperation
    {
        private opLessEqual (Expression arg0, Operator op, Expression arg1)
            : base(arg0, op, arg1)
        {
        }

        public new class Factory : BinaryOperation.Factory
        {
            public Factory ()
                : base(typeof(opLessEqual), Operator.Tag.LessEqual)
            {

            }

            protected override BinaryOperation Create (Expression arg0, Operator op, Expression arg1)
            {
                return new opLessEqual(arg0, op, arg1);
            }
        }

        public override void AcceptVisitor (IExpressionVisitor visitor)
        {
            visitor.Visit(this);
        }
        public override void AcceptVisitor (IBinaryOperationVisitor visitor)
        {
            visitor.Visit(this);
        }
    }

    public sealed class opGreateEqual : BinaryOperation
    {
        private opGreateEqual (Expression arg0, Operator op, Expression arg1)
            : base(arg0, op, arg1)
        {
        }

        public new class Factory : BinaryOperation.Factory
        {
            public Factory ()
                : base(typeof(opGreateEqual), Operator.Tag.GreatEqual)
            {

            }

            protected override BinaryOperation Create (Expression arg0, Operator op, Expression arg1)
            {
                return new opGreateEqual(arg0, op, arg1);
            }
        }

        public override void AcceptVisitor (IExpressionVisitor visitor)
        {
            visitor.Visit(this);
        }
        public override void AcceptVisitor (IBinaryOperationVisitor visitor)
        {
            visitor.Visit(this);
        }
    }


    // AdditiveExpression
    public sealed class opAdd : BinaryOperation
    {
        private opAdd (Expression arg0, Operator op, Expression arg1)
            : base(arg0, op, arg1)
        {
        }

        public new class Factory : BinaryOperation.Factory
        {
            public Factory ()
                : base(typeof(opAdd), Operator.Tag.Plus)
            {

            }

            protected override BinaryOperation Create (Expression arg0, Operator op, Expression arg1)
            {
                return new opAdd(arg0, op, arg1);
            }
        }

        public override void AcceptVisitor (IExpressionVisitor visitor)
        {
            visitor.Visit(this);
        }
        public override void AcceptVisitor (IBinaryOperationVisitor visitor)
        {
            visitor.Visit(this);
        }
    }

    public sealed class opSub : BinaryOperation
    {
        private opSub (Expression arg0, Operator op, Expression arg1)
            : base(arg0, op, arg1)
        {
        }

        public new class Factory : BinaryOperation.Factory
        {
            public Factory ()
                : base(typeof(opSub), Operator.Tag.Minus)
            {

            }

            protected override BinaryOperation Create (Expression arg0, Operator op, Expression arg1)
            {
                return new opSub(arg0, op, arg1);
            }
        }

        public override void AcceptVisitor (IExpressionVisitor visitor)
        {
            visitor.Visit(this);
        }
        public override void AcceptVisitor (IBinaryOperationVisitor visitor)
        {
            visitor.Visit(this);
        }
    }


    // MultiplicativeExpression
    public sealed class opMul : BinaryOperation
    {
        private opMul (Expression arg0, Operator op, Expression arg1)
            : base(arg0, op, arg1)
        {
        }

        public new class Factory : BinaryOperation.Factory
        {
            public Factory ()
                : base(typeof(opMul), Operator.Tag.Asterisk)
            {

            }

            protected override BinaryOperation Create (Expression arg0, Operator op, Expression arg1)
            {
                return new opMul(arg0, op, arg1);
            }
        }

        public override void AcceptVisitor (IExpressionVisitor visitor)
        {
            visitor.Visit(this);
        }
        public override void AcceptVisitor (IBinaryOperationVisitor visitor)
        {
            visitor.Visit(this);
        }
    }

    public sealed class opDiv : BinaryOperation
    {
        private opDiv (Expression arg0, Operator op, Expression arg1)
            : base(arg0, op, arg1)
        {
        }

        public new class Factory : BinaryOperation.Factory
        {
            public Factory ()
                : base(typeof(opDiv), Operator.Tag.Slash)
            {

            }

            protected override BinaryOperation Create (Expression arg0, Operator op, Expression arg1)
            {
                return new opDiv(arg0, op, arg1);
            }
        }

        public override void AcceptVisitor (IExpressionVisitor visitor)
        {
            visitor.Visit(this);
        }
        public override void AcceptVisitor (IBinaryOperationVisitor visitor)
        {
            visitor.Visit(this);
        }
    }

    public sealed class opMod : BinaryOperation
    {
        private opMod (Expression arg0, Operator op, Expression arg1)
            : base(arg0, op, arg1)
        {
        }

        public new class Factory : BinaryOperation.Factory
        {
            public Factory ()
                : base(typeof(opMod), Operator.Tag.Percent)
            {

            }

            protected override BinaryOperation Create (Expression arg0, Operator op, Expression arg1)
            {
                return new opMod(arg0, op, arg1);
            }
        }

        public override void AcceptVisitor (IExpressionVisitor visitor)
        {
            visitor.Visit(this);
        }
        public override void AcceptVisitor (IBinaryOperationVisitor visitor)
        {
            visitor.Visit(this);
        }
    }
}