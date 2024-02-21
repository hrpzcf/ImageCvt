using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace WebpCvt
{
    internal static class CommonExt
    {
        internal static void Notify(this PropertyChangedEventHandler handler,
            INotifyPropertyChanged parent, [CallerMemberName] string name = null)
        {
            handler?.Invoke(parent, new PropertyChangedEventArgs(name));
        }

        internal static void SetNotify<T>(this PropertyChangedEventHandler handler,
            INotifyPropertyChanged parent, ref T property, T value, [CallerMemberName] string name = null)
        {
            property = value;
            handler?.Invoke(parent, new PropertyChangedEventArgs(name));
        }

        internal static bool ContainsPath(this List<ParamPackage> packageList, string fullPath)
        {
            foreach (ParamPackage package in packageList)
            {
                if (package.FullPath.Equals(fullPath, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
