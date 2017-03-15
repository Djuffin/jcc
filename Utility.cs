using System;
using System.Collections.Generic;
using System.Text;

namespace jcc
{
    public class Pair<First, Second>
    {
        private First first;
        private Second second;

        public Pair (First first, Second second)
        {
            this.first = first;
            this.second = second;
        }

        public override bool Equals (object obj)
        {
            Pair<First, Second> p = obj as Pair<First, Second>;
            if (p == null)
                return false;
            return p.first.Equals(first) && p.second.Equals(second);
        }

        public override int GetHashCode ()
        {
            return first.GetHashCode() ^ first.GetHashCode();
        }
    }
   
}
