/* ****************************************************************************
 *
 * Copyright (c) Microsoft Corporation. 
 *
 * This source code is subject to terms and conditions of the Microsoft Public License. A 
 * copy of the license can be found in the License.html file at the root of this distribution. If 
 * you cannot locate the  Microsoft Public License, please send an email to 
 * dlr@microsoft.com. By using this source code in any fashion, you are agreeing to be bound 
 * by the terms of the Microsoft Public License.
 *
 * You must not remove this notice, or any other, from this software.
 *
 *
 * ***************************************************************************/

using System;
using System.Reflection;
using System.Reflection.Emit;
using Microsoft.Scripting.Generation;
using System.Diagnostics;
using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting.Ast {
    public class UnaryExpression : Expression {
        private Expression /*!*/ _operand;
        private Type /*!*/ _type;

        public static MethodInfo Converter;

        internal UnaryExpression(AstNodeType nodeType, Expression /*!*/ expression, Type /*!*/ type)
            : base(nodeType) {
            _operand = expression;
            _type = type;
        }

        public Expression Operand {
            get { return _operand; }
          set { _operand = value; }
        }

        public override Type Type {
            get { return _type; }
        }

        public override void SetLoc(SourceSpan span)
        {
          if (span.IsValid)
          {
            Operand.SetLoc(span);
          }
        }

        public override bool IsConstant(object value)
        {
          if (value == null && NodeType == AstNodeType.Convert && !Type.IsValueType)
          {
            return Operand.IsConstant(value);
          }
          return base.IsConstant(value);
        }

        public override void Emit(CodeGen cg) {

          //if (NodeType == AstNodeType.Convert && _operand is BoundExpression)
          //{
          //  var be = _operand as BoundExpression;
          //  var tv = be.Variable.GetTypedVariable(Type);
          //  if (tv != null)
          //  {
          //    tv.
          //    return;
          //  }
          //}          
          if (NodeType == AstNodeType.Convert && _operand is ConstantExpression &&
            _operand.Type == typeof(bool) && _type == typeof(object))
          {
            _operand.EmitAs(cg, typeof(object));
          }
          else
          {
            _operand.Emit(cg);

            EmitLocation(cg);

            switch (NodeType)
            {
              case AstNodeType.Convert:
                if (_type != _operand.Type && _type.Name == "Callable" && Converter != null)
                {
                  cg.EmitCall(Converter);
                }
                else
                {
                  cg.EmitCast(_operand.Type, _type);
                }
                break;

              case AstNodeType.Not:
                if (_operand.Type == typeof(bool))
                {
                  cg.Emit(OpCodes.Ldc_I4_0);
                  cg.Emit(OpCodes.Ceq);
                }
                else
                {
                  cg.Emit(OpCodes.Not);
                }
                break;
              case AstNodeType.Negate:
                cg.Emit(OpCodes.Neg);
                break;
              case AstNodeType.OnesComplement:
                cg.Emit(OpCodes.Not);
                break;
              default:
                throw new NotImplementedException();
            }
          }
        }

        public override void EmitBranchFalse(CodeGen cg, Label label)
        {
          if (NodeType == AstNodeType.Not)
          {
            _operand.EmitBranchTrue(cg, label);
          }
          else
          {
            base.EmitBranchFalse(cg, label);
          }
        }

        public override void EmitBranchTrue(CodeGen cg, Label label)
        {
          if (NodeType == AstNodeType.Not)
          {
            _operand.EmitBranchFalse(cg, label);
          }
          else
          {
            base.EmitBranchTrue(cg, label);
          }
        }

        internal override void EmitAddress(CodeGen cg, Type asType) {
            if (NodeType == AstNodeType.Convert && Type == asType) {
                _operand.EmitAddress(cg, asType);
            } else {
                base.EmitAddress(cg, asType);
            }
        }


#if FULL
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1800:DoNotCastUnnecessarily")]
        protected override object DoEvaluate(CodeContext context) {
            object x = _operand.Evaluate(context);
            switch (NodeType) {
#if FULL
                case AstNodeType.Convert:
                    return Cast.Explicit(x, _type);
#endif
                case AstNodeType.Not:
                    if (x is bool) return (bool)x ? RuntimeHelpers.False : RuntimeHelpers.True;
                    if (x is int) return (int)~(int)x;
                    if (x is long) return (long)~(long)x;
                    if (x is short) return (short)~(short)x;
                    if (x is uint) return (uint)~(uint)x;
                    if (x is ulong) return (ulong)~(ulong)x;
                    if (x is ushort) return (ushort)~(ushort)x;
                    if (x is byte) return (byte)~(byte)x;
                    if (x is sbyte) return (sbyte)~(sbyte)x;
                    throw new InvalidOperationException("can't perform unary not on type " + CompilerHelpers.GetType(x).Name);

                case AstNodeType.Negate:
                    if (x is int) return (int)(-(int)x);
                    if (x is long) return (long)(-(long)x);
                    if (x is short) return (short)(-(short)x);
                    if (x is float) return -(float)x;
                    if (x is double) return -(double)x;
                    throw new InvalidOperationException("can't negate type " + CompilerHelpers.GetType(x).Name);

                default:
                    throw new NotImplementedException();
            }
        } 
#endif


        public void SetType(Type type)
        {
          _type = type;
        }
    }

    /// <summary>
    /// Factory methods.
    /// </summary>
    public static partial class Ast {
        public static UnaryExpression Convert(Expression expression, Type type) {
            Contract.RequiresNotNull(expression, "expression");
            Contract.RequiresNotNull(type, "type");

            if (!type.IsVisible) {
                throw new ArgumentException(String.Format(Resources.TypeMustBeVisible, type.FullName));
            }

            return new UnaryExpression(AstNodeType.Convert, expression, type);
        }

        public static Expression ConvertHelper(Expression expression, Type type) {
            Contract.RequiresNotNull(expression, "expression");
            Contract.RequiresNotNull(type, "type");

            if (expression.Type != type) {
              if (type.IsAssignableFrom(expression.Type) && type != typeof(object))
              {
              }
              else
              {
                expression = Convert(expression, type);
              }
            }
            return expression;
        }

        public static UnaryExpression Negate(Expression expression) {
            Contract.RequiresNotNull(expression, "expression");
            Contract.Requires(TypeUtils.IsArithmetic(expression.Type) && !TypeUtils.IsUnsigned(expression.Type), "expression", "Expression must be signed numeric type");

            return new UnaryExpression(AstNodeType.Negate, expression, expression.Type);
        }

        public static UnaryExpression Not(Expression expression) {
            Contract.RequiresNotNull(expression, "expression");
            Contract.Requires(TypeUtils.IsIntegerOrBool(expression.Type), "expression", "Expression type must be integer or boolean.");

            return new UnaryExpression(AstNodeType.Not, expression, expression.Type);
        }
    }
}
