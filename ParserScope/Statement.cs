using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using jcc.LexerScope;

namespace jcc.ParserScope
{
    public abstract class Statement : Node
    {
        public class Factory : NodeFactory
        {
            public Type[] Expressions = new Type[] { typeof(Block),  typeof(IfStatement),
                typeof(ForStatement), typeof(WhileStatement),  typeof(BreakStatement), typeof(ReturnStatement), typeof(ContinueStatement),
                typeof(ExpressionStatement), typeof(Declare)};
            public override Node Make (Parser parser)
            {
                return parser.AssertOneOfNodes<Statement>(Expressions);
            }
        }
        public virtual void AcceptVisitor (IStatementVisitor visitor)
        {
            visitor.Visit(this);
        }
    }

    public class ExpressionStatement : Statement
    {
        private Expression innerExpression;

        public Expression InnerExpression
        {
            get
            {
                return innerExpression;
            }
        }

        private ExpressionStatement (Expression expr, Operator semicolon)
        {
            innerExpression = expr;
            region = semicolon.Region.Clone();
            if (innerExpression != null)
            {
                innerExpression.Parent = this;
                region = innerExpression.Region | region;
            }
        }

        public new class Factory : NodeFactory
        {
            //expression_statement ::= expression? ';'
            public override Node Make (Parser parser)
            {
                Expression expr = parser.TryGetNode<Expression>();
                Operator semicolon = parser.AssertOperator(Operator.Tag.Semicolon);
                return new ExpressionStatement(expr, semicolon);
            }
        }

        public override XmlNode ToXml (XmlDocument doc)
        {
            XmlNode result = base.ToXml(doc);
            if (innerExpression != null)
                result.AppendChild(innerExpression.ToXml(doc));
            return result;
        }
        public override void AcceptVisitor (IStatementVisitor visitor)
        {
            visitor.Visit(this);
        }
    }

    public class Block : Statement, IEnumerable<Statement>
    {
        private List<Statement> statements;

        public int Count
        {
            get
            {
                return statements.Count;
            }
        }

        public Statement this[int index]
        {
            get
            {
                return statements[index];
            }
        }


        private Block (Operator startBrace, List<Statement> statements, Operator stopBrace)
        {
            this.statements = statements;
            region = startBrace.Region | stopBrace.Region;
            foreach (Statement s in statements)
            {
                s.Parent = this;
                region = region | s.Region;
            }

        }

        public new class Factory : NodeFactory
        {
            public override Node Make (Parser parser)
            {
                Operator startBrace = parser.AssertOperator(Operator.Tag.LeftBracket);
                List<Statement> list = new List<Statement>();
                while (true)
                {
                    Statement stmt = parser.TryGetNode<Statement>();
                    if (stmt == null)
                        break;
                    list.Add(stmt);
                }
                Operator stopBrace = parser.AssertOperator(Operator.Tag.RightBracket);
                return new Block(startBrace, list, stopBrace);
            }

            public override bool CanStartsWith (Token token)
            {
                Operator op = token as Operator;
                if (op == null || op.tag != Operator.Tag.LeftBracket)
                    return false;
                return true;
            }
        }

        public override XmlNode ToXml (XmlDocument doc)
        {
            XmlNode result = base.ToXml(doc);
            foreach (Statement s in statements)
                result.AppendChild(s.ToXml(doc));
            return result;
        }
        public override void AcceptVisitor (IStatementVisitor visitor)
        {
            visitor.Visit(this);
        }

        #region IEnumerable<Statement> Members

        public IEnumerator<Statement> GetEnumerator ()
        {
            return statements.GetEnumerator();
        }

        #endregion

        #region IEnumerable Members

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator ()
        {
            return statements.GetEnumerator();
        }

        #endregion
    }


    public class Declare : Statement,  IEnumerable<Variable>
    {
        private TypeNode type;
        private List<Variable> IdList;
        private List<Expression> InitList;

        public TypeNode Type
        {
            get
            {
                return type;
            }
        }
        public int Count
        {
            get
            {
                return IdList.Count;
            }
        }
        public Variable this[int index]
        {
            get
            {
                return IdList[index];
            }
        }
        public Expression GetInitExpression (int index)
        {
            return InitList[index];
        }

        private Declare (TypeNode type, List<Variable> IdList, List<Expression> InitList, TextRegion region)
        {
            this.type = type;
            this.IdList = IdList;
            this.InitList = InitList;
            this.region = region;
            type.Parent = this;
            foreach (Variable id in IdList)
                id.Parent = this;
        }

        public new class Factory : NodeFactory
        {
            //declare ::= type identifier ('=' expression)? {',' identifier  ('=' expression)? } ';'
            public override Node Make (Parser parser)
            {
                // type
                TypeNode type = parser.AssertNode<TypeNode>();
                TextRegion region = type.Region;
                // id
                List<Variable> IdList = new List<Variable>();
                List<Expression> InitList = new List<Expression>();
                Variable id = parser.AssertNode<Variable>();
                Expression init = null;
                Token eq = parser.TryGetOperator(Operator.Tag.Assignment);
                if (eq != null)
                    init = parser.AssertNode<Expression>();
                InitList.Add(init);
                region = region | id.Region;
                IdList.Add(id);
                // {, id}
                try
                {
                    while (true)
                    {
                        region = region | parser.AssertOperator(Operator.Tag.Comma).Region;
                        id = parser.AssertNode<Variable>();
                        init = null;
                        eq = parser.TryGetOperator(Operator.Tag.Assignment);
                        if (eq != null)
                            init = parser.AssertNode<Expression>();
                        InitList.Add(init);
                        region = region | id.Region;
                        IdList.Add(id);
                    }
                }
                catch (ParseException)
                {
                }
                //;
                Operator semicolon = parser.AssertOperator(Operator.Tag.Semicolon);
                region = region | semicolon.Region;
                return new Declare(type, IdList, InitList, region);
            }
        }
        public override XmlNode ToXml (XmlDocument doc)
        {
            XmlNode result = base.ToXml(doc);
            result.AppendChild(type.ToXml(doc));
            XmlNode ids = doc.CreateElement("Variables");
            result.AppendChild(ids);
            for (int i = 0; i < Count; i++)
            {
                XmlNode var = IdList[i].ToXml(doc);
                ids.AppendChild(var);
                Expression init = InitList[i];
                if (init == null)
                    continue;
                XmlNode initNode = doc.CreateElement("Initialization");
                initNode.AppendChild(init.ToXml(doc));
                var.AppendChild(initNode);
                
            }
            return result;
        }
        public override void AcceptVisitor (IStatementVisitor visitor)
        {
            visitor.Visit(this);
        }

        #region IEnumerable<Variable> Members

        public IEnumerator<Variable> GetEnumerator ()
        {
            return IdList.GetEnumerator();
        }
        
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator ()
        {
            return IdList.GetEnumerator();
        }

        #endregion
    }

    public class IfStatement : Statement
    {
        private Statement yesBranch;
        private Statement noBranch;
        private Expression condition;

        public Statement YesBranch
        {
            get
            {
                return yesBranch;
            }
        }

        public Statement NoBranch
        {
            get
            {
                return noBranch;
            }
        }

        public Expression Condition
        {
            get
            {
                return condition;
            }
        }

        private IfStatement (Expression condition, Statement yesBranch, Statement noBranch, TextRegion region)
        {
            this.yesBranch = yesBranch;
            this.noBranch = noBranch;
            this.condition = condition;
            this.region = region;
            yesBranch.Parent = this;
            if (noBranch != null)
                noBranch.Parent = this;
            condition.Parent = this;
        }

        public new class Factory : NodeFactory
        {
            //if_statement ::= 'if' '(' expression ')' statement ('else' statement)?
            public override Node Make (Parser parser)
            {
                //if
                Token t = parser.AssertOperator(Operator.Tag._if);
                TextRegion region = t.Region;
                // (
                t = parser.AssertOperator(Operator.Tag.LeftPar);
                Expression condition = parser.AssertNode<Expression>();
                region = condition.Region | region | t.Region;
                // )
                t = parser.AssertOperator(Operator.Tag.RightPar);

                //body
                Statement yesBranch = parser.AssertNode<Statement>();
                region = t.Region | region | yesBranch.Region;

                //else
                Statement noBranch = null;
                t = parser.TryGetOperator(Operator.Tag._else);
                if (t != null)
                {
                    noBranch = parser.AssertNode<Statement>();
                    region = region | noBranch.Region | t.Region;
                }
                return new IfStatement(condition, yesBranch, noBranch, region);
            }
            public override bool CanStartsWith (Token token)
            {
                Operator op = token as Operator;
                if (op == null || op.tag != Operator.Tag._if)
                    return false;
                return true;
            }
        }

        public override XmlNode ToXml (XmlDocument doc)
        {
            XmlNode result = base.ToXml(doc);

            XmlNode cond = doc.CreateElement("Condition");
            cond.AppendChild(condition.ToXml(doc));
            result.AppendChild(cond);

            XmlNode yes = doc.CreateElement("YesBranch");
            yes.AppendChild(yesBranch.ToXml(doc));
            result.AppendChild(yes);

            if (noBranch != null)
            {
                XmlNode no = doc.CreateElement("NoBranch");
                no.AppendChild(noBranch.ToXml(doc));
                result.AppendChild(no);
            }

            return result;
        }
        public override void AcceptVisitor (IStatementVisitor visitor)
        {
            visitor.Visit(this);
        }
    }

    public abstract class LoopStatement : Statement
    {
    }

    public class WhileStatement : LoopStatement
    {
        private Statement body;
        private Expression condition;

        public Statement Body
        {
            get
            {
                return body;
            }
        }

        public Expression Condition
        {
            get
            {
                return condition;
            }
        }

        private WhileStatement (Expression condition, Statement body, TextRegion region)
        {
            this.body = body;
            this.condition = condition;
            this.region = region;
            body.Parent = this;
            condition.Parent = this;
        }

        public new class Factory : NodeFactory
        {
            //while_statement ::= 'while' '(' expression ')' statement
            public override Node Make (Parser parser)
            {
                //while
                Token t = parser.AssertOperator(Operator.Tag._while);
                TextRegion region = t.Region;
                // (
                t = parser.AssertOperator(Operator.Tag.LeftPar);
                Expression condition = parser.AssertNode<Expression>();
                region = condition.Region | region | t.Region;
                // )
                t = parser.AssertOperator(Operator.Tag.RightPar);

                //body
                Statement body = parser.AssertNode<Statement>();
                region = t.Region | region | body.Region;

                return new WhileStatement(condition, body, region);
            }
            public override bool CanStartsWith (Token token)
            {
                Operator op = token as Operator;
                if (op == null || op.tag != Operator.Tag._while)
                    return false;
                return true;
            }
        }

        public override XmlNode ToXml (XmlDocument doc)
        {
            XmlNode result = base.ToXml(doc);

            XmlNode cond = doc.CreateElement("Condition");
            cond.AppendChild(condition.ToXml(doc));
            result.AppendChild(cond);

            XmlNode bodyNode = doc.CreateElement("Body");
            bodyNode.AppendChild(body.ToXml(doc));
            result.AppendChild(bodyNode);

            return result;
        }
        public override void AcceptVisitor (IStatementVisitor visitor)
        {
            visitor.Visit(this);
        }
    }

    public class ForStatement : LoopStatement
    {
        private Statement body;
        private Expression init, condition, loop;

        public Statement Body
        {
            get
            {
                return body;
            }
        }

        public Expression Condition
        {
            get
            {
                return condition;
            }
        }

        public Expression Init
        {
            get
            {
                return init;
            }
        }

        public Expression Loop
        {
            get
            {
                return loop;
            }
        }

        private ForStatement (Expression init, Expression condition, Expression loop, Statement body, TextRegion region)
        {
            this.body = body;
            this.condition = condition;
            this.init = init;
            this.loop = loop;
            this.region = region;
            body.Parent = this;
            if (condition != null)
                condition.Parent = this;
            if (loop != null)
                loop.Parent = this;
            if (init != null)
                init.Parent = this;
        }

        public new class Factory : NodeFactory
        {
            private Expression GetExpression(Parser parser, bool semicolon, ref TextRegion region)
            {
                Expression expr = parser.TryGetNode<Expression>();
                if (semicolon)
                    region = parser.AssertOperator(Operator.Tag.Semicolon).Region | region;
                if (expr != null)
                    region = region | expr.Region;
                return expr;
            }

            //for_statement ::= 'for' '(' expression? ';' expression? ';'  expression? ')' statement
            public override Node Make (Parser parser)
            {
                //for
                Token t = parser.AssertOperator(Operator.Tag._for);
                TextRegion region = t.Region;
                // (
                t = parser.AssertOperator(Operator.Tag.LeftPar);
                region = region | t.Region;

                //init; conditioin; loop;
                Expression init = GetExpression(parser, true, ref region);
                Expression condition = GetExpression(parser, true, ref region);
                Expression loop = GetExpression(parser, false, ref region);
                
                // )
                t = parser.AssertOperator(Operator.Tag.RightPar);
                region = region | t.Region;

                //body
                Statement body = parser.AssertNode<Statement>();
                region = region | body.Region;

                return new ForStatement(init, condition, loop, body, region);
            }
            public override bool CanStartsWith (Token token)
            {
                Operator op = token as Operator;
                if (op == null || op.tag != Operator.Tag._for)
                    return false;
                return true;
            }
        }

        public override XmlNode ToXml (XmlDocument doc)
        {
            XmlNode result = base.ToXml(doc);

            if (init != null)
            {
                XmlNode node = doc.CreateElement("Init");
                node.AppendChild(init.ToXml(doc));
                result.AppendChild(node);
            }

            if (condition != null)
            {
                XmlNode node = doc.CreateElement("Condition");
                node.AppendChild(condition.ToXml(doc));
                result.AppendChild(node);
            }

            if (loop != null)
            {
                XmlNode node = doc.CreateElement("Loop");
                node.AppendChild(loop.ToXml(doc));
                result.AppendChild(node);
            }


            XmlNode bodyNode = doc.CreateElement("Body");
            bodyNode.AppendChild(body.ToXml(doc));
            result.AppendChild(bodyNode);

            return result;
        }
        public override void AcceptVisitor (IStatementVisitor visitor)
        {
            visitor.Visit(this);
        }
    }

    public class ReturnStatement : Statement
    {
        private Expression result;

        public Expression Result
        {
            get
            {
                return result;
            }
        }

        private ReturnStatement (Expression result, TextRegion region)
        {
            this.result = result;
            this.region = region;
            if (result != null)
                result.Parent = this;
        }

        public new class Factory : NodeFactory
        {
            public override Node Make (Parser parser)
            {
                //return
                Token t = parser.AssertOperator(Operator.Tag._return);
                TextRegion region = t.Region;

                //value
                Expression result = parser.TryGetNode<Expression>();

                //;
                t = parser.AssertOperator(Operator.Tag.Semicolon);

                region = t.Region | region;
                if (result != null)
                    region = region | result.Region;

                return new ReturnStatement(result, region);
            }
            public override bool CanStartsWith (Token token)
            {
                Operator op = token as Operator;
                if (op == null || op.tag != Operator.Tag._return)
                    return false;
                return true;
            }
        }

        public override XmlNode ToXml (XmlDocument doc)
        {
            XmlNode xmlResult = base.ToXml(doc);
            if (result != null)
            {
                XmlNode resultNode = doc.CreateElement("Result");
                resultNode.AppendChild(result.ToXml(doc));
                xmlResult.AppendChild(resultNode);
            }
            return xmlResult;
        }
        public override void AcceptVisitor (IStatementVisitor visitor)
        {
            visitor.Visit(this);
        }
    }

    public class BreakStatement : Statement
    {

        private BreakStatement (TextRegion region)
        {
            this.region = region;
        }

        public new class Factory : NodeFactory
        {
            public override Node Make (Parser parser)
            {
                //break
                Token t = parser.AssertOperator(Operator.Tag._break);
                TextRegion region = t.Region;

                //;
                t = parser.AssertOperator(Operator.Tag.Semicolon);

                region = t.Region | region;

                return new BreakStatement(region);
            }
            public override bool CanStartsWith (Token token)
            {
                Operator op = token as Operator;
                if (op == null || op.tag != Operator.Tag._break)
                    return false;
                return true;
            }
        }

        public override void AcceptVisitor (IStatementVisitor visitor)
        {
            visitor.Visit(this);
        }
    }

    public class ContinueStatement : Statement
    {

        private ContinueStatement (TextRegion region)
        {
            this.region = region;
        }

        public new class Factory : NodeFactory
        {
            public override Node Make (Parser parser)
            {
                //continue
                Token t = parser.AssertOperator(Operator.Tag._continue);
                TextRegion region = t.Region;

                //;
                t = parser.AssertOperator(Operator.Tag.Semicolon);

                region = t.Region | region;

                return new ContinueStatement(region);
            }
            public override bool CanStartsWith (Token token)
            {
                Operator op = token as Operator;
                if (op == null || op.tag != Operator.Tag._continue)
                    return false;
                return true;
            }
        }

        public override void AcceptVisitor (IStatementVisitor visitor)
        {
            visitor.Visit(this);
        }

    }
}