using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace OutWit.Common.Locker
{
    public class GlobalLocker : IDisposable
    {
        #region Constructors

        static GlobalLocker()
        {
            Lockers = new ConcurrentDictionary<string, bool>();
        }

        public GlobalLocker(string lockerId = "")
        {
            LockerId = lockerId;

            if (!Lockers.ContainsKey(LockerId))
                Lockers.TryAdd(LockerId, true);
        }

        #endregion

        #region Functions

        public static GlobalLocker Lock(string lockerId = "")
        {
            return new GlobalLocker(lockerId);
        }

        public static bool IsLocked(string lockerId = "")
        {
            return Lockers.ContainsKey(lockerId);
        } 

        #endregion

        #region IDisposable

        public void Dispose()
        {
            if (Lockers.ContainsKey(LockerId))
                Lockers.TryRemove(LockerId, out var value);
        } 

        #endregion

        #region Properties

        public bool Locked => IsLocked(LockerId);

        public string LockerId { get; }

        #endregion

        #region Static Properties

        private static ConcurrentDictionary<string, bool> Lockers { get; }

        #endregion

    }
}
