using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bruhlang {
    internal class TreeCreator {
        static Dictionary<string, int> operatorPriority = new Dictionary<string, int> {
            {"+", 1},
            {"-", 1},
            {"*", 2},
            {"/", 2},
            {"^", 3},
            {"=", 0},
            {"%",0},
            {"..",0}
        };
        public static Node Create(List<Token> tokens) {
            Node root = new Node(null, "Scope", null, null);
            Node currentNode = root;
            Node currentScope = root;
            Node currentStatement = root;
            Node currentList = root;
            void AddOperator(Token token) {
                // if it has a lower presedance than the operator above, then swap them around
                Node? prevNode = currentNode.Nodes.ElementAtOrDefault(currentNode.Nodes.Count - 1);
                int currentNodePriority = operatorPriority.ContainsKey(prevNode.Value ?? "") ? operatorPriority[prevNode.Value ?? ""] : 0;
                int tokenPriority = operatorPriority.ContainsKey(token.Value) ? operatorPriority[token.Value] : 0;
                if (prevNode.Type == "Operator" && currentNodePriority < tokenPriority) {
                    Node operatorNode = new Node(prevNode, "Operator", token.Value, token);
                    Node prevNode2 = prevNode.Nodes[^2];
                    currentNode = operatorNode;
                    MoveNode(prevNode2, currentNode);
                    return;
                }
                /*if (token.Value == "..") {
                    while (currentNode.Type == "Operator") {
                        currentNode = currentNode.Parent;
                    }
                    Node statementNode = new Node(currentNode, "Statement", null, CloneToken(token, "Statement"));
                    MoveNode(currentNode.Nodes[^2], statementNode);
                    currentNode = statementNode;
                    prevNode = statementNode.Nodes[^1];
                }*/
                Node assignmentNode = new Node(currentNode, token.Type, token.Value, token);

                //if (token.Type == "Assignment" && currentNode.Type == "Keyword" && currentNode.Value == "for") return;
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
                    (token.Type != "Equality" || currentNode.Type != "Keyword") && (currentNode.Type != "Equality" || token.Type != "Operator")) {
                    currentNode = currentNode.Parent;
                }
                if (currentNode.Type == "Identifier" && token.Type != "Dot" && currentNode.Nodes.Count != 0 && currentNode.Nodes[^1].Type != "Dot") {
                    currentNode = currentNode.Parent;
                }

                if (token.Type == "Keyword") {
                    if (token.Value == "else") {
                        while (currentNode.Type != "Keyword" && currentNode.Value != "if" && currentNode.Type != "Scope") {
                            currentNode = currentNode.Parent;
                        }
                        
                    }
                    Node keyNode = new Node(currentNode, "Keyword", token.Value, token);
                    currentNode = keyNode;

                    if (token.Value == "else") {
                        MoveNode(keyNode, currentNode.Parent.Nodes[^2]);
                        while (currentNode.Parent.Nodes.Count > 3) {
                            MoveNode(currentNode, currentNode.Parent.Nodes[^2].Nodes[0]);
                        }
                    } else if (token.Value == "and" || token.Value == "or") {
                        MoveNode(keyNode.Parent.Nodes[^2], keyNode);
                    } else if (token.Value == "in") {
                        currentNode = keyNode.Parent;
                    }
                } else if (token.Type == "Identifier") {
                    new Node(currentNode, "Identifier", token.Value, token);
                    if (currentNode.Type == "Keyword" && currentNode.Value == "var") {
                        new Node(currentNode, "Identifier", token.Value, token);
                    }
                } else if (token.Type == "Operator" || token.Type == "Assignment" || token.Type == "Equality" || token.Type == "ShortHand") {
                    // if it has a lower presedance than the operator above, then swap them around
                    AddOperator(token);
                } else if (token.Type == "Number" || token.Type == "Bool" || token.Type == "String" || token.Type == "Null" || token.Type == "Separator") {
                    new Node(currentNode, token.Type, token.Value, token);
                } else if (token.Type == "EOL") {
                    // return back to the current scope
                    currentNode = currentScope;
                } else if (token.Type == "EOS") {
                    currentNode = currentStatement.Parent;
                    if (currentNode.Type == "FunctionCall") {
                        currentNode = currentNode.Parent;
                    }
                } else if (token.Type == "StatementStart") {
                    // start new statement
                    Node statementNode = new Node(currentNode, "Statement", null, token);
                    currentNode = statementNode;
                    currentStatement = currentNode;
                    if (currentNode.Parent.Nodes.Count > 1 && currentNode.Parent.Nodes[^2].Type == "Identifier" &&
                        (tokenI <= 1 || (tokenI > 1 && (tokens[tokenI-2].Type != "Keyword" || tokens[tokenI-2].Value != "function")))) {
                        // function call
                        Node functionCall = new Node(currentNode.Parent, "FunctionCall", null, token);
                        MoveNode(currentNode.Parent.Nodes[^3], functionCall);
                        MoveNode(statementNode, functionCall);
                    }
                } else if (token.Type == "ScopeStart") {
                    while ((currentNode.Parent.Type == "Keyword" || currentNode.Type == "Negate") && (currentNode.Type != "Keyword" || (currentNode.Value != "else" && currentNode.Value != "if" && currentNode.Value != "function"))) {
                        currentNode = currentNode.Parent;
                    }
                    Node statementNode = new Node(currentNode, "Scope", null, token);
                    currentNode = statementNode;
                    currentScope = currentNode;
                } else if (token.Type == "ScopeEnd") {
                    while (currentNode.Parent.Type != "Scope") {
                        currentNode = currentNode.Parent;
                    }
                    currentNode = currentNode.Parent;
                    currentScope = currentNode;
                } else if (token.Type == "UnaryMinus") {
                    Node? inspectingNode = currentNode.Nodes.ElementAtOrDefault(currentNode.Nodes.Count-1);
                    if (inspectingNode != null && currentNode.Type != "Operator" && (inspectingNode.Type == "Operator" || inspectingNode.Type == "Statement") && (inspectingNode.Type == "Statement" || inspectingNode.Nodes.Count > 1)) {
                        AddOperator(CloneToken(token, "Operator", "+")); // turn the operation to + (-x)
                    }
                    Node minusOperator = new Node(currentNode, "UnaryMinus", null, token);
                    currentNode = minusOperator;
                } else if (token.Type == "Negator") {
                    Node negation = new Node(currentNode, "Negate", null, token);
                    currentNode = negation;
                } else if (token.Type == "ListStart") {
                    if (currentNode.Nodes.Count != 0 && (currentNode.Nodes[^1].Type == "Identifier" || currentNode.Nodes[^1].Type == "Statement")&& currentNode.Type != "Assignment") {
                        if (currentNode.Nodes[^1].Type == "Statement") {
                            // todo: currently, you are only allowed to index identifiers
                            // maybe make a temp identifier system? ALso idk about function calls. whatever.
                        }
                        currentList = currentNode.Nodes[^1];
                        currentNode = currentList;
                        new Node(currentNode, "Dot", null, token);
                        continue;
                    }
                    currentList = new Node(currentNode, "List", null, token);
                    currentNode = currentList;
                } else if (token.Type == "ListEnd") {
                    currentNode = currentList.Parent;
                } else if (token.Type == "Dot") {
                    if (currentNode.Type != "Identifier") {
                        currentNode = currentNode.Nodes[^1];
                    }
                    new Node(currentNode, "Dot", null, token);
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
        static Token CloneToken(Token token, string newType, string? newValue = null) {
            Token newToken = new Token(newType, newValue);
            newToken.CharacterCount = token.CharacterCount;
            newToken.LineCount = token.LineCount;
            return newToken;
        }
    }
}
