using NUnit.Framework;
using NUnit.Framework.Constraints;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OutWit.Common.NUnit
{
    public class NotEqualModelBaseExpression
    {
        public Constraint EqualTo(object? expected)
        {
            return new NotConstraint(new EqualModelBaseConstraint(expected));
        }
        
        public NotConstraint Null => new NotConstraint(Is.Null);
        public NotConstraint True => new NotConstraint(Is.True);
        public NotConstraint False => new NotConstraint(Is.False);
        public NotConstraint Empty => new NotConstraint(Is.Empty);
    }
}
