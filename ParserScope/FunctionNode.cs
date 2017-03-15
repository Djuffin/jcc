using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using jcc.LexerScope;

namespace jcc.ParserScope
{
    public class FunctionDefinition : Node
    {
        private TypeNode returnType;
        private string name;
        private ArgListDefinition args;
        private Statement body;

        public TypeNode ReturnType
        {
            get
            {
                return returnType;
            }
        }
        public string Name
        {
            get
            {
                return name;
            }
        }
        public ArgListDefinition Args
        {
            get
            {
                return args;
            }
        }
        public Statement Body
        {
            get
            {
                return body;
            }
        }

        private FunctionDefinition (TypeNode returnType, string name, ArgListDefinition args, Statement body, TextRegion region)
        {
            this.returnType = returnType;
            this.name = name;
            this.args = args;
            this.body = body;
            returnType.Parent = args.Parent = body.Parent = this;
            this.region = region;
        }

        public class Factory : NodeFactory
        {
            public override Node Make (Parser parser)
            {
                TypeNode type = parser.AssertNode<TypeNode>();
                IdentifierToken nameToken = parser.AssertToken<IdentifierToken>();
                TextRegion region = type.Region | nameToken.Region;
                region = region | parser.AssertOperator(Operator.Tag.LeftPar).Region;
                ArgListDefinition args = parser.AssertNode<ArgListDefinition>();
                region = region | args.Region;
                region = region | parser.AssertOperator(Operator.Tag.RightPar).Region;
                Statement body = parser.AssertNode<Block>();
                region = region | body.Region;
                return new FunctionDefinition(type, nameToken.Text, args, body, region);
            }
        }

        public override XmlNode ToXml (XmlDocument doc)
        {
            XmlNode result = base.ToXml(doc);

            XmlNode node = doc.CreateElement("ReturnType");
            node.AppendChild(returnType.ToXml(doc));
            result.AppendChild(node);

            XmlAttribute nameAttr = doc.CreateAttribute("Name");
            nameAttr.Value = name;
            result.Attributes.Append(nameAttr);

            result.AppendChild(args.ToXml(doc));

            node = doc.CreateElement("Body");
            node.AppendChild(body.ToXml(doc));
            result.AppendChild(node);

            return result;            
        }

    }

    public class ArgumentDefinition : Node
    {
        private TypeNode type;
        private string name;

        public TypeNode Type
        {
            get
            {
                return type;
            }
        }
        public string Name
        {
            get
            {
                return name;
            }
        }

        private ArgumentDefinition (TypeNode type, IdentifierToken name)
        {
            region = type.Region | name.Region;
            this.type = type;
            this.name = name.Text;
            type.Parent = this;
        }


        public class Factory : NodeFactory
        {
            public override Node Make (Parser parser)
            {
                TypeNode type = parser.AssertNode<TypeNode>();
                IdentifierToken name = parser.AssertToken<IdentifierToken>();
                return new ArgumentDefinition(type, name);
            }
        }

        public override XmlNode ToXml (XmlDocument doc)
        {
            XmlNode result = base.ToXml(doc);
            result.AppendChild(type.ToXml(doc));
            XmlAttribute nameAttr = doc.CreateAttribute("Name");
            nameAttr.Value = name;
            result.Attributes.Append(nameAttr);
            return result;
        }

    }

    public class ArgListDefinition : Node, IEnumerable<ArgumentDefinition>
    {
        List<ArgumentDefinition> list;

        public int Count
        {
            get
            {
                return list.Count;
            }
        }

        public ArgumentDefinition this[int index]
        {
            get
            {
                return list[index];
            }
        }


        private ArgListDefinition (List<ArgumentDefinition> list)
        {
            this.list = list;
            region = new TextRegion();
            foreach (ArgumentDefinition arg in list)
            {
                region = region | arg.Region;
                arg.Parent = this;
            }
        }

        

        public class Factory : NodeFactory
        {
            public override Node Make (Parser parser)
            {
                List<ArgumentDefinition> list = new List<ArgumentDefinition>();
                bool first = true;
                while (true)
                {
                    ArgumentDefinition arg = parser.TryGetNode<ArgumentDefinition>();
                    if (arg == null)
                        if (first)
                            break;
                        else
                            throw new ParseException(typeof(ArgListDefinition), new TextRegion(parser.Position));
                    list.Add(arg);
                    first = false;
                    Operator comma = parser.TryGetOperator(Operator.Tag.Comma);
                    if  (comma == null)  break;
                }
                return new ArgListDefinition(list);
            }
        }
        public override XmlNode ToXml (XmlDocument doc)
        {
            XmlNode result = base.ToXml(doc);
            foreach (ArgumentDefinition arg in list)
                result.AppendChild(arg.ToXml(doc));
            return result;
        }


        #region IEnumerable<ArgumentDefinition> Members

        public IEnumerator<ArgumentDefinition> GetEnumerator ()
        {
            return list.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator ()
        {
            return list.GetEnumerator();
        }

        #endregion
    }

}