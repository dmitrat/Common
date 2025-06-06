using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace OutWit.Common.Abstract
{
    public abstract class NotifyPropertyChangedBase : INotifyPropertyChanged
    {
        #region Events

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion

        #region PropertyChanged

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }
}
