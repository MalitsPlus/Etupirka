using Etupirka.Dialog;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
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
using Etupirka.Properties;

namespace Etupirka.Views
{
    /// <summary>
    /// Interaction logic for PlayTimeYear.xaml
    /// </summary>
    public partial class PlayTimeYear : UserControl
    {
        private SQLiteConnection conn;
        private List<TimeSummary> sd;

        public PlayTimeYear() {
            sd = new List<TimeSummary>();
            InitializeComponent();
            PlayTimeListView.ItemsSource = sd;
        }

		public void Setup(SQLiteConnection _conn) {
			conn = _conn;
			using (SQLiteCommand command = conn.CreateCommand()) {
				conn.Open();
				DateTime t = new DateTime(2000, 1, 1);
				DateTime d = new DateTime(DateTime.Today.Year, 1, 1);
				while (d >= t) {
					command.CommandText = @"SELECT SUM(p.playtime) t
										FROM games g,playtime p 
										WHERE g.uid=p.game AND date(p.datetime) between date('" + d.ToString("yyyy-01-01 00:00:00") + @"') 
										AND date('" + d.ToString("yyyy-12-31 23:59:59") + @"') ";
					if (Settings.Default.hideNukige) {
						command.CommandText += @" AND g.nukige = 0 ";
                    }
					using (SQLiteDataReader reader = command.ExecuteReader()) {

						if (reader.Read()) {
							if (reader.IsDBNull(0)) {
								//	sd.Add(new TimeSummary(d.ToString("yyyy-MM-dd"), 0));

							} else {
								string time = reader["t"].ToString();
								if (!time.Equals("0"))
								sd.Add(new TimeSummary(d.ToString("yyyy-01-01"),
									Convert.ToInt32(time)));
							}
						} else {
							//sd.Add(new TimeSummary(d.ToString("yyyy"), 0));
						}
					}
					d = d.AddYears(-1);
				}
				conn.Close();
			}
		}

		private void PlayTimeListView_MouseDoubleClick(object sender, MouseButtonEventArgs e) {
			var listView = (ListView)sender;
			var item = listView.ContainerFromElement((DependencyObject)e.OriginalSource) as ListViewItem;
			if (item != null) {
				TimeSummary g = (TimeSummary)PlayTimeListView.SelectedItem;
				if (g.time != 0) {
					int othersNum = 0;
					List<GameTimeSummary> otherTitles = new List<GameTimeSummary>();
					List<GameTimeSummary> glist = new List<GameTimeSummary>();
					using (SQLiteCommand command = conn.CreateCommand()) {
						string hideNukige = "";
						if (Settings.Default.hideNukige) {
							hideNukige = @" AND g.nukige = 0 ";
						}
						conn.Open();
						command.CommandText = 
							@"SELECT g.title,SUM(p.playtime) playtime
							FROM  games g,playtime p 
							WHERE g.uid=p.game AND p.datetime BETWEEN 
							date('" + g.d.ToString("yyyy-01-01 00:00:00") + @"') 
							AND date('" + g.d.ToString("yyyy-12-31 23:59:59") + @"') " + hideNukige + @"
							GROUP BY g.uid 
							ORDER BY SUM(p.playtime) DESC ";
						using (SQLiteDataReader reader = command.ExecuteReader()) {
							List<GameTimeSummary> innerList = new List<GameTimeSummary>();
							while (reader.Read()) {
								int playTime = Convert.ToInt32(reader["playtime"].ToString());
								innerList.Add(new GameTimeSummary(reader["title"].ToString(), playTime, GraphType.Yearly));
							}
							if (innerList.Count <= 20 || Settings.Default.showAllGameInYearGraph) {
								// if count <= 20, show them all
								glist = innerList;
                            } else {
								int othersTime = 0;
								int count = 0;
								innerList.ForEach( it => {
									if (count++ < 20) {
										glist.Add(it);
                                    } else if (it.t > 5 * 3600) {
										glist.Add(it);
                                    } else {
										othersTime += it.t;
										othersNum++;
										otherTitles.Add(it);
									}
								});
								if (othersNum != 0) {
									glist.Add(new GameTimeSummary($"その他{othersNum}本", othersTime, GraphType.Yearly));
                                }
							}
						}
					}
					conn.Close();
					Dialog.GameTimeBarGraph2 gtbg = new GameTimeBarGraph2(glist, g.d.ToString("yyyy-01-01") + "~" + g.d.ToString("yyyy-12-31"), GraphType.Yearly, otherTitles.Count == 0 ? null : otherTitles);
					gtbg.Owner = Window.GetWindow(this);
					if (gtbg.ShowDialog() == true) {

					}
				}
			}
		}
	}
}
