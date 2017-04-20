using System;
using System.Collections.Generic;
using System.Linq;

namespace factor10.Obj2Db.Formula
{
    public class EvaluateRpn
    {
        private class FunctionDescriptor
        {
            public readonly int NumberOfArguments; // -1 means variable argument count
            public readonly Action<Stack<RpnItemOperand>, int> Evaluate;
            public readonly Type Type;

            public FunctionDescriptor(int numberOfArguments, Action<Stack<RpnItemOperand>, int> evaluate, Type type)
            {
                NumberOfArguments = numberOfArguments;
                Evaluate = evaluate;
                Type = type;
            }

        }

        private class RpnItemAction : RpnItem
        {
            public Action<Stack<RpnItemOperand>> Action;
        }

        private readonly Dictionary<string, FunctionDescriptor> _functions;

        private readonly List<RpnItem> _original;

        public EvaluateRpn(
            Rpn rpn,
            List<NameAndType> entityFields = null)
        {
            _functions = new Dictionary<string, FunctionDescriptor>
            {
                {"min", new FunctionDescriptor(-1, funcMin, typeof(double))},
                {"max", new FunctionDescriptor(-1, funcMax, typeof(double))},
                {"first", new FunctionDescriptor(-1, funcFirst, typeof(string))},
                {"str", new FunctionDescriptor(1, (stack, args) => push(stack, stack.Pop().String), typeof(string))},
                {"val", new FunctionDescriptor(1, (stack, args) => push(stack, stack.Pop().Numeric), typeof(double))},
                {"int", new FunctionDescriptor(1, (stack, args) => push(stack, (int) stack.Pop().Numeric), typeof(double))},
                {"len", new FunctionDescriptor(1, (stack, args) => push(stack, stack.Pop().String?.Length), typeof(double))},
            };

            _original = rpn.Result.ToList();

            var operatorEvaluator = new Dictionary<Operator, Action<Stack<RpnItemOperand>>>
            {
                {Operator.Negation, stack => calcUnary(stack, x => -x)},
                {Operator.Not, stack => calcUnary(stack, x => x != 0 ? 1 : 0)},
                {Operator.Division, stack => calcBinary(stack, (x, y) => x / y)},
                {Operator.Minus, stack => calcBinary(stack, (x, y) => x - y)},
                {Operator.Multiplication, stack => calcBinary(stack, (x, y) => x * y)},
                {Operator.Addition, stack => calcBinary(stack, (x, y) => x + y)},
                {Operator.Concat, concat},
                {Operator.And, stack => calcBinary(stack, (x, y) => (x != 0) && (y != 0) ? 1 : 0)},
                {Operator.Or, stack => calcBinary(stack, (x, y) => (x != 0) || (y != 0) ? 1 : 0)},
                {Operator.Question, calcQuestion},
                {Operator.NullCoalescing, calcNullCoalescing},
                {
                    Operator.Equal, stack => calcBinary(stack, (x, y) => x == y ? 1 : 0,
                        (x, y) => new RpnItemOperandNumeric(string.CompareOrdinal(x, y) == 0 ? 1 : 0))
                },
                {
                    Operator.Lt, stack => calcBinary(stack, (x, y) => x < y ? 1 : 0,
                        (x, y) => new RpnItemOperandNumeric(string.CompareOrdinal(x, y) < 0 ? 1 : 0))
                },
                {
                    Operator.EqLt, stack => calcBinary(stack, (x, y) => x <= y ? 1 : 0,
                        (x, y) => new RpnItemOperandNumeric(string.CompareOrdinal(x, y) <= 0 ? 1 : 0))
                },
                {
                    Operator.Gt, stack => calcBinary(stack, (x, y) => x > y ? 1 : 0,
                        (x, y) => new RpnItemOperandNumeric(string.CompareOrdinal(x, y) > 0 ? 1 : 0))
                },
                {
                    Operator.EqGt, stack => calcBinary(stack, (x, y) => x >= y ? 1 : 0,
                        (x, y) => new RpnItemOperandNumeric(string.CompareOrdinal(x, y) >= 0 ? 1 : 0))
                },
                {
                    Operator.NotEqual, stack => calcBinary(stack, (x, y) => x != y ? 1 : 0,
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
                    _original[i] = new RpnIndexedVariable(x, entityFields[x].Type != typeof(string));
                }

            typeEval();

            for (var i = 0; i < _original.Count; i++)
            {
                var itmOperator = _original[i] as RpnItemOperator;
                if (itmOperator != null)
                {
                    Action<Stack<RpnItemOperand>> action = stack => operatorEvaluator[itmOperator.Operator](stack);
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
                    Action<Stack<RpnItemOperand>> action = stack => descriptor.Evaluate(stack, argumentCount);
                    _original[i] = new RpnItemAction {Action = action};
                    continue;
                }
            }
        }

        public List<int> GetVariableIndexes()
        {
            return _original.OfType<RpnIndexedVariable>().Select(_ => _.Index).ToList();
        }

        public RpnItemOperand Eval(object[] variables = null)
        {
            var stack = new Stack<RpnItemOperand>();

            foreach (var item in _original)
            {
                var operand = item as RpnItemOperand;
                if (operand != null)
                {
                    stack.Push(operand);
                    continue;
                }

                var itemAction = item as RpnItemAction;
                if (itemAction != null)
                {
                    itemAction.Action(stack);
                    continue;
                }

                var variable = item as RpnIndexedVariable;
                if (variable != null)
                {
                    stack.Push(variable.Resolve(variables));
                    continue;
                }

                throw new Exception("What the hell happened here?");
            }

            return stack.Single();
        }

        public RpnItemOperand ResultingType { get; private set; }

        private void typeEval()
        {
            var stack = new Stack<RpnItemOperand>();

            for(var i=0;i<_original.Count;i++)
            {
                var item = _original[i];
                var operand = item as RpnItemOperand;
                if (operand != null)
                {
                    stack.Push(operand);
                    continue;
                }

                var variable = item as RpnIndexedVariable;
                if (variable != null)
                {
                    stack.Push(variable.IsNumeric ? (RpnItemOperand) new RpnItemOperandNumeric(0) : new RpnItemOperandString(""));
                    continue;
                }

                var itemOperator = item as RpnItemOperator;
                if (itemOperator != null)
                {
                    switch (itemOperator.Operator)
                    {
                        case Operator.Addition:
                        {
                            var op2 = stack.Pop();
                            var op1 = stack.Pop();
                            if (op1 is RpnItemOperandString || op2 is RpnItemOperandString)
                            {
                                _original[i] = new RpnItemOperator(Operator.Concat);
                                push(stack, "");
                            }
                            else
                                push(stack, 0);
                            break;
                        }
                        case Operator.Negation:
                        case Operator.Not:
                            stack.Pop();
                            stack.Push(new RpnItemOperandNumeric(0));
                            break;
                        case Operator.NullCoalescing:
                        case Operator.Question:
                        {
                            var op2 = stack.Pop();
                            stack.Pop();
                            stack.Push(op2);
                            break;
                        }
                        default:
                            stack.Pop();
                            stack.Pop();
                            push(stack, 0);
                            break;
                    }
                    continue;
                }

                var itemFunction = item as RpnItemFunction;
                if (itemFunction != null)
                {
                    for (var j = 0; j < itemFunction.ArgumentCount; j++)
                        stack.Pop();
                    if(_functions[itemFunction.Name].Type==typeof(string))
                        push(stack, "");
                    else
                        push(stack, 0);
                    continue;
                }

                throw new Exception("What the hell happened here?");
            }

            ResultingType = stack.Single();
        }

        private void funcFirst(Stack<RpnItemOperand> stack, int argCount)
        {
            RpnItemOperand result = null;
            while (argCount-- > 0)
            {
                var test = stack.Pop();
                if (!test.IsNull)
                    result = test;
            }
            stack.Push(result);
        }

        private void funcMin(Stack<RpnItemOperand> stack, int argCount)
        {
            RpnItemOperand result = null;
            while (argCount-- > 0)
            {
                var test = stack.Pop();
                if (!(test is RpnItemOperandNumeric))
                    continue;
                if(result==null || test.Numeric < result.Numeric)
                    result = test;
            }
            stack.Push(result);
        }

        private void funcMax(Stack<RpnItemOperand> stack, int argCount)
        {
            RpnItemOperand result = null;
            while (argCount-- > 0)
            {
                var test = stack.Pop();
                if (!(test is RpnItemOperandNumeric))
                    continue;
                if (result == null || test.Numeric > result.Numeric)
                    result = test;
            }
            stack.Push(result);
        }

        private void calcBinary(Stack<RpnItemOperand> stack, Func<double, double, double> funcNum, Func<string, string, RpnItemOperand> funcStr = null)
        {
            var op2 = stack.Pop();
            var op1 = stack.Pop();
            if (funcStr != null && (op1 is RpnItemOperandString || op2 is RpnItemOperandString))
                stack.Push(funcStr(op1.String, op2.String));
            else
                push(stack, funcNum(op1.Numeric, op2.Numeric));
        }

        private void concat(Stack<RpnItemOperand> stack)
        {
            var op2 = stack.Pop();
            var op1 = stack.Pop();
            push(stack, op1.String + op2.String);
        }

        private void calcUnary(Stack<RpnItemOperand> stack, Func<double, double> func)
        {
            push(stack, func(stack.Pop().Numeric));
        }

        private void calcQuestion(Stack<RpnItemOperand> stack)
        {
            var op2 = stack.Pop();
            var op1 = stack.Pop();
            stack.Push(op1.Numeric == 0 ? new RpnItemOperandString(null) : op2);
        }

        private void calcNullCoalescing(Stack<RpnItemOperand> stack)
        {
            var op2 = stack.Pop();
            var op1 = stack.Pop();
            stack.Push(!op1.IsNull ? op1 : op2);
        }

        private void push(Stack<RpnItemOperand> stack, double value)
        {
            stack.Push(new RpnItemOperandNumeric(value));    
        }

        private void push(Stack<RpnItemOperand> stack, string value)
        {
            stack.Push(new RpnItemOperandString(value));
        }

        private void push(Stack<RpnItemOperand> stack, double? value)
        {
            stack.Push(value.HasValue
                ? new RpnItemOperandNumeric(value.Value)
                : new RpnItemOperandNumericNull());
        }

    }

}
