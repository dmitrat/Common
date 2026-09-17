using System.Collections;
using System.Reflection;
using OutWit.Common.Abstract;
using OutWit.Common.NUnit;

namespace OutWit.Common.Fat.Tests.Utils
{
    /// <summary>
    /// Checks that a <see cref="ModelBase"/> keeps its contract: <c>Clone</c> copies every
    /// settable property and <c>Is</c> looks at every one of them.
    /// </summary>
    /// <remarks>
    /// Each property is changed on a clone through reflection, which is also how
    /// <c>With(x =&gt; x.Property, value)</c> sets init-only properties. The sample must
    /// hold non-null nested models and non-empty lists, so there is something to change.
    /// </remarks>
    internal static class ModelBaseContract
    {
        #region Functions

        public static void AssertHolds<TModel>(TModel sample)
            where TModel : ModelBase
        {
            var clone = sample.Clone();
            Assert.That(clone, Is.Not.SameAs(sample));
            Assert.That(clone, Was.EqualTo(sample));

            var properties = typeof(TModel).GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(p => p.CanWrite).ToList();
            Assert.That(properties, Is.Not.Empty);

            foreach (var property in properties)
            {
                var original = property.GetValue(sample);
                if (original is ModelBase or IList)
                    Assert.That(property.GetValue(clone), Is.Not.SameAs(original), $"Clone() shares {property.Name} with the original");

                var changed = sample.Clone();
                property.SetValue(changed, Different(property.PropertyType, original));

                Assert.That(changed, Was.Not.EqualTo(sample), $"Is() ignores {property.Name}");
                Assert.That(changed.Clone(), Was.EqualTo(changed), $"Clone() drops {property.Name}");
            }
        }

        private static object? Different(Type type, object? value)
        {
            var underlying = Nullable.GetUnderlyingType(type);
            if (underlying != null)
                return value == null ? Different(underlying, Activator.CreateInstance(underlying)) : null;

            if (type == typeof(bool))
                return !(bool)value!;

            if (type.IsEnum)
            {
                var values = Enum.GetValues(type);
                int index = Array.IndexOf(values, value);
                return values.GetValue((index + 1) % values.Length);
            }

            if (type == typeof(string))
                return value + "*";

            if (type.IsPrimitive)
                return Convert.ChangeType(Convert.ToInt64(value) + 1, type);

            if (value is ModelBase model)
                return ChangeFirstProperty(model);

            if (value is IList { Count: > 0 } list)
            {
                var shorter = Array.CreateInstance(type.GetGenericArguments()[0], list.Count - 1);
                for (int i = 0; i < shorter.Length; i++)
                    shorter.SetValue(list[i], i);
                return shorter;
            }

            throw new NotSupportedException($"No way to change a {type.Name} holding {value ?? "null"}.");
        }

        private static ModelBase ChangeFirstProperty(ModelBase model)
        {
            var copy = model.Clone();
            var property = copy.GetType().GetProperties().First(p => p.CanWrite && (p.PropertyType.IsPrimitive || p.PropertyType.IsEnum));
            property.SetValue(copy, Different(property.PropertyType, property.GetValue(copy)));
            return copy;
        }

        #endregion
    }
}
