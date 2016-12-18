using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace factor10.Obj2Db.Formula
{
    public class EvaluateRpn
    {
        private readonly List<RpnItem> _original;  
        private List<RpnItem> _work;
        private int _i;

        private readonly Action[] _operatorEvaluator;

        public class RpnItemOperandNumeric2 : RpnItemOperandNumeric
        {
            private readonly Func<double> _accessor;

            public override double Value  => _accessor();

            public RpnItemOperandNumeric2(Func<double> accessor)
            {
                _accessor = accessor;
            }

            public override string ToString()
            {
                return String;
            }

       }

        public class RpnItemOperandString2 : RpnItemOperandString
        {
            private readonly Func<string> _accessor;
            public override string String => _accessor();

            public RpnItemOperandString2(Func<string> accessor)
            {
                _accessor = accessor;
            }

            public override string ToString()
            {
                return String;
            }

        }

        public EvaluateRpn(
            Rpn rpn,
            List<Tuple<string,Type>> entityFields = null)
        {
            _original = rpn.Result.ToList();

            var operatorEvaluator = new Dictionary<Operator, Action>
            {
                {Operator.Negation, () => calcUnary(x => -x)},
                {Operator.Not, () => calcUnary(x => x != 0 ? 1 : 0)},
                {Operator.Division, () => calcBinary((x, y) => x/y)},
                {Operator.Minus, () => calcBinary((x, y) => x - y)},
                {Operator.Multiplication, () => calcBinary((x, y) => x*y)},
                {Operator.Addition, () => calcBinary((x, y) => x + y, (x, y) => new RpnItemOperandString(x + y))},
                {Operator.And, () => calcBinary((x, y) => (x != 0) && (y != 0) ? 1 : 0)},
                {Operator.Or, () => calcBinary((x, y) => (x != 0) || (y != 0) ? 1 : 0)},
                {Operator.Question, calcQuestion},
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
                    var x = entityFields.FindIndex(_ => _.Item1 == itm.Name);
                    if (x < 0)
                        throw new ArgumentException($"Unknown varable '{itm.Name}'");
                    if(entityFields[x].Item2==typeof(string))
                        _original[i] = new RpnItemOperandString2(() => _variables[x]?.ToString());
                    else
                        _original[i] = new RpnItemOperandNumeric2(() => (_variables[x] as IConvertible)?.ToDouble(null) ?? 0);
                }

        }

        private object[] _variables;

        public RpnItemOperand Eval(object[] variables = null)
        {
            _variables = variables;
            _work = _original.ToList();

            var functionEvaluator = new Dictionary<string, Action>
            {
                {"min", funcMin},
                {"max", funcMax},
                {"first", funcFirst},
                {"tail", funcTail},
            };

            while (_work.Count > 1)
            {
                _i = _work.FindIndex(_ => !(_ is RpnItemOperand));
                var item = _work[_i];

                var itemOperator = item as RpnItemOperator;
                if (itemOperator != null)
                {
                    _operatorEvaluator[(int)itemOperator.Operator]();
                    continue;
                }

                var itemFunction = item as RpnItemFunction;
                if (itemFunction != null)
                {
                    functionEvaluator[itemFunction.Name]();
                    _work.RemoveRange(_i - itemFunction.ArgumentCount, itemFunction.ArgumentCount);
                    continue;
                }

                throw new Exception("What the hell happened here?");
            }

            return (RpnItemOperand) _work.Single();
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
