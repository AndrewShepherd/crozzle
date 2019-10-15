using crozzle;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace crozzle_desktop
{
	/// <summary>
	/// Interaction logic for WorskpaceView.xaml
	/// </summary>
	public partial class WorkspaceView : UserControl
	{
		public static DependencyProperty WorkspaceDependencyProperty = DependencyProperty.Register(
			nameof(Workspace),
			typeof(crozzle.Workspace),
			typeof(WorkspaceView),
			new PropertyMetadata
			{
				PropertyChangedCallback = (d, e) =>
				{
					if (d is WorkspaceView workspaceView)
					{
						workspaceView.Workspace = (e.NewValue as Workspace);
					}
				},
			}
		);

		public WorkspaceView()
		{
			InitializeComponent();
		}

		private Workspace _workspace;
		public Workspace Workspace
		{
			get => _workspace;
			set
			{
				if(_workspace != value)
				{
					_workspace = value;
					//this.SetValue(UserControl.DataContextProperty, _workspace);
					//this.DataContext = _workspace;
				}
			}
		}

		public int RefreshInterval
		{
			get;
			set;
		}
	}
}
