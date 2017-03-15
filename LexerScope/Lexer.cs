using System;
using System.Collections.Generic;
using System.Text;

namespace jcc.LexerScope
{
    /// <summary>
    /// Represents a tokens harvester, which parse a sourse code to the tokens' collection.
    /// </summary>
    public class Lexer
    {
        private readonly string text;
        private int position;
        private static TokenFactory[] factories;

        static Lexer ()
        {
            factories = new TokenFactory[] 
            {
                new Keyword.Factory(),
                new IdentifierToken.Factory(),

                new DoubleConstToken.Factory(),
                new IntConstToken.Factory(),
                new StringConstToken.Factory(), 

                new Space.Factory(),
                new Delimiter.Factory(),
                new InvalidToken.Factory()
            };
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:jcc.LexerScope.Lexer"/> class.
        /// </summary>
        /// <param name="reader">TextReader - source of program's text.</param>
        public Lexer (System.IO.TextReader reader)
        {
						if (reader == null)
                throw new ArgumentNullException("Text");
            text = reader.ReadToEnd();
            position = 0;
        }


        /// <summary>
        /// Initializes a new instance of the <see cref="T:jcc.LexerScope.Lexer"/> class.
        /// </summary>
        /// <param name="Text">Source of program's text</param>
        public Lexer (string Text)
        {
            if (Text == null)
                throw new ArgumentNullException("Text");
            text = Text;
            position = 0;
        }


        /// <summary>
        /// Gets the tokens.
        /// </summary>
        /// <returns>Array of all tokens.</returns>
        public Token[] GetTokens ()
        {
            List<Token> tokens = new List<Token>();
            position = 0;
            while (position < text.Length)
            {
                Token token = GetToken();
                if (!(token is Space))
                    tokens.Add(token);
            }
            return tokens.ToArray();
        }

        /// <summary>
        /// Gets the token.
        /// </summary>
        /// <returns>Next token</returns>
        private Token GetToken ()
        {
            int delta = 0;
            Token result = null;
            foreach (TokenFactory factory in factories)
            {
                result = factory.MakeToken(text, position, out delta);
                if (result != null)
                    break;
            }
            if (result == null)
                throw new InvalidOperationException("Can't find factory fot this token");
            position += delta;
            return result;
        }

    }
}
