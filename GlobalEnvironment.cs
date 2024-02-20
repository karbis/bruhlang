using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bruhlang {
    public class GlobalEnvironment {
        public Dictionary<string, dynamic?> Env = new Dictionary<string, dynamic?>();

        public GlobalEnvironment() {
            Env.Add("print", (Func<dynamic?[], dynamic?>)((dynamic?[] args) => {
                Console.WriteLine(string.Join(" ", args));
                return null;
            }));
        }
    }
}
