using System.ComponentModel;
using OutWit.Common.MVVM.Blazor.ViewModels;

namespace OutWit.Common.MVVM.Blazor.Tests.ViewModels
{
    /// <summary>
    /// Test view model for testing ViewModelBase functionality.
    /// </summary>
    public class TestViewModel : ViewModelBase
    {
        #region Fields

        private string m_name = string.Empty;
        private int m_count;
        private string? m_lastChangedProperty;

        #endregion

        #region Functions

        public new void RaisePropertyChanged(string? propertyName)
        {
            base.RaisePropertyChanged(propertyName);
        }

        public TResult? TestCheck<TResult>(Func<TResult> action, TResult? onError = default)
        {
            return Check(action, onError);
        }

        public bool TestCheck(Func<bool> action)
        {
            return Check(action);
        }

        public void TestCheck(Action action)
        {
            Check(action);
        }

        #endregion

        #region Event Handlers

        protected override void OnPropertyChanged(string? propertyName)
        {
            LastChangedProperty = propertyName;
        }

        #endregion

        #region Properties

        public string Name
        {
            get => m_name;
            set => SetProperty(ref m_name, value);
        }

        public int Count
        {
            get => m_count;
            set => SetProperty(ref m_count, value);
        }

        public string? LastChangedProperty
        {
            get => m_lastChangedProperty;
            private set => m_lastChangedProperty = value;
        }

        #endregion
    }
}
