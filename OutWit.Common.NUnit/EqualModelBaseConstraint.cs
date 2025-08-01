using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using NUnit.Framework.Constraints;
using OutWit.Common.Abstract;
using OutWit.Common.Values;

namespace OutWit.Common.NUnit
{ 
    public class EqualModelBaseConstraint : EqualConstraint
    {
        public EqualModelBaseConstraint(object? expected) 
            : base(expected)
        {
            Expected = expected;
        }

        public override ConstraintResult ApplyTo<TActual>(TActual actual)
        {
            return Expected.Check(actual)
                ? new ConstraintResult(this, actual, ConstraintStatus.Success)
                : new ConstraintResult(this, actual, ConstraintStatus.Failure);
        }
        
        private object? Expected { get; }
    }
}
