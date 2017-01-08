using System;
using System.Collections.Generic;
using System.Linq;

namespace factor10.Obj2Db.Formula
{
    public class EvaluateRpn
    {
        private class FunctionDescriptor
        {
            public readonly int NumberOfArguments; // -1 variable
            public readonly Action<int> Evaluate;
//            public Action<int> TypeEvaluate;

            public FunctionDescriptor(int numberOfArguments, Action<int> evaluate, Action<int> typeEvaluate)
            {
                NumberOfArguments = numberOfArguments;
                Evaluate = evaluate;
//                TypeEvaluate = typeEvaluate;
            }

        }

        private class RpnItemAction : RpnItem
        {
            public Action Action;
        }

        private readonly Dictionary<string, FunctionDescriptor> _functions;

        private readonly List<RpnItem> _original;

        public EvaluateRpn(
            Rpn rpn,
            List<NameAndType> entityFields = null)
        {
            _functions = new Dictionary<string, FunctionDescriptor>
            {
                {"min", new FunctionDescriptor(-1, funcMin, null)},
                {"max", new FunctionDescriptor(-1, funcMax, null)},
                {"first", new FunctionDescriptor(-1, funcFirst, null)},
                {"str", new FunctionDescriptor(1, _ => push(_stack.Pop().String), null)},
                {"val", new FunctionDescriptor(1, _ => push(_stack.Pop().Numeric), null)},
                {"int", new FunctionDescriptor(1, _ => push((int) _stack.Pop().Numeric), null)},
                {"len", new FunctionDescriptor(1, _ => push(_stack.Pop().String?.Length), null)},
            };

            _original = rpn.Result.ToList();

            var operatorEvaluator = new Dictionary<Operator, Action>
            {
                {Operator.Negation, () => calcUnary(x => -x)},
                {Operator.Not, () => calcUnary(x => x != 0 ? 1 : 0)},
                {Operator.Division, () => calcBinary((x, y) => x / y)},
                {Operator.Minus, () => calcBinary((x, y) => x - y)},
                {Operator.Multiplication, () => calcBinary((x, y) => x * y)},
                {Operator.Addition, () => calcBinary((x, y) => x + y)},
                {Operator.Concat, concat},
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
                        _original[i] = new RpnItemOperandStringVariable(() => _variables[x]?.ToString());
                    else
                        _original[i] = new RpnItemOperandNumericVariable(
                            () => (_variables[x] as IConvertible)?.ToDouble(null) ?? 0,
                            () => _variables[x] == null);
                }

            typeEval();

            for (var i = 0; i < _original.Count; i++)
            {
                var itmOperator = _original[i] as RpnItemOperator;
                if (itmOperator != null)
                {
                    Action action = () => operatorEvaluator[itmOperator.Operator]();
                    _original[i] = new RpnItemAction {Action = action};
                    continue;
                }
                var itmFunction = _original[i] as RpnItemFunction;
                if (itmFunction != null)
                {
                    var argumentCount = itmFunction.ArgumentCount;
                    var descriptor = _functions[itmFunction.Name];
                    if (descriptor.NumberOfArguments != -1 && descriptor.NumberOfArguments != argumentCount)
                        throw new ArgumentException(
                            $"Wrong number of arguments to '{itmFunction.Name}' function. Expected {descriptor.NumberOfArguments}, but was {argumentCount}");
                    Action action = () => descriptor.Evaluate(argumentCount);
                    _original[i] = new RpnItemAction {Action = action};
                    continue;
                }
            }
        }

        private object[] _variables;
        private Stack<RpnItemOperand> _stack;

        public RpnItemOperand Eval(object[] variables = null)
        {
            _variables = variables;
            _stack = new Stack<RpnItemOperand>();

            foreach (var item in _original)
            {
                var operand = item as RpnItemOperand;
                if (operand != null)
                {
                    _stack.Push(operand);
                    continue;
                }

                var itemAction = item as RpnItemAction;
                if (itemAction != null)
                {
                    itemAction.Action();
                    continue;
                }

                throw new Exception("What the hell happened here?");
            }

            return _stack.Single();
        }

        public RpnItemOperand ResultingType { get; private set; }

        private void typeEval()
        {
            _stack = new Stack<RpnItemOperand>();

            for(var i=0;i<_original.Count;i++)
            {
                var item = _original[i];
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
                            if (op1 is RpnItemOperandString || op2 is RpnItemOperandString)
                            {
                                _original[i] = new RpnItemOperator(Operator.Concat);
                                push("");
                            }
                            else
                                push(0);
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
                            push(0);
                            break;
                    }
                    continue;
                }

                var itemFunction = item as RpnItemFunction;
                if (itemFunction != null)
                {
                    _functions[itemFunction.Name].Evaluate(itemFunction.ArgumentCount);
                    continue;
                }

                throw new Exception("What the hell happened here?");
            }

            ResultingType = _stack.Single();
        }

        private void funcFirst(int argCount)
        {
            RpnItemOperand result = null;
            while (argCount-- > 0)
            {
                var test = _stack.Pop();
                if (!test.IsNull)
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
                push(funcNum(op1.Numeric, op2.Numeric));
        }

        private void concat()
        {
            var op2 = _stack.Pop();
            var op1 = _stack.Pop();
            push(op1.String + op2.String);
        }

        private void calcUnary(Func<double, double> func)
        {
            push(func(_stack.Pop().Numeric));
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
            _stack.Push(!op1.IsNull ? op1 : op2);
        }

        private void push(double value)
        {
            _stack.Push(new RpnItemOperandNumeric(value));    
        }

        private void push(string value)
        {
            _stack.Push(new RpnItemOperandString(value));
        }

        private void push(double? value)
        {
            _stack.Push(value.HasValue
                ? new RpnItemOperandNumeric(value.Value)
                : new RpnItemOperandNumericNull());
        }

    }

}
