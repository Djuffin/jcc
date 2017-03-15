using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using jcc.ParserScope;
using jcc.CodeGenScope;
using System.Xml;

namespace jcc
{
    class Program
    {
        static void OutXml(Node node)
        {
            XmlDocument doc = new XmlDocument();
            doc.AppendChild(node.ToXml(doc));
            System.Console.ForegroundColor = ConsoleColor.Green;
            doc.Save(Console.Out);
        }


        static void Main (string[] args)
        {
            string filename;
            string outfile = "out.exe";
            if (args.Length < 1 || args.Length > 2)
            {
                Console.WriteLine("usage: jcc.exe codefile.c [output.exe]");
                return;
            }
            if (args.Length == 2)
                outfile = args[1];
            filename = args[0];
            if (!File.Exists(filename))
            {
                Console.WriteLine("can't find file {0}", filename);
                return;
            }
            string src = new StreamReader(filename).ReadToEnd();
            ErrorsCollection errors = new ErrorsCollection();
            Parser p = null;
            ILCodeGenerator cg = null;
            try
            {
                LexerScope.Lexer lex = new LexerScope.Lexer(src);
                LexerScope.Token[] toks = lex.GetTokens();
                p = new Parser(toks, typeof(Statement));
                ProgramNode root = p.AssertNode<ProgramNode>();
                cg = new ILCodeGenerator(root);
                cg.CreateAssembly(outfile);
                if (cg.ValidCode)
                    return;
            }
            catch (ParseException e)
            {
                errors.Add(new Error(e));
            }
            catch (AnalizeException e)
            {
                errors.Add(new Error(e));
            }
            if (p != null)
                errors = errors + p.Errors;
            if (cg != null)
                errors = errors + cg.Errors;
            Console.WriteLine("Compile failed");
            foreach (Error e in errors)
            {
                string region = e.Region.ToString(src);
                Console.WriteLine(e.Message + " " + region);
            }

        }
    }
}
