# Bruhlang
A Interpreted programming language made in C#, inspired by Lua, C#, and JavaScript

# Notes
This language is very unstable and will sometimes not run even tho everything is by the grammar

Please do not use this as an example on how to create a programming language

This language is currently missing some features a standard programming language would have, as it is not finished

Currently there is no error handling at all, meaing you are allowed to break some of the grammatical rules

# How to run
1. Download the .exe file in the Releases
2. Open up a command prompt in the same directory as the exe
3. Run: bruhlang "path/to/file"
   * Or just drag the text file onto the .exe
   * Optionally, you may add the -d flag to get debug information

# Grammar
## Variable declaration
```cs
var foo = "bar";
```
## If statements
```cs
if statement {

} else if statement2 {

} else {

}
```
## For loops
```cs
for i = start, end {

}
```
Break and continue don't exist yet.
## While loops
```cs
while statement {

}
```
## Logical operators
```
if statement1 or statement2 and !statement3 {

}
```
## Operators
```
+ - * / ^ %
= += -= *= /= ^= %=
>= == <= < > != 
++ --
.. ..=
```
Last row is for strings
## Ternary operator
A "real" version doesnt exist yet, but you are allowed to do this, as logical operators don't only return booleans
```lua
var x = ((1 == 1) and "1 is equal to 1") or ((1 > 2) and "1 is bigger than 2");
print(x)
```
Would output "1 is equal to 1"

The parantheses shouldn't be required but currently are due to a bug in the AST creator.
## Arrays/dictionaries
Doesn't exist yet.
## Functions
```lua
function foo(bar) {
    return bar + 1;
}

print(foo(1))
```
## Standard library
```lua
print("hello", "world")
```
