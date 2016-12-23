using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection.Emit;

namespace factor10.Obj2Db.Formula
{
    public class CompileRpn
    {
        private readonly List<RpnItem> _original;  

        private readonly Action<ILGenerator>[] _operatorEvaluator;

        public Func<object[], object> Evaluate;

        public CompileRpn(
            Rpn rpn,
            List<NameAndType> entityFields = null)
        {
            _original = rpn.Result.ToList();

            var operatorEvaluator = new Dictionary<Operator, Action<ILGenerator>>
            {
                {Operator.Negation, _ => _.Emit(OpCodes.Neg)},
                {Operator.Not, _ => _.Emit(OpCodes.Not)},
                {Operator.Division, _ => _.Emit(OpCodes.Div)},
                {Operator.Minus, _ => _.Emit(OpCodes.Sub)},
                {Operator.Multiplication, _ => _.Emit(OpCodes.Mul)},
                {Operator.Addition, _ => _.Emit(OpCodes.Add)}, // how to handle strings?
                {Operator.And, _ => _.Emit(OpCodes.And)},
                {Operator.Or, _ => _.Emit(OpCodes.Or)},
                {Operator.Equal, _ => _.Emit(OpCodes.Ceq)},
                {Operator.Lt, _ => _.Emit(OpCodes.Clt)},
                {Operator.Gt, _ => _.Emit(OpCodes.Cgt)},
                {
                    Operator.NotEqual, _ =>
                    {
                        _.Emit(OpCodes.Ceq);
                        _.Emit(OpCodes.Ldc_I4_0);
                        _.Emit(OpCodes.Ceq);
                    }
                },
                {
                    Operator.EqGt, _ =>
                    {
                        _.Emit(OpCodes.Clt);
                        _.Emit(OpCodes.Ldc_I4_0);
                        _.Emit(OpCodes.Ceq);
                    }
                },
                {
                    Operator.EqLt, _ =>
                    {
                        _.Emit(OpCodes.Cgt);
                        _.Emit(OpCodes.Ldc_I4_0);
                        _.Emit(OpCodes.Ceq);
                    }
                },
                //{Operator.Question, calcQuestion},
            };

            _operatorEvaluator = new Action<ILGenerator>[operatorEvaluator.Keys.Max(_ => (int) _) + 1];
            foreach (var p in operatorEvaluator)
                _operatorEvaluator[(int) p.Key] = p.Value;

            var method = new DynamicMethod("", typeof(object), new[] {typeof(object[])}, GetType().Module);
            var il = method.GetILGenerator();

            var typeStack = new Stack<Type>();

            foreach (var item in _original)
            {
                var itemNumeric = item as RpnItemOperandNumeric;
                if (itemNumeric != null)
                {
                    il.Emit(OpCodes.Ldc_R8, itemNumeric.Numeric);
                    typeStack.Push(typeof(double));
                    continue;
                }

                var itemOperator = item as RpnItemOperator;
                if (itemOperator != null)
                {
                    if (itemOperator.IsUnary())
                    {
                        typeStack.Pop();
                    }
                    else
                    {
                        typeStack.Pop();
                        typeStack.Pop();
                    }
                    operatorEvaluator[itemOperator.Operator](il);
                    typeStack.Push(typeof(double));
                    continue;
                }

                var itemVariable = item as RpnItemOperandVariable;
                if (itemVariable != null)
                {
                    var x = entityFields.FindIndex(_ => _.Name == itemVariable.Name);
                    if (x < 0)
                        throw new ArgumentException($"Unknown varable '{itemVariable.Name}'");
                    if (entityFields[x].Type != typeof(double))
                        throw new NotImplementedException();
                    else
                    {
                        il.Emit(OpCodes.Ldarg_0);
                        il.Emit(OpCodes.Ldc_I4, x);
                        il.Emit(OpCodes.Ldelem_Ref);
                        il.Emit(OpCodes.Unbox_Any, typeof(double));
                    }
                    //_original[i] = new RpnItemOperandNumeric2(() => (_variables[x] as IConvertible)?.ToDouble(null) ?? 0);
                    typeStack.Push(typeof(double));
                    continue;
                }

                throw new Exception("What the hell happened here?");
            }

            if(typeStack.Count!=1)
                throw new Exception("What the hell happened here?");

            il.Emit(OpCodes.Box, typeof(double));
            il.Emit(OpCodes.Ret);

            Evaluate = (Func<object[], object>) method.CreateDelegate(typeof(Func<object[], object>));
        }

    }

}
