using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using jcc;

namespace jcc.ParserScope
{
    /// <summary>
    /// Largest node. Root of the parse tree. This is complete program on the our language.
    /// </summary>
    public class ProgramNode : Node
    {
        List<Declare> declares;
        List<FunctionDefinition> functions;

        /// <summary>
        /// Gets the functions count.
        /// </summary>
        /// <value>The functions count.</value>
        public int FunctionsCount
        {
            get
            {
                return functions.Count;
            }
        }
        /// <summary>
        /// Gets the function.
        /// </summary>
        /// <param name="index">The index of wanted function.</param>
        /// <returns>Instance of a FunctionDefinition</returns>
        public FunctionDefinition GetFunction (int index)
        {
            return functions[index];
        }

        /// <summary>
        /// Gets the declares count.
        /// </summary>
        /// <value>The declares count.</value>
        public int DeclaresCount
        {
            get
            {
                return declares.Count;
            }
        }

        /// <summary>
        /// Gets the declare.
        /// </summary>
        /// <param name="index">The index of wanted declare.</param>
        /// <returns>Instance of a Declare</returns>
        public Declare GetDeclare (int index)
        {
            return declares[index];
        }

        private ProgramNode (List<Declare> declares, List<FunctionDefinition> functions)
        {
            region = new TextRegion();
            this.declares = declares;
            this.functions = functions;
            foreach (FunctionDefinition function in functions)
            {
                region = region | function.Region;
                function.Parent = this;
            }
            foreach (Declare declare in declares)
            {
                region = region | declare.Region;
                declare.Parent = this;
            }
        }

        /// <summary>
        /// Factory for automatic creating instance of this node 
        /// </summary>
        public class Factory : NodeFactory
        {
            /// <summary>
            /// Makes the node from specified parser.
            /// </summary>
            /// <param name="parser">The parser.</param>
            /// <returns>The Node</returns>
            /// <exception cref="T:jcc.ParserScope.ParseException">No node supported in this factory exist at current position position of parser</exception>
            public override Node Make (Parser parser)
            {
                Type[] members = new Type[] { typeof(Declare), typeof(FunctionDefinition) };
                List<Declare> declares = new List<Declare>();
                List<FunctionDefinition> functions = new List<FunctionDefinition>();
                while (!parser.AtEnd)
                {
                    Node node = parser.AssertOneOfNodes<Node>(members);
                    if (node is Declare)
                    {
                        declares.Add((Declare)node);
                        continue;
                    }

                    if (node is FunctionDefinition)
                    {
                        functions.Add((FunctionDefinition)node);
                        continue;
                    }
                    throw new ParseException(typeof(ProgramNode), new TextRegion());
                }
                return new ProgramNode(declares, functions);
            }
        }

        /// <summary>
        /// Toes the XML representation of node with the all its chils.
        /// </summary>
        /// <param name="doc">The parent XML document</param>
        /// <returns>The XML Node.</returns>
        public override XmlNode ToXml (XmlDocument doc)
        {
            XmlNode root = base.ToXml(doc);
            foreach (Declare declare in declares)
                root.AppendChild(declare.ToXml(doc));
            foreach (FunctionDefinition function in functions)
                root.AppendChild(function.ToXml(doc));
            return root;
        }

    }
}
