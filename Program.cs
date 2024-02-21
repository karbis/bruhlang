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
                Console.WriteLine("  --  Environment  --  ");
                Console.WriteLine(ReadScopes(Parser.Env));
                Console.WriteLine("");
                Console.WriteLine("Took " + watch.ElapsedMilliseconds + "ms to run");
            } 
        }
        public static string ReadScopes(Scope inputScope, int depth = 0) {
            string indent = new string(' ', depth * 4);
            string str = indent + "Scope: \n";
            indent += "    ";
            foreach (KeyValuePair<string, dynamic?> variable in inputScope.Variables) {
                str += indent + variable.Key + ": " + variable.Value + "\n";
            }
            foreach (Scope newScope in inputScope.Scopes) {
                str += ReadScopes(newScope, depth + 1);
            }
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
