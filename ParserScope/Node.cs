using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using jcc.LexerScope;

namespace jcc.ParserScope
{
    /// <summary>
    /// Factory for create (parse) node with
    /// </summary>
    public abstract class NodeFactory
    {
        /// <summary>
        /// Makes the node from specified parser.
        /// </summary>
        /// <param name="parser">The parser.</param>
        /// <returns>The Node</returns>
        /// <exception cref="T:jcc.ParserScope.ParseException">No node supported in this factory exist at current position position of parser</exception>        
        public abstract Node Make (Parser parser);

        /// <summary>
        /// Determines whether this factory node [can starts with] the specified token.
        /// </summary>
        /// <param name="token">The token.</param>
        /// <returns>
        /// 	<c>true</c> if this factory node [can starts with] the specified token; otherwise, <c>false</c>.
        /// </returns>
        public virtual bool CanStartsWith (Token token)
        {
            return true;
        }
    }

    /// <summary>
    /// Represents a syntactical node in the parse tree 
    /// </summary>
    public abstract class Node
    {
        protected Node parent;
        protected TextRegion region;

        /// <summary>
        /// Gets or sets the parent node.
        /// </summary>
        public Node Parent
        {
            get
            {
                return parent;
            }
            set
            {
                parent = value;
            }
        }
        /// <summary>
        /// Gets merge the region of this node and all its child nodes
        /// </summary>
        public TextRegion Region
        {
            get 
            {
                return region;
            }
        }


        public override string ToString ()
        {
            return GetType().Name;
        }

        /// <summary>
        /// Toes the XML representation of node with the all its chils.
        /// </summary>
        /// <param name="doc">The parent XML document </param>
        /// <returns>The XML Node.</returns>
        public virtual XmlNode ToXml (XmlDocument doc)
        {
            XmlElement result = doc.CreateElement(ToString());
            XmlAttribute attr = doc.CreateAttribute("region");
            attr.Value = region.ToString();
            result.Attributes.Append(attr);
            return result;
        }

    }

 


}
                                                            