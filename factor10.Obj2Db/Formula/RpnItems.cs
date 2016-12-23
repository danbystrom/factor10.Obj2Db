using System;
using System.Collections.Generic;
using System.Globalization;

namespace factor10.Obj2Db.Formula
{
    public enum Operator
    {
        LeftP,
        RightP,
        Comma,
        Question,
        Equal,
        NotEqual,
        Gt,
        Lt,
        EqGt,
        EqLt,
        Addition,
        Minus,
        Multiplication,
        Division,
        Or,
        And,
        Negation,
        Not,
        NullCoalescing,
    }

    public abstract class RpnItem
    {
    }


    public abstract class RpnItemOperand : RpnItem
    {
        public abstract double Numeric { get; }
        public abstract string String { get; }
    }


    public class RpnItemOperandVariable : RpnItemOperand
    {
        public readonly string Name;

        public RpnItemOperandVariable(string name)
        {
            Name = name;
        }

        public override string ToString()
        {
            return Name;
        }

        public override double Numeric => 0;
        public override string String => "";
    }


    public class RpnItemOperandString : RpnItemOperand
    {
        public virtual string Value { get; }

        public RpnItemOperandString(string value)
        {
            Value = value;
        }

        protected RpnItemOperandString()
        { }

        public override string ToString()
        {
            return Value != null ? $"\"{Value}\"" : "null";
        }

        public override double Numeric
        {
            get
            {
                double val;
                double.TryParse(Value ?? "0", out val);
                return val;
            }
        }

        public override string String => Value;
    }


    public class RpnItemOperandNumeric : RpnItemOperand
    {
        public virtual double Value { get; }

        public RpnItemOperandNumeric(double value)
        {
            Value = value;
        }

        protected RpnItemOperandNumeric()
        { }

        public override string ToString()
        {
            return String;
        }

        public override double Numeric => Value;
        public override string String => Value.ToString(CultureInfo.InvariantCulture);
    }


    public class RpnItemOperator : RpnItem
    {
        public static readonly List<string> Operators  = new List<string>
        {
            "(",
            ")",
            ",",
            "?",
            "==",
            "!=",
            ">",
            "<",
            ">=",
            "<=",
            "+",
            "-",
            "*",
            "/",
            "|",
            "&",
            "-",
            "!",
            "??",
        };

        public readonly Operator Operator;

        public RpnItemOperator(Operator op)
        {
            Operator = op;
        }

        public static int OpIndex(string str, int i)
        {
            if (i < str.Length-1)
            {
                var j = Operators.IndexOf(str.Substring(i, 2));
                if (j >= 0)
                    return j;
            }
            return Operators.IndexOf(str.Substring(i, 1));
        }

        public bool IsUnary()
        {
            return Operator == Operator.Negation || Operator == Operator.Not;
        }

        public bool ShuntIt(RpnItem other)
        {
            if (IsUnary() || other == null || other is RpnItemFunction)
                return false;
            return other is RpnItemOperand || Operator <= ((RpnItemOperator) other).Operator;
        }

        public override string ToString()
        {
            return Operators[(int) Operator];
        }
    }


    public class RpnItemFunction : RpnItem
    {
        public readonly string Name;
        public int ArgumentCount = 1;

        public RpnItemFunction(string name)
        {
            Name = name;
        }

        public override string ToString()
        {
            return Name + "(";
        }
    }

    public class RpnItemOperandNumeric2 : RpnItemOperandNumeric
    {
        private readonly Func<double> _accessor;

        public override double Value => _accessor();

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

}
