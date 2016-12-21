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
        private List<RpnItem> _work;
        private int _i;

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
                {Operator.NotEqual, _ =>
                    {
                        _.Emit(OpCodes.Ceq);
                        _.Emit(OpCodes.Ldc_I4_0);
                        _.Emit(OpCodes.Ceq);
                    }
                },
                {Operator.EqGt, _ =>
                    {
                        _.Emit(OpCodes.Clt);
                        _.Emit(OpCodes.Ldc_I4_0);
                        _.Emit(OpCodes.Ceq);
                    }
                },
                {Operator.EqLt, _ =>
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

           var method = new DynamicMethod("", typeof (object), new[] {typeof (object[])}, GetType().Module);
            var il = method.GetILGenerator();

            foreach (var item in _original)
            {
                var itemNumeric = item as RpnItemOperandNumeric;
                if (itemNumeric != null)
                {
                    il.Emit(OpCodes.Ldc_R8, itemNumeric.Numeric);
                    continue;
                }

                var itemOperator = item as RpnItemOperator;
                if (itemOperator != null)
                { 
                    operatorEvaluator[itemOperator.Operator](il);
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
                        continue;
                }

                //var itemFunction = item as RpnItemFunction;
                //if (itemFunction != null)
                //{
                //    functionEvaluator[itemFunction.Name]();
                //    _work.RemoveRange(_i - itemFunction.ArgumentCount, itemFunction.ArgumentCount);
                //    continue;
                //}

                throw new Exception("What the hell happened here?");
            }

            //ilPushVariable(0);
            //ilPushVariable(1);
            //il.Emit(OpCodes.Add);
            il.Emit(OpCodes.Box, typeof(double));
            il.Emit(OpCodes.Ret);
            Evaluate = (Func<object[], object>)method.CreateDelegate(typeof(Func<object[], object>));
        }

        private void funcTail()
        {
            var main = peekStr(2);
            var pos = main.IndexOf(peekStr(1), StringComparison.Ordinal);
            _work[_i] = new RpnItemOperandString(pos >= 0 ? main.Substring(pos + 1) : "");
        }

        private void funcFirst()
        {
            var argCount = ((RpnItemFunction) _work[_i]).ArgumentCount;
            string result = null;
            var i = argCount + 1;
            while (i > 1 && result == null)
                result = peekStr(--i);
            _work[_i] = _work[_i - i];
        }

        private void funcMin()
        {
            var argCount = ((RpnItemFunction)_work[_i]).ArgumentCount;
            var result = double.MaxValue;
            var i = argCount + 1;
            while (i > 1)
                result = Math.Min(result, peekNum(--i));
            _work[_i] = new RpnItemOperandNumeric(result);
        }

        private void funcMax()
        {
            var argCount = ((RpnItemFunction)_work[_i]).ArgumentCount;
            var result = double.MinValue;
            var i = argCount + 1;
            while (i > 1)
                result = Math.Max(result, peekNum(--i));
            _work[_i] = new RpnItemOperandNumeric(result);
        }

        private void calcBinary(Func<double, double, double> funcNum, Func<string, string, RpnItemOperand> funcStr = null)
        {
            if (funcStr != null && _work[_i - 2] is RpnItemOperandString && _work[_i - 1] is RpnItemOperandString)
                _work[_i] = funcStr(peekStr(2), peekStr(1));
            else
                _work[_i] = new RpnItemOperandNumeric(funcNum(peekNum(2), peekNum(1)));
            _work.RemoveRange(_i - 2, 2);
        }

        private void calcUnary(Func<double, double> func)
        {
            _work[_i] = new RpnItemOperandNumeric(func(peekNum(1)));
            _work.RemoveAt(_i - 1);
        }

        private double peekNum(int i)
        {
            return ((RpnItemOperand) _work[_i - i]).Numeric;
        }

        private string peekStr(int i)
        {
            return ((RpnItemOperand)_work[_i - i]).String;
        }

        private void calcQuestion()
        {
            _work[_i] = peekNum(2) == 0
                ? new RpnItemOperandString(null)
                : _work[_i] = _work[_i - 1];
            _work.RemoveRange(_i - 2, 2);
        }

    }

}
