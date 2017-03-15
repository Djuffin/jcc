using System;
using System.Collections.Generic;
using System.Text;
using jcc.ParserScope;
using jcc.CodeGenScope;

namespace jcc
{
    public class Error
    {
        public readonly string Message;
        public readonly TextRegion Region;

        public Error (string Message, TextRegion Region)
        {
            this.Message = Message;
            this.Region = Region;
        }

        public Error (AnalizeException e)
        {
            this.Message = "Analizer have said:  " + e.Message;
            if (e.Node != null)
                this.Region = e.Node.Region;
        }

        public Error (ParseException e)
        {
            this.Message = "Parser have said:  " + e.Message;
            this.Region = e.region;
        }

        public override string ToString ()
        {
            return Message + " " + Region.ToString();
        }
    }


    public class ErrorsCollection : IEnumerable<Error>
    {
        private List<Error> errors = new List<Error>();

        public ErrorsCollection ()
        {
        }

        public void Add(Error error)
        {
            errors.Add(error);
        }

        public int Count
        {
            get
            {
                return errors.Count;
            }
        }

        public Error this[int index]
        {
            get
            {
                return errors[index];
            }
        }

        public static ErrorsCollection operator + (ErrorsCollection col1, ErrorsCollection col2)
        {
            ErrorsCollection result = new ErrorsCollection();
            result.errors.AddRange(col1);
            result.errors.AddRange(col2);
            return result;
        }

        #region IEnumerable<Error> Members

        public IEnumerator<Error> GetEnumerator ()
        {
            return errors.GetEnumerator();
        }

        #endregion

        #region IEnumerable Members

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator ()
        {
            return errors.GetEnumerator();
        }

        #endregion
    }
}
