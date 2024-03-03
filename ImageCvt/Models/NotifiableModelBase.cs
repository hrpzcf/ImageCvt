﻿using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ImageCvt
{
    public abstract class NotifiableModelBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public void NotifyPropertyChanged([CallerMemberName] string name = null)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public void SetPropNotify<T>(ref T property, T value, [CallerMemberName] string name = null)
        {
            property = value;
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
