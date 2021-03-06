﻿using System;
using System.Collections.Generic;

namespace factor10.Obj2Db
{
    public class Aggregator
    {
        private readonly double[] _intermediateResult;
        private int _count;

        private readonly EntityAggregation[] _fieldAggregators;
        private readonly Action<object[]>[] _aggregations;

        public Aggregator(EntityAggregation[] fieldAggregators, int parentEffectiveFieldCount)
        {
            var dic = new Dictionary<AggregationType, Action<int, object>>
            {
                {AggregationType.Sum, (index, value) => _intermediateResult[index] += obj2Dbl(value, 0)},
                {AggregationType.Avg, (index, value) => _intermediateResult[index] += obj2Dbl(value, 0)},
                {AggregationType.Max, (index, value) => _intermediateResult[index] = Math.Max(_intermediateResult[index], obj2Dbl(value, double.MinValue))},
                {AggregationType.Min, (index, value) => _intermediateResult[index] = Math.Min(_intermediateResult[index], obj2Dbl(value, double.MinValue))}
            };

            _intermediateResult = new double[parentEffectiveFieldCount];
            _fieldAggregators = fieldAggregators;
            var q = new List<Action<object[]>>();
            for (var i = 0; i < fieldAggregators.Length; i++)
            {
                var sourceIndex = fieldAggregators[i].SourceIndex;
                var destinationIndex = i;
                if (fieldAggregators[i].AggregationType == AggregationType.Count)
                    q.Add(_ => { });
                else
                {
                    var action = dic[fieldAggregators[i].AggregationType];
                    if (_fieldAggregators[i].FieldType == typeof(double))
                        q.Add(subresult => action(destinationIndex, (double) subresult[sourceIndex]));
                    else  // could be improved, i guess
                        q.Add(subresult => action(destinationIndex, obj2Dbl(subresult[sourceIndex], 0)));
                }
            }
            _aggregations = q.ToArray();
        }

        private static double obj2Dbl(object obj, double def)
        {
            return (obj as IConvertible)?.ToDouble(null) ?? def;
        }

        public void Begin()
        {
            for (var i = 0; i < _fieldAggregators.Length; i++)
                switch (_fieldAggregators[i].AggregationType)
                {
                    case AggregationType.Min:
                        _intermediateResult[i] = double.MaxValue;
                        break;
                    case AggregationType.Max:
                        _intermediateResult[i] = double.MinValue;
                        break;
                    default:
                        _intermediateResult[i] = 0;
                        break;
                }
            _count = 0;
        }

        public void Update(object[] subresult)
        {
            if (subresult == null)
                return;
            for (var i = 0; i < _aggregations.Length; i++)
                _aggregations[i](subresult);
            _count++;
        }

        public void End(object[] result)
        {
            for (var i = 0; i < _fieldAggregators.Length; i++)
            {
                var fa = _fieldAggregators[i];
                object obj;
                switch (fa.AggregationType)
                {
                    case AggregationType.Min:
                    case AggregationType.Max:
                        obj = _count > 0
                            ? fa.CoherseType(_intermediateResult[i])
                            : null;
                        break;
                    case AggregationType.Avg:
                        obj = _count > 0
                            ? fa.CoherseType(_intermediateResult[i]/_count)
                            : null;
                        break;
                    case AggregationType.Count:
                        obj = _count;
                        break;
                    default:
                        obj = fa.CoherseType(_intermediateResult[i]);
                        break;
                }
                result[fa.ResultSetIndex] = fa.Evaluate(obj);
            }

        }

    }

}
