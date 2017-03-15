using System;
using System.Collections.Generic;
using System.Text;
using jcc.ParserScope;

namespace jcc.CodeGenScope
{
    /// <summary>
    /// Represents a scope of variable naming.
    /// Stores the collection variables for this scope.
    /// Has reference to the parent context for recursive search.
    /// </summary>
    public class Context
    {
        private Context parent; // if null then it's global scope
        private Dictionary<string, CodeObject> map = new Dictionary<string, CodeObject>();

        /// <summary>
        /// Gets the parent context
        /// </summary>
        public Context Parent
        {
            get
            {
                return parent;
            }
        }

        /// <summary>
        /// Queries the object with the specified name, performing a case-sensitive search. 
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns>The the found code object or null if there is no object with this name.</returns>
        public CodeObject QueryObject (string name)
        {
            CodeObject result = null;
            if (!map.TryGetValue(name, out result) && parent != null)
                result = parent.QueryObject(name);
            return result;
        }

        /// <summary>
        /// Adds the new object to this context
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="obj">The object.</param>
        public void DefineObject (string name, CodeObject obj)
        {
            if (map.ContainsKey(name))
                throw new AnalizeException("The object with same name already exist in this context.");
            map.Add(name, obj);
        }


        /// <summary>
        /// Enumerates all objects in this context
        /// </summary>
        /// <returns>Array of code objects</returns>
        public CodeObject[] EnumAllObjects ()
        {
            CodeObject[] vars = new CodeObject[map.Values.Count];
            map.Values.CopyTo(vars, 0);
            return vars;
        }

        public Context (Context parent)
        {
            this.parent = parent;
        }
    }
}