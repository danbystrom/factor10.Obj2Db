using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace factor10.Obj2Db.Formula
{

    public class Rpn
    {
        public readonly string Expression;
        public readonly List<RpnItem> Result = new List<RpnItem>();

        private readonly Stack<RpnItem> _stack = new Stack<RpnItem>();  
        private int _i;

        private readonly Regex _legalOperand = new Regex("^([[@-Z\\d._$#]+)", RegexOptions.IgnoreCase);

        public Rpn(string expression)
        {
            Expression = expression;
            var expectedOperand = true;
            while (_i < Expression.Length)
            {
                var token = getToken();
                if (token == null)
                    break;
                if (token is RpnItemOperand && expectedOperand)
                {
                    Result.Add(token);
                    expectedOperand = false;
                }
                else if (token is RpnItemFunction && expectedOperand)
                    _stack.Push(token);
                else if (token is RpnItemOperator)
                {
                    var op = (RpnItemOperator) token;
                    if (expectedOperand)
                        switch (op.Operator)
                        {
                            case Operator.LeftP:
                            case Operator.Not:
                                break;
                            case Operator.Minus:
                                op = new RpnItemOperator(Operator.Negation);
                                break;
                            case Operator.Addition:
                                continue;
                            default:
                                throw new Exception($"Expected operand but found operator {op.Operator}");
                        }
                    expectedOperand = op.Operator != Operator.RightP;
                    handleItemOperator(op);
                }
            }

            while (_stack.Any())
            {
                var item = _stack.Pop();
                if ((item as RpnItemOperator)?.Operator == Operator.LeftP)
                    throw new Exception("Open parenthisis");
                Result.Add(item);
            }

            for (var i = 1; i < Result.Count; i++)
                if (Result[i - 1] is RpnItemOperandNumeric && (Result[i] as RpnItemOperator)?.Operator == Operator.Negation)
                {
                    Result[i - 1] = new RpnItemOperandNumeric(-((RpnItemOperandNumeric) Result[i - 1]).Value);
                    Result.RemoveAt(i--);
                }
        }

        private void handleItemOperator(RpnItemOperator op)
        {
            switch (op.Operator)
            {
                case Operator.Comma:
                    while (!(_stack.Peek() is RpnItemFunction))
                        Result.Add(_stack.Pop());
                    ((RpnItemFunction) _stack.Peek()).ArgumentCount++;
                    return;
                case Operator.LeftP:
                    _stack.Push(op);
                    return;
                case Operator.RightP:
                    while (true)
                    {
                        var pop = _stack.Pop();
                        if (pop is RpnItemFunction)
                        {
                            Result.Add(pop);
                            return;
                        }
                        if ((pop as RpnItemOperator)?.Operator == Operator.LeftP)
                            return;
                        Result.Add(pop);
                    }
             }

            while (_stack.Any() && op.ShuntIt(_stack.Peek() as RpnItemOperator))
                Result.Add(_stack.Pop());
            _stack.Push(op);
        }

        private RpnItem getToken()
        {
            if (!moveToNextNonWhiteSpace())
                return null;

            var opInxex = RpnItemOperator.OpIndex(Expression, _i);
            if (opInxex >= 0)
            {
                _i += RpnItemOperator.Operators[opInxex].Length;
                return new RpnItemOperator((Operator) opInxex);
            }

            var m = _legalOperand.Matches(Expression.Substring(_i));
            if (m.Count == 1)
            {
                var x = m[0].Value;
                _i += x.Length;
                double num;
                if (double.TryParse(x, out num))
                    return new RpnItemOperandNumeric(num);
                if (!moveToNextNonWhiteSpace() || Expression[_i] != '(')
                    return x != "null"
                        ? (RpnItem) new RpnItemOperandVariable(x)
                        : new RpnItemOperandString(null);
                _i++;
                return new RpnItemFunction(x);
            }

            switch (Expression[_i++])
            {
                case '\"':
                case '\'':
                    return getString(Expression[_i - 1]);
            }

            return null;
        }

        private RpnItem getString(char terminator)
        {
            var start = _i;
            while (Expression[++_i] != terminator)
                ;
            return new RpnItemOperandString(Expression.Substring(start, ++_i - start - 1));
        }

        private bool moveToNextNonWhiteSpace()
        {
            while (_i < Expression.Length && Expression[_i] == ' ')
                _i++;
            return _i < Expression.Length;
        }

        public override string ToString()
        {
            return string.Join(" ", Result.Select(_ => _.ToString()));
        }

    }

}
