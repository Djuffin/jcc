using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace jcc.LexerScope
{
    /// <summary>
    /// Abstract token. It is the piace of a source code, that have place and inner text
    /// </summary>
    public abstract class Token
    {
        protected TextRegion region;

        /// <summary>
        /// Token inner text
        /// </summary>
        public abstract string Text
        {
            get;
        }

        /// <summary>
        /// Region of this token
        /// </summary>
        public virtual TextRegion Region
        {
            get
            {
                return region;
            }
        }
    }


    /// <summary>
    /// Represents the factory that can paarse the specify type of tokens
    /// </summary>
    public abstract class TokenFactory
    {
        /// <summary>
        /// Try makes the token.
        /// </summary>
        /// <param name="Text">The source code</param>
        /// <param name="Position">Position in the source code</param>
        /// <param name="length">Length of the new token. (out)</param>
        /// <returns></returns>
        public abstract Token MakeToken (string Text, int Position, out int length);
    }


    /// <summary>
    /// Represents the command (i.e. means some action)
    /// </summary>
    public abstract class Operator : Token
    {
        public readonly Tag tag;

        protected Operator(Tag t)
        {
            tag = t;
        }

        /// <summary>
        /// All types of operators
        /// </summary>
        public enum Tag
        {
            //keywords
            _char,
            _do,
            _else,
            _object,
            _for,
            _if,
            _return,
            _string,
            _while,
            _void,
            _bool,
            _int,
            _double,
            _break,
            _continue,

            //Delimiters
            Comma,
            //Dot,
            Semicolon,
            Colon,
            Minus,
            Plus,
            Asterisk,
            Slash,
            Percent,
            LeftBracket,
            RightBracket,
            Xor,
            BitwiseAnd,
            BitwiseOr,
            Increment,
            Decrement,
            And,
            Or,
            Pling,
            Equal,
            LessEqual,
            GreatEqual,
            Less,
            Great,
            NotEqual,
            Assignment,
            LeftPar,
            RightPar
        }
    }


    /// <summary>
    ///  Token of identifier
    /// </summary>
    public class IdentifierToken : Token
    {
        string name;
        private IdentifierToken (string Text, TextRegion region)
        {
            this.region = region;
            this.name = Text;
        }

        public override string Text
        {
            get
            {
                return name;
            }
        }

        /// <summary>
        /// Factory (text parser) for IdentifierToken
        /// </summary>
        public class Factory : TokenFactory
        {
            /// <summary>
            /// Try makes the Identifier
            /// </summary>
            public override Token MakeToken (string Text, int Position, out int length)
            {
                length = 0;
                int index = Position;
                char c = Text[index];
                if (Char.IsLetter(c) || c == '_')
                {
                    try
                    {
                        c = Text[++index];
                        while (Char.IsLetterOrDigit(c) || c == '_' || c == '.')
                            c = Text[++index];
                    }
                    catch (IndexOutOfRangeException)
                    {
                    }
                    length = index - Position;
                    return new IdentifierToken(Text.Substring(Position, length), new TextRegion(Position, index - 1));
                }
                else
                    return null;
            }
        }
    }

    public class InvalidToken : Token
    {
        string text;
        private InvalidToken (string Text, TextRegion region)
        {
            this.region = region;
            this.text = Text;
        }

        public override string Text
        {
            get
            {
                return text;
            }
        }

        public class Factory : TokenFactory
        {
            public override Token MakeToken (string Text, int Position, out int length)
            {
                string text = Text[Position].ToString();
                length = 1;
                return new InvalidToken(text, new TextRegion(Position, Position + 1));
            }
        }
    }

    public abstract class ConstToken : Token
    {

    }

    public class IntConstToken : ConstToken
    {
        int constValue;
        private IntConstToken (string Text, TextRegion region)
        {
            this.region = region;
            constValue = int.Parse(Text);
        }

        public int Value
        {
            get
            {
                return constValue;
            }
        }

        public override string Text
        {
            get
            {
                return constValue.ToString();
            }
        }

        public class Factory : TokenFactory
        {
            public override Token MakeToken (string Text, int Position, out int length)
            {
                length = 0;
                int index = Position;
                char c = Text[index];
                if (Char.IsDigit(c))
                {
                    try
                    {
                        c = Text[++index];
                        while (Char.IsDigit(c))
                            c = Text[++index];
                    }
                    catch (IndexOutOfRangeException)
                    {
                    }
                    length = index - Position;
                    return new IntConstToken(Text.Substring(Position, length), new TextRegion(Position, index - 1));
                }
                else
                    return null;
            }
        }
    }

    public class DoubleConstToken : ConstToken
    {
        double constValue;
        private DoubleConstToken (string Text, TextRegion region)
        {
            this.region = region;
            constValue = double.Parse(Text);
        }

        public double Value
        {
            get
            {
                return constValue;
            }
        }

        public override string Text
        {
            get
            {
                return constValue.ToString();
            }
        }

        public class Factory :TokenFactory
        {
            public override Token MakeToken (string Text, int Position, out int length)
            {
                length = 0;
                int index = Position;
                char c = Text[index];
                if (Char.IsDigit(c))
                {
                    try
                    {
                        c = Text[++index];
                        while (Char.IsDigit(c))
                            c = Text[++index];
                        if (c == '.')
                        {
                            c = Text[++index];
                            while (Char.IsDigit(c))
                                c = Text[++index];
                        }
                        else
                            return null;
                    }
                    catch (IndexOutOfRangeException)
                    {
                    }
                    length = index - Position;
                    return new DoubleConstToken(Text.Substring(Position, length), new TextRegion(Position, index - 1));
                }
                else
                    return null;
            }
        }
    }

    public class StringConstToken : ConstToken
    {
        string constValue;
        private StringConstToken (string Text, TextRegion region)
        {
            this.region = region;
            constValue = Text;
        }

        public string Value
        {
            get
            {
                return constValue;
            }
        }

        public override string Text
        {
            get
            {
                return constValue.ToString();
            }
        }
        public class Factory : TokenFactory
        {
            public override Token MakeToken (string Text, int Position, out int length)
            {
                length = 0;
                int index = Position;
                char c = Text[index];
                if (c == '\'')
                {
                    try
                    {
                        c = Text[++index];
                        while (c != '\'')
                            c = Text[++index];
                    }
                    catch (IndexOutOfRangeException)
                    {
                    }
                    index++;
                    length = index - Position;
                    return new StringConstToken(Text.Substring(Position + 1, length - 2), new TextRegion(Position, index - 1));
                }
                return null;
            }
        }
    }

    public class Space : Token
    {
        public readonly bool IsComment;
        private string text;

        public override string Text
        {
            get
            {
                return text;
            }
        }

        public bool HasNewLine
        {
            get
            {
                return text.IndexOf("\n") != -1;
            }
        }


        private Space (string text, TextRegion region, bool IsComment)
        {
            this.region = region;
            this.text = text;
            this.IsComment = IsComment;
        }

        public class Factory : TokenFactory
        {
            public override Token MakeToken (string Text, int Position, out int length)
            {
                length = 0;
                int index = Position;
                char c = Text[index];
                if (c == ' ' || c == '\t' || c == '\n' || c == '\r')
                {
                    try
                    {
                        while (c == ' ' || c == '\t' || c == '\n' || c == '\r')
                            c = Text[++index];
                    }
                    catch (IndexOutOfRangeException)
                    {
                    }
                    length = index - Position;
                    return new Space(Text.Substring(Position, length), new TextRegion(Position, index - 1), false);
                }
                else if (c == '/')
                {
                    c = Text[++index];
                    if (c == '/')
                    {
                        try
                        {
                            c = Text[++index];
                            while (c != '\n')
                                c = Text[++index];
                        }
                        catch (IndexOutOfRangeException)
                        {
                        }
                        length = index - Position;
                        return new Space(Text.Substring(Position, length), new TextRegion(Position, index - 1), true);
                    }

                }
                return null;
            }
        }
    }

    public class Delimiter : Operator
    {

        private string text;
        private static Dictionary<string, Tag> map;

        static Delimiter ()
        {
            map = new Dictionary<string, Tag>();
            map["=="] = Tag.Equal;
            map["!="] = Tag.NotEqual;
            map["<="] = Tag.LessEqual;
            map[">="] = Tag.GreatEqual;
            map["&&"] = Tag.And;
            map["||"] = Tag.Or;
            map["++"] = Tag.Increment;
            map["--"] = Tag.Decrement;
            map["<"] = Tag.Less;
            map[">"] = Tag.Great;
            map[","] = Tag.Comma;
           // map["."] = Tag.Dot;
            map[";"] = Tag.Semicolon;
            map[":"] = Tag.Colon;
            map["-"] = Tag.Minus;
            map["+"] = Tag.Plus;
            map["*"] = Tag.Asterisk;
            map["%"] = Tag.Percent;
            map["/"] = Tag.Slash;
            map["{"] = Tag.LeftBracket;
            map["}"] = Tag.RightBracket;
            map["^"] = Tag.Xor;
            map["&"] = Tag.BitwiseAnd;
            map["|"] = Tag.BitwiseOr;
            map["!"] = Tag.Pling;
            map["="] = Tag.Assignment;
            map["("] = Tag.LeftPar;
            map[")"] = Tag.RightPar;
        }

        private Delimiter (string text, TextRegion region, Tag tag)
            : base(tag)
        {
            this.region = region;
            this.text = text;
        }

        public override string Text
        {
            get
            {
                return text;
            }
        }

        public class Factory : TokenFactory
        {
            public override Token MakeToken (string Text, int Position, out int length)
            {
                length = 0;
                for (int n = 2; n > 0; n--)
                {
                    try
                    {
                        string txt = Text.Substring(Position, n);
                        if (map.ContainsKey(txt))
                        {
                            Delimiter.Tag tag = map[txt];
                            length = n;
                            return new Delimiter(txt, new TextRegion(Position, Position + n - 1), tag);
                        }
                    }
                    catch
                    {
                    }
                }
                return null;
            }
        }

    }

    public class Keyword : Operator
    {

        string name;

        private Keyword (string Text, TextRegion region)
            : base( (Tag)Enum.Parse(typeof(Tag), '_' + Text, false) )
        {
            this.region = region;
            this.name = Text;
        }

        public override string Text
        {
            get
            {
                return name;
            }
        }

        public class Factory : TokenFactory
        {
            public override Token MakeToken (string Text, int Position, out int length)
            {
                length = 0;
                int index = Position;
                char c = Text[index];
                if (Char.IsLetter(c) || c == '_')
                {
                    try
                    {
                        c = Text[++index];
                        while (Char.IsLetterOrDigit(c) || c == '_')
                            c = Text[++index];
                    }
                    catch (IndexOutOfRangeException)
                    {
                    }
                    length = index - Position;
                    string substr = Text.Substring(Position, length);
                    foreach (string s in Enum.GetNames(typeof(Tag)))
                        if (s == "_" + substr)
                            return new Keyword(substr, new TextRegion(Position, index - 1));
                }
                return null;
            }
        }
    }
}

