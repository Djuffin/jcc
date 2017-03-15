using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection.Emit;
using System.Reflection;
using System.Threading;
using jcc.ParserScope;

namespace jcc.CodeGenScope
{

    /// <summary>
    /// Represents the local variable
    /// </summary>
    public class ILLocal : ILCodeObject
    {
        LocalBuilder self;

        /// <summary>
        /// Initializes a new instance of the <see cref="T:ILLocal"/> class.
        /// </summary>
        /// <param name="type">The type of the variable.</param>
        /// <param name="name">The name of the variable.</param>
        /// <param name="generator">The IL generator.</param>
        internal ILLocal (TypeEntity type, string name, ILCodeGenerator generator)
            : base(type, generator)
        {
            this.type = type;
            Type clrType = ILTypeTranslator.Translate(type);
            self = IL.DeclareLocal(clrType);
            generator.CurrentContext.DefineObject(name, this);
        }

        public override void GenerateCode ()
        {
            IL.Emit(OpCodes.Ldloc, self);
        }

        public override void GenerateStore ()
        {
            IL.Emit(OpCodes.Dup);
            IL.Emit(OpCodes.Stloc, self);
        }

    }

    /// <summary>
    /// Represents the global variable
    /// </summary>
    public class ILGlobal : ILCodeObject
    {
        FieldBuilder self;

        /// <summary>
        /// Initializes a new instance of the <see cref="T:ILGlobal"/> class.
        /// </summary>
        /// <param name="type">The type of the variable.</param>
        /// <param name="name">The name of the variable.</param>
        /// <param name="generator">The IL generator.</param>
        internal ILGlobal (TypeEntity type, string name, ILCodeGenerator generator)
            : base(type, generator)
        {
            this.type = type;
            Type clrType = ILTypeTranslator.Translate(type);
            self = generator.MainClass.DefineField(name, clrType, FieldAttributes.Public | FieldAttributes.Static);
            generator.GlobalContext.DefineObject(name, this);
        }

        public override void GenerateCode ()
        {
            IL.Emit(OpCodes.Ldsfld, self);
        }

        public override void GenerateStore ()
        {
            IL.Emit(OpCodes.Dup);
            IL.Emit(OpCodes.Stsfld, self);
        }
    }

    /// <summary>
    /// Represents the function argument, into the function
    /// </summary>
    public class ILArgument : ILCodeObject
    {
        ParameterBuilder self;
        MethodBuilder method;

        /// <summary>
        /// Initializes a new instance of the <see cref="T:ILArgument"/> class.
        /// </summary>
        /// <param name="type">The type of the argument.</param>
        /// <param name="name">The name of the argument.</param>
        /// <param name="generator">The IL generator.</param>
        /// <param name="method">The parent method.</param>
        /// <param name="index">The index of this argument in arguments array of parent method.</param>
        internal ILArgument (TypeEntity type, string name, ILCodeGenerator generator, MethodBuilder method, int index)
            : base(type, generator)
        {
            this.type = type;
            this.method = method;
            Type clrType = ILTypeTranslator.Translate(type);
            self = method.DefineParameter(index, ParameterAttributes.None, name);
            generator.CurrentContext.DefineObject(name, this);
        }

        public override void GenerateCode ()
        {
            IL.Emit(OpCodes.Ldarg_S, self.Position - 1);
        }

        public override void GenerateStore ()
        {
            IL.Emit(OpCodes.Dup);
            IL.Emit(OpCodes.Starg, self.Position - 1);
        }
    }

    /// <summary>
    /// Represents the function.
    /// </summary>
    public class ILFunctionRef : ILCodeObject
    {
        MethodInfo self;
        FunctionDefinition node;
        FunctionCall call = null;

        bool system; //this is system function

        /// <summary>
        /// Initializes a new instance of the <see cref="T:ILFunctionRef"/> class.
        /// </summary>
        /// <param name="generator">The generator.</param>
        /// <param name="node">The node which declare this function</param>
        /// <param name="self">The new MethodInfo object for this function</param>
        internal ILFunctionRef (ILCodeGenerator generator, FunctionDefinition node, MethodInfo self)
        {
            this.self = self;
            this.generator = generator;
            this.node = node;
            type = new TypeEntity(node.ReturnType);
            generator.GlobalContext.DefineObject(node.Name, this);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:ILFunctionRef"/> class.
        /// This is not user defined function, it's .NET framework static method
        /// </summary>
        /// <param name="generator">The IL generator.</param>
        /// <param name="self">The new MethodInfo object of .NET Framework method</param>
        private ILFunctionRef (ILCodeGenerator generator, MethodInfo self)
        {
            system = true;
            this.self = self;
            this.generator = generator;
            type = ILTypeTranslator.Translate(self.ReturnType);
        }

        /// <summary>
        /// Looks for .NET method
        /// </summary>
        /// <param name="generator">The IL generator.</param>
        /// <param name="callNode">The node which called the .NET static method</param>
        /// <returns>ILFunctionRef for called .NET static method.
        /// <c>null</c> - if there is no static method with this name exist in .NET framework</returns>
        public static ILFunctionRef LookForDotNetMethod (ILCodeGenerator generator, FunctionCall callNode)
        {
            string name = callNode.Name;
            int index = name.LastIndexOf('.');
            if (index == -1)
                return null;
            string typeName = name.Substring(0, index);
            string methodName = name.Substring(index + 1);
            System.Type type = System.Type.GetType(typeName);
            if (type == null)
                type = System.Type.GetType("System." + typeName);
            if (type == null)
                return null;


            List<Type> argTypes = new List<Type>();
            ExpressionGenerator g =  new ExpressionGenerator(generator);
            foreach (Expression arg in callNode)
                argTypes.Add(ILTypeTranslator.Translate(g.Create(arg).Type));

            MethodInfo method = type.GetMethod(methodName, argTypes.ToArray());
            if (method == null || !method.IsStatic)
                return null;

            return new ILFunctionRef(generator, method);
        }


        /// <summary>
        /// Sets the calling node before GenerateCode ()
        /// </summary>
        /// <remarks>
        /// Yes, I know. It's ugly hack :(
        /// </remarks>         
        /// <param name="call">The call node.</param>
        public void SetCallNode (FunctionCall call)
        {
            this.call = call;
            if (!system)
                if (call.Count != node.Args.Count)
                    throw new AnalizeException("Function " + node.Name + ", missing parameters number", call);
        }

        //

        /// <summary>
        /// Generates the code for function calling, before call this function must be called SetCallNode()
        /// </summary>
        public override void GenerateCode ()
        {
            if (call == null)
                throw new ArgumentException("You should before call SetCallNode() method");

            int index = 0;
            foreach (Expression arg in call)
            {
                ILCodeObject argObject = generator.ExprEvaluator.Create(arg);
                argObject.GenerateCode();
                if (!system)
                    try
                    {
                        ILTypeTranslator.GenerateImplicitCast(IL, argObject.Type, new TypeEntity(node.Args[index].Type));
                    }
                    catch (AnalizeException )
                    {
                        string message = string.Format("Function {0}, missing parameters type. Has \"{1}\" instead of \"{2}\"", node.Name, argObject.Type, new TypeEntity(node.Args[index].Type));
                        throw new AnalizeException(message, call);
                    }
            }
            IL.Emit(OpCodes.Call, self);
            if (type == TypeEntity.Object && self.ReturnType.IsValueType) //post boxing
                IL.Emit(OpCodes.Box, self.ReturnType);
            call = null;
        }

        public override void GenerateStore ()
        {
            throw new AnalizeException("Can't assign value to function call.", node);
        }
    }

    /// <summary>
    /// Represents the const
    /// </summary>
    public class ILConst : ILCodeObject, IConstVisitor
    {
        Const node;
        bool checkType = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="T:ILConst"/> class.
        /// </summary>
        /// <param name="generator">The IL generator.</param>
        /// <param name="node">The node with this const.</param>
        internal ILConst (ILCodeGenerator generator, Const node)
        {
            this.generator = generator;
            this.node = node;
            CheckType();
        }

        public override void GenerateCode ()
        {
            node.AcceptVisitor(this);
        }

        public override void GenerateStore ()
        {
            throw new AnalizeException("Can't assign value to constant.", node);
        }

        /// <summary>
        /// Evaluates the type of this const
        /// </summary>
        private void CheckType ()
        {
            checkType = true;
            node.AcceptVisitor(this);
            checkType = false;
        }

        #region IConstVisitor Members

        void IConstVisitor.Visit (Const This)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        /// <summary>
        /// Generate code or evaluate the type for int const
        /// </summary>
        void IConstVisitor.Visit (IntConst This)
        {
            if (checkType)
            {
                type = TypeEntity.Int;
                return;
            }
            IL.Emit(OpCodes.Ldc_I4, This.Value);
            
        }

        /// <summary>
        /// Generate code or evaluate the type for double const
        /// </summary>
        void IConstVisitor.Visit (DoubleConst This)
        {
            if (checkType)
            {
                type = TypeEntity.Double;
                return;
            }
            IL.Emit(OpCodes.Ldc_R8, This.Value);
        }

        /// <summary>
        /// Generate code or evaluate the type for string const
        /// </summary>
        void IConstVisitor.Visit (StringConst This)
        {
            if (checkType)
            {
                type = TypeEntity.String;
                return;
            }
            IL.Emit(OpCodes.Ldstr, This.Value);
        }

        #endregion
    }

    /// <summary>
    /// Represents the binary operation 
    /// </summary>
    public class ILBinaryOperation : ILCodeObject, IBinaryOperationVisitor
    {
        BinaryOperation node;
        ILCodeObject arg0, arg1;
        bool checkType = false;
        static MethodInfo Concat = typeof(string).GetMethod("Concat", new Type[] { typeof(string), typeof(string) });

        /// <summary>
        /// Initializes a new instance of the <see cref="T:ILBinaryOperation"/> class.
        /// </summary>
        /// <param name="generator">The IL generator.</param>
        /// <param name="node">The node with this binary operation.</param>
        internal ILBinaryOperation (ILCodeGenerator generator, BinaryOperation node)
        {
            this.generator = generator;
            this.node = node;
            arg0 = generator.ExprEvaluator.Create(node.Arg0);
            arg1 = generator.ExprEvaluator.Create(node.Arg1);
            CheckType();
        }

        public override TypeEntity Type
        {
            get
            {
                return type;
            }
        }

        /// <summary>
        /// Evaluates the values of  argumants.
        /// </summary>
        private void GenerateArgs ()
        {
            arg0.GenerateCode();
            ILTypeTranslator.GenerateImplicitCast(IL, arg0.Type, Type);
            arg1.GenerateCode();
            ILTypeTranslator.GenerateImplicitCast(IL, arg1.Type, Type);
        }

        public override void GenerateCode ()
        {
            node.AcceptVisitor(this);
        }

        /// <summary>
        /// Evaluates the type of result
        /// </summary>
        private void CheckType ()
        {
            checkType = true;
            node.AcceptVisitor(this);
            checkType = false;
        }
        /// <summary>
        /// Evaluates the type of most part of all binary oprations
        /// </summary>
        private void CommonTypeCheck ()
        {
            type = TypeEntity.Compatibility(arg0.Type, arg1.Type);
            if (type == null)
            {
                string message = string.Format("Two incompatibility types in binary operation. \"{0}\" and \"{1}\"", arg0.Type, arg1.Type);
                throw new AnalizeException(message, node);
            }
        }

        public override void GenerateStore ()
        {
            throw new AnalizeException("Can't assign value to result of binary operation.", node);
        }

        #region IBinaryOperationVisitor Members

        void IBinaryOperationVisitor.Visit (BinaryOperation This)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        void IBinaryOperationVisitor.Visit (opAssignment This)
        {
            if (checkType)
            {
                CommonTypeCheck();
                return;
            }
            try
            {
                arg0.GenerateStore(arg1);
            }
            catch (AnalizeException e)
            {
                throw new AnalizeException(e.Message, node);
            }
        }

        void IBinaryOperationVisitor.Visit (opAdd This)
        {
            if (checkType)
            {
                CommonTypeCheck();
                if (!type.IsNumeric && type != TypeEntity.String)
                    throw new AnalizeException("Can't apply this operation to this type", node);                
                return;
            }
            GenerateArgs();
            if (type == TypeEntity.String)
                IL.Emit(OpCodes.Call, Concat);
            else
                IL.Emit(OpCodes.Add);
        }

        void IBinaryOperationVisitor.Visit (opSub This)
        {
            if (checkType)
            {
                CommonTypeCheck();
                if (!type.IsNumeric)
                    throw new AnalizeException("Can't apply this operation to nonumeric type", node);                
                return;
            }
            GenerateArgs();
            IL.Emit(OpCodes.Sub);
        }

        void IBinaryOperationVisitor.Visit (opMod This)
        {
            if (checkType)
            {
                CommonTypeCheck();
                if (!type.IsNumeric)
                    throw new AnalizeException("Can't apply this operation to nonumeric type", node);                
                return;
            }
            GenerateArgs();
            IL.Emit(OpCodes.Rem);
        }

        void IBinaryOperationVisitor.Visit (opDiv This)
        {
            if (checkType)
            {
                CommonTypeCheck();
                if (!type.IsNumeric)
                    throw new AnalizeException("Can't apply this operation to nonumeric type", node);                
                return;
            }
            GenerateArgs();
            IL.Emit(OpCodes.Div);
        }

        void IBinaryOperationVisitor.Visit (opMul This)
        {
            if (checkType)
            {
                CommonTypeCheck();
                if (!type.IsNumeric)
                    throw new AnalizeException("Can't apply this operation to nonumeric type", node);                
                return;
            }
            GenerateArgs();
            IL.Emit(OpCodes.Mul);
        }

        void IBinaryOperationVisitor.Visit (opEqual This)
        {
            if (checkType)
            {
                CommonTypeCheck();
                if (!type.IsNumeric)
                    throw new AnalizeException("Can't apply this operation to nonumeric type", node);                
                return;
            }
            GenerateArgs();
            IL.Emit(OpCodes.Ceq);
        }

        void IBinaryOperationVisitor.Visit (opNotEqual This)
        {
            if (checkType)
            {
                CommonTypeCheck();
                if (!type.IsNumeric)
                    throw new AnalizeException("Can't apply this operation to nonumeric type", node);                
                return;
            }
            GenerateArgs();
            IL.Emit(OpCodes.Ceq);
            IL.Emit(OpCodes.Ldc_I4_0);
            IL.Emit(OpCodes.Ceq);
        }

        void IBinaryOperationVisitor.Visit (opGreat This)
        {
            if (checkType)
            {
                CommonTypeCheck();
                if (!type.IsNumeric)
                    throw new AnalizeException("Can't apply this operation to nonumeric type", node);                
                return;
            }
            GenerateArgs();
            IL.Emit(OpCodes.Cgt);
        }

        void IBinaryOperationVisitor.Visit (opLess This)
        {
            if (checkType)
            {
                CommonTypeCheck();
                if (!type.IsNumeric)
                    throw new AnalizeException("Can't apply this operation to nonumeric type", node);                
                return;
            }
            GenerateArgs();
            IL.Emit(OpCodes.Clt);
        }

        void IBinaryOperationVisitor.Visit (opGreateEqual This)
        {
            if (checkType)
            {
                CommonTypeCheck();
                if (!type.IsNumeric)
                    throw new AnalizeException("Can't apply this operation to nonumeric type", node);                
                return;
            }
            GenerateArgs();
            IL.Emit(OpCodes.Clt);
            IL.Emit(OpCodes.Ldc_I4_0);
            IL.Emit(OpCodes.Ceq);
        }

        void IBinaryOperationVisitor.Visit (opLessEqual This)
        {
            if (checkType)
            {
                CommonTypeCheck();
                if (!type.IsNumeric)
                    throw new AnalizeException("Can't apply this operation to nonumeric type", node);                
                return;
            }
            GenerateArgs();
            IL.Emit(OpCodes.Cgt);
            IL.Emit(OpCodes.Ldc_I4_0);
            IL.Emit(OpCodes.Ceq);
        }

        void IBinaryOperationVisitor.Visit (opOR This)
        {
            if (checkType)
            {
                CommonTypeCheck();
                if (!type.IsNumeric)
                    throw new AnalizeException("Can't apply this operation to nonumeric type", node);                
                return;
            }
            GenerateArgs();
            IL.Emit(OpCodes.Or);
        }

        void IBinaryOperationVisitor.Visit (opAND This)
        {
            if (checkType)
            {
                CommonTypeCheck();
                if (!type.IsNumeric)
                    throw new AnalizeException("Can't apply this operation to nonumeric type", node);                
                return;
            }
            GenerateArgs();
            IL.Emit(OpCodes.And);
        }

        #endregion
    }

    /// <summary>
    /// Represents the unary operation 
    /// </summary>
    public class ILUnaryOperation : ILCodeObject, IUnaryOperationVisitor
    {
        UnaryOperation node;
        ILCodeObject arg;
        bool checkType = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="T:ILUnaryOperation"/> class.
        /// </summary>
        /// <param name="generator">The IL generator.</param>
        /// <param name="node">The node with this unary operation.</param>
        internal ILUnaryOperation (ILCodeGenerator generator, UnaryOperation node)
        {
            this.generator = generator;
            this.node = node;
            arg = generator.ExprEvaluator.Create(node.Arg);
            CheckType();
        }

        public override void GenerateCode ()
        {
            node.AcceptVisitor(this);
        }

        /// <summary>
        /// Evaluates the type of result
        /// </summary>
        private void CheckType ()
        {
            checkType = true;
            node.AcceptVisitor(this);
            checkType = false;
        }

        public override void GenerateStore ()
        {
            throw new AnalizeException("Can't assign value to result of unary operation.", node);
        }

        #region IUnaryOperationVisitor Members

        void IUnaryOperationVisitor.Visit (UnaryOperation This)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        void IUnaryOperationVisitor.Visit (opUnaryMinus This)
        {
            if (checkType)
            {
                if (!arg.Type.IsNumeric)
                    throw new AnalizeException("Can't apply unary minus to nonnumeric type", This);
                type = arg.Type;
                return;
            }
            arg.GenerateCode();
            IL.Emit(OpCodes.Neg);
        }

        void IUnaryOperationVisitor.Visit (opUnaryPlus This)
        {
            if (checkType)
            {
                if (!arg.Type.IsNumeric)
                    throw new AnalizeException("Can't apply unary plus to nonnumeric type", This);
                type = arg.Type;
                return;
            }
            arg.GenerateCode();
        }

        void IUnaryOperationVisitor.Visit (opPrefixDecrement This)
        {
            if (checkType)
            {
                if (!arg.Type.IsNumeric)
                    throw new AnalizeException("Can't apply decrement to nonnumeric type", This);
                type = arg.Type;
                return;
            }
            arg.GenerateCode();
            IL.Emit(OpCodes.Ldc_I4_1);
            IL.Emit(OpCodes.Sub);
            arg.GenerateStore();
        }

        void IUnaryOperationVisitor.Visit (opPrefixIncrement This)
        {
            if (checkType)
            {
                if (!arg.Type.IsNumeric)
                    throw new AnalizeException("Can't apply increment to nonnumeric type", This);
                type = arg.Type;
                return;
            }
            arg.GenerateCode();
            IL.Emit(OpCodes.Ldc_I4_1);
            IL.Emit(OpCodes.Add);
            arg.GenerateStore();
        }


        void IUnaryOperationVisitor.Visit (opPostfixDecrement This)
        {
            if (checkType)
            {
                if (!arg.Type.IsNumeric)
                    throw new AnalizeException("Can't apply decrement to nonnumeric type", This);
                type = arg.Type;
                return;
            }
            arg.GenerateCode();
            IL.Emit(OpCodes.Dup);
            IL.Emit(OpCodes.Ldc_I4_1);
            IL.Emit(OpCodes.Sub);
            arg.GenerateStore();
            IL.Emit(OpCodes.Pop);
        }

        void IUnaryOperationVisitor.Visit (opPostfixIncrement This)
        {
            if (checkType)
            {
                if (!arg.Type.IsNumeric)
                    throw new AnalizeException("Can't apply increment to nonnumeric type", This);
                type = arg.Type;
                return;
            }
            arg.GenerateCode();
            IL.Emit(OpCodes.Dup);
            IL.Emit(OpCodes.Ldc_I4_1);
            IL.Emit(OpCodes.Add);
            arg.GenerateStore();
            IL.Emit(OpCodes.Pop);
        }

        void IUnaryOperationVisitor.Visit (opNot This)
        {
            if (checkType)
            {
                if (!arg.Type.IsNumeric)
                    throw new AnalizeException("Can't apply operation NOT to nonnumeric type", This);
                type = arg.Type;
                return;
            }
            arg.GenerateCode();
            IL.Emit(OpCodes.Ldc_I4_0);
            IL.Emit(OpCodes.Ceq);
        }

        #endregion
    }

    /// <summary>
    /// Represents the type cast
    /// </summary>
    public class ILTypeCast : ILCodeObject
    {
        TypeCast node;
        ILCodeObject arg;

        internal ILTypeCast (ILCodeGenerator generator, TypeCast node)
        {
            this.generator = generator;
            this.node = node;
            arg = generator.ExprEvaluator.Create(node.InnerExpression);
            type = new TypeEntity(node.CastingTypeNode);
        }

        public override void GenerateStore ()
        {
            throw new AnalizeException("Can't assign value to type cast.", node);
        }

        public override void GenerateCode ()
        {
            arg.GenerateCode();
            try
            {
                ILTypeTranslator.GenerateExplicitCast(IL, arg.Type, type);
            }
            catch (AnalizeException e)
            {
                throw new AnalizeException(e.Message, node);
            }
        }
    }

    /// <summary>
    /// Represents the generator of IL code for expressions.
    /// </summary>
    public class ExpressionGenerator : IExpressionVisitor
    {
        private ILCodeObject result;
        private ILCodeGenerator generator;

        public ExpressionGenerator (ILCodeGenerator generator)
        {
            this.generator = generator;
        }

        /// <summary>
        /// Creates the specified expression (unary operation, binary operation, var & etc)
        /// by its node
        /// </summary>
        /// <param name="expression">The node of expression.</param>
        /// <returns>Codeobject for this expression</returns>
        public ILCodeObject Create (Expression expression)
        {
            result = null;
            expression.AcceptVisitor(this);
            return result;
        }

        /// <summary>
        /// Generates the code for evaluate results of expression 
        /// </summary>
        /// <param name="expression">The expression.</param>
        public void GenerateCode (Expression expression)
        {
            Create(expression).GenerateCode();
        }

        /// <summary>
        /// Generates the code for evaluate results of expression, and delete this result from the stack
        /// </summary>
        /// <param name="expression">The expression.</param>
        public void GenerateCodeWithoutResult (Expression expression)
        {
            if (expression == null)
                return;
            ILCodeObject expObj = Create(expression);
            expObj.GenerateCode();
            if (expObj.Type != TypeEntity.Void)
                generator.CurrentIL.Emit(OpCodes.Pop);
        }


        /// <summary>
        /// It's fast way to call the generator.CurrentContext.QueryObject(name)
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns>Codeobject with this name and within current context</returns>
        private ILCodeObject GetNamedObject (string name)
        {
            return (ILCodeObject)generator.CurrentContext.QueryObject(name);
        }


        #region IExpressionVisitor Members

        void IExpressionVisitor.Visit (Expression This)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        void IExpressionVisitor.Visit (BinaryOperation This)
        {
            result =  new ILBinaryOperation(generator, This);
        }

        void IExpressionVisitor.Visit (UnaryOperation This)
        {
            result = new ILUnaryOperation(generator, This);
        }

        void IExpressionVisitor.Visit (Variable This)
        {
            string name = This.Name;
            result = GetNamedObject(This.Name);
            if (result == null || (! (result is ILGlobal || result is ILLocal || result is ILArgument)))
                throw new AnalizeException("The variable " + name + " does not exist in the current context", This);
        }

        void IExpressionVisitor.Visit (Parentheses This)
        {
            This.InnerExpression.AcceptVisitor(this);
        }

        void IExpressionVisitor.Visit (Const This)
        {
            result = new ILConst(generator, This);
        }

        void IExpressionVisitor.Visit (TypeCast This)
        {
            result = new ILTypeCast(generator, This);
        }

        void IExpressionVisitor.Visit (FunctionCall This)
        {
            string name = This.Name;
            ILFunctionRef call = GetNamedObject(name) as ILFunctionRef;
            if (call == null)
                call = ILFunctionRef.LookForDotNetMethod(generator, This);
            if (call == null)
                throw new AnalizeException("The function " + name + " does not exist in the current context (or have wrong signature)", This);
            call.SetCallNode(This);
            result = call;
        }

        #endregion
    }

}
