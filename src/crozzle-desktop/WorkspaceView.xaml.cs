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

		private DateTime _lastDateTimeSet = default(DateTime);

		private static Workspace BestScoringWorkspace(Workspace w1, Workspace w2)
		{
			if (w1 == null)
				return w2;
			if (w2 == null)
				return w1;
			return w1.Score > w2.Score ? w1 : w2;
		}

		private Workspace _winningWorkspace = null;

		public Workspace Workspace
		{
			get => _workspace;
			set
			{
				if(_workspace != value)
				{
					_workspace = value;
					_winningWorkspace = BestScoringWorkspace(_winningWorkspace, _workspace);
					if ((DateTime.Now - _lastDateTimeSet).TotalMilliseconds > this.RefreshInterval)
					{
						_lastDateTimeSet = DateTime.Now;
						MainGrid.DataContext = _winningWorkspace;
						_winningWorkspace = null;
					}
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
