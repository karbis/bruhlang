using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace bruhlang {
    internal class Parser {
        public static Scope Env = new Scope();
        public string? ErrorMsg;
        Node Tree;
        Scope CurrentScope;
        public FunctionContext CurrentContext;
        public dynamic? Result;
        public Parser(Node tree) {
            Tree = tree;
            CurrentScope = Env;
            Env.Variables = new GlobalEnvironment(this).Env;
            CurrentContext = new FunctionContext();

            ParseNode(tree);
            Result = CurrentContext.Result;
        }
        dynamic? ParseNode(Node node) {
            if (StoppedExecution()) return null;
            if (node.Type == "Keyword" && node.Value == "var") {
                CheckValidNode(node.Nodes[0], node, "Can not declare variable because variable name is invalid", "Identifier");
                if (StoppedExecution()) return null;
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
                var tuple = GetIdentifierScope(node);
                dynamic? scope = tuple.Value.scope;
                dynamic nodeVal = tuple.Value.nodeVal;
                if (scope == null) {
                    Error("Variable '" + nodeVal + "' doesn't exist", node);
                    return null;
                }
                return scope[nodeVal];
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
                foreach (Node node2 in node.Nodes) {
                    CurrentScope = new Scope(CurrentScope);
                    Scope assignedScope = CurrentScope;
                    dynamic? result = ParseNode(node2);

                    if (StoppedExecution()) return null;
                    //CurrentScope = assignedScope;
                }
            } else if (node.Type == "Keyword" && node.Value == "if") {
                dynamic? result = ParseNode(node.Nodes[0]);
                if (ToBool(result)) {
                    ParseNode(node.Nodes[1]);
                } else if (node.Nodes[^1].Type == "Keyword") {
                    ParseNode(node.Nodes[^1].Nodes[0]);
                }
            } else if (node.Type == "Equality") {
                return ParseEquality(node);
            } else if (node.Type == "Keyword" && node.Value == "for") {
                // check for identifiedr WHATEVER
                CheckValidNode(node.Nodes[0], node, "Can not start for loop because variable is invalid", "Identifier");
                CheckValidNode(node.Nodes[1], node, "Can not start for loop because missing keyword 'in'", "Keyword", "in");
                CheckValidNode(node.Nodes[3], node, "Can not start for loop because of a missing comma", "Separator");
                if (StoppedExecution()) return null;

                dynamic? start = ParseNode(node.Nodes[2]);
                CurrentScope = new Scope(CurrentScope);
                Scope assignedScope = CurrentScope;

                if (node.Nodes[5].Type != "Separator" && node.Nodes[5].Type != "Scope") {
                    Error("Invalid for loop structure, perhaps you're missing a comma?", node);
                    return null;
                }
                int scopeIndex = 5;
                dynamic? incrementAmount = 1d;
                if (node.Nodes[5].Type == "Separator") {
                    scopeIndex = 7;
                    incrementAmount = ParseNode(node.Nodes[6]);
                    if (StoppedExecution()) return null;
                }
                //CurrentScope.Variables[node.Nodes[0].Value] = start;

                dynamic? end = ParseNode(node.Nodes[4]);
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
                        assignedScope.Variables[node.Nodes[0].Value] = i;
                        ParseNode(node.Nodes[scopeIndex]);
                        if (StoppedExecution()) return null;
                    }
                } else {
                    for (double i = start; i >= end; i += incrementAmount) {
                        assignedScope.Variables[node.Nodes[0].Value] = i;
                        ParseNode(node.Nodes[scopeIndex]);
                        if (StoppedExecution()) return null;
                    }
                }
            } else if (node.Type == "Keyword" && node.Value == "while") {
                while (true) {
                    dynamic? result = ParseNode(node.Nodes[0]);
                    if (!ToBool(result) || StoppedExecution()) break;
                    ParseNode(node.Nodes[1]);
                }
            } else if (node.Type == "ShortHand") {
                var tuple = GetIdentifierScope(node.Nodes[0]);
                dynamic? scope = tuple.Value.scope;
                dynamic nodeVal = tuple.Value.nodeVal;
                if (node.Value == "++") {
                    scope[nodeVal]++;
                } else if (node.Value == "--") {
                    scope[nodeVal]--;
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
                    if (node2.Type == "Separator") continue;
                    CheckValidNode(node2, node, "Function argument list had invalid argument name");
                    if (j % 2 == 1 && node2.Type != "Separator") {
                        Error("Argument list did not have a comma", node);
                        return null;
                    }
                }
                Func<dynamic?[], int> funcReference = (dynamic?[] args) => {
                    CurrentScope = new Scope(CurrentScope);
                    Scope oldScope = CurrentScope;
                    int i = 0;
                    foreach (Node node2 in node.Nodes[0].Nodes) {
                        CurrentScope.Variables.Add(node2.Value, args.ElementAtOrDefault(i));
                        i++;
                    }
                    ParseNode(node.Nodes[1]);
                    //oldScope.Parent.Scopes.Remove(oldScope);
                    return 0;
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
                    if (i%2 == 1 && node2.Type != "Separator") {
                        Error("Argument list did not have a comma", node);
                        return null;
                    }
                    if (node2.Type == "Separator") continue;
                    args.Add(ParseNode(node2));
                    if (StoppedExecution()) return null;
                }
                FunctionContext oldContext = CurrentContext;
                CurrentContext = new FunctionContext();
                FunctionContext newContext = CurrentContext;

                var tuple = GetIdentifierScope(node.Nodes[0]);
                dynamic? scope = tuple.Value.scope;
                dynamic nodeVal = tuple.Value.nodeVal;
                scope[nodeVal](args.ToArray());
                CurrentContext = oldContext;
                return newContext.Result;
            } else if (node.Type == "Keyword" && node.Value == "return") {
                if (CurrentContext.Result != null) {
                    Error("Attempt to return more than once",node);
                    return null;
                }
                CurrentContext.Result = ParseNode(node.Nodes[0]);
                return null;
            } else if (node.Type == "Keyword" && node.Value == "and") {
                dynamic? result1 = ParseNode(node.Nodes[0]);
                dynamic? result2 = ParseNode(node.Nodes[1]);
                if ((ToBool(result1) && ToBool(result2)) || ToBool(result1)) {
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
            } else if (node.Type == "List") {
                if (node.Nodes.Count % 2 == 0 && node.Nodes.Count != 0) {
                    Error("Can not create list as there is a separator with no value following it in the list", node);
                    return null;
                }
                int i = 0;
                LangList list = new LangList();

                foreach (Node node2 in node.Nodes) {
                    if (i % 2 == 1 && node2.Type != "Separator") {
                        Error("Missing comma in list", node);
                        return null;
                    }
                    list.Insert(ParseNode(node2));
                    if (StoppedExecution()) return null;
                }

                return list;
            }
            return null;
        }

        bool ToBool(dynamic? val) {
            return !(val is null) && (!(val is bool) || (val is bool && val != false));
        }
        
        bool StoppedExecution() {
            return ErrorMsg != null || !(CurrentContext.Result is null);
        }

        (dynamic? scope, dynamic nodeVal)? GetIdentifierScope(Node identifier) { // returns the dictionary and the index name
            Scope scope = CurrentScope;
            dynamic? dict = null;
            dynamic index = "";
            while (scope!= null) {
                if (scope.Variables.ContainsKey(identifier.Value)) {
                    dict = scope.Variables;
                    index = identifier.Value;
                    break;
                }
                scope = scope.Parent;
            }
            if (dict is null) {
                return null;
            }

            if (dict[index] is LangList) {
                int i = -1;
                foreach (Node node in identifier.Nodes) {
                    i++;
                    if (node.Type != "Dot" && i%2 == 0) {
                        Error("Missing dot in list accessing", node);
                        return null;
                    }
                    if (node.Type == "Dot") continue;
                    if (dict is null || index is null) {
                        Error("Attempt to index nil", node);
                        return null;
                    }
                    dynamic val = ParseNode(node);
                    if (StoppedExecution()) return null;
                    dict = dict[index];
                    index = val;
                }
            }
            
            return (dict, index);
        }

        public void Error(string msg, Node node) {
            if (ErrorMsg != null) return;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(msg);
            Console.ForegroundColor = ConsoleColor.DarkCyan;
            //Console.WriteLine("-- Info --");
            string indent = "  ";
            if (node.AttachedToken != null) {
                Console.WriteLine(indent+"Line " + node.AttachedToken.LineCount + ", Character " + node.AttachedToken.CharacterCount);
                Console.WriteLine(indent+"Token " + node.AttachedToken.Value + ", Type " + node.AttachedToken.Type);
            }
            Console.WriteLine(indent+"Node " + node.Value + ", Type " + node.Type);
            //Console.WriteLine("----------");
            Console.ResetColor();
            ErrorMsg = msg;
            //throw new Exception(msg);
        }
        void CheckValidNode(Node node, Node currentNode, string task = "", string intendedType = "Identifier", string? intendedValue = null) {
            if (node.Type == intendedType) return;
            if (intendedValue != null && node.Value == intendedValue) return;
            Error(task + ". Expected " + intendedType + ", got " + node.Type, currentNode);
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
            var tuple = GetIdentifierScope(node.Nodes[0]); // tuples r stupid
            dynamic? scope = tuple.Value.scope;
            dynamic nodeVal = tuple.Value.nodeVal;
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
