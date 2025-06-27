using System;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Security.Cryptography;
using OutWit.Common.Abstract;
using OutWit.Common.Exceptions;

namespace OutWit.Common.Utils
{
    public static class ExceptionUtils
    {
        public static void ThrowDelegateException<TClass>(Delegate fun, Enum status)
            where TClass : class
        {
            var errorMessage = $"Method: {fun.Target}.{fun.Method.Name}; Status: {status}";
            throw new ExceptionOf<TClass>(errorMessage);
        }


    }
}
