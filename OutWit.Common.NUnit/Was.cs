using NUnit.Framework;
using NUnit.Framework.Constraints;

namespace OutWit.Common.NUnit
{
    public class Was
    {
        public static EqualConstraint EqualTo(object? expected)
        {
            return new EqualModelBaseConstraint(expected);
        }

        public static NotEqualModelBaseExpression Not => new ();
        
        public static NullConstraint Null => Is.Null;
        public static TrueConstraint True => Is.True;
        public static FalseConstraint False => Is.False;
        public static EmptyConstraint Empty => Is.Empty;
    }
}
