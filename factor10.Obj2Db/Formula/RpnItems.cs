﻿using System;
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
        Mod,
        Or,
        And,
        Negation,
        Not,
        NullCoalescing,
        Concat
    }

    public abstract class RpnItem
    {
    }


    public abstract class RpnItemOperand : RpnItem
    {
        public abstract double Numeric { get; }
        public abstract string String { get; }
        public abstract bool IsNull { get; }
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
        public override bool IsNull => false;
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
        public override bool IsNull => Value == null;
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
        public override bool IsNull => false;
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
            "%",
            "|",
            "&",
            "-",
            "!",
            "??",
            "++"
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


    public class RpnItemOperandNumericNull : RpnItemOperandNumeric
    {
        public override bool IsNull => true;
    }

    public class RpnIndexedVariable : RpnItem
    {
        public readonly int Index;
        public readonly bool IsNumeric;

        public RpnIndexedVariable(int index, bool isNumeric)
        {
            Index = index;
            IsNumeric = isNumeric;
        }

        public RpnIndexedVariable(int index, Type type)
        {
            Index = index;
            IsNumeric = type != typeof(string) && type != typeof(Guid);
        }

        public RpnItemOperand Resolve(object[] variables)
        {
            if (IsNumeric)
            {
                var ic = variables[Index] as IConvertible;
                return ic != null
                    ? new RpnItemOperandNumeric(ic.ToDouble(null))
                    : new RpnItemOperandNumericNull();
            }
            return new RpnItemOperandString(variables[Index]?.ToString());
        }

    }

}
