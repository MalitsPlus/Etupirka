﻿using Etupirka.Dialog;
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
	/// Interaction logic for PlayTime30Days.xaml
	/// </summary>
	public partial class PlayTime30Days : UserControl
	{
		private SQLiteConnection conn;
		private List<TimeSummary> sd;

		public PlayTime30Days()
		{
			sd = new List<TimeSummary>();
			InitializeComponent();
			PlayTimeListView.ItemsSource = sd;
		}

		public void Setup(SQLiteConnection _conn){
			conn = _conn;
			using (SQLiteCommand command = conn.CreateCommand())
			{
				string hideNukige = "";
				if (Settings.Default.hideNukige) {
					hideNukige = @" AND g.nukige = 0 ";
				}
				conn.Open();
				DateTime d = DateTime.Today;
				for(int i=0;i<30;i++,d=d.AddDays(-1)){
						command.CommandText = @"SELECT SUM(p.playtime) t ,p.datetime  
										FROM games g,playtime p 
										WHERE g.uid=p.game " + hideNukige + @"
										AND date(p.datetime) = date('" + d.ToString("yyyy-MM-dd")+@"') 
										GROUP BY p.datetime";
						using (SQLiteDataReader reader = command.ExecuteReader())
						{

						if (reader.Read())
						{
							sd.Add(new TimeSummary(reader["datetime"].ToString(),
								Convert.ToInt32(reader["t"].ToString())));

						}
						else
						{
							sd.Add(new TimeSummary(d.ToString("yyyy-MM-dd"), 0));
						}
						}
				}
				conn.Close();
			}
		}

		private void PlayTimeListView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
		{
			var listView = (ListView)sender;
			var item = listView.ContainerFromElement((DependencyObject)e.OriginalSource) as ListViewItem;
			if (item != null)
			{
				TimeSummary g = (TimeSummary)PlayTimeListView.SelectedItem;
				if (g.time != 0)
				{
					List<GameTimeSummary> glist = new List<GameTimeSummary>();
					
					using (SQLiteCommand command = conn.CreateCommand())
					{
						string hideNukige = "";
						if (Settings.Default.hideNukige) {
							hideNukige = @" AND g.nukige = 0 ";
						}
						conn.Open();
						command.CommandText = @"SELECT g.title, p.playtime 
											FROM games g,playtime p 
											WHERE g.uid=p.game " + hideNukige + @"
											AND p.datetime=date('"
											+ g.d.ToString("yyyy-MM-dd")+@"') ";
						using (SQLiteDataReader reader = command.ExecuteReader())
						{
							while (reader.Read())
							{
								glist.Add(new GameTimeSummary(reader["title"].ToString(), Convert.ToInt32(reader["playtime"].ToString())));
							}
						}
					}
					conn.Close();
					Dialog.GameTimeGraph gtg = new GameTimeGraph(glist, g.d.ToString("yyyy-MM-dd"), GraphType.Last30Days);
					gtg.Owner = Window.GetWindow(this);
					if (gtg.ShowDialog()==true)
					{

					}

				}

			}
		}
	}
}
