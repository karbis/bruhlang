using System.Text;
using System.Diagnostics;

namespace bruhlang {
    internal class Program {
        static void Main(string[] args) {
            string fileName = args.ElementAtOrDefault(0) ?? "code.txt";
            if (!File.Exists(fileName)) {
                Console.WriteLine("File doesn't exist!");
                return;
            }
            bool debugMode = (args.ElementAtOrDefault(1) ?? "") == "-d" || Debugger.IsAttached;
            List<Token> tokens = Lexer.Parse(File.ReadAllText(fileName));
            if (debugMode) {
                Console.WriteLine("  --  Tokens  --  ");
                Console.WriteLine(ReadTokens(tokens));
            }
            Node tree = TreeCreator.Create(tokens);
            if (debugMode) {
                Console.WriteLine("  --  AST  --  ");
                Console.WriteLine(ReadAST(tree));
                Console.WriteLine("  --  Runtime --  ");
            }
            Stopwatch watch = new Stopwatch();
            watch.Start();
            Parser result = new Parser(tree);
            watch.Stop();
            if (debugMode) {
                Console.WriteLine("");
                Console.WriteLine("  --  Performance  --  ");
                Console.WriteLine("Took " + watch.ElapsedMilliseconds + "ms to run");
                Tests.Test();
            } 
        }
        public static string ReadList(LangList inputList, int depth = 0) {
            string indent = new string(' ', depth * 4);
            string oldIndent = indent;
            string str = indent+"[\n";
            indent += "    ";
            foreach (KeyValuePair<dynamic, dynamic> variable in inputList.Get()) {
                if (variable.Key is null || variable.Value is null) continue;
                if (variable.Value is LangList) {
                    str += indent + variable.Key + ": " + ReadList(variable.Value, depth+1) + "\n";
                } else {
                    str += indent + variable.Key + ": " + variable.Value + "\n";
                }
            }
            str += oldIndent + "]";
            //foreach (Scope newScope in inputScope.Scopes) {
            //    str += ReadScopes(newScope, depth + 1);
            //}
            return str;
        }
        public static string ReadAST(Node inputNode, Node? currentNode = null, int depth = 0) {
            string indent = new string(' ', depth * 4);
            string str = indent + inputNode.Type + ": " + inputNode.Value + (currentNode == inputNode ? " !!!" : "") + "\n";
            foreach (Node node in inputNode.Nodes) {
                str += ReadAST(node, currentNode, depth+1);
            }
            return str;
        }
        public static string ReadTokens(List<Token> result) {
            string visual = "";
            foreach (Token token in result) {
                visual += "[" + token.Type + ": [" + token.Value + "]]\n";
            }
            return visual;
        }
    }
}
