using System;
using System.ComponentModel;
using OutWit.Common.Abstract;

namespace OutWit.Common.MVVM.ViewModels
{
    public abstract class ViewModelBase<TApplicationVm> : NotifyPropertyChangedBase, IDisposable
        where TApplicationVm : class
    {
        #region Constructors

        protected ViewModelBase(TApplicationVm applicationVm)
        {
            ApplicationVm = applicationVm;
        }

        #endregion

        #region Functions

        protected TResult Check<TResult>(Func<TResult> action, TResult onError = default!)
        {
            try
            {
                return action();
            }
            catch (Exception)
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
            catch (Exception)
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
            catch (Exception)
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
