using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection.Emit;
using System.Reflection;
using jcc.ParserScope;

namespace jcc.CodeGenScope
{
    /// <summary>
    /// Represents the code generator for the all kinds of statements
    /// </summary>
    public class StatementGenerator : IStatementVisitor
    {
        private ILCodeGenerator generator;
        /// <summary>
        /// It's the fast reference to the IL Generator.
        /// </summary>
        private ILGenerator IL
        {
            get
            {
                return generator.CurrentIL;
            }
        }

        public StatementGenerator (ILCodeGenerator generator)
        {
            this.generator = generator;
        }

        /// <summary>
        /// Generates the code for the statement node.
        /// </summary>
        /// <param name="node">The statement node.</param>
        public void GenerateCode (Statement node)
        {
            try
            {
                node.AcceptVisitor(this);
            }
            catch (AnalizeException e)
            {
                generator.SwallowException(e);
            }
        }

        #region IStatementVisitor Members

        void IStatementVisitor.Visit (Statement This)
        {
            throw new Exception("This method never should not be called");
        }

        /// <summary>
        /// Generates the IL code for <c>if else</c> construction
        /// </summary>
        /// <param name="This">The [if] node.</param>
        void IStatementVisitor.Visit (IfStatement This)
        {
            bool HasElse = This.NoBranch != null;
            Label elseLabel = HasElse ? IL.DefineLabel() : new Label();
            Label endLabel = IL.DefineLabel();

            //Calculate Condition
            generator.ExprEvaluator.GenerateCode(This.Condition);
            if (HasElse) 
                IL.Emit(OpCodes.Brfalse, elseLabel);
            else
                IL.Emit(OpCodes.Brfalse, endLabel);

            //True branch
            generator.StmtEvaluator.GenerateCode(This.YesBranch);

            //Else Branch
            if (HasElse)
            {
                IL.Emit(OpCodes.Br, endLabel);
                IL.MarkLabel(elseLabel);
                generator.StmtEvaluator.GenerateCode(This.NoBranch);
            }
            IL.MarkLabel(endLabel);
        }

        /// <summary>
        /// Generates the IL code for <c>while</c> construction
        /// </summary>
        /// <param name="This">The [while] node.</param>
        void IStatementVisitor.Visit (WhileStatement This)
        {
            Label end = IL.DefineLabel();
            Label body = IL.DefineLabel();
            Label condition = IL.DefineLabel();
            generator.LoopEnter(condition, end);

            //goto condition
            IL.Emit(OpCodes.Br, condition);

            //body
            IL.MarkLabel(body);
            generator.StmtEvaluator.GenerateCode(This.Body);

            //condition
            IL.MarkLabel(condition);
            generator.ExprEvaluator.GenerateCode(This.Condition);
            IL.Emit(OpCodes.Brtrue, body);

            IL.MarkLabel(end);
            generator.LoopLeave();
        }

        /// <summary>
        /// Generates the IL code for <c>for</c> construction
        /// </summary>
        /// <param name="This">The [for] node.</param>
        void IStatementVisitor.Visit (ForStatement This)
        {
            Label end = IL.DefineLabel();
            Label body = IL.DefineLabel();
            Label condition = IL.DefineLabel();
            Label loop = IL.DefineLabel();
            generator.LoopEnter(loop, end);

            //init
            generator.ExprEvaluator.GenerateCodeWithoutResult(This.Init);

            //goto condition
            IL.Emit(OpCodes.Br, condition);

            //body
            IL.MarkLabel(body);
            generator.StmtEvaluator.GenerateCode(This.Body);

            //loop
            IL.MarkLabel(loop);
            generator.ExprEvaluator.GenerateCodeWithoutResult(This.Loop);

            //condition
            IL.MarkLabel(condition);
            generator.ExprEvaluator.GenerateCode(This.Condition);
            IL.Emit(OpCodes.Brtrue, body);

            IL.MarkLabel(end);
            generator.LoopLeave();
        }

        /// <summary>
        /// Generates the IL code for declaration construction
        /// </summary>
        /// <param name="This">The declaration node.</param>
        void IStatementVisitor.Visit (Declare This)
        {
            TypeEntity type = new TypeEntity(This.Type);
            int index = 0;
            foreach (Variable var in This)
            {
                ILLocal local = new ILLocal(type, var.Name, generator);
                Expression init = This.GetInitExpression(index++);
                if (init != null)
                {
                    try
                    {
                        local.GenerateStore(generator.ExprEvaluator.Create(init));
                    }
                    catch (AnalizeException e)
                    {
                        throw new AnalizeException(e.Message, This);
                    }
                    IL.Emit(OpCodes.Pop);
                }

            }
        }

        /// <summary>
        /// Generates the IL code for <c>break</c> construction
        /// </summary>
        /// <param name="This">The [break] node.</param>
        void IStatementVisitor.Visit (BreakStatement This)
        {
            Label? label = generator.BreakLabel;
            if (!label.HasValue)
            {
                throw new AnalizeException("No enclosing loop out of which to break", This);
            }
            IL.Emit(OpCodes.Br, label.Value);
        }

        /// <summary>
        /// Generates the IL code for <c>continue</c> construction
        /// </summary>
        /// <param name="This">The [continue] node.</param>
        void IStatementVisitor.Visit (ContinueStatement This)
        {
            Label? label = generator.ContinueLabel;
            if (!label.HasValue)
            {
                throw new AnalizeException("No enclosing loop out of which to continue", This);
            }
            IL.Emit(OpCodes.Br, label.Value);
        }

        /// <summary>
        /// Generates the IL code for <c>return</c> construction
        /// </summary>
        /// <param name="This">The [return] node.</param>
        void IStatementVisitor.Visit (ReturnStatement This)
        {
            if (This.Result == null)
            {
                if (generator.CurrentFunction.Type != TypeEntity.Void)
                    throw new AnalizeException("This function must return a value", This);
            }
            else
                try
                {
                    ILCodeObject result = generator.ExprEvaluator.Create(This.Result);
                    result.GenerateCode();
                    ILTypeTranslator.GenerateImplicitCast(IL, result.Type, generator.CurrentFunction.Type);
                }
                catch (AnalizeException e)
                {
                    throw new AnalizeException(e.Message, This);
                }
            IL.Emit(OpCodes.Ret);
        }

        /// <summary>
        /// Generates the IL code for code block
        /// </summary>
        /// <param name="This">The [for] node.</param>
        void IStatementVisitor.Visit (Block This)
        {
            generator.BlockEnter();
            foreach (Statement stmt in This)
                GenerateCode(stmt);
            generator.BlockLeave();
        }

        /// <summary>
        /// Generates the IL code for expression as statement (i.e. function call)
        /// </summary>
        void IStatementVisitor.Visit (ExpressionStatement This)
        {
            generator.ExprEvaluator.GenerateCodeWithoutResult(This.InnerExpression);
        }

        #endregion
    }
}