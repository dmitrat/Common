using System;
using OutWit.Common.Abstract;
using OutWit.Common.Attributes;
using OutWit.Common.Values;

namespace OutWit.Common.MVVM.Navigation.Sample.Core.Models
{
    /// <summary>
    /// One recording in the sample's store.
    /// </summary>
    public sealed class Study : ModelBase
    {
        #region Constructors

        public Study(int id, string patient, DateTime recordedUtc, string notes = "")
        {
            Id = id;
            Patient = patient;
            RecordedUtc = recordedUtc;
            Notes = notes;
        }

        #endregion

        #region ModelBase

        public override bool Is(ModelBase modelBase, double tolerance = DEFAULT_TOLERANCE)
        {
            if (modelBase is not Study other)
                return false;

            return Id.Is(other.Id)
                   && Patient.Is(other.Patient)
                   && RecordedUtc == other.RecordedUtc
                   && Notes.Is(other.Notes);
        }

        public override ModelBase Clone()
        {
            return new Study(Id, Patient, RecordedUtc, Notes);
        }

        #endregion

        #region Properties

        [ToString]
        public int Id { get; }

        [ToString]
        public string Patient { get; }

        [ToString(Format = "yyyy-MM-dd")]
        public DateTime RecordedUtc { get; }

        public string Notes { get; }

        #endregion
    }
}
