using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using jcc.LexerScope;

namespace jcc.ParserScope
{
    public abstract class Const : Expression
    {

        protected Const (TextRegion region)
        {
            this.region = region;
        }

        public abstract string Text
        {
            get;
        }

        public new class Factory : NodeFactory
        {
            Type[] constTypes = new Type[] { typeof(IntConst), typeof(DoubleConst), typeof(StringConst) };

            public override Node Make (Parser parser)
            {
                return parser.AssertOneOfNodes<Const>(constTypes);
            }
        }

        public override XmlNode ToXml (XmlDocument doc)
        {
            XmlNode result = base.ToXml(doc);
            XmlAttribute attr = doc.CreateAttribute("Value");
            attr.Value = Text;
            result.Attributes.Append(attr);
            return result;
        }

        public override void AcceptVisitor (IExpressionVisitor visitor)
        {
            visitor.Visit(this);
        }

        public virtual void AcceptVisitor (IConstVisitor visitor)
        {
            visitor.Visit(this);
        }
    }

    public class DoubleConst : Const
    {
        private double value;

        public double Value
        {
            get
            {
                return value;
            }
        }

        public override string Text
        {
            get
            {
                return value.ToString();
            }
        }

        private DoubleConst (DoubleConstToken token)
            : base(token.Region)
        {
            value = token.Value;
        }

        public new class Factory : Const.Factory
        {
            public override Node Make (Parser parser)
            {
                DoubleConstToken token = parser.AssertToken<DoubleConstToken>();
                return new DoubleConst(token);
            }
        }
        public override void AcceptVisitor (IConstVisitor visitor)
        {
            visitor.Visit(this);
        }
    }

    public class IntConst : Const
    {
        private int value;

        public int Value
        {
            get
            {
                return value;
            }
        }

        public override string Text
        {
            get
            {
                return value.ToString();
            }
        }

        private IntConst (IntConstToken token)
            : base(token.Region)
        {
            value = token.Value;
        }

        public new class Factory : Const.Factory
        {
            public override Node Make (Parser parser)
            {
                IntConstToken token = parser.AssertToken<IntConstToken>();
                return new IntConst(token);
            }
        }
        public override void AcceptVisitor (IConstVisitor visitor)
        {
            visitor.Visit(this);
        }
    }

    public class StringConst : Const
    {
        private string value;

        public string Value
        {
            get
            {
                return value;
            }
        }

        public override string Text
        {
            get
            {
                return value;
            }
        }

        private StringConst (StringConstToken token)
            : base(token.Region)
        {
            value = token.Value;
        }

        public new class Factory : Const.Factory
        {
            public override Node Make (Parser parser)
            {
                StringConstToken token = parser.AssertToken<StringConstToken>();
                return new StringConst(token);
            }
        }
        public override void AcceptVisitor (IConstVisitor visitor)
        {
            visitor.Visit(this);
        }
    }
}