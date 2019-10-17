using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Windows.Input;

namespace crozzle_desktop
{
	class DelegateCommand : ICommand
	{
		private readonly Action _execute;
		private readonly Func<object, bool> _canExecute;

		private readonly string _canExecutePropertyName = null;
		private readonly INotifyPropertyChanged _canExecuteSource = null;

		public DelegateCommand(Action execute, Expression<Func<bool>> canExecute)
		{
			this._execute = execute;
			this._canExecute = ((_) => canExecute.Compile()());
			if(canExecute.Body is MemberExpression memberExpression)
			{
				if(memberExpression.Member is PropertyInfo propertyInfo)
				{
					_canExecutePropertyName = propertyInfo.Name;
				}
				if(
					(memberExpression.Expression as ConstantExpression)?.Value is INotifyPropertyChanged n
				)
				{
					_canExecuteSource = n;
					n.PropertyChanged += EventSourcePropertyChanged;
				}
			}
		}

		private void EventSourcePropertyChanged(object sender, PropertyChangedEventArgs args)
		{
			if(args.PropertyName == _canExecutePropertyName)
			{
				CanExecuteChanged?.Invoke(this, EventArgs.Empty);
			}
		}

		public event EventHandler CanExecuteChanged;

		bool ICommand.CanExecute(object parameter) => this._canExecute(parameter);

		void ICommand.Execute(object parameter) => _execute();
	}
}
