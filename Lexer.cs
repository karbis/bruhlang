using Microsoft.Win32.SafeHandles;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace bruhlang {
    public class Token {
        public string Type = "";
        public string Value;
        public int? CharacterCount = -1;
        public int? LineCount = -1;

        public Token(string type = "", string? value = null) {
            Type = type;
            Value = value;
        }
    }
    internal class Lexer {
        static string[] operators = ["+=", "-=", "*=", "/=", "^=", "%=", "..=", "!=", ">=", "<=", "+", "-", "*", "/", "^", "=", ")", ";", "}", "<", ">", "{", ",", "%", "..", "!", "("];
        static string[] doubleOperators = ["==", "!=", ">=", "<=", "+=", "-=", "*=", "/=", "^=", "%=", "++", "--", "..="];
        static string[] shortHandOperators = ["+=", "-=", "*=", "/=", "^=", "%=", "=", "..="];
        static string[] keywords = ["var", "if", "else", "for", "while", "true", "false", "nil", "and", "or", "function", "return"];
        static string[] shorterHandOperators = ["++", "--"];

        static Dictionary<string, string> escapedCharacters = new Dictionary<string, string> {
            {"\\\\", "\\"},
            {"\\'","'"},
            {"\\\"","\""},
            {"\\n","\n"},
            {"\\r","\r"},
            {"\\t","\t"},
            {"\\0","\0"},
            {"\\b","\b"},
            {"\\a","\a"},
            {"\\f","\f"},
            {"\\v","\v"},
        };
        public static List<Token> Parse(string statement) {
            List<Token> tokens = new List<Token>();

            string stack = "";
            int charCount = 0;
            int lineCount = 1;
            for (int i = 0; i < statement.Length; i++) {
                string tok = statement[i].ToString();
                stack += tok;
                charCount++;
                if (tok == "\n") {
                    lineCount++;
                    charCount = 0;
                }
                Token[]? result = ShouldCreateToken(stack);
                if (result != null) {
                    if (result[0] != null) {
                        result[0].LineCount = lineCount;
                        result[0].CharacterCount = charCount;
                        tokens.Add(result[0]);
                    }
                    if (result[1] != null) {
                        result[1].LineCount = lineCount;
                        result[1].CharacterCount = charCount;
                        tokens.Add(result[1]);
                    }
                    stack = "";
                    continue;
                }
            }
            if (!string.IsNullOrWhiteSpace(stack)) {
                tokens.Add(CreateToken(RemoveSpaces(stack)));
            }
            if (tokens[tokens.Count - 1].Type != "EOS") {
                tokens.Add(CreateToken(";"));
            }

            CorrectTokens(tokens);

            return tokens;
        }

        static Token CreateToken(string stack) {
            if (stack == "") return null;
            Token token = new Token();
            if (stack == ";") {
                token.Type = "EOL"; // end of line
            } else if (stack == ")") {
                token.Type = "EOS"; // end of statement
            } else if (stack == "true" || stack == "false") {
                token.Type = "Bool";
            } else if (stack == "nil") {
                token.Type = "Null";
            } else if (keywords.Contains(stack)) {
                token.Type = "Keyword";
            } else if (double.TryParse(stack, out _)) {
                token.Type = "Number";
            } else if (stack == "(") {
                token.Type = "StatementStart";
            } else if (shortHandOperators.Contains(stack)) {
                token.Type = "Assignment";
            } else if (stack == "{") {
                token.Type = "ScopeStart";
            } else if (stack == "}") {
                token.Type = "ScopeEnd";
            } else if (stack == ",") {
                token.Type = "TupleSeparator";
            } else if (stack == "!") {
                token.Type = "Negator";
            } else if (shorterHandOperators.Contains(stack)) {
                token.Type = "ShortHand";
            } else if (doubleOperators.Contains(stack) || stack == ">" || stack == "<") {
                token.Type = "Equality";
            } else if (operators.Contains(stack)) {
                token.Type = "Operator";
            } else {
                token.Type = "Identifier";
            }
            token.Value = stack;
            return token;
        }
        static string RemoveSpaces(string input) {
            return new string(input.ToCharArray().Where(c => !char.IsWhiteSpace(c) && c != '\n').ToArray());
        }
        static Token[]? ShouldCreateToken(string stack) {
            Token[] tokens = new Token[2];
            string noSpacesStack = RemoveSpaces(stack);
            if ((noSpacesStack.StartsWith("\"") || noSpacesStack.StartsWith("'"))) {
                if (StringProperlyEnded(noSpacesStack)) {
                    string str = stack.TrimStart();
                    tokens[0] = new Token("String", EscapeString(str.Substring(1,str.Length-2)));
                    return tokens;
                }
                return null;
            }
            if (noSpacesStack.StartsWith("(") || noSpacesStack.StartsWith("!")) {
                tokens[0] = CreateToken(noSpacesStack);
                return tokens;
            }
            string trimedStart = stack.TrimStart();
            if (trimedStart.StartsWith("-") && !trimedStart.StartsWith("- ")) {
                tokens[0] = new Token("UnaryMinus", "-");
                return tokens;
            }
            if (noSpacesStack == "{" || noSpacesStack == ")" || noSpacesStack == "}" || noSpacesStack == "=" || noSpacesStack == ".." || noSpacesStack == ";") {
                tokens[0] = CreateToken(noSpacesStack);
                return tokens;
            }
            if (double.TryParse(noSpacesStack, out _) && IsSeparator(stack[^1])) {
                tokens[0] = CreateToken(noSpacesStack);
                return tokens;
            }

            foreach (string op in operators) {
                if ((noSpacesStack.EndsWith(op) || noSpacesStack.StartsWith(op)) && (noSpacesStack != op || (IsSeparator(stack[0]) && IsSeparator(stack[^1]))) && (op != "-" || !noSpacesStack.StartsWith(op))) {
                    // ^ last statement is for negative numbers
                    tokens[0] = CreateToken(noSpacesStack.Substring(0, noSpacesStack.Length-op.Length));
                    tokens[1] = CreateToken(noSpacesStack.Substring(noSpacesStack.Length - op.Length, op.Length));
                    break;
                }
            }
            foreach (string key in keywords) {
                if ((noSpacesStack.EndsWith(key) && IsSeparator(stack[0])) || (noSpacesStack.StartsWith(key) && IsSeparator(stack[^1]))) {
                    tokens[0] = CreateToken(noSpacesStack.Substring(0, key.Length));
                    break;
                }
            }

            if (stack.EndsWith(" ") && tokens[0] == null) {
                tokens[0] = CreateToken(noSpacesStack);
            }

            if (tokens[0] == null && tokens[1] == null) {
                return null;
            }
            return tokens;
        }

        static bool IsSeparator(char c) {
            return c == ' ' || c == ')' || c == '}' || c == '(' || c == '{';
        }

        static bool StringProperlyEnded(string str) {
            if ((str.StartsWith("\"") && str.EndsWith("\"")) ||
               (str.StartsWith("'") && str.EndsWith("'"))) {
                if (str.Length == 1) return false;
                int escapedCount = 0;
                for (int i = str.Length - 1; i >= 0; i--) {
                    if (str.Substring(i, 1) == "\\") {
                        escapedCount += 1;
                        continue;
                    }
                    break;
                }
                return escapedCount % 2 == 0;
            }
            return false;
        }

        static string EscapeString(string str) {
            string newString = "";
            string stack = "";
            foreach (char c in str) {
                string cha = c.ToString();
                stack += cha;

                foreach (KeyValuePair<string, string> pair in escapedCharacters) {
                    if (!stack.EndsWith(pair.Key)) continue;
                    //if (IsEscaped(stack.Substring(0, stack.Length - 2))) continue;
                    newString += stack.Substring(0, stack.Length - pair.Key.Length);
                    newString += pair.Value;
                    stack = "";
                    break;
                }
            }
            newString += stack;

            return newString;
        }

        static void CorrectTokens(List<Token> tokens) {
            string stack = "";
            int i = -1;
            while (i < tokens.Count-1) {
                i++;
                Token token = tokens[i];
                if (token.Type != "Operator" && token.Type != "Equality" && token.Type != "UnaryMinus" && token.Type != "Assignment" && token.Type != "Negator") {
                    stack = "";
                    continue;
                };
                stack += token.Value;
                foreach (string op in doubleOperators) {
                    if (!stack.EndsWith(op) || (stack == op && token.Value == op)) continue;
                    stack = "";
                    tokens.RemoveAt(i);
                    tokens.RemoveAt(i - 1);
                    tokens.Insert(i-1, CreateToken(op));
                    i--;
                    break;
                }
            }
        }
    }
}
