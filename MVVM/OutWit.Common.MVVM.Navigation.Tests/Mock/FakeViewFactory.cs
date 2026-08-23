using System;
using System.Collections.Generic;
using OutWit.Common.MVVM.Navigation.Interfaces;

namespace OutWit.Common.MVVM.Navigation.Tests.Mock
{
    /// <summary>
    /// An IViewFactory that knows the view model types it was told about and builds a
    /// <see cref="FakeView"/> for them.
    /// </summary>
    public sealed class FakeViewFactory : IViewFactory
    {
        #region Fields

        private readonly HashSet<Type> m_known = new();

        #endregion

        #region Constructors

        public FakeViewFactory(params Type[] known)
        {
            foreach (var type in known)
                m_known.Add(type);
        }

        #endregion

        #region IViewFactory

        public bool CanBuild(Type viewModelType)
        {
            return m_known.Contains(viewModelType);
        }

        public object Build(object viewModel)
        {
            if (ThrowOnBuild != null)
                throw ThrowOnBuild;

            if (!m_known.Contains(viewModel.GetType()))
                throw new InvalidOperationException($"No view for {viewModel.GetType().Name}");

            return new FakeView(viewModel);
        }

        #endregion

        #region Properties

        public Exception? ThrowOnBuild { get; set; }

        #endregion
    }
}
