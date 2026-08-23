using System;

namespace OutWit.Common.MVVM.Navigation.Tests.Mock
{
    /// <summary>
    /// A view model whose constructor throws.
    /// </summary>
    public sealed class ThrowingViewModel
    {
        public ThrowingViewModel()
        {
            throw new InvalidOperationException("Constructor failure");
        }
    }
}
