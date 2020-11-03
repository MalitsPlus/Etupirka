using MahApps.Metro.Controls;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Globalization;
using System.IO;
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
	/// Interaction logic for PlayTimeStatisticDialog.xaml
	/// </summary>
	public class TimeSummary
	{
		public TimeSummary(string dat, int val)
		{
			d = DateTime.ParseExact(dat, "yyyy-MM-dd", CultureInfo.InvariantCulture);
			time = val;
		}
		public DateTime d { get; set; }
		public string DayString
		{
			get{
				return d.ToString("yyyy-MM-dd");
			}
		}

		public string MonthString
		{
			get
			{
				return d.ToString("yyyy-MM");
			}
		}

		public string YearString {
			get {
				return d.ToString("yyyy");
			}
		}

		public int time { get; set; }
		public string TimeString
		{
			get{
				int s = time % 60;
				int m = (time / 60);
				int h = m / 60;
				m %= 60;
				return h + @"時間" + m + @"分" + s + @"秒";
			}
		}
	}

	public class GameTimeSummary
	{
		
		public GameTimeSummary(string game, int time, GraphType type = GraphType.All)
		{
			Game = game;
			t = time;
			_type = type;
			if (_type == GraphType.Yearly) {
				PlayTime = Math.Round((Convert.ToDouble(t) / 3600), 2);
			} else {
				PlayTime = Math.Round((Convert.ToDouble(t) / 60), 2);
			}
		}
		// second
		public int t;
		public int n;
		private GraphType _type;

		public string Description {
			get {
				return "プレイ時間";
            }
        }
		public string Game{ get;set; }
		public double PlayTime { get; private set; }
	}

	public partial class PlayTimeStatisticDialog : MetroWindow
	{

		public SQLiteConnection conn;

		public PlayTimeStatisticDialog(string db_path)
		{
			InitializeComponent();
			if (Properties.Settings.Default.disableGlowBrush)
			{
				this.GlowBrush = null;
			}

			conn = new SQLiteConnection("Data Source=" + db_path);
			if (File.Exists(db_path))
			{
				PlayTimeLast30Days.Setup(conn);
				PlayTimeWeek.Setup(conn);
				PlayTimeMonth.Setup(conn);
				PlayTimeYear.Setup(conn);
				PlayTimeAll.Setup(conn);
			}
		}
	}
}
