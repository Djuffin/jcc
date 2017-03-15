using System;
using System.Collections.Generic;
using System.Text;

namespace jcc
{
    public class TextRegion
    {
        public readonly bool Empty;
        public readonly int Start, End;
        public TextRegion (int start, int end)
        {
            Start = start;
            End = end;
            Empty = false;
        }

        public TextRegion (int place)
        {
            Start = place;
            End = place;
            Empty = false;
        }

        //empty region
        public TextRegion ()
        {
            Empty = true;
        }

        public static TextRegion operator | (TextRegion reg1, TextRegion reg2)
        {
            if (reg1.Empty)
                return reg2;
            if (reg2.Empty)
                return reg1;
            return new TextRegion(Math.Min(reg1.Start, reg2.Start), Math.Max(reg1.End, reg2.End));
        }


        public override string ToString ()
        {
            if (Empty)
                return "[]";
            return string.Format("[{0} - {1}]", Start, End);
        }

        public string ToString (string text)
        {
            if (Empty)
                return "[]";
            int lines = 1;
            int lastNewLine = 0;
            int index;
            for (index = 0; index < Start; index++)
                if (text[index] == '\x0d')
                {
                    lines++;
                    lastNewLine = index + 1;
                }
            string result = string.Format("[({0}, {1}) - ", lines, index - lastNewLine);
            for (; index < End; index++)
                if (text[index] == '\x0d')
                {
                    lines++;
                    lastNewLine = index + 1;
                }
            return result + string.Format("({0}, {1})]", lines, index - lastNewLine);
        }

        public TextRegion Clone ()
        {
            if (Empty)
                return new TextRegion();
            return new TextRegion(Start, End);
        }

    }
}
