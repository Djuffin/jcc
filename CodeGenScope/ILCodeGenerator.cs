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
    /// The exception that is thrown when appear error in time of codegeneration
    /// </summary>
    public class AnalizeException : Exception
    {
        private Node node;

        /// <summary>
        /// Node with which appear error.
        /// </summary>
        public Node Node
        {
            get
            {
                return node;
            }
        }

        public AnalizeException ()
        {
        }

        public AnalizeException (string message)
            : base(message)
        {
        }

        public AnalizeException (string message, Node node)
            : base(message)
        {
            this.node = node;
        }

        public AnalizeException (string message, Exception e)
            : base(message, e)
        {
        }
    }

    /// <summary>
    /// Generate  Microsoft Intermediate Language from the Abstract syntax tree (AST)
    /// </summary>
    public class ILCodeGenerator
    {
        private ProgramNode Root;
        private Context globalContext;
        private Context currentContext;
        private ILFunctionRef currentFunction;
        private ILGenerator il;
        private TypeBuilder mainClass;
        private ExpressionGenerator expressionEvaluator;
        private StatementGenerator statementEvaluator;
        private Stack<Label> continueLabels = new Stack<Label>(); 
        private Stack<Label> breakLabels = new Stack<Label>();
        private MethodInfo entryPoint;
        private ErrorsCollection errors = new ErrorsCollection();

        /// <summary>
        /// Gets the collection of all codegen errors.
        /// </summary>
        public ErrorsCollection Errors
        {
            get
            {
                return errors;
            }
        }

        /// <summary>
        /// Gets the function generated in this time.
        /// </summary>
        public ILFunctionRef CurrentFunction
        {
            get
            {
                return currentFunction;
            }
        }

        /// <summary>
        /// Gets the label for continue jamp (i.e. loop condition).
        /// </summary>
        /// <value>The Nellable type, null - continue is unavailable</value>
        public Label? ContinueLabel 
        {
            get
            {
                if (continueLabels.Count == 0)
                    return null;
                return continueLabels.Peek();
            }
        }

        /// <summary>
        /// Gets the label for break jamp (i.e. loop exit).
        /// </summary>
        /// <value>The Nellable type, null - break is unavailable</value>
        public Label? BreakLabel
        {
            get
            {
                if (breakLabels.Count == 0)
                    return null;
                return breakLabels.Peek();
            }
        }

        /// <summary>
        /// Gets the global context (global variable scope).
        /// </summary>
        public Context GlobalContext
        {
            get
            {
                return globalContext;
            }
        }

        /// <summary>
        /// Gets the current context (current block variable scope).
        /// </summary>
        /// <value>The current context.</value>
        public Context CurrentContext
        {
            get
            {
                return currentContext;
            }
        }

        /// <summary>
        /// Gets the  IL generator for the current function.
        /// </summary>
        /// <value>The current ILGenerator.</value>
        public ILGenerator CurrentIL
        {
            get
            {
                return il;
            }
        }

        /// <summary>
        /// Gets the TypeBuilder for glabal (singular and invisible) class.
        /// </summary>
        public TypeBuilder MainClass
        {
            get
            {
                return mainClass;
            }
        }

        /// <summary>
        /// Gets the special code generator, only for expressions
        /// </summary>
        public ExpressionGenerator ExprEvaluator
        {
            get
            {
                return expressionEvaluator;
            }
        }

        /// <summary>
        /// Gets the special code generator, only for statements
        /// </summary>
        public StatementGenerator StmtEvaluator
        {
            get
            {
                return statementEvaluator;
            }
        }

        /// <summary>
        /// Gets a value indicating whether  code is valid.
        /// </summary>
        /// <value><c>true</c> if has no errors; otherwise, <c>false</c>.</value>
        public bool ValidCode
        {
            get
            {
                return errors.Count == 0;
            }
        }

        /// <summary>
        /// Swallows the exception, and add corresponding error to the errors collection  
        /// </summary>
        /// <param name="e">The AnalizeException for adding to the compile errors </param>
        public void SwallowException(AnalizeException e)
        {
            errors.Add(new Error(e));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:ILCodeGenerator"/> class.
        /// </summary>
        /// <param name="root">The root of AST for codegeneration</param>
        public ILCodeGenerator (ProgramNode root)
        {
            Root = root;
            globalContext = new Context(null);
            currentContext = globalContext;
            expressionEvaluator = new ExpressionGenerator(this);
            statementEvaluator = new StatementGenerator(this);
        }

        /// <summary>
        /// It's called when codegenerator enter to the loop block, 
        /// this method is required for the <c>continue</c> and the <c>break</c> keywords implementation
        /// </summary>
        /// <param name="continueLabel">The continue label.</param>
        /// <param name="breakLabel">The break label.</param>
        public void LoopEnter (Label continueLabel, Label breakLabel)
        {
            continueLabels.Push(continueLabel);
            breakLabels.Push(breakLabel);
        }

        /// <summary>
        /// Leaves last entered  loop block, terminate LoopEnter() method call
        /// </summary>
        public void LoopLeave ()
        {
            continueLabels.Pop();
            breakLabels.Pop();
        }

        /// <summary>
        /// It's called when codegenerator enter to the code block.
        /// It create a new variable naming context
        /// </summary>
        public void BlockEnter()
        {
            currentContext = new Context(currentContext);
            il.BeginScope();
        }

        /// <summary>
        /// Leaves last entered  code block, terminate BlockEnter() method call
        /// </summary>
        public void BlockLeave ()
        {
            if (currentContext.Parent == null)
                throw new Exception("Leave block without enter.");
            currentContext = currentContext.Parent;
            il.EndScope();
        }


        /// <summary>
        /// Processes the all global declarations in program.
        /// This method creates objects (ILGlobal) for global variables,
        /// and adds it the global context
        /// </summary>
        /// <param name="declare">The declare.</param>
        private void ProcessGlobalDeclare (Declare declare)
        {
            TypeEntity type = new TypeEntity(declare.Type);
            int index = 0;
            foreach (Variable var in declare)
            {
                new ILGlobal(type, var.Name, this);
                if (declare.GetInitExpression(index++) != null)
                    throw new AnalizeException("Can't initialize global variable", declare);
            }
        }

        /// <summary>
        /// Generates the all functions.
        /// </summary>
        private void GenerateMethods ()
        {
            for (int i = 0; i < Root.FunctionsCount ; i++)
                GenerateMethod(Root.GetFunction(i));
        }

        /// <summary>
        /// Generates function for some function node
        /// </summary>
        /// <param name="funcNode">The function node.</param>
        private void GenerateMethod (FunctionDefinition funcNode)
        {
            Type returnType = ILTypeTranslator.Translate(funcNode.ReturnType);
            List<Type> paramsTypes = new List<Type>();
            foreach (ArgumentDefinition arg in funcNode.Args)
                paramsTypes.Add(ILTypeTranslator.Translate(arg.Type));
            MethodBuilder currentMethod = mainClass.DefineMethod(funcNode.Name, MethodAttributes.Static | MethodAttributes.Public, returnType, paramsTypes.ToArray());
            currentFunction = new ILFunctionRef(this, funcNode, currentMethod);

            il = currentMethod.GetILGenerator();
            BlockEnter();

            int index = 1;
            foreach (ArgumentDefinition arg in funcNode.Args)
                new ILArgument(new TypeEntity(arg.Type), arg.Name, this, currentMethod, index++);
            statementEvaluator.GenerateCode(funcNode.Body);
            il.Emit(OpCodes.Ret);
            BlockLeave();
            entryPoint = currentMethod;
        }

        /// <summary>
        /// Generates the fields in global class for global variables
        /// </summary>
        private void GenerateFields ()
        {
            for (int i = 0; i < Root.DeclaresCount; i++)
                ProcessGlobalDeclare(Root.GetDeclare(i));
        }

        /// <summary>
        /// Creates the assembly and save it to the disc
        /// </summary>
        /// <returns><c>true</c> - okay! otherwise false</returns>
        public bool CreateAssembly (string Name)
        {
            AssemblyBuilder bldAssembly = Thread.GetDomain().DefineDynamicAssembly(new AssemblyName(Name), AssemblyBuilderAccess.RunAndSave);
            ModuleBuilder bldModule = bldAssembly.DefineDynamicModule("MainModule", Name);
            mainClass = bldModule.DefineType("GlobalType");
            GenerateFields();
            GenerateMethods();
            if (ValidCode)
            {
                mainClass.CreateType();
                bldAssembly.SetEntryPoint(entryPoint);
                bldAssembly.Save(Name);
                return false;
            }
            return true;
        }

    }
}
