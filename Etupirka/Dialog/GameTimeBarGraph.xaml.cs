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
using MahApps.Metro.Controls;
using LiveCharts;
using LiveCharts.Wpf;
using LiveCharts.Defaults;
using LiveCharts.Configurations;

namespace Etupirka.Dialog
{
    /// <summary>
    /// Interaction logic for GameTimeBarGraph.xaml
    /// </summary>
    public partial class GameTimeBarGraph : MetroWindow
    {

        List<GameTimeSummary> glist;
        public SeriesCollection seriesCollection { get; set; }
        public string[] labels { get; set; }
        public Func<double, string> formatter { get; set; }
        public string mainTitle { get; set; }
        public string subtitle { get; set; }

        public GameTimeBarGraph(List<GameTimeSummary> g, string subtitle, GraphType type, int othersNum = 0) {
            glist = g;
            List<double> timeList = new List<double>();
            int totalTime = 0;
            g.ForEach(it => {
                totalTime += it.t;
                timeList.Add(it.PlayTime);
            });
            TimeSummary summary = new TimeSummary("1970-01-01", totalTime);
            mainTitle = "タイトル別プレイ時間（hours）";
            subtitle = $"{ subtitle }\r\nトータルプレイ時間：{ summary.TimeString }、合計{ g.Count + othersNum - 1 }本";

            InitializeComponent();

            CartesianMapper<double> mapper = Mappers.Xy<double>()
                .X((value, index) => value)
                .Y((value, index) => index)
                .Fill(value => value > 20 ? new SolidColorBrush(Color.FromRgb(133, 222, 80)) : null)
                .Stroke(value => value > 20 ? new SolidColorBrush(Color.FromRgb(1, 171, 88)) : null);

            seriesCollection = new SeriesCollection();
            List<string> gameTitleList = new List<string>();
            g.ForEach(it => {
                gameTitleList.Add(it.Game);
            });

            seriesCollection.Add(new RowSeries {
                Values = new ChartValues<double>(timeList),
                Fill = new SolidColorBrush(Color.FromRgb(238, 83, 80)),
            });

            seriesCollection.Configuration = mapper;

            labels = gameTitleList.ToArray();
            formatter = value => value.ToString("N");
            DataContext = this;

            //if (Properties.Settings.Default.disableGlowBrush) {
            //    this.GlowBrush = null;
            //}
            //int totalTime = 0;
            //g.ForEach(it => {
            //    totalTime += it.t;
            //});
            //chart.SelectedBrush = null;
            //TimeSummary summary = new TimeSummary("1970-01-01", totalTime);
            //chart.ChartSubTitle = $"{ subtitle }\r\nトータルプレイ時間：{ summary.TimeString }、合計{ g.Count + othersNum - 1 }本";
            //if (type == GraphType.Yearly) {
            //    chart.ChartTitle = "タイトル別プレイ時間（hours）";
            //}
            //crt.ItemsSource = glist;
        }
    }
}
