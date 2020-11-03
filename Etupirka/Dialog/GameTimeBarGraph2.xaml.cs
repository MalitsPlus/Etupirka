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
using De.TorstenMandelkow.MetroChart;
using MahApps.Metro.Controls;

namespace Etupirka.Dialog
{
    /// <summary>
    /// Interaction logic for GameTimeBarGraph2.xaml
    /// </summary>
    public partial class GameTimeBarGraph2 : MetroWindow
    {
        public ObservableCollection<GameTimeSummary> glist { get; private set; }

        public GameTimeBarGraph2(List<GameTimeSummary> g, string subtitle, GraphType type, List<GameTimeSummary> otherTitles = null) {
            glist = new ObservableCollection<GameTimeSummary>();
            InitializeComponent();
            if (Properties.Settings.Default.disableGlowBrush) {
                this.GlowBrush = null;
            }
            int totalTime = 0;

            chart.Series.Clear();

            g.ForEach(it => {
                totalTime += it.t;
                glist.Add(it);
                chart.Series.Add(new ChartSeries {
                    SeriesTitle = it.Game,
                    DisplayMember = "Description",
                    ValueMember = "PlayTime",
                    ItemsSource = new ObservableCollection<GameTimeSummary> { it },
                });
            });

            string otherString = "";

            if (otherTitles != null) {
                otherString += "その他：";
                otherTitles.ForEach(it => {
                    otherString += $"「{ it.Game }」";
                });
            }
            
            DataContext = new Graph2Context { Hint = otherString };

            chart.SelectedBrush = null;

            TimeSummary summary = new TimeSummary("1970-01-01", totalTime);
            chart.ChartSubTitle = $"{ subtitle }\r\nトータルプレイ時間：{ summary.TimeString }、合計{ g.Count + otherTitles.Count - 1 }本";
            if (type == GraphType.Yearly) {
                chart.ChartTitle = "タイトル別プレイ時間（hours）";
            }
        }
    }

    public class Graph2Context
    {
        public string Hint { get; set; }
    }
}
