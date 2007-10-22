using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using IronScheme.Runtime;

namespace IronScheme.Compiler
{
  static partial class Generator
  {
    static readonly MethodInfo Builtins_Cons = typeof(Builtins).GetMethod("Cons", new Type[] { typeof(object) });
    static readonly MethodInfo Builtins_Append = typeof(Builtins).GetMethod("Append");
    static readonly MethodInfo Builtins_ToImproper = typeof(Builtins).GetMethod("ToImproper");

    readonly static MethodInfo Macro_MakeVarArgX = typeof(Runtime.Macro).GetMethod("MakeVarArgX");
    readonly static MethodInfo Macro_Make = typeof(Runtime.Macro).GetMethod("Make");

    static readonly MethodInfo Closure_Make = typeof(Closure).GetMethod("Make");
    static readonly MethodInfo Closure_MakeVarArgsX = typeof(Closure).GetMethod("MakeVarArgX");

    static readonly MethodInfo Builtins_IsTrue = typeof(Builtins).GetMethod("IsTrue");
    static readonly MethodInfo Builtins_Display = typeof(Builtins).GetMethod("Display", new Type[] { typeof(object) });

  }
}