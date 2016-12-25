using System;
using System.Collections.Generic;
using System.Linq;

namespace factor10.Obj2Db.Formula
{
    public class EvaluateRpn
    {
        private readonly List<RpnItem> _original;

        private readonly Action[] _operatorEvaluator;

        public EvaluateRpn(
            Rpn rpn,
            List<NameAndType> entityFields = null)
        {
            _original = rpn.Result.ToList();

            var operatorEvaluator = new Dictionary<Operator, Action>
            {
                {Operator.Negation, () => calcUnary(x => -x)},
                {Operator.Not, () => calcUnary(x => x != 0 ? 1 : 0)},
                {Operator.Division, () => calcBinary((x, y) => x / y)},
                {Operator.Minus, () => calcBinary((x, y) => x - y)},
                {Operator.Multiplication, () => calcBinary((x, y) => x * y)},
                {Operator.Addition, () => calcBinary((x, y) => x + y, (x, y) => new RpnItemOperandString(x + y))},
                {Operator.And, () => calcBinary((x, y) => (x != 0) && (y != 0) ? 1 : 0)},
                {Operator.Or, () => calcBinary((x, y) => (x != 0) || (y != 0) ? 1 : 0)},
                {Operator.Question, calcQuestion},
                {Operator.NullCoalescing, calcNullCoalescing},
                {
                    Operator.Equal, () => calcBinary((x, y) => x == y ? 1 : 0,
                        (x, y) => new RpnItemOperandNumeric(string.CompareOrdinal(x, y) == 0 ? 1 : 0))
                },
                {
                    Operator.Lt, () => calcBinary((x, y) => x < y ? 1 : 0,
                        (x, y) => new RpnItemOperandNumeric(string.CompareOrdinal(x, y) < 0 ? 1 : 0))
                },
                {
                    Operator.EqLt, () => calcBinary((x, y) => x <= y ? 1 : 0,
                        (x, y) => new RpnItemOperandNumeric(string.CompareOrdinal(x, y) <= 0 ? 1 : 0))
                },
                {
                    Operator.Gt, () => calcBinary((x, y) => x > y ? 1 : 0,
                        (x, y) => new RpnItemOperandNumeric(string.CompareOrdinal(x, y) > 0 ? 1 : 0))
                },
                {
                    Operator.EqGt, () => calcBinary((x, y) => x >= y ? 1 : 0,
                        (x, y) => new RpnItemOperandNumeric(string.CompareOrdinal(x, y) >= 0 ? 1 : 0))
                },
                {
                    Operator.NotEqual, () => calcBinary((x, y) => x != y ? 1 : 0,
                        (x, y) => new RpnItemOperandNumeric(string.CompareOrdinal(x, y) != 0 ? 1 : 0))
                },
            };

            _operatorEvaluator = new Action[operatorEvaluator.Keys.Max(_ => (int) _) + 1];
            foreach (var p in operatorEvaluator)
                _operatorEvaluator[(int) p.Key] = p.Value;

            if (entityFields != null)
                for (var i = 0; i < _original.Count; i++)
                {
                    var itm = _original[i] as RpnItemOperandVariable;
                    if (itm == null)
                        continue;
                    var x = entityFields.FindIndex(_ => _.Name == itm.Name);
                    if (x < 0)
                        throw new ArgumentException($"Unknown varable '{itm.Name}'");
                    if (entityFields[x].Type == typeof(string))
                        _original[i] = new RpnItemOperandString2(() => _variables[x]?.ToString());
                    else
                        _original[i] = new RpnItemOperandNumeric2(() => (_variables[x] as IConvertible)?.ToDouble(null) ?? 0);
                }

        }

        private object[] _variables;
        private Stack<RpnItemOperand> _stack;

        public RpnItemOperand Eval(object[] variables = null)
        {
            _variables = variables;
            _stack = new Stack<RpnItemOperand>();

            var functionEvaluator = new Dictionary<string, Action<int>>
            {
                {"min", funcMin},
                {"max", funcMax},
                {"first", funcFirst},
                {"tail", funcTail},
            };

            foreach (var item in _original)
            {
                var operand = item as RpnItemOperand;
                if (operand != null)
                {
                    _stack.Push(operand);
                    continue;
                }

                var itemOperator = item as RpnItemOperator;
                if (itemOperator != null)
                {
                    _operatorEvaluator[(int) itemOperator.Operator]();
                    continue;
                }

                var itemFunction = item as RpnItemFunction;
                if (itemFunction != null)
                {
                    functionEvaluator[itemFunction.Name](itemFunction.ArgumentCount);
                    continue;
                }

                throw new Exception("What the hell happened here?");
            }

            return _stack.Single();
        }

        public RpnItemOperand TypeEval()
        {
            _stack = new Stack<RpnItemOperand>();

            var functionEvaluator = new Dictionary<string, Action<int>>
            {
                {"min", funcMin},
                {"max", funcMax},
                {"first", funcFirst},
                {"tail", funcTail},
            };

            foreach (var item in _original)
            {
                var operand = item as RpnItemOperand;
                if (operand != null)
                {
                    _stack.Push(operand);
                    continue;
                }

                var itemOperator = item as RpnItemOperator;
                if (itemOperator != null)
                {
                    switch (itemOperator.Operator)
                    {
                        case Operator.Addition:
                        {
                            var op2 = _stack.Pop();
                            var op1 = _stack.Pop();
                            _stack.Push(op1 is RpnItemOperandString || op2 is RpnItemOperandString
                                ? (RpnItemOperand) new RpnItemOperandString("")
                                : new RpnItemOperandNumeric(0));
                            break;
                        }
                        case Operator.Negation:
                        case Operator.Not:
                            _stack.Pop();
                            _stack.Push(new RpnItemOperandNumeric(0));
                            break;
                        case Operator.NullCoalescing:
                        case Operator.Question:
                            {
                                var op2 = _stack.Pop();
                            _stack.Pop();
                            _stack.Push(op2);
                            break;
                        }
                        default:
                            _stack.Pop();
                            _stack.Pop();
                            _stack.Push(new RpnItemOperandNumeric(0));
                            break;
                    }
                    continue;
                }

                var itemFunction = item as RpnItemFunction;
                if (itemFunction != null)
                {
                    functionEvaluator[itemFunction.Name](itemFunction.ArgumentCount);
                    continue;
                }

                throw new Exception("What the hell happened here?");
            }

            return _stack.Single();
        }
        private void funcTail(int argCount)
        {
            //var main = peekStr(2);
            //var pos = main.IndexOf(peekStr(1), StringComparison.Ordinal);
            //_work[_i] = new RpnItemOperandString(pos >= 0 ? main.Substring(pos + 1) : "");
        }

        private void funcFirst(int argCount)
        {
            RpnItemOperand result = null;
            while (argCount-- > 0)
            {
                var test = _stack.Pop();
                if (test.String != null)
                    result = test;
            }
            _stack.Push(result);
        }

        private void funcMin(int argCount)
        {
            RpnItemOperand result = null;
            while (argCount-- > 0)
            {
                var test = _stack.Pop();
                if (!(test is RpnItemOperandNumeric))
                    continue;
                if(result==null || test.Numeric < result.Numeric)
                    result = test;
            }
            _stack.Push(result);
        }

        private void funcMax(int argCount)
        {
            RpnItemOperand result = null;
            while (argCount-- > 0)
            {
                var test = _stack.Pop();
                if (!(test is RpnItemOperandNumeric))
                    continue;
                if (result == null || test.Numeric > result.Numeric)
                    result = test;
            }
            _stack.Push(result);
        }

        private void calcBinary(Func<double, double, double> funcNum, Func<string, string, RpnItemOperand> funcStr = null)
        {
            var op2 = _stack.Pop();
            var op1 = _stack.Pop();
            if (funcStr != null && (op1 is RpnItemOperandString || op2 is RpnItemOperandString))
                _stack.Push(funcStr(op1.String, op2.String));
            else
                _stack.Push(new RpnItemOperandNumeric(funcNum(op1.Numeric, op2.Numeric)));
        }

        private void calcUnary(Func<double, double> func)
        {
            _stack.Push(new RpnItemOperandNumeric(func(_stack.Pop().Numeric)));
        }

        private void calcQuestion()
        {
            var op2 = _stack.Pop();
            var op1 = _stack.Pop();
            _stack.Push(op1.Numeric == 0 ? new RpnItemOperandString(null) : op2);
        }

        private void calcNullCoalescing()
        {
            var op2 = _stack.Pop();
            var op1 = _stack.Pop();
            _stack.Push(op1.String != null ? op1 : op2);
        }

    }

}
