using De.TorstenMandelkow.MetroChart;
using MahApps.Metro.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Etupirka.Dialog
{
	/// <summary>
	/// Interaction logic for GameTimeStatisticDialog.xaml
	/// </summary>
	public partial class GameTimeStatisticDialog : MetroWindow
	{
		public ObservableCollection<TimeSummary> tlist { get; private set; }
		public string game;

		public GameTimeStatisticDialog(List<TimeSummary> t, string g)
		{
			tlist = new ObservableCollection<TimeSummary>();
			this.game = g;
			InitializeComponent();
			if (Properties.Settings.Default.disableGlowBrush)
			{
				this.GlowBrush = null;
			}

			int totalTime = 0;

			t.Reverse();

			chart.Series.Clear();
			t.ForEach(it => {
				totalTime += it.time;
				tlist.Add(it);
				chart.Series.Add(new ChartSeries {
					SeriesTitle = it.MonthDayString,
					DisplayMember = "Description",
					ValueMember = "PlayTime",
					ItemsSource = new ObservableCollection<TimeSummary> { it },
				});
			});

			TimeSummary summary = new TimeSummary("1970-01-01", totalTime);
			chart.ChartTitle = game;
			chart.ChartSubTitle = $"トータルプレイ時間：{ summary.TimeString }";
			chart.SelectedBrush = null;

			//PlayTimeListView.ItemsSource = tlist;
		}
	}
}
