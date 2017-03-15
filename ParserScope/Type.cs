using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using jcc.LexerScope;

namespace jcc.ParserScope
{
    public class TypeNode : Node
    {
        string name;
        public string Name
        {
            get
            {
                return name;
            }
        }


        private TypeNode (string name, TextRegion region)
        {
            this.region = region;
            this.name = name;
        }

        public class Factory : NodeFactory
        {

            public override Node Make (Parser parser)
            {
                Keyword keyword = parser.AssertToken<Keyword>();
                Operator.Tag tag = keyword.tag;
                string name;
                switch (tag)
                {
                    case Operator.Tag._int:
                        name = "int";
                        break;
                    case Operator.Tag._string:
                        name = "string";
                        break;
                    case Operator.Tag._double:
                        name = "double";
                        break;
                    case Operator.Tag._void:
                        name = "void";
                        break;
                    case Operator.Tag._object:
                        name = "object";
                        break;
                    default:
                        throw new ParseException(typeof(TypeNode), keyword.Region);
                }
                        
                return new TypeNode(name, keyword.Region);
            }

            public override bool CanStartsWith (Token token)
            {
                return token is Keyword;
            }
        }

        public override XmlNode ToXml (XmlDocument doc)
        {
            XmlNode result = base.ToXml(doc);
            XmlAttribute attr = doc.CreateAttribute("TypeName");
            attr.Value = Name;
            result.Attributes.Append(attr);
            return result;
        }

    }
}