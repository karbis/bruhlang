using System;
using System.Collections.Generic;
using System.Linq;
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
        Dictionary<dynamic, dynamic> Dict = new Dictionary<dynamic, dynamic>();

        public List<KeyValuePair<dynamic, dynamic>> Get() {
            List<KeyValuePair<dynamic, dynamic>> list = [.. Dict];
            list.Sort((KeyValuePair<dynamic, dynamic> a, KeyValuePair<dynamic, dynamic> b) => {
                if (a.Key is double && b.Key is double) {
                    return ((double)a.Key).CompareTo((double)b.Key);
                } else if (a.Key is double) {
                    return -1;
                } else if (b.Key is double) {
                    return 1;
                }
                return 0;
            });

            return list;
        }

        public dynamic? Index(dynamic i) {
            try {
                return Dict[i];
            }
            catch {
                return null;
            }
        }

        public void Set(dynamic i, dynamic v) {
            bool exists = !(Index(i) is null);
            if (v is null && exists) {
                Dict.Remove(i);
                return;
            }
            if (i is null) {
                return;
            }
            if (!exists) {
                Dict.Add(i, v);
                return;
            }
            Dict[i] = v;
        }
    }
}