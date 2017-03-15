using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection.Emit;
using jcc.ParserScope;
using System.Reflection;


namespace jcc.CodeGenScope
{
    /// <summary>
    /// Represents the TYPE of any object in our language 
    /// </summary>
    public class TypeEntity
    {
        private enum Tag
        {
            _int,
            _string,
            _double,
            _char,
            _void,
            _object
        }
        private Tag tag;

        public static TypeEntity Int = new TypeEntity(Tag._int);
        //public static TypeEntity Char = new TypeEntity(Tag._char);
        public static TypeEntity Double = new TypeEntity(Tag._double);
        public static TypeEntity String = new TypeEntity(Tag._string);
        public static TypeEntity Void = new TypeEntity(Tag._void);
        public static TypeEntity Object = new TypeEntity(Tag._object);

        public string Name
        {
            get
            {
                return tag.ToString().Substring(1);
            }
        }
        public override string ToString ()
        {
            return Name;
        }


        /// <summary>
        /// Gets a value indicating whether this instance is numeric type.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if this instance is numeric type; otherwise, <c>false</c>.
        /// </value>
        public bool IsNumeric
        {
            get
            {
                switch (tag)
                {
                    case Tag._int:
                    case Tag._double:
                        return true;
                }
                return false;
            }
        }

        /// <summary>
        /// Gets a value indicating whether this instance is integer type.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if this instance is integer type; otherwise, <c>false</c>.
        /// </value>
        public bool IsInteger
        {
            get
            {
                switch (tag)
                {
                    case Tag._int:
                        return true;
                }
                return false;
            }
        }

        /// <summary>
        /// Gets a value indicating whether this instance is float point type.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if this instance is float point type; otherwise, <c>false</c>.
        /// </value>
        public bool IsFloatPoint
        {
            get
            {
                switch (tag)
                {
                    case Tag._double:
                        return true;
                }
                return false;
            }
        }

        /// <summary>
        /// Gets a value indicating whether this instance is signed type.
        /// </summary>
        /// <value><c>true</c> if this instance is signed type; otherwise, <c>false</c>.</value>
        public bool IsSigned
        {
            get
            {
                switch (tag)
                {
                    case Tag._int:
                    case Tag._double:
                        return true;
                }
                return false;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:TypeEntity"/> class by its name
        /// </summary>
        /// <param name="name">The name of type.</param>
        public TypeEntity (string name)
        {
            try
            {
                tag = (Tag)Enum.Parse(typeof(Tag), "_" + name, true);
            }
            catch (Exception e)
            {
                throw new AnalizeException("There is no type with this name:" + name, e);
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:TypeEntity"/> class by its node.
        /// </summary>
        /// <param name="node">The node.</param>
        public TypeEntity (jcc.ParserScope.TypeNode node)
            : this (node.Name)
        {
            try
            {
            }
            catch (AnalizeException )
            {
                throw new AnalizeException("There is no type with this name:" + node.Name, node);
            }

        }

        private TypeEntity (Tag tag)
        {
            this.tag = tag;
        }

        public override bool Equals (object obj)
        {
            TypeEntity type = obj as TypeEntity;
            return Equals(type);
        }

        public bool Equals (TypeEntity type)
        {
            if ((object)type == null)
                return false;
            return tag == type.tag;
        }

        public static bool operator == (TypeEntity type1, TypeEntity type2)
        {
            if (object.ReferenceEquals(type1, type2))
                return true;

            if ((object)type1 == null || (object)type2 == null)
                return false;

            return type1.Equals(type2);
        }

        public static bool operator != (TypeEntity type1, TypeEntity type2)
        {
            return !(type1 == type2);
        }

        public override int GetHashCode ()
        {
            return (int)tag;
        }

        /// <summary>
        /// Determines whether this instance can be casted to the specified SRC.
        /// </summary>
        /// <param name="src">The SRC.</param>
        /// <param name="dst">The DST.</param>
        /// <returns>
        /// 	<c>true</c> if this SRC can be casted to the specified DST; otherwise, <c>false</c>.
        /// </returns>
        public static bool CanCastTo (TypeEntity src, TypeEntity dst)
        {
            if (src == dst)
                return true;
            if (dst.tag == Tag._string)
                return true;
            if (dst.tag == Tag._object)
                return true;
            return src.IsNumeric && dst.IsNumeric;
        }

        /// <summary>
        /// Determines whether this instance can be implicit casted to the specified SRC.
        /// </summary>
        /// <param name="src">The SRC.</param>
        /// <param name="dst">The DST.</param>
        /// <returns>
        /// 	<c>true</c> if this SRC can be casted to the specified DST; otherwise, <c>false</c>.
        /// </returns>
        public static bool CanSafeCastTo (TypeEntity src, TypeEntity dst)
        {
            if (src == dst)
                return true;
            if (dst.tag == Tag._object)
                return true;
            return src.tag == Tag._int && dst.tag == Tag._double;
        }


        /// <summary>
        /// Text compatibilities of the specified types
        /// </summary>
        /// <param name="type1">First type</param>
        /// <param name="type2">Second type</param>
        /// <returns><c>true</c> - those types compatibilities; otherwice, <c>false</c></returns>
        public static TypeEntity Compatibility (TypeEntity type1, TypeEntity type2)
        {
            if (type1 == type2)
                return type1;
            if (CanSafeCastTo(type1, type2))
                return type2;
            if (CanSafeCastTo(type2, type1))
                return type1;
            return null;
        }
    }

    /// <summary>
    /// Represents a way for transtalte types between TypeEntery and .NET Type class.
    /// Alse uses for generate code for type casting
    /// </summary>
    class ILTypeTranslator
    {
        static MethodInfo ToStringMethod = typeof(object).GetMethod("ToString", new Type[] { });

        public static Type Translate (TypeEntity inType)
        {
            if (inType == TypeEntity.Int)
                return typeof(int);
            if (inType == TypeEntity.Double)
                return typeof(double);
            if (inType == TypeEntity.String)
                return typeof(string);
            if (inType == TypeEntity.Void)
                return typeof(void);
            if (inType == TypeEntity.Object)
                return typeof(object);
            return null;
        }

        public static TypeEntity Translate (Type inType)
        {
            if (inType == typeof(int))
                return TypeEntity.Int;
            if (inType == typeof(double))
                return TypeEntity.Double;
            if (inType == typeof(string))
                return TypeEntity.String;
            if (inType == typeof(void))
                return TypeEntity.Void;
            return TypeEntity.Object;
        }

        public static Type Translate (TypeNode inType)
        {
            return Translate(new TypeEntity(inType));
        }

        public static void GenerateImplicitCast (ILGenerator il, TypeEntity SourceType, TypeEntity DestType)
        {
            if (SourceType == DestType)
                return;

            if (!TypeEntity.CanSafeCastTo(SourceType, DestType))
                throw new AnalizeException(string.Format("Can't cast \"{0}\" to \"{1}\"", SourceType.Name, DestType.Name));

            GenCast(il, SourceType, DestType);
        }

        public static void GenerateExplicitCast (ILGenerator il, TypeEntity SourceType, TypeEntity DestType)
        {
            if (SourceType == DestType)
                return;

            if (!TypeEntity.CanCastTo(SourceType, DestType))
                throw new AnalizeException(string.Format("Can't cast \"{0}\" to \"{1}\"", SourceType.Name, DestType.Name));

            GenCast(il, SourceType, DestType);
        }

        private static void GenCast (ILGenerator il, TypeEntity SourceType, TypeEntity DestType)
        {
            if (DestType == TypeEntity.Int)
                il.Emit(OpCodes.Conv_I4);
            else if (DestType == TypeEntity.Double)
                il.Emit(OpCodes.Conv_R8);
            else if (DestType == TypeEntity.String)
            {
                Type clrType = Translate(SourceType);
                if (clrType.IsValueType)
                    il.Emit(OpCodes.Box, clrType);
                il.Emit(OpCodes.Callvirt, ToStringMethod);
            }
            else if (DestType == TypeEntity.Object)
            {
                Type clrType = Translate(SourceType);
                if (clrType.IsValueType)
                    il.Emit(OpCodes.Box, clrType);
            }
                
        }

    }


}
