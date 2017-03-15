using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection.Emit;
using System.Reflection;
using System.Threading;

namespace jcc.CodeGenScope
{
    /// <summary>
    /// MS IL specified version of <see cref="jcc.CodeGenScope.CodeObject"/> 
    /// </summary>
    public abstract class ILCodeObject : CodeObject
    {
        protected ILCodeGenerator generator;
        protected ILCodeObject ()
        {
        }
        protected ILCodeObject (TypeEntity type, ILCodeGenerator generator)
            : base(type)
        {
            this.generator = generator;
        }

        /// <summary>
        /// It's the fast reference to the IL Generator.
        /// </summary>
        protected ILGenerator IL
        {
            get
            {
                return generator.CurrentIL;
            }
        }

        public override void GenerateStore (CodeObject value)
        {
            value.GenerateCode();
            ILTypeTranslator.GenerateImplicitCast(IL, value.Type, Type);
            GenerateStore();
        }

        public abstract void GenerateStore ();

    }

}
