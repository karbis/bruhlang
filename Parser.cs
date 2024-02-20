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
        Node Tree;
        Scope CurrentScope;
        public Parser(Node tree) {
            Tree = tree;
            CurrentScope = Env;
            Env.Variables = new GlobalEnvironment().Env;

            ParseNode(tree);
        }
        dynamic? ParseNode(Node node) {
            if (node.Type == "Keyword" && node.Value == "var") {
                if (node.Nodes[0].Type != "Identifier") {
                    Error("Error whilst declaring variable, variable declared isn't an identifier!");
                    return null;
                }
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
                return GetIdentifierScope(node.Value).Variables[node.Value];
            } else if (node.Type == "Statement") {
                /*dynamic? result = null;
                string? upcomingOperator = null;
                foreach (Node node2 in node.Nodes) {
                    if (node2.Type == "Keyword" && (node2.Value == "and" || node2.Value == "or")) {
                        if (upcomingOperator != null) return null;
                        upcomingOperator = node2.Value;
                        continue;
                    }
                    if (upcomingOperator == null && result == null) {
                        result = ParseNode(node2);
                        continue;
                    }
                    if (upcomingOperator != null) {
                        string binary = upcomingOperator;
                        upcomingOperator = null;
                        if (binary == "and" && !ToBool(result)) continue;
                        if (binary == "or" && ToBool(result)) break;
                        dynamic? result2 = ParseNode(node2);
                        if ((binary == "or" && !ToBool(result) && ToBool(result2)) || (binary == "and" && ToBool(result))) {
                            result = result2;
                        }
                    }
                }
                return result;*/
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
                    CurrentScope = assignedScope;

                    if (node.Value == "Returnable" && node2 == node.Nodes[^1]) {
                        returnVal = result;
                    }
                }

                if (node.Parent == null) return returnVal;
                //assignedScope.Parent.Scopes.Remove(assignedScope);

                // fix potential weird "scope" leak
                assignedScope.Dead = true;
                if (CurrentScope.Scopes.Count == 0 || CurrentScope.Dead) {
                    Scope oldScope = CurrentScope;
                    CurrentScope = CurrentScope.Parent;
                    CurrentScope.Scopes.Remove(oldScope);
                //    return null;
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
                dynamic? start = ParseNode(node.Nodes[1]);
                CurrentScope = new Scope(CurrentScope);
                //CurrentScope.Variables[node.Nodes[0].Value] = start;
                dynamic? end = ParseNode(node.Nodes[2]);
                for (var i = start; i <= end; i++) {
                    CurrentScope.Variables[node.Nodes[0].Value] = i;
                    ParseNode(node.Nodes[3]);
                }
                CurrentScope.Variables.Remove(node.Nodes[0].Value);
            } else if (node.Type == "Keyword" && node.Value == "while") {
                // check for identifiedr WHATEVER
                while (true) {
                    dynamic? result = ParseNode(node.Nodes[0]);
                    if (!ToBool(result)) break;
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
                    node.Nodes.RemoveAt(0);
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
                List<dynamic?> args = new List<dynamic?>();
                foreach (Node node2 in node.Nodes[1].Nodes) {
                    args.Add(ParseNode(node2));
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
        void Error(string msg) {
            throw new Exception(msg);
        }
        dynamic? ParseOperator(Node node) {
            dynamic? leftSide = ParseNode(node.Nodes[0]);
            dynamic? rightSide = ParseNode(node.Nodes[1]);
            switch (node.Value) {
                case "+":
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
                    if (rightSide.GetType() == typeof(double)) {
                        rightSide = rightSide.ToString();
                    }
                    scope[nodeVal] += rightSide;
                    break;
            }
        }
    }
}
