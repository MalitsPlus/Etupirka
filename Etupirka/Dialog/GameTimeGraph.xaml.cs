using System;
using System.Collections.Generic;
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
    /// Interaction logic for GameTimeGraph.xaml
    /// </summary>
    public partial class GameTimeGraph : MetroWindow
    {
        List<GameTimeSummary> glist;
        public GameTimeGraph(List<GameTimeSummary> g, string subtitle, GraphType type, int othersNum = 0) {
            glist = g;
            InitializeComponent();
            if (Properties.Settings.Default.disableGlowBrush) {
                this.GlowBrush = null;
            }
            int totalTime = 0;
            g.ForEach(it => {
                totalTime += it.t;
            });
            chart.SelectedBrush = null;
            TimeSummary summary = new TimeSummary("1970-01-01", totalTime);
            chart.ChartSubTitle = $"{ subtitle }\r\nトータルプレイ時間：{ summary.TimeString }、合計{ g.Count + othersNum - 1 }本";
            if (type == GraphType.Yearly) {
                chart.ChartTitle = "タイトル別プレイ時間（hours）";
            }
            crt.ItemsSource = glist;
        }
    }

    public enum GraphType
    {
        Last30Days,
        Weekly,
        Monthly,
        Yearly,
        All
    }
}
