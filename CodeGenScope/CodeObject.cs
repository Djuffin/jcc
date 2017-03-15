using System;
using System.Collections.Generic;
using System.Text;
using jcc.ParserScope;

namespace jcc.CodeGenScope
{
    /// <summary>
    /// Represents some typed code object that can generate value (i.e. calculate self value) 
    /// and store other value
    /// </summary>
    public abstract class CodeObject
    {
        protected TypeEntity type;

        public virtual TypeEntity Type
        {
            get
            {
                return type;
            }
        }

        /// <summary>
        /// Generates the code for calculating self value
        /// </summary>
        public abstract void GenerateCode ();

        /// <summary>
        /// Generates the code for calculate the value of parameter and store it in this object
        /// </summary>
        /// <param name="value">The value for store.</param>
        public abstract void GenerateStore (CodeObject value);

        protected CodeObject (TypeEntity type)
        {
            this.type = type;
        }

        protected CodeObject ()
        {
        }

    }

}
