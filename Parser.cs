using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace bruhlang {
    public class Scope {
        public Dictionary<string, dynamic?> Variables = new Dictionary<string, dynamic?>();
        public Scope? Parent;
        public List<Scope> Scopes = new List<Scope>();
        public bool Dead = false; // preparing for threads, might not even need it

        public Scope(Scope? parent = null) {
            Parent = parent;
            if (parent != null) {
                parent.Scopes.Add(this);
            }
        }
    }
    internal class Parser {
        public static Scope Env = new Scope();
        public string? ErrorMsg;
        Node Tree;
        Scope CurrentScope;
        public Parser(Node tree) {
            Tree = tree;
            CurrentScope = Env;
            Env.Variables = new GlobalEnvironment().Env;

            ParseNode(tree);
        }
        dynamic? ParseNode(Node node) {
            if (ErrorMsg != null) return null;
            if (node.Type == "Keyword" && node.Value == "var") {
                CheckValidNode(node.Nodes[0], node, "Can not declare variable because variable name is invalid", "Identifier");
                CurrentScope.Variables.Add(node.Nodes[0].Value, null);
                if (node.Nodes[1].Value != "Identifier") {
                    ParseNode(node.Nodes[1]);
                }
            } else if (node.Type == "Assignment") {
                // errors to add: node.nodes[0] is not a identifier
                // node.nodes[0] is not defined
                ParseAssignment(node);
            } else if (node.Type == "Operator") {
                return ParseOperator(node);
            } else if (node.Type == "Identifier") {
                Scope scope = GetIdentifierScope(node.Value);
                if (scope == null) {
                    Error("Variable '" + node.Value + "' doesn't exist", node);
                    return null;
                }
                return scope.Variables[node.Value];
            } else if (node.Type == "Statement") {
                return ParseNode(node.Nodes[0]);
            } else if (node.Type == "UnaryMinus") {
                return -ParseNode(node.Nodes[0]);
            } else if (node.Type == "Number") {
                return double.Parse(node.Value);
            } else if (node.Type == "Bool") {
                return node.Value == "true";
            } else if (node.Type == "String") {
                return node.Value;
            } else if (node.Type == "Scope") {
                //if (node.Parent == null || node.Parent.Type != "Keyword" || node.Parent.Value != "for") {
                    CurrentScope = new Scope(CurrentScope);
                //}
                Scope assignedScope = CurrentScope;
                dynamic? returnVal = null;
                foreach (Node node2 in node.Nodes) {
                    dynamic? result = ParseNode(node2);
                    if (ErrorMsg != null) return null;
                    CurrentScope = assignedScope;

                    if (node.Value == "Returnable" && node2 == node.Nodes[^1]) {
                        returnVal = result;
                    }
                }

                if (node.Parent == null) return returnVal;
                //assignedScope.Parent.Scopes.Remove(assignedScope);

                assignedScope.Dead = true;
                if (CurrentScope.Scopes.Count == 0 || CurrentScope.Dead) {
                    Scope oldScope = CurrentScope;
                    CurrentScope = CurrentScope.Parent;
                    CurrentScope.Scopes.Remove(oldScope);
                }
                //CurrentScope = CurrentScope.Parent;
                return returnVal;
            } else if (node.Type == "Keyword" && node.Value == "if") {
                dynamic? result = ParseNode(node.Nodes[0]);
                if (ToBool(result)) {
                    ParseNode(node.Nodes[1]);
                } else if (node.Nodes[^1].Type == "Keyword" && node.Nodes[^1].Value == "else") {
                    ParseNode(node.Nodes[^1].Nodes[0]);
                }
            } else if (node.Type == "Equality") {
                return ParseEquality(node);
            } else if (node.Type == "Keyword" && node.Value == "for") {
                // check for identifiedr WHATEVER
                CheckValidNode(node.Nodes[0], node, "Can not start for loop because variable is invalid", "Identifier");
                CheckValidNode(node.Nodes[2], node, "Can not start for loop because of a missing comma", "TupleSeparator");
                if (ErrorMsg != null) return null;

                dynamic? start = ParseNode(node.Nodes[1]);
                CurrentScope = new Scope(CurrentScope);

                if (node.Nodes[4].Type != "TupleSeparator" && node.Nodes[4].Type != "Scope") {
                    Error("Invalid for loop structure, perhaps you're missing a comma?", node);
                    return null;
                }
                int scopeIndex = 4;
                dynamic? incrementAmount = 1;
                if (node.Nodes[4].Type == "TupleSeparator") {
                    scopeIndex = 6;
                    incrementAmount = ParseNode(node.Nodes[5]);
                    if (ErrorMsg != null) return null;
                }
                //CurrentScope.Variables[node.Nodes[0].Value] = start;

                dynamic? end = ParseNode(node.Nodes[3]);
                if (!(start is double) || !(end is double) || !(incrementAmount is double)) {
                    Error("For loop start/end/increment values weren't a number, types were:\nStart - " + start.GetType() + "\nEnd - " + end.GetType() + "\nIncrement - "
                        + incrementAmount.GetType(), node);
                    return null;
                } else {
                    start = (double)start;
                    end = (double)end;
                    incrementAmount = (double)incrementAmount;
                }

                if (incrementAmount > 0) {
                    for (double i = start; i <= end; i += incrementAmount) {
                        CurrentScope.Variables[node.Nodes[0].Value] = i;
                        ParseNode(node.Nodes[scopeIndex]);
                        if (ErrorMsg != null) return null;
                    }
                } else {
                    for (double i = start; i >= end; i += incrementAmount) {
                        CurrentScope.Variables[node.Nodes[0].Value] = i;
                        ParseNode(node.Nodes[scopeIndex]);
                        if (ErrorMsg != null) return null;
                    }
                }
                CurrentScope.Variables.Remove(node.Nodes[0].Value);
            } else if (node.Type == "Keyword" && node.Value == "while") {
                while (true) {
                    dynamic? result = ParseNode(node.Nodes[0]);
                    if (!ToBool(result) || ErrorMsg != null) break;
                    ParseNode(node.Nodes[1]);
                }
            } else if (node.Type == "ShortHand") {
                Dictionary<string, dynamic?> scope = GetIdentifierScope(node.Nodes[0].Value).Variables;
                if (node.Value == "++") {
                    scope[node.Nodes[0].Value]++;
                } else if (node.Value == "--") {
                    scope[node.Nodes[0].Value]--;
                }
            } else if (node.Type == "Negate") {
                return !ToBool(ParseNode(node.Nodes[0]));
            } else if (node.Type == "Keyword" && node.Value == "function") {
                bool anonymous = node.Nodes[0].Type == "Statement";
                string? funcName = anonymous ? null : node.Nodes[0].Value;  
                if (!anonymous) {
                    CheckValidNode(node.Nodes[0], node, "Can not create function because name is invalid", "Identifier");
                    node.Nodes.RemoveAt(0);
                }
                int j = -1;
                foreach (Node node2 in node.Nodes[0].Nodes) {
                    j++;
                    if (node2.Type == "TupleSeparator") continue;
                    CheckValidNode(node2, node, "Function argument list had invalid argument name");
                    if (ErrorMsg != null) return null;
                    if (j % 2 == 1 && node2.Type != "TupleSeparator") {
                        Error("Argument list did not have a comma", node);
                        return null;
                    }
                }
                Func<dynamic?[],dynamic?> funcReference = (dynamic?[] args) => {
                    CurrentScope = new Scope(CurrentScope);
                    Scope oldScope = CurrentScope;
                    int i = 0;
                    foreach (Node node2 in node.Nodes[0].Nodes) {
                        CurrentScope.Variables.Add(node2.Value, args.ElementAtOrDefault(i));
                        i++;
                    }
                    dynamic? result = ParseNode(node.Nodes[1]);
                    oldScope.Parent.Scopes.Remove(oldScope);
                    return result;
                };

                if (!anonymous) {
                    CurrentScope.Variables.Add(funcName, funcReference);
                } else {
                    return funcReference;
                }
            } else if (node.Type == "FunctionCall") {
                CheckValidNode(node.Nodes[0], node, "Can not call function because function is invalid", "Identifier");
                List<dynamic?> args = new List<dynamic?>();
                int i = -1;
                foreach (Node node2 in node.Nodes[1].Nodes) {
                    i++;
                    if (i%2 == 1 && node2.Type != "TupleSeparator") {
                        Error("Argument list did not have a comma", node);
                        return null;
                    }
                    args.Add(ParseNode(node2));
                    if (ErrorMsg != null) return null;
                }
                dynamic? result = GetIdentifierScope(node.Nodes[0].Value).Variables[node.Nodes[0].Value](args.ToArray());
                return result;
            } else if (node.Type == "Keyword" && node.Value == "return") {
                return ParseNode(node.Nodes[0]);
            } else if (node.Type == "Keyword" && node.Value == "and") {
                dynamic? result1 = ParseNode(node.Nodes[0]);
                dynamic? result2 = ParseNode(node.Nodes[1]);
                if (ToBool(result1) && ToBool(result2)) {
                    return result2;
                } else {
                    return result1;
                }
            } else if (node.Type == "Keyword" && node.Value == "or") {
                dynamic? result1 = ParseNode(node.Nodes[0]);
                dynamic? result2 = ParseNode(node.Nodes[1]);
                if (!ToBool(result1) && ToBool(result2)) {
                    return result2;
                } else {
                    return result1;
                }
            }
            return null;
        }

        bool ToBool(dynamic? val) {
            return !(val is null) && (!(val is bool) || (val is bool && val != false));
        }

        Scope? GetIdentifierScope(string name) {
            Scope scope = CurrentScope;
            while (scope!= null) {
                if (scope.Variables.ContainsKey(name)) {
                    return scope;
                }
                scope = scope.Parent;
            }
            return null;
        }
        void Error(string msg, Node node) {
            if (ErrorMsg != null) return;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(msg);
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine("-- Info --");
            if (node.AttachedToken != null) {
                Console.WriteLine("Line " + node.AttachedToken.LineCount + ", Character " + node.AttachedToken.CharacterCount);
                Console.WriteLine("Token " + node.AttachedToken.Value + ", Type " + node.AttachedToken.Type);
            }
            Console.WriteLine("Node " + node.Value + ", Type " + node.Type);
            Console.WriteLine("----------");
            Console.ResetColor();
            ErrorMsg = msg;
            //throw new Exception(msg);
        }
        void CheckValidNode(Node node, Node currentNode, string task = "", string intendedType = "Identifier", string? intendedValue = null) {
            if (node.Type == intendedType) return;
            if (intendedValue != null && node.Value == intendedValue) return;
            Error(task + ", expected " + intendedType + ", got " + node.Type, currentNode);
        }

        dynamic? ParseOperator(Node node) {
            dynamic? leftSide = ParseNode(node.Nodes[0]);
            dynamic? rightSide = ParseNode(node.Nodes[1]);
            switch (node.Value) {
                case "+":
                    if ((leftSide is string) || (rightSide is string)) {
                        Error("Attempt to add string", (leftSide is string) ? node.Nodes[0] : node.Nodes[1]);
                        return null;
                    }
                    return leftSide + rightSide;
                case "-":
                    return leftSide - rightSide;
                case "*":
                    return leftSide * rightSide;
                case "/":
                    return leftSide / rightSide;
                case "^":
                    return Math.Pow(leftSide, rightSide);
                case "%":
                    return leftSide % rightSide;
                case "..":
                    if (!(leftSide is string) && !(rightSide is string)) {
                        Error("Attempt to concatenate a non-string", !(leftSide is string) ? node.Nodes[0] : node.Nodes[1]);
                        return null;
                    }
                    if (leftSide.GetType() == typeof(double)) {
                        leftSide = leftSide.ToString();
                    }
                    if (rightSide.GetType() == typeof(double)) {
                        rightSide = rightSide.ToString();
                    }
                    return leftSide + rightSide;
            }
            return null;
        }
        bool? ParseEquality(Node node) {
            dynamic? leftSide = ParseNode(node.Nodes[0]);
            dynamic? rightSide = ParseNode(node.Nodes[1]);
            switch (node.Value) {
                case "==":
                    return leftSide == rightSide;
                case ">=":
                    return leftSide >= rightSide;
                case "<=":
                    return leftSide <= rightSide;
                case ">":
                    return leftSide > rightSide;
                case "<":
                    return leftSide < rightSide;
                case "!=":
                    return leftSide != rightSide;
            }
            return null;
        }
        void ParseAssignment(Node node) {
            dynamic? rightSide = ParseNode(node.Nodes[1]);
            string nodeVal = node.Nodes[0].Value;
            Dictionary<string, dynamic?> scope = GetIdentifierScope(nodeVal).Variables;
            switch (node.Value) {
                case "=":
                    scope[nodeVal] = rightSide;
                    break;
                case "+=":
                    if (scope[nodeVal] is string) {
                        Error("Attempt to add string", node.Nodes[0]);
                        break;
                    }
                    scope[nodeVal] += rightSide;
                    break;
                case "-=":
                    scope[nodeVal] -= rightSide;
                    break;
                case "*=":
                    scope[nodeVal] *= rightSide;
                    break;
                case "/=":
                    scope[nodeVal] /= rightSide;
                    break;
                case "^=":
                    dynamic? val = scope[nodeVal];
                    scope[nodeVal] = Math.Pow((double)val, (double)rightSide);
                    break;
                case "%=":
                    scope[nodeVal] %= rightSide;
                    break;
                case "..=":
                    if (!(scope[nodeVal] is string)) {
                        Error("Attempt to concatenate non-string", node.Nodes[0]);
                        break;
                    }
                    if (rightSide.GetType() == typeof(double)) {
                        rightSide = rightSide.ToString();
                    }
                    scope[nodeVal] += rightSide;
                    break;
            }
        }
    }
}
