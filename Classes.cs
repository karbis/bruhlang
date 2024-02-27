using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace bruhlang {
    public class Scope {
        public Dictionary<string, dynamic?> Variables = new Dictionary<string, dynamic?>();
        public Scope? Parent;

        public Scope(Scope? parent = null) {
            Parent = parent;
        }
    }
    public class FunctionContext {
        public dynamic? Result;
    }
    public class Token {
        public string Type = "";
        public string? Value;
        public int? CharacterCount = -1;
        public int? LineCount = -1;

        public Token(string type = "", string? value = null) {
            Type = type;
            Value = value;
        }
    }

    public class LangList {
        public Dictionary<dynamic, dynamic> Dict = new Dictionary<dynamic, dynamic>();
        public List<dynamic> List = new List<dynamic>();

        public List<KeyValuePair<dynamic, dynamic>> Get() {
            List<KeyValuePair<dynamic, dynamic>> list = [];
            int i = 1;
            foreach (dynamic v in List) {
                list.Add(new KeyValuePair<dynamic, dynamic>(i, v));
                i++;
            }
            list = [.. list, .. Dict];

            return list;
        }

        public dynamic? Index(dynamic i) {
            if (i is double && double.IsInteger(i) && i > 0) {
                try {
                    return List[(int)(i-1)];
                }
                catch {
                    return null;
                }
            }
            try {
                return Dict[i];
            }
            catch {
                return null;
            }
        }

        public void Set(dynamic i, dynamic v) {
            bool exists = !(Index(i) is null);
            dynamic toUse = (i is double && double.IsInteger(i) && i > 0) ? List : Dict;
            bool isList = toUse is List<dynamic>; // can probably do some optimizations here
            if (v is null && exists) {
                toUse.Remove(i);
                return;
            }
            if (i is null) {
                return;
            }
            if (!exists) {
                if (isList) {
                    Insert(v, (int)i);
                } else {
                    toUse.Add(i, v);
                }
                return;
            }
            if (isList) {
                toUse[(int)i] = v;
            } else {
                toUse[i] = v;
            }
        }

        public void Insert(dynamic v, int? i = null) {
            if (i is null) {
                List.Add(v);
            } else {
                for (int j = List.Count+1; j < i; j++) {
                    List.Add(null);
                }
                if (i == List.Count + 1) {
                    List.Add(v);
                    return;
                }
                List.Insert((int)i, v);

            }
        }

        public dynamic? this[dynamic index] {
            get {
                return Index(index);
            }
            set {
                Set(index, value);
            }
        }
    }
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

    public class Identifier(dynamic parent, dynamic index) {
        public dynamic Parent = parent;
        public dynamic Index = index;
    }
}