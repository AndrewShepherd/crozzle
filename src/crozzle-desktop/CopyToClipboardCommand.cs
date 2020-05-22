using crozzle;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Input;

namespace crozzle_desktop
{
	public class CopyToClipboardCommand : ICommand
	{
		public event EventHandler CanExecuteChanged;

		public CopyToClipboardCommand()
		{
		}
		bool ICommand.CanExecute(object parameter) =>
			true;
		//	(parameter as Workspace != null);

		void ICommand.Execute(object parameter)
		{
			var workspace = (Workspace)parameter;
			StringBuilder sb = new StringBuilder();
			sb.AppendLine(workspace.BoardRepresentation);
			sb.AppendLine(workspace.GenerateScoreBreakdown());
			Clipboard.SetText(sb.ToString());
		}
	}
}
