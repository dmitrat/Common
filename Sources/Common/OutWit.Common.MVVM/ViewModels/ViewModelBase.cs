using System;
using System.ComponentModel;
using OutWit.Common.Logging.Aspects;

namespace OutWit.Common.MVVM.ViewModels
{
    [Log]
    public abstract class ViewModelBase<TApplicationVm> : INotifyPropertyChanged, IDisposable
        where TApplicationVm : class
    {
        #region Events

        public event PropertyChangedEventHandler PropertyChanged = delegate { };

        #endregion

        #region Constructors

        protected ViewModelBase(TApplicationVm applicationVm)
        {
            ApplicationVm = applicationVm;
        }

        #endregion

        #region Functions

        protected TResult Check<TResult>(Func<TResult> action, TResult onError = default)
        {
            try
            {
                return action();
            }
            catch (Exception e)
            {
                return onError;
            }
        }

        protected bool Check(Func<bool> action)
        {
            try
            {
                return action();
            }
            catch (Exception e)
            {
                return false;
            }
        }

        protected void Check(Action action)
        {
            try
            {
                action();
            }
            catch (Exception e)
            {
            }
        } 

        #endregion

        #region IDisposable

        public virtual void Dispose()
        {
        } 

        #endregion

        #region Properties

        protected TApplicationVm ApplicationVm { get; }

        #endregion
    }
}
