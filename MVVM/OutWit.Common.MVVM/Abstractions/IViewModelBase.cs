using System;
using System.ComponentModel;

namespace OutWit.Common.MVVM.Abstractions
{
    /// <summary>
    /// Base interface for all view models
    /// </summary>
    public interface IViewModelBase : INotifyPropertyChanged, IDisposable
    {
    }
}
