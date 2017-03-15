using System;
using System.Collections.Generic;
using System.Xml;

namespace jcc.ParserScope
{
    /// <summary>
    /// Represents visitor for classes derived from the <see cref="jcc.ParserScope.Expression"/>
    /// </summary>
    public interface IExpressionVisitor
    {
        void Visit (Expression This);

        void Visit (BinaryOperation This);

        void Visit (UnaryOperation This);

        void Visit (Variable This);

        void Visit (Parentheses This);

        void Visit (Const This);

        void Visit (TypeCast This);

        void Visit (FunctionCall This);

    }

    /// <summary>
    /// Represents visitor for classes derived from the <see cref="jcc.ParserScope.Statement"/>
    /// </summary>
    public interface IStatementVisitor
    {
        void Visit (Statement This);

        void Visit (IfStatement This);

        void Visit (WhileStatement This);

        void Visit (ForStatement This);

        void Visit (Declare This);

        void Visit (BreakStatement This);

        void Visit (ContinueStatement This);

        void Visit (ReturnStatement This);

        void Visit (Block This);

        void Visit (ExpressionStatement This);
    }

    /// <summary>
    /// Represents visitor for classes derived from the <see cref="jcc.ParserScope.BinaryOperation"/>
    /// </summary>
    public interface IBinaryOperationVisitor
    {
        void Visit (BinaryOperation This);
        void Visit (opAssignment This);

        void Visit (opAdd This);
        void Visit (opSub This);

        void Visit (opMod This);
        void Visit (opDiv This);
        void Visit (opMul This);

        void Visit (opEqual This);
        void Visit (opNotEqual This);
        void Visit (opGreat This);
        void Visit (opLess This);
        void Visit (opGreateEqual This);
        void Visit (opLessEqual This);

        void Visit (opOR This);
        void Visit (opAND This);

    }

    /// <summary>
    /// Represents visitor for classes derived from the <see cref="jcc.ParserScope.UnaryOperation"/>
    /// </summary>
    public interface IUnaryOperationVisitor
    {
        void Visit (UnaryOperation This);
        void Visit (opUnaryMinus This);
        void Visit (opUnaryPlus This);
        void Visit (opNot This);
        void Visit (opPrefixDecrement This);
        void Visit (opPrefixIncrement This);
        void Visit (opPostfixDecrement This);
        void Visit (opPostfixIncrement This);

    }

    /// <summary>
    /// Represents visitor for classes derived from the <see cref="jcc.ParserScope.Const"/>
    /// </summary>
    public interface IConstVisitor
    {
        void Visit (Const This);

        void Visit (IntConst This);

        void Visit (DoubleConst This);

        void Visit (StringConst This);
    }


}  