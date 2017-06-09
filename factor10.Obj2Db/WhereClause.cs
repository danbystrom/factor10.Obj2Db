﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using factor10.Obj2Db.Formula;

namespace factor10.Obj2Db
{
    public sealed class WhereClause
    {
        private readonly EvaluateRpn _formula;
        public readonly bool IsBasedOnAggregation;

        public readonly List<Entity> UsedEntities;
        public readonly List<Entity> UnusedEntities;

        public WhereClause(string formula, List<Entity> fields)
        {
            _formula = EntityFormula.CreateEvaluator(formula, fields);
            var usedIndexes = _formula.GetVariableIndexes();
            IsBasedOnAggregation = EntityFormula.IsEvaluatorBasedOnAggregation(usedIndexes, fields);
            if (IsBasedOnAggregation)
                return;
            var usedResultIndexes = usedIndexes.Where(_ => _ < fields.Count).Select(_ => fields[_].ResultSetIndex).ToList();
            UsedEntities = new List<Entity>();
            UnusedEntities = new List<Entity>();
            foreach(var field in fields)
                if (usedResultIndexes.Contains(field.ResultSetIndex))
                    UsedEntities.Add(field);
                else 
                    UnusedEntities.Add(field);
        }

        public bool PassesFilterPre(object[] rowResult)
        {
            if (IsBasedOnAggregation)
                return true;  // let it thru and catch it in PassesFilterPost instead
            return _formula.Eval(rowResult).Numeric > 0;
        }

        public bool PassesFilterPost(object[] rowResult)
        {
            if (!IsBasedOnAggregation)
                return true;  // should have been handled PassesFilterPre 
            return _formula.Eval(rowResult).Numeric > 0;
        }

    }

}
