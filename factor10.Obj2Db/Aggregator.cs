using System;
using System.Collections.Generic;

namespace factor10.Obj2Db
{
    public class Aggregator
    {
        private readonly double[] _intermediateResult;
        private int _count;

        private readonly EntityAggregation[] _fieldAggregators;
        private readonly Action<object[]>[] _aggregations;

        public Aggregator(EntityClass entity, EntityAggregation[] fieldAggregators)
        {
            var dic = new Dictionary<AggregationType, Action<int, object>>
            {
                {AggregationType.Sum, (index, value) => _intermediateResult[index] += obj2Dbl(value, 0)},
                {AggregationType.Avg, (index, value) => _intermediateResult[index] += obj2Dbl(value, 0)},
                {AggregationType.Count, (index, value) => { }},
                {AggregationType.Max, (index, value) => _intermediateResult[index] = Math.Max(_intermediateResult[index], obj2Dbl(value, double.MinValue))},
                {AggregationType.Min, (index, value) => _intermediateResult[index] = Math.Min(_intermediateResult[index], obj2Dbl(value, double.MinValue))}
            };

            _intermediateResult = new double[100];
            _fieldAggregators = fieldAggregators;
            var q = new List<Action<object[]>>();
            for (var i = 0; i < fieldAggregators.Length; i++)
            {
                var sourceIndex = fieldAggregators[i].SourceIndex;
                var destinationIndex = i;
                var action = dic[fieldAggregators[i].AggregationType];
                Action<object[]> w = subresult =>
                    action(destinationIndex, (subresult[sourceIndex] as IConvertible)?.ToDouble(null) ?? 0);
                q.Add(w);
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
                result[_fieldAggregators[i].ResultSetIndex] = _fieldAggregators[i].CoherseType(_intermediateResult[i]);
        }

    }

}
