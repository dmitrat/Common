using System.Collections.Generic;
using System.Linq;
using MemoryPack;
using OutWit.Common.Abstract;
using OutWit.Common.Collections;
using OutWit.Common.Forms.Utils;

namespace OutWit.Common.Forms.Model
{
    /// <summary>
    /// What is currently in a form: a value per field key.
    /// </summary>
    /// <remarks>
    /// Its own type rather than a bare dictionary, because this is what crosses the wire in both
    /// directions — filled in by whoever draws the form, read by whoever declared it — and because
    /// the things worth doing to it are the same wherever it goes.
    /// </remarks>
    [MemoryPackable]
    public partial class FormValues : ModelBase
    {
        #region Constructors

        /// <remarks>
        /// Marked because there is more than one, and the generator will not choose:
        /// deserialisation fills the properties, so the empty one is the one it wants.
        /// </remarks>
        [MemoryPackConstructor]
        public FormValues()
        {
        }

        public FormValues(IDictionary<string, FormValue> values)
        {
            Values = new Dictionary<string, FormValue>(values);
        }

        #endregion

        #region Functions

        /// <summary>
        /// Everything the schema declares, at its defaults.
        /// </summary>
        /// <remarks>
        /// So that a form arrives complete: whoever draws it never has to decide what an absent
        /// value means, and the difference between a default and a choice survives in the state.
        /// </remarks>
        public static FormValues Of(FormSchema schema)
        {
            var values = new FormValues();

            // A mirror carries no default of its own: the original's stands, wherever the mirror
            // comes in the tree.
            foreach (var field in schema.Fields().Where(field => !field.IsMirror))
            {
                values.Values[field.Key] = new FormValue(field.DefaultValue,
                    field.DefaultValue == null ? FormValueState.NotAvailable : FormValueState.Default);
            }

            return values;
        }

        /// <summary>The value of a field, with where it came from, or nothing.</summary>
        public FormValue? Get(string key)
        {
            return Values.TryGetValue(key, out var value) ? value : null;
        }

        /// <summary>What is in a field, or nothing.</summary>
        public string? Text(string key) => Get(key)?.Value;

        /// <summary>
        /// Puts a value into a field, saying where it came from — somebody chose it unless said
        /// otherwise — and hands the bag back so that several can be set in a line.
        /// </summary>
        public FormValues Set(string key, string? value, FormValueState state = FormValueState.Set)
        {
            Values[key] = new FormValue(value, state);

            return this;
        }

        /// <summary>Whether there is a value for this key at all, whatever it is.</summary>
        public bool Has(string key) => Values.ContainsKey(key);

        /// <remarks>
        /// Written out rather than composed from <c>[ToString]</c>, because the one useful thing to
        /// say about a bag of values in a log is how many there are: the contents are somebody's
        /// patient data as often as not, and a model that prints them by default prints them into
        /// every log that ever touches it.
        /// </remarks>
        public override string ToString() => $"{Values.Count} values";

        #endregion

        #region Model Base

        public override bool Is(ModelBase modelBase, double tolerance = DEFAULT_TOLERANCE)
        {
            return modelBase is FormValues values && Values.Is(values.Values);
        }

        public override FormValues Clone()
        {
            return new FormValues(Values.ToDictionary(pair => pair.Key,
                pair => pair.Value.Clone()));
        }

        #endregion

        #region Properties

        [MemoryPackOrder(0)]
        public Dictionary<string, FormValue> Values { get; set; } = [];

        #endregion
    }
}
