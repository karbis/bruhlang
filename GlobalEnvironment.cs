using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace bruhlang {
    internal class GlobalEnvironment {
        public Dictionary<string, dynamic?> Env = new Dictionary<string, dynamic?>();

        public GlobalEnvironment(Parser parser) {
            Env.Add("print", (Func<dynamic?[], int>)((dynamic?[] args) => {
                Console.WriteLine(string.Join(" ", args));
                return 0;
            }));

            Env.Add("math", new Math(parser));
        }

        class Math : LangList {
            public Math(Parser parser) {
                Dict.Add("random", (Func<dynamic?[], int>)((dynamic?[] args) => {
                    if (args.Length == 0) {
                        parser.CurrentContext.Result = Random.Shared.NextDouble();
                    } else if (args.Length == 1) {
                        parser.CurrentContext.Result = Random.Shared.Next(1, (int)args[0]);
                    } else {
                        parser.CurrentContext.Result = Random.Shared.Next((int)args[0], (int)args[1]);
                    }

                    return 0;
                }));
            }
        }
    }
}
