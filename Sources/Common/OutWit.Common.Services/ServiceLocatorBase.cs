using OutWit.Common.Services.Interfaces;
using System;
using System.Collections;
using System.Collections.Generic;
using OutWit.Common.Exceptions;

namespace OutWit.Common.Services
{
    public abstract class ServiceLocatorBase : IServiceLocator, IEnumerable<KeyValuePair<Type, object>>
    {
        #region Fields

        private readonly Dictionary<Type, object> m_services = new Dictionary<Type, object>();

        #endregion

        #region Constructor

        protected ServiceLocatorBase()
        {
            IsDisposed = false;
        }

        #endregion

        #region Functions

        public TService Resolve<TService>()
        {
            if (IsDisposed)
                throw (new ExceptionOf<ServiceLocatorBase>("ServiceLocator is disposed"));

            try
            {
                if (!m_services.ContainsKey(typeof(TService)))
                    throw new InvalidOperationException($"Service {typeof(TService).Name} is not registered");

                return (TService)m_services[typeof(TService)];
            }
            catch (Exception e)
            {
                throw new ExceptionOf<ServiceLocatorBase>($"Can not resolve service: {typeof(TService).Name}", e);
            }

        }

        public void Register<TService>(TService instance)
        {
            if (IsDisposed)
                throw (new ExceptionOf<ServiceLocatorBase>("ServiceLocator is disposed"));

            try
            {
                if (m_services.ContainsKey(typeof(TService)))
                    m_services[typeof(TService)] = instance;
                else
                    m_services.Add(typeof(TService), instance);
            }
            catch (Exception e)
            {
                throw new ExceptionOf<ServiceLocatorBase>($"Can not register service: {typeof(TService).Name}", e);
            }
        }

        #endregion

        #region IEnumerable

        public IEnumerator<KeyValuePair<Type, object>> GetEnumerator()
        {
            return m_services.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            m_services.Clear();
            IsDisposed = true;
        }

        #endregion

        #region Properties

        public bool IsDisposed { get; private set; }


        #endregion

    }
}
