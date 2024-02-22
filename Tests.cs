﻿using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bruhlang {
    internal class Tests {
        public static void Test() {
            List<dynamic?> tests = new List<dynamic?> {
                "return 1 != 1 and '1 is not 1' or 1 == 1 and '1 is 1' or 'none matching'",
                "1 is 1",
                "return 'hello' .. 10 - 1 ..'world'",
                "hello9world",
                "return 5^3*(2+1)",
                375,
                "if false or true and !false { return true; }",
                true,
                "return 1- -1",
                2
            };


            for (int i = 0; i < tests.Count; i += 2) {
                if (RunText(tests[i]) != tests[i+1]) {
                    Console.ForegroundColor = ConsoleColor.DarkRed;
                    Console.WriteLine("Test #" + ((i/2) + 1) + " has failed.");
                    Console.ResetColor();
                }
            }
        }
        static dynamic? RunText(string text) {
            Parser result = new Parser(TreeCreator.Create(Lexer.Parse(text)));
            return result.Result ?? result.ErrorMsg;
        }
    }
}