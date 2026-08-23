using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using OutWit.Common.MVVM.Navigation.Sample.Core.Models;

namespace OutWit.Common.MVVM.Navigation.Sample.Core.Services
{
    /// <summary>
    /// The sample's data, with a deliberate delay on every read: it is what makes the
    /// navigation behaviour visible. While a screen is loading, the navigation bar stays
    /// live — the outlet is released the moment the screen is shown — and leaving mid-load
    /// cancels the load.
    /// </summary>
    public sealed class StudyStore
    {
        #region Constants

        private const int LOAD_DELAY_MILLISECONDS = 1200;

        #endregion

        #region Fields

        private readonly List<Study> m_studies;

        #endregion

        #region Constructors

        public StudyStore()
        {
            m_studies = Enumerable.Range(1, 12)
                .Select(id => new Study(id, $"Patient {id:00}", new DateTime(2026, 1, 1).AddDays(id * 3), $"Notes for study {id}."))
                .ToList();
        }

        #endregion

        #region Functions

        /// <summary>
        /// All studies. Honours the token: navigating away mid-load cancels it.
        /// </summary>
        public async Task<IReadOnlyList<Study>> LoadAllAsync(CancellationToken cancellation)
        {
            await Task.Delay(LOAD_DELAY_MILLISECONDS, cancellation);

            return m_studies.ToArray();
        }

        /// <summary>
        /// One study, or null.
        /// </summary>
        public async Task<Study?> LoadAsync(int id, CancellationToken cancellation)
        {
            await Task.Delay(LOAD_DELAY_MILLISECONDS / 2, cancellation);

            return m_studies.FirstOrDefault(study => study.Id == id);
        }

        /// <summary>
        /// Replaces a study's notes.
        /// </summary>
        public void Save(Study study)
        {
            var index = m_studies.FindIndex(candidate => candidate.Id == study.Id);

            if (index >= 0)
                m_studies[index] = study;
        }

        #endregion
    }
}
