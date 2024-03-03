﻿using System;
using System.Windows.Input;

namespace ImageCvt
{
    internal class RelayCommand : ICommand
    {
        private readonly Action<object> _execute;
        private readonly Func<object, bool> _canExecute;

        public event EventHandler CanExecuteChanged;

        public RelayCommand(Action<object> execute)
        {
            this._execute = execute;
        }

        public RelayCommand(Action<object> execute, Func<object, bool> canExecute)
        {
            this._execute = execute;
            this._canExecute = canExecute;
        }

        /// <summary>
        /// 需要在主线程调用才有作用
        /// </summary>
        public void RaiseCanExecuteEvent()
        {
            this.CanExecuteChanged?.Invoke(null, EventArgs.Empty);
        }

        public void Execute(object parameter)
        {
            this._execute?.Invoke(parameter);
        }

        public bool CanExecute(object parameter)
        {
            return this._canExecute is null || this._canExecute(parameter);
        }
    }
}
