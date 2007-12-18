#region License
/* ****************************************************************************
 * Copyright (c) Llewellyn Pritchard. 
 *
 * This source code is subject to terms and conditions of the Microsoft Public License. 
 * A copy of the license can be found in the License.html file at the root of this distribution. 
 * By using this source code in any fashion, you are agreeing to be bound by the terms of the 
 * Microsoft Public License.
 *
 * You must not remove this notice, or any other, from this software.
 * ***************************************************************************/
#endregion

#if R6RS
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Scripting.Generation;
using Microsoft.Scripting;
using System.Reflection;
using Microsoft.Scripting.Utils;
using System.Reflection.Emit;
using System.Collections;

namespace IronScheme.Runtime.R6RS
{
  public abstract class Condition : Exception
  {
    public override string ToString()
    {
      RecordTypeDescriptor rtd = Records.RecordRtd(this) as RecordTypeDescriptor;

      string tail = "";

      List<string> ii = new List<string>();
      for (int i = 0; i < rtd.fields.Count; i++)
      {
        ICallable a = Records.RecordAccessor(rtd, i) as ICallable;
        ii.Add(Builtins.DisplayFormat(a.Call(this)));
      }

      tail = string.Join(" ", ii.ToArray());

      return string.Format("{0,-12}{1}", GetType().Name.Replace("$", "&") + (tail.Length > 0 ? ":" : ""), tail);
    }

    public override string Message
    {
      get { return ToString(); }
    }
  }

  sealed class CompoundCondition : Condition
  {
    internal object[] conds;
    public CompoundCondition(object[] conds)
    {
      this.conds = conds;
    }

    public override string ToString()
    {
      List<string> c = new List<string>();
      foreach (object e in conds)
      {
        c.Add(e.ToString());
      }
      return string.Format("{0}", string.Join(Environment.NewLine, c.ToArray()));
    }
  }

  public class Conditions : Builtins
  {
    [Builtin("condition")]
    public static object Condition(params object[] conds)
    {
      if (conds.Length == 1)
      {
        return conds[0];
      }
      return new CompoundCondition(conds);
    }

    [Builtin("simple-condition")]
    public static object SimpleCondition(object cond)
    {
      if (cond is CompoundCondition)
      {
        return VectorToList(((CompoundCondition)cond).conds);
      }
      return List(cond);
    }

    [Builtin("condition?")]
    public static object IsCondition(object cond)
    {
      return cond is Exception;
    }

    //(condition-predicate rtd)
    [Builtin("condition-predicate")]
    public static object ConditionPredicate(object rtd)
    {
      RecordTypeDescriptor t = RequiresNotNull<RecordTypeDescriptor>(rtd);

      CallTarget1 p = delegate(object cond)
      {
        CallTarget1 recp = Delegate.CreateDelegate(typeof(CallTarget1), t.predicate) as CallTarget1;

        if (cond is CompoundCondition)
        {
          CompoundCondition cc = (CompoundCondition)cond;
          foreach (object ic in cc.conds)
          {
            if (IsTrue(recp(ic)))
            {
              return true;
            }
          }
          return false;
        }
        else
        {
          return recp(cond);
        }
      };

      return Closure.Make(Context, p);
    }

    //(condition-accessor rtd proc)
    [Builtin("condition-accessor")]
    public static object ConditionAccessor(object rtd, object proc)
    {
      RecordTypeDescriptor t = RequiresNotNull<RecordTypeDescriptor>(rtd);
      ICallable c = RequiresNotNull<ICallable>(proc);

      CallTarget1 p = delegate(object cond)
      {
        if (cond is CompoundCondition)
        {
          CompoundCondition cc = (CompoundCondition)cond;
          if (cc.conds.Length == 0)
          {
            // error?
            return false;
          }
          else
          {
            foreach (object e in cc.conds)
            {
              if (e.GetType() == t.type)
              {
                return c.Call(e);
              }
            }
            return false;
          }
        }
        else
        {
          return c.Call(cond);
        }
      };
      return Closure.Make(Context, p);
    }

  }
}
#endif