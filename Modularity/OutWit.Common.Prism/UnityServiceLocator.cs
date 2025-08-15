using OutWit.Common.Exceptions;
using OutWit.Common.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity;

namespace OutWit.Common.Prism
{
    public abstract class UnityServiceLocator : IServiceLocator
    {
        #region Constructor

        protected UnityServiceLocator()
        {
            Container = new UnityContainer();
            IsDisposed = false;
        }

        #endregion

        #region Functions

        public TService Resolve<TService>()
        {
            if (IsDisposed)
                throw (new ExceptionOf<UnityServiceLocator>("ServiceLocator is disposed"));

            try
            {
                return Container.Resolve<TService>();
            }
            catch (Exception e)
            {
                return default(TService);
            }

        }

        public void Register<TService>(TService instance)
        {
            if (IsDisposed)
                throw (new ExceptionOf<UnityServiceLocator>("ServiceLocator is disposed"));

            try
            {
                Container.RegisterInstance<TService>(instance);
            }
            catch (Exception e)
            {
                throw new ExceptionOf<UnityServiceLocator>($"Can not register service: {typeof(TService).Name}");
            }
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            Container?.Dispose();

            IsDisposed = true;
        }

        #endregion

        #region Properties

        public UnityContainer Container { get; }

        public bool IsDisposed { get; private set; }


        #endregion
    }
}
