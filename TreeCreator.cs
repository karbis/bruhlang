using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bruhlang {
    public class Node {
        public string Value = null;
        public List<Node> Nodes = new List<Node>();
        public string Type = "Unnamed";
        public Node Parent;
        public Token? AttachedToken;

        public Node(Node? parent = null, string? type = null, string? value = null, Token? token = null) {
            Parent = parent;
            Value = value;
            Type = type;
            AttachedToken = token;

            if (parent != null) {
                parent.Nodes.Add(this);
            }
        }
    }
    internal class TreeCreator {
        static Dictionary<string, int> operatorPriority = new Dictionary<string, int> {
            {"+", 1},
            {"-", 1},
            {"*", 2},
            {"/", 2},
            {"^", 3},
            {"=", 0},
            {"%",0}
        };
        public static Node Create(List<Token> tokens) {
            Node root = new Node(null, "Scope", "1", null);
            Node currentNode = root;
            Node currentScope = root;
            void AddOperator(Token token) {
                if (token.Type == "Assignment" && currentNode.Type == "Keyword" && currentNode.Value == "for") return;
                // if it has a lower presedance than the operator above, then swap them around
                int currentNodePriority = operatorPriority.ContainsKey(currentNode.Value ?? "") ? operatorPriority[currentNode.Value ?? ""] : 0;
                int tokenPriority = operatorPriority.ContainsKey(token.Value) ? operatorPriority[token.Value] : 0;
                if (currentNode.Type == "Operator" && currentNodePriority > tokenPriority) {
                    Node operatorNode = new Node(currentNode.Parent, "Operator", token.Value, token);
                    Node prevNode2 = currentNode.Parent.Nodes[^1];
                    currentNode = operatorNode;
                    MoveNode(prevNode2, currentNode);
                    return;
                }
                Node? prevNode = currentNode.Nodes.ElementAtOrDefault(currentNode.Nodes.Count - 1);
                Node assignmentNode = new Node(currentNode, token.Type, token.Value, token);
                currentNode = assignmentNode;

                if (prevNode == null) { return; }
                MoveNode(prevNode, currentNode);
            }

            int tokenI = -1;
            foreach (Token token in tokens) {
                tokenI++;
                //Console.WriteLine(Program.ReadAST(root, currentNode));
                //Console.WriteLine(token.Type);
                if ((currentNode.Type == "Equality" || currentNode.Type == "Operator" || (currentNode.Type == "Keyword" && (currentNode.Value == "and" || currentNode.Value == "or"))) && currentNode.Nodes.Count > 1 &&
                    (token.Type != "Equality" || currentNode.Type != "Keyword")) {
                    currentNode = currentNode.Parent;
                }

                if (token.Type == "Keyword") {
                    if (token.Value == "else") {
                        bool changed = false;
                        while (currentNode.Type != "Keyword" && currentNode.Value != "if" && currentNode.Type != "Scope") {
                            currentNode = currentNode.Parent;
                            changed = true;
                        }
                        if (changed) {
                            currentNode = currentNode.Parent;
                        }
                    }
                    Node keyNode = new Node(currentNode, "Keyword", token.Value, token);
                    currentNode = keyNode;

                    if (token.Value == "else") {
                        MoveNode(keyNode, currentNode.Parent.Nodes[^2]);
                    } else if (token.Value == "and" || token.Value == "or") {
                        MoveNode(keyNode.Parent.Nodes[^2], keyNode);
                        //Console.WriteLine(Program.ReadAST(root));
                        /*if (currentNode.Parent.Type == "Keyword" && currentNode.Parent.Value == (token.Value == "or" ? "and" : "or")) {
                            MoveNode(currentNode, currentNode.Parent.Parent);
                            //Console.WriteLine(Program.ReadAST(root));
                            MoveNode(currentNode.Parent.Nodes[^2], currentNode);
                            //Console.WriteLine(Program.ReadAST(root));
                            MoveNode(currentNode.Nodes[0], currentNode.Nodes[1]);
                            //Console.WriteLine(Program.ReadAST(root));
                        }*/
                        //TODO fix
                    }
                } else if (token.Type == "Identifier") {
                    new Node(currentNode, "Identifier", token.Value, token);
                    if (currentNode.Type == "Keyword" && currentNode.Value == "var") {
                        new Node(currentNode, "Identifier", token.Value, token);
                    }
                } else if (token.Type == "Operator" || token.Type == "Assignment" || token.Type == "Equality" || token.Type == "ShortHand") {
                    // if it has a lower presedance than the operator above, then swap them around
                    AddOperator(token);
                } else if (token.Type == "Number" || token.Type == "Bool" || token.Type == "String" || token.Type == "Null" || token.Type == "TupleSeparator") {
                    new Node(currentNode, token.Type, token.Value, token);
                } else if (token.Type == "EOL") {
                    // return back to the current scope
                    currentNode = currentScope;
                } else if (token.Type == "EOS") {
                    while (currentNode.Parent.Type == "Statement") {
                        currentNode = currentNode.Parent;
                    }
                    currentNode = currentNode.Parent;
                    if (currentNode.Type == "FunctionCall") {
                        currentNode = currentNode.Parent;
                    }
                } else if (token.Type == "StatementStart") {
                    // start new statement
                    Node statementNode = new Node(currentNode, "Statement", null, token);
                    currentNode = statementNode;
                    if (tokenI != 0 && tokens[tokenI - 1].Type == "Identifier" &&
                        (tokenI <= 1 || (tokenI > 1 && (tokens[tokenI-2].Type != "Keyword" || tokens[tokenI-2].Value != "function")))) {
                        // function call
                        Node functionCall = new Node(currentNode.Parent, "FunctionCall", null, token);
                        MoveNode(currentNode.Parent.Nodes[^3], functionCall);
                        MoveNode(statementNode, functionCall);
                    }
                } else if (token.Type == "ScopeStart") {
                    while ((currentNode.Parent.Type == "Keyword" || currentNode.Type == "Negate") && (currentNode.Type != "Keyword" || (currentNode.Value != "else" && currentNode.Value != "function"))) {
                        currentNode = currentNode.Parent;
                    }
                    Node statementNode = new Node(currentNode, "Scope", null, token);
                    if (currentNode.Type == "Keyword" && currentNode.Value == "function") {
                        statementNode.Value = "Returnable";
                    }
                    currentNode = statementNode;
                    currentScope = currentNode;
                } else if (token.Type == "ScopeEnd") {
                    while (currentNode.Parent.Type != "Scope") {
                        currentNode = currentNode.Parent;
                    }
                    currentNode = currentNode.Parent;
                    currentScope = currentNode;
                } else if (token.Type == "UnaryMinus") {
                    if (currentNode.Type == "Operator" && currentNode.Nodes.Count > 1) {
                        AddOperator(new Token("Operator", "+")); // turn the operation to + (-x)
                    }
                    Node minusOperator = new Node(currentNode, "UnaryMinus", null, token);
                    currentNode = minusOperator;
                } else if (token.Type == "Negator") {
                    Node negation = new Node(currentNode, "Negate", null, token);
                    currentNode = negation;
                }

                if ((token.Type != "UnaryMinus" && currentNode.Type == "UnaryMinus") || (token.Type != "Negator" && currentNode.Type == "Negate")) {
                    currentNode = currentNode.Parent;
                }
            }

            return root;
        }
        static void MoveNode(Node nodeToMove, Node newNodeLocation) {
            if (nodeToMove == newNodeLocation) return;
            nodeToMove.Parent.Nodes.Remove(nodeToMove);
            newNodeLocation.Nodes.Add(nodeToMove);
            nodeToMove.Parent = newNodeLocation;
        }
    }
}
