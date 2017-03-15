using System;
using System.Collections.Generic;
using System.Text;
using ParseCachePair = jcc.Pair<int, System.Type>;
using jcc.LexerScope;

namespace jcc.ParserScope
{

    /// <summary>
    /// The exception that is thrown when <see cref="T:jcc.ParserScope.Parser"/> reject some type of node in some place. 
    /// </summary>
    public class ParseException : Exception
    {
        public Type[] EntityTypes;
        public TextRegion region;
        public Operator.Tag Tag;

        public ParseException (Type[] EntityTypes, TextRegion region)
        {
            this.EntityTypes = EntityTypes;
            this.region = region;
        }

        public ParseException (Type EntityType, TextRegion region)
        {
            this.EntityTypes = new Type[] { EntityType };
            this.region = region;
        }

        public ParseException (Operator.Tag Tag, TextRegion region)
        {
            this.EntityTypes = new Type[] { typeof(Operator) };
            this.Tag = Tag;
            this.region = region;
        }

        public ParseException (Type EntityType, ParseException Inner)
        {
            this.EntityTypes = new Type[] { EntityType };
            this.region = Inner.region;
        }

        public override string Message
        {
            get
            {
                if (EntityTypes.Length != 1)
                {
                    return string.Format("Types array, region: {0}", region);
                }
                else if (EntityTypes[0] == typeof(Operator))
                {
                    return string.Format("Operator {1}, region: {0}", region, Tag);
                }
                return string.Format("Type {1}, region: {0}", region, EntityTypes[0]);
            }
        }


    }

    /// <summary>
    /// This is collection of local parse result (i.e. successes and fails).
    /// It was created for increase performance of the parse process    
    /// </summary>
    /// <remarks>
    /// This class should be used only in <see cref="T:jcc.ParserScope.Parser"/> 
    /// </remarks>
    internal class ParseCache
    {

        Dictionary<ParseCachePair, Node> map = new Dictionary<ParseCachePair, Node>();

        /// <summary>
        /// Puts the parse success to the cache.
        /// </summary>
        /// <param name="position">The position of node.</param>
        /// <param name="node">The successful node.</param>
        public void PutSuccess (int position, Node node)
        {
            map[new ParseCachePair(position, node.GetType())] = node;
        }

        /// <summary>
        /// Puts the parse  to the cache.
        /// </summary>
        /// <param name="position">The position of node.</param>
        /// <param name="type">The type of unsuccessful node.</param>
        public void PutFail (int position, Type type)
        {
            map[new ParseCachePair(position, type)] = null;
        }



        /// <summary>
        /// Gets the node with specified position, type.
        /// </summary>
        /// <param name="position">The position of the necessary node.</param>
        /// <param name="type">The type of the necessary node.</param>
        /// <param name="exist">if set to <c>true</c> this ndoe exist in cahce. (out)</param>
        /// <returns></returns>
        public Node Get (int position, Type type, out bool exist)
        {
            Node result;
            exist = map.TryGetValue(new ParseCachePair(position, type), out result);
            return result;
        }

        /// <summary>
        /// Clears this cache.
        /// </summary>
        public void Clear ()
        {
            map.Clear();
        }
    }

    /// <summary>
    /// Parser! Parse the sequence of tokens to the parse tree.
    /// </summary>
    /// <remarks>
    /// The base principle of this parser is "Only specific node know how its should be created".
    /// Eache node provide the chil of NodeFactory, only this factory can create instance of this node.
    /// It looks like we implemaet top to bottom lazy parse algorithm, but in real we can change this algorithm 
    /// to the any other in any level of nodes. (i.e. we can implement bottom to low parse only for expressions)
    /// 
    /// General task of this class is the supply of node factoryes by services
    /// </remarks>
    public class Parser
    {
        private Token[] tokens;
        private int position;
        private ParseCache cache;
        private Dictionary<Type, NodeFactory> FactoryMap = new Dictionary<Type, NodeFactory>();
        private Type cellNodeType;  // some atomic node (i.e. statement or function)
        private ErrorsCollection errors = new ErrorsCollection();


        /// <summary>
        /// Initializes a new instance of the <see cref="T:jcc.ParserScope.Parser"/> class.
        /// </summary>
        /// <param name="tokens">The tokens for parse.</param>
        public Parser (Token[] tokens)
        {
            this.tokens = tokens;
            position = 0;
            cache = new ParseCache();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:jcc.ParserScope.Parser"/> class.
        /// </summary>
        /// <param name="tokens">The tokens for parse</param>
        /// <param name="cellNodeType">Type of cell's node. (i.e. statement or function)</param>
        public Parser (Token[] tokens, Type cellNodeType)
            :this(tokens)
        {
            this.cellNodeType = cellNodeType;
        }

        /// <summary>
        /// Gets the next token from the sequence
        /// </summary>
        /// <returns>Next node</returns>
        private Token GetToken ()
        {
            if (position == tokens.Length)
                return null;
            return tokens[position++];
        }
        /// <summary>
        /// Returns the token.
        /// </summary>
        private void UngetToken ()
        {
            System.Diagnostics.Debug.Assert(position != 0, "Can't unget token, no token was geting");
            position--;
        }
        /// <summary>
        /// Skips tokens to the end of specified region.
        /// </summary>
        /// <param name="region">The region.</param>
        private void Skip (TextRegion region)
        {
            while (position < tokens.Length && tokens[position].Region.End <= region.End)
                position++;
        }
        /// <summary>
        /// Skips all tokens till new line.
        /// </summary>
        private void SkipToNewLine()
        {
            while (position != tokens.Length)
            {
                Space s = GetToken() as Space;
                if (s != null && s.HasNewLine)
                    return;
            }
        }
        /// <summary>
        /// Puts the bookmark.
        /// </summary>
        /// <returns>The bookmark</returns>
        private int PutBookmark ()
        {
            return position;
        }
        /// <summary>
        /// Restores the bookmark.
        /// </summary>
        /// <param name="bookmark">The bookmark.</param>
        private void RestoreBookmark (int bookmark)
        {
            position = bookmark;
        }
        /// <summary>
        /// Sees the next token, without geting it from sequence
        /// </summary>
        /// <returns>The next tooken</returns>
        private Token SeeToken ()
        {
            if (position == tokens.Length)
                return null;
            return tokens[position];
        }

        /// <summary>
        /// Gets all obtained errors.
        /// </summary>
        /// <value>The errors.</value>
        public ErrorsCollection Errors
        {
            get
            {
                return errors;
            }
        }

        /// <summary>
        /// Gets a value indicating whether [at end].
        /// </summary>
        /// <value><c>true</c> if [at end]; otherwise, <c>false</c>.</value>
        public bool AtEnd
        {
            get
            {
                return position == tokens.Length;
            }
        }

        /// <summary>
        /// Gets the current position in tokens' sequence.
        /// </summary>
        /// <value>The position.</value>
        public int Position
        {
            get
            {
                return position;
            }
        }

        /// <summary>
        /// Gets the factory for this type of node
        /// </summary>
        /// <param name="NodeType">Type of the node.</param>
        /// <returns>Factory</returns>
        private NodeFactory GetFactory (Type NodeType)
        {
            NodeFactory factory;
            if (!FactoryMap.TryGetValue(NodeType, out factory))
            {
                Type factoryType = NodeType.GetNestedType("Factory");
                factory = Activator.CreateInstance(factoryType) as NodeFactory;
                if (factory == null)
                    throw new Exception("Can't create factory for this type");
                FactoryMap[NodeType] = factory;
            }
            return factory;
        }

        /// <summary>
        /// Asserts the token.
        /// </summary>
        /// <typeparam name="TokenType">Type of token, that we want to obtain</typeparam>
        /// <returns>The token</returns>
        public TokenType AssertToken<TokenType> () where TokenType : Token
        {
            TokenType token = GetToken() as TokenType;
            if (token == null)
            {
                UngetToken();
                throw new ParseException(typeof(TokenType), new TextRegion(position)); //no thrown
            }
            return token;
        }

        /// <summary>
        /// Asserts the operator with the specific tag
        /// </summary>
        /// <param name="TokenTag">The token tag.</param>
        /// <returns>The operator</returns>
        /// <exception cref="T:jcc.ParserScope.ParseException">No token with this tag exist at this position</exception>
        public Operator AssertOperator (Operator.Tag TokenTag)
        {
            Operator token = GetToken() as Operator;
            if (token != null && token.tag == TokenTag)
                return token;
            UngetToken();
            throw new ParseException(TokenTag, new TextRegion(position));
        }


        /// <summary>
        /// Tries the get operator. 
        /// </summary>
        /// <remarks>If no token with this tag exist in this position then return null.</remarks>
        /// <param name="TokenTag">The token tag.</param>
        /// <returns>The operator</returns>
        public Operator TryGetOperator (Operator.Tag TokenTag)
        {
            try
            {
                return AssertOperator(TokenTag);
            }
            catch (ParseException)
            {
                return null;
            }
        }

        /// <summary>
        /// Asserts the node of the specific type.
        /// </summary>
        /// <typeparam name="NodeType">Type of node, that we want obtain</typeparam>
        /// <returns>The node</returns>
        /// <exception cref="T:jcc.ParserScope.ParseException">No node of this type exist at this position</exception>
        public NodeType AssertNode<NodeType> () where NodeType : Node
        {
            return (NodeType)AssertNode(typeof(NodeType));
        }

        /// <summary>
        /// Tries the get node. 
        /// </summary>
        /// <remarks>If no node of this type exist in this position then return null.</remarks>
        /// <param name="NodeType">The node type.</param>
        /// <returns>The node</returns>
        public NodeType TryGetNode<NodeType> () where NodeType : Node
        {
            try
            {
                return AssertNode<NodeType>();
            }
            catch (ParseException)
            {
                return null;
            }
        }


        /// <summary>
        /// Asserts the node of the specific type.
        /// </summary>
        /// <param name="NodeType">Type of the node.</param>
        /// <returns>The node</returns>
        /// <exception cref="T:jcc.ParserScope.ParseException">No node of this type exist at this position</exception>
        public Node AssertNode (Type NodeType)
        {
            Node result;
            bool exist;

            //check in cache
            result = cache.Get(position, NodeType, out exist);
            if (exist)
            {
                if (result == null)
                    throw new ParseException(NodeType, new TextRegion(position));   //no thrown
                Skip(result.Region);
                return result;
            }

            int bookmark = PutBookmark();
            try
            {
                result = GetFactory(NodeType).Make(this);
            }
            catch (ParseException e)
            {
                RestoreBookmark(bookmark);
                cache.PutFail(position, NodeType);
                throw new ParseException(NodeType, e);
            }
            if (result == null)
            {
                RestoreBookmark(bookmark);
                cache.PutFail(position, NodeType);
                throw new ParseException(NodeType, new TextRegion(position)); //no thrown
            }
            if (NodeType == cellNodeType)
                cache.Clear();
            else
                cache.PutSuccess(bookmark, result);
            return result;
        }

        /// <summary>
        /// Asserts the one nodes of the enumerated type.
        /// </summary>
        /// <param name="types">The types.</param>
        /// <returns>The node</returns>
        /// <exception cref="T:jcc.ParserScope.ParseException">No node of those types exist at this position</exception>
        public BaseNodeType AssertOneOfNodes<BaseNodeType> (Type[] types) where BaseNodeType : Node
        {
            Type NodeType = typeof(BaseNodeType);
            Token topToken = SeeToken();
            foreach (Type type in types)
            {
                //check in cache
                bool exist;
                Node cacheResult = cache.Get(position, type, out exist);
                if (exist && cacheResult == null)
                    continue;

                //check first token
                NodeFactory factory = GetFactory(type);
                if (!factory.CanStartsWith(topToken))
                    continue;

                //try get node
                try
                {
                    return (BaseNodeType)AssertNode(type);
                }
                catch (ParseException)
                {
                }
            }
            //if (typeof(BaseNodeType) == finalNode)
            //{

            //    errors.Add(new Error(new ParseException(types, new TextRegion(position))));
            //    SkipToNewLine();
            //    if (position == tokens.Length)
            //        throw new ParseException(types, new TextRegion(position));
            //    return AssertOneOfNodes<BaseNodeType>(types);
            //}
            //else
                throw new ParseException(types, new TextRegion(position));
        }

    }
}
