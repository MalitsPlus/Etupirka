﻿using System;
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
using System.Windows.Navigation;
using System.Windows.Shapes;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.Win32;
using System.Net;
using System.Threading;
using System.IO;
using System.Xml;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Runtime.Serialization.Formatters.Binary;
using SatoruErogeTimer;
using System.Diagnostics;
using System.ComponentModel;
using Hardcodet.Wpf.TaskbarNotification;
using System.Media;
using System.Collections;
using System.Xml.Linq;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Controls.Primitives;
using System.Data.SQLite;
using Etupirka.Dialog;
using System.Threading.Tasks;
using Etupirka.Properties;
using System.Text.RegularExpressions;

namespace Etupirka
{
    public enum StatusBarStatus
    {
        watching = 0, resting = 1, erogehelper = 2
    }

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow, INotifyPropertyChanged
    {
        #region Property
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name) {
            if (PropertyChanged == null) return;
            PropertyChanged(this, new PropertyChangedEventArgs(name));
        }

        public int ItemCount {
            get {
                if (items == null) {
                    return 0;
                }
                return items.Count;
            }
        }

        public string TotalTime {
            get {
                if (items == null) return "";
                int t = 0;
                foreach (GameExecutionInfo i in items) {
                    t += i.TotalPlayTime;
                }
                return t / 3600 + @"時間" + (t / 60) % 60 + @"分";
            }
        }

        public bool WatchProc {
            get {
                return Properties.Settings.Default.watchProcess;
            }
            set {
                Properties.Settings.Default.watchProcess = value;

                OnPropertyChanged("WatchProc");
                OnPropertyChanged("StatusBarStat");
            }
        }

        public bool HideNukige {
            get {
                return Properties.Settings.Default.hideNukige;
            }
            set {
                Properties.Settings.Default.hideNukige = value;
                loadGridData(false);
            }
        }

        bool erogeHelper = false;
        public bool ErogeHelper {
            get {
                return erogeHelper;
            }
            set {

                if (!value) {
                    GameListView.Visibility = Visibility.Visible;
                    MenuBar.Visibility = Visibility.Visible;
                    this.ShowTitleBar = true;

                } else {
                    GameListView.Visibility = Visibility.Hidden;
                    MenuBar.Visibility = Visibility.Hidden;
                    this.ShowTitleBar = false;
                }

                erogeHelper = value;
                OnPropertyChanged("ErogeHelper");
                OnPropertyChanged("StatusBarStat");
            }
        }

        private GameExecutionInfo currentFocused;
        private GameExecutionInfo currentRunning;

        public StatusBarStatus StatusBarStat {
            get {
                if (ErogeHelper) return StatusBarStatus.erogehelper;
                if (WatchProc) return StatusBarStatus.watching;
                return StatusBarStatus.resting;
            }
        }

        #endregion

        #region Hotkey
        public HotKey _hotkeyWatchProc;
        public HotKey _hotkeyErogeHelper;
        public HotKey _hotkeyDoSceenShot;
        private void OnHotKeyHandler_WatchProc(HotKey hotKey) {
            if (Properties.Settings.Default.playVoice) {
                if (WatchProc) {
                    SoundPlayer sp = new SoundPlayer(Properties.Resources.stop2);
                    sp.Play();
                } else {
                    SoundPlayer sp = new SoundPlayer(Properties.Resources.start2);
                    sp.Play();

                }
            }
            WatchProc = !WatchProc;
        }

        private void OnHotKeyHandler_ErogeHelper(HotKey hotKey) {
            ErogeHelper = !ErogeHelper;
        }

        public void OnHotKeyHandler_DoSceenShot(HotKey hotKey) {
            int width = PrimaryScreen.DESKTOP.Width;
            int height = PrimaryScreen.DESKTOP.Height;
            //int width = (int)SystemParameters.PrimaryScreenWidth;
            //int height = (int)SystemParameters.PrimaryScreenHeight;
            // Creates an image
            Bitmap image = new Bitmap(width, height);
            Graphics imgGraphics = Graphics.FromImage(image);
            // Fullfills the canvas with pure black, otherwise RGB(0,0,0) will be a dead pixel 
            imgGraphics.Clear(System.Drawing.Color.Black);
            // Sets the area
            imgGraphics.CopyFromScreen(0, 0, 0, 0, new System.Drawing.Size(width, height));

            string fileName = Settings.Default.fileName;
            string folderPath = Settings.Default.screenShotSavePath;

            // Priority: Focused > Running(Random) > Others 
            if (currentFocused != null) {
                folderPath += "\\" + currentFocused.Title;
                fileName = fileName.Replace("%game%", currentFocused.Title);
            } else if (currentRunning != null) {
                folderPath += "\\" + currentRunning.Title;
                fileName = fileName.Replace("%game%", currentRunning.Title);
            } else {
                folderPath += "\\" + "その他";
                fileName = fileName.Replace("%game%", "その他");
            }
            
            if (!Directory.Exists(folderPath)) {
                Directory.CreateDirectory(folderPath);
            }

            string hatena = "";
            int num = 3;
            var matches = new Regex("#+").Match(fileName);
            if (matches.Success) {
                num = matches.Value.Length;
                for (int i = 0; i < num; i++) {
                    hatena += "?";
                }
                fileName = fileName.Replace(matches.Value, hatena);
            } else {
                hatena = "???";
                fileName = fileName + hatena;
            }
            string formatter = "{0:D" + num + "}";

            // Check duplicate file name
            string[] existFileNames = Directory.GetFiles(folderPath, fileName + ".png");

            if (existFileNames.Length == 0) {
                // if no files
                image.Save(folderPath + "\\" + fileName.Replace(hatena, String.Format(formatter, 1)) + ".png", ImageFormat.Png);
            } else {
                // get the max num 
                Array.Sort(existFileNames);
                int lastNum = int.Parse(existFileNames[existFileNames.Length - 1].Substring(folderPath.Length + matches.Index + 1, num)) + 1;
                image.Save(folderPath + "\\" + fileName.Replace(hatena, String.Format(formatter, lastNum)) + ".png", ImageFormat.Png);
            }
            image.Dispose();
            imgGraphics.Dispose();
        }

        /*		private void OnHotKeyHandler_Screenshot(HotKey hotKey)
				{
					System.Drawing.Rectangle bounds = this.Bounds;
					using (Bitmap bitmap = new Bitmap(bounds.Width, bounds.Height))
					{
						using (Graphics g = Graphics.FromImage(bitmap))
						{
							g.CopyFromScreen(new System.Drawing.Point(bounds.Left, bounds.Top), Point.Empty, bounds.Size);
						}
						bitmap.Save("C://test.jpg", ImageFormat.Jpeg);
					}
				}*/
        #endregion

        private ObservableCollection<GameExecutionInfo> items;

        private ObservableCollection<GameExecutionInfo> itemsForShow;

        //private Dictionary<string, TimeData> timeDict;

        private System.Windows.Threading.DispatcherTimer watchProcTimer;

        private ProcessInfoCache processInfoCache = new ProcessInfoCache();

        private DBManager db;

        public MainWindow() {

            if (Settings.Default.UpgradeRequired) {
                Settings.Default.Upgrade();
                Settings.Default.UpgradeRequired = false;
                Settings.Default.Save();
            }

            InitializeComponent();
            tbico.DoubleClickCommand = new ShowAppCommand(this);
            tbico.DataContext = this;

            if (Settings.Default.Do_minimize) {
                this.Hide();
                Settings.Default.Do_minimize = false;
                Settings.Default.Save();
            }

            db = new DBManager(Utility.userDBPath);
            Utility.im = new InformationManager(Utility.infoDBPath);

            loadGridData(true);

            //_hotkeyWatchProc = new HotKey(Key.F9, KeyModifier.Alt, OnHotKeyHandler_WatchProc);
            //_hotkeyErogeHelper = new HotKey(Key.F8, KeyModifier.Alt, OnHotKeyHandler_ErogeHelper);
            if (Settings.Default.enableScreenShot) {
                _hotkeyDoSceenShot = new HotKey(Key.D, KeyModifier.Alt, OnHotKeyHandler_DoSceenShot);
            }

            RegisterInStartup(Properties.Settings.Default.setStartUp);
            if (Properties.Settings.Default.disableGlowBrush) {
                this.GlowBrush = null;
            }

            watchProcTimer = new System.Windows.Threading.DispatcherTimer();
            watchProcTimer.Tick += new EventHandler(dispatcherTimer_Tick);
            watchProcTimer.Interval = new TimeSpan(0, 0, Properties.Settings.Default.monitorInterval);
            watchProcTimer.Start();

            if (String.IsNullOrEmpty(Settings.Default.screenShotSavePath)) {
                Settings.Default.screenShotSavePath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            }
        }

        #region Function
        private async void loadGridData(bool isInit) {
            GameListView.ItemsSource = null;
            GameListView.ItemsSource = await Task.Run(() => {
                if (isInit) {
                    items = new ObservableCollection<GameExecutionInfo>();
                    db.LoadGame(items);
                }

                // hide nukige if the option is enabled 
                itemsForShow = new ObservableCollection<GameExecutionInfo>();
                foreach (var it in items) {
                    if (Settings.Default.hideNukige && it.IsNukige) {
                        continue;
                    }
                    itemsForShow.Add(it);
                }
                UpdateStatus();
                OnPropertyChanged("ItemCount");
                return itemsForShow;
            });
            GameListView.SelectedItem = null;
        }

        //private void doCheckUpdate() {
        //    try {
        //        string str = NetworkUtility.GetString("http://etupirka.halcyons.org/checkversion.php");
        //        Version lastestVersion = new Version(str);
        //        Version myVersion = new Version(FileVersionInfo.GetVersionInfo(System.Reflection.Assembly.GetExecutingAssembly().Location).FileVersion);
        //        if (lastestVersion > myVersion) {
        //            if (MessageBox.Show("Version " + str + " が見つかりました、更新しますか？", "Etupirkaを更新する", MessageBoxButton.YesNo) == MessageBoxResult.Yes) {
        //                Process.Start("https://github.com/Aixile/Etupirka/releases");
        //            }
        //        }
        //    } catch {

        //    }

        //}

        /// <summary>
        /// This method shall be called async in every certain seconds, or be called sync in some user actions.
        /// </summary>
        /// <param name="time"></param>
        private void UpdateStatus(int time = 0) {
            IntPtr actWin = Utility.GetForegroundWindow();
            int calcID;
            Utility.GetWindowThreadProcessId(actWin, out calcID);
            var currentProc = Process.GetProcessById(calcID);
            try {
                if (System.IO.Path.GetFileName(currentProc.MainModule.FileName) == "main.bin") //SoftDenchi DRM
                {
                    calcID = Utility.ParentProcessUtilities.GetParentProcess(calcID).Id;
                }
            } catch (Exception e) {
                Console.WriteLine(e);
            }

            System.Console.WriteLine(calcID);
            bool play_flag = false;

            Process[] proc = Process.GetProcesses();

            Dictionary<string, bool> dic = new Dictionary<string, bool>();


            using (var scopedAccess = processInfoCache.scopedAccess()) {
                foreach (Process p in proc) {
                    try {
                        string path = processInfoCache.getProcessPath(p);
                        if (path == "") {
                            continue;
                        }
                        bool isForeground = p.Id == calcID;
                        if (dic.ContainsKey(path)) {
                            dic[path] |= isForeground;
                        } else {
                            dic[path] = isForeground;
                        }
                    } catch (Exception e) {
                        // Console.WriteLine(e);
                    }
                }
            }

            string statusBarText = "";
            string trayTipText = "Etupirka Version " + FileVersionInfo.GetVersionInfo(System.Reflection.Assembly.GetExecutingAssembly().Location).FileVersion;

            GameExecutionInfo currentGame = null;
            GameExecutionInfo currentUnfocused = null;
            foreach (GameExecutionInfo i in items) {
                bool running = false;
                if (i.UpdateStatus2(dic, ref running, time))
                // if (i.UpdateStatus(proc, calcID,ref running, time))
                {
                    if (time != 0) {
                        //string date = DateTime.Now.Date.ToString("yyyy-MM-dd");

                        db.UpdateTimeNow(i.UID, time);
                    }
                    db.UpdateGameTimeInfo(i.UID, i.TotalPlayTime, i.FirstPlayTime, i.LastPlayTime);
                    if (i.Status == ProcStat.Focused) {
                        play_flag = true;
                        currentGame = i;
                        if (Properties.Settings.Default.hideListWhenPlaying) {
                            ErogeHelper = true;
                        }
                    } 
                } else if (i.Status == ProcStat.Unfocused) {
                    currentUnfocused = i;
                }
                System.Console.WriteLine(running);
                if (running) {
                    trayTipText += "\n" + i.Title + " : " + i.TotalPlayTimeString;
                }
            }

            currentFocused = currentGame;
            currentRunning = currentUnfocused;

            dic.Clear();

            // Update UI in main thread 
            this.Dispatcher.BeginInvoke(
             new Action(() => {

                 if (play_flag && currentGame != null) {
                     PlayMessage.Content = currentGame.Title + " : " + currentGame.TotalPlayTimeString;
                 } else {
                     PlayMessage.Content = statusBarText;

                     if (Properties.Settings.Default.hideListWhenPlaying) {
                         ErogeHelper = false;
                     }
                 }
                 tbico.ToolTipText = trayTipText;

                 OnPropertyChanged("TotalTime");
             }));
        }

        private void RegisterInStartup(bool isChecked) {
            try {
                RegistryKey registryKey = Registry.CurrentUser.OpenSubKey
                        ("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
                if (isChecked) {
                    if (Settings.Default.minimizeAtStartup) {
                        registryKey.SetValue("Etupirka", "\"" + System.Reflection.Assembly.GetEntryAssembly().Location + "\" -min");

                    } else {
                        registryKey.SetValue("Etupirka", System.Reflection.Assembly.GetEntryAssembly().Location);
                    }
                } else {
                    registryKey.DeleteValue("Etupirka");
                }
            } catch {

            }
        }

        void ImportXML(string dataPath) {
            GameExecutionInfo[] erogeList = null;
            System.Xml.Serialization.XmlSerializer serializer2 = new System.Xml.Serialization.XmlSerializer(typeof(GameExecutionInfo[]));
            System.IO.StreamReader sr = new System.IO.StreamReader(dataPath);
            erogeList = (GameExecutionInfo[])serializer2.Deserialize(sr);
            sr.Close();
            if (erogeList != null) {
                foreach (GameExecutionInfo i in erogeList) {
                    if (i.UID == null) {
                        i.GenerateUID();
                    }
                    items.Add(i);
                }
            }
            db.InsertOrIgnoreGame(items);
            UpdateStatus();
            OnPropertyChanged("ItemCount");
        }

        void ImportXML_Overwrite(string dataPath) {
            GameExecutionInfo[] erogeList = null;
            System.Xml.Serialization.XmlSerializer serializer2 = new System.Xml.Serialization.XmlSerializer(typeof(GameExecutionInfo[]));
            System.IO.StreamReader sr = new System.IO.StreamReader(dataPath);
            erogeList = (GameExecutionInfo[])serializer2.Deserialize(sr);
            sr.Close();
            if (erogeList != null) {
                foreach (GameExecutionInfo i in erogeList) {
                    if (i.UID == null) {
                        i.GenerateUID();
                    }
                    items.Add(i);
                }
            }
            db.InsertOrReplaceGame(items);
            UpdateStatus();
            OnPropertyChanged("ItemCount");
        }

        public void ExportXML(string dataPath) {
            System.Xml.Serialization.XmlSerializer serializer = new System.Xml.Serialization.XmlSerializer(typeof(GameExecutionInfo[]));
            System.IO.StreamWriter sw = new System.IO.StreamWriter(dataPath, false);
            serializer.Serialize(sw, items.ToArray());
            sw.Close();
        }

        void ExportXML(string dataPath, List<GameExecutionInfo> a) {
            System.Xml.Serialization.XmlSerializer serializer = new System.Xml.Serialization.XmlSerializer(typeof(GameExecutionInfo[]));
            System.IO.StreamWriter sw = new System.IO.StreamWriter(dataPath, false);
            serializer.Serialize(sw, a.ToArray());
            sw.Close();
        }

        void ImportTimeDict(string dataPath) {
            Dictionary<string, TimeData> timeDict = new Dictionary<string, TimeData>();
            try {
                XElement xElem = XElement.Load(dataPath);
                timeDict = xElem.Descendants("day").ToDictionary(
                    x => ((string)x.Attribute("d")).Replace('/', '-'), x => new TimeData(x.Descendants("game").ToDictionary(
                          y => (string)y.Attribute("uid"), y => (int)y.Attribute("value"))));
            } catch {
                timeDict = new Dictionary<string, TimeData>();
            }

            db.AddPlayTime(timeDict);

        }

        void ImportTimeDict_Overwrite(string dataPath) {
            Dictionary<string, TimeData> timeDict = new Dictionary<string, TimeData>();
            try {
                XElement xElem = XElement.Load(dataPath);
                timeDict = xElem.Descendants("day").ToDictionary(
                    x => ((string)x.Attribute("d")).Replace('/', '-'), x => new TimeData(x.Descendants("game").ToDictionary(
                         y => (string)y.Attribute("uid"), y => (int)y.Attribute("value"))));
            } catch {
                timeDict = new Dictionary<string, TimeData>();
            }

            db.InsertOrReplaceTime(timeDict);
        }

        void ExportTimeDict(string dataPath) {
            Dictionary<string, TimeData> timeDict = db.GetPlayTime();

            XElement xElem = new XElement("days",
                    timeDict.Select(x => new XElement("day",
                    new XAttribute("d", x.Key),
                    new XElement("games",
                        ((TimeData)x.Value).d.Select(
                         y => new XElement("game",
                             new XAttribute("uid", y.Key),
                             new XAttribute("value", y.Value)))))));
            xElem.Save(dataPath);
        }

        void ExportTimeDict(string dataPath, List<GameExecutionInfo> a) {
            Dictionary<string, TimeData> timeDict = db.GetPlayTime(a);

            XElement xElem = new XElement("days",
                    timeDict.Select(x => new XElement("day",
                    new XAttribute("d", x.Key),
                    new XElement("games",
                        ((TimeData)x.Value).d.Select(
                         y => new XElement("game",
                             new XAttribute("uid", y.Key),
                             new XAttribute("value", y.Value)))))));
            xElem.Save(dataPath);
        }

        private async Task<List<GameInfo>> searchGameFromES(GameExecutionInfo g) {

            List<GameInfo> searchedGames = new List<GameInfo>();

            if (!string.IsNullOrEmpty(g.Title)) {
                string encodeString = Uri.EscapeDataString(g.Title);
                var document = await NetworkUtility.GetHtmlDocument($"http://erogamescape.dyndns.org/~ap2/ero/toukei_kaiseki/kensaku.php?category=game&word_category=name&mode=normal&word={encodeString}");
                var rows = document.GetElementById("result").GetElementsByTagName("tr");

                // include title row
                if (rows.Count > 1) {

                    for (int i = 1; i < rows.Count; i++) {
                        // TODO: fetch ESID 
                        string href = rows[i].GetElementsByTagName("td")[0].GetElementsByTagName("a")[0].GetAttribute("href");
                        string esid = "0";

                        Match match = Regex.Match(href, @"game\.php\?game=(\d+)#ad$");
                        if (match.Success == true) {
                            esid = match.Groups[1].Value;
                        }
                        string thisTitle = rows[i].GetElementsByTagName("td")[0].GetElementsByTagName("a")[0].InnerText;
                        string thisBrand = rows[i].GetElementsByTagName("td")[1].InnerText;
                        string thisSaleDay = rows[i].GetElementsByTagName("td")[2].InnerText;
                        DateTime thisSaleDate = DateTime.ParseExact(thisSaleDay, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
                        searchedGames.Add(new GameInfo(esid, thisTitle, thisBrand, thisSaleDate));
                    }
                }
            }
            return searchedGames;
        }
        #endregion

        #region Event

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e) {
            //StreamWriter writer=new StreamWriter(@"D:\etupirka.log", true);
            //writer.WriteLine("test");
            e.Cancel = false;
            if (Settings.Default.askBeforeExit) {
                try {
                    Dialog.AskExitDialog acd = new Dialog.AskExitDialog();
                    acd.Owner = this;
                    e.Cancel = true;
                    if (acd.ShowDialog() == true) {
                        e.Cancel = false;
                        if (acd.DoNotDisplay) {
                            Settings.Default.askBeforeExit = false;
                            Settings.Default.Save();
                        }
                    }
                } catch {
                }

            }
            base.OnClosing(e);
        }

        private void GameListView_KeyDown(object sender, KeyEventArgs e) {
            if (e.Key == Key.Enter) {
                DoOpenGame();
            } else if (e.Key == Key.Delete) {

                DoDelete();
            }

        }

        protected override void OnStateChanged(EventArgs e) {
            if (WindowState == System.Windows.WindowState.Minimized)
                this.Hide();

            base.OnStateChanged(e);
        }


        private void doUpdate() {
            UpdateStatus(Properties.Settings.Default.monitorInterval);
        }

        private void dispatcherTimer_Tick(object sender, EventArgs e) {
            if (Properties.Settings.Default.watchProcess) {
                Thread t = new Thread(doUpdate);
                t.Start();

            }
            //	UpdateStatus(Properties.Settings.Default.monitorInterval);
        }

        private void MetroWindow_Deactivated(object sender, EventArgs e) {
            GameListView.UnselectAll();
        }

        private void GameListView_MouseDoubleClick(object sender, MouseButtonEventArgs e) {
            var listView = (ListView)sender;
            var item = listView.ContainerFromElement((DependencyObject)e.OriginalSource) as ListViewItem;
            if (item != null) {
                GameExecutionInfo g = (GameExecutionInfo)GameListView.SelectedItem;
                if (g != null) g.run();
            }
        }
        /*	private void MWindow_Closing(object sender, CancelEventArgs e)
            {
                ExportXML(Utility.strSourcePath + @"data.xml");
            }*/
        #endregion

        #region MenuRegion
        private void AddGameFromESID_Click(object sender, RoutedEventArgs e) {
            Dialogs.AddGameFromESIDDialog ad = new Dialogs.AddGameFromESIDDialog();
            ad.Owner = this;
            if (ad.ShowDialog() == true) {
                GameExecutionInfo i = new GameExecutionInfo(ad.Value);
                items.Add(i);
                db.InsertOrReplaceGame(i);

                UpdateStatus();
                OnPropertyChanged("ItemCount");
            }

        }
        private void AddGameFromProcess_Click(object sender, RoutedEventArgs e) {
            Dialog.ProcessDialog pd = new Dialog.ProcessDialog();
            pd.Owner = this;
            if (pd.ShowDialog() == true) {
                GameExecutionInfo i = new GameExecutionInfo(pd.SelectedProc.ProcTitle, pd.SelectedProc.ProcPath);
                items.Add(i);
                db.InsertOrReplaceGame(i);

                loadGridData(false);
                //UpdateStatus();
                //OnPropertyChanged("ItemCount");
            }
        }

        private void SettingButton_Click(object sender, RoutedEventArgs e) {
            Dialogs.GlobalSettingDialog gd = new Dialogs.GlobalSettingDialog();
            gd.Owner = this;
            int mi = Properties.Settings.Default.monitorInterval;
            bool watchProc = Properties.Settings.Default.watchProcess;
            bool enableScr = Properties.Settings.Default.enableScreenShot;
            if (gd.ShowDialog() == true) {
                GameListView.Items.Refresh();
                RegisterInStartup(Properties.Settings.Default.setStartUp);
                bool watchProc_a = Properties.Settings.Default.watchProcess;
                if (watchProc != watchProc_a) {
                    WatchProc = watchProc_a;
                }
                bool enableScr_a = Properties.Settings.Default.enableScreenShot;
                if (enableScr ^ enableScr_a) {
                    if (enableScr_a) {
                        _hotkeyDoSceenShot = new HotKey(Key.D, KeyModifier.Alt, OnHotKeyHandler_DoSceenShot);
                    } else {
                        if (_hotkeyDoSceenShot != null) {
                            _hotkeyDoSceenShot.Unregister();
                        }
                    }
                }
                if (mi != Properties.Settings.Default.monitorInterval) {
                    watchProcTimer.Stop();
                    watchProcTimer.Interval = new TimeSpan(0, 0, Properties.Settings.Default.monitorInterval);
                    watchProcTimer.Start();
                }
            }
        }

        private void ImportList_Click(object sender, RoutedEventArgs e) {
            OpenFileDialog openFileDialog1 = new OpenFileDialog();
            openFileDialog1.Multiselect = false;
            openFileDialog1.Filter = "Etupirka GameData XMLFile(*.xml)|*.xml";
            openFileDialog1.FilterIndex = 1;
            if (openFileDialog1.ShowDialog() == true) {
                ImportXML(openFileDialog1.FileName);
            }
        }

        private void ImportList_Overwrite_Click(object sender, RoutedEventArgs e) {
            OpenFileDialog openFileDialog1 = new OpenFileDialog();
            openFileDialog1.Multiselect = false;
            openFileDialog1.Filter = "Etupirka GameData XMLFile(*.xml)|*.xml";
            openFileDialog1.FilterIndex = 1;
            if (openFileDialog1.ShowDialog() == true) {
                ImportXML_Overwrite(openFileDialog1.FileName);
            }
        }

        private void ImportPlayTimeList_Click(object sender, RoutedEventArgs e) {
            OpenFileDialog openFileDialog1 = new OpenFileDialog();
            openFileDialog1.Multiselect = false;
            openFileDialog1.Filter = "Etupirka PlayTimeData XMLFile(*.xml)|*.xml";
            openFileDialog1.FilterIndex = 1;
            if (openFileDialog1.ShowDialog() == true) {
                ImportTimeDict(openFileDialog1.FileName);
            }
        }

        private void ImportPlayTimeList_Overwrite_Click(object sender, RoutedEventArgs e) {
            OpenFileDialog openFileDialog1 = new OpenFileDialog();
            openFileDialog1.Multiselect = false;
            openFileDialog1.Filter = "Etupirka PlayTimeData XMLFile(*.xml)|*.xml";
            openFileDialog1.FilterIndex = 1;
            if (openFileDialog1.ShowDialog() == true) {
                ImportTimeDict_Overwrite(openFileDialog1.FileName);
            }
        }

        private void ExportList_Click(object sender, RoutedEventArgs e) {
            SaveFileDialog openFileDialog1 = new SaveFileDialog();
            openFileDialog1.Filter = "ErogeTimerXMLFile(*.xml)|*.xml";
            openFileDialog1.FilterIndex = 1;
            if (openFileDialog1.ShowDialog() == true) {
                ExportXML(openFileDialog1.FileName);
                ExportTimeDict(System.IO.Path.ChangeExtension(openFileDialog1.FileName, ".time.xml"));
            }
        }

        private void ExportSelect_Click(object sender, RoutedEventArgs e) {
            SaveFileDialog openFileDialog1 = new SaveFileDialog();
            openFileDialog1.Filter = "ErogeTimerXMLFile(*.xml)|*.xml";
            openFileDialog1.FilterIndex = 1;
            List<GameExecutionInfo> t = GameListView.SelectedItems.Cast<GameExecutionInfo>().ToList();
            if (openFileDialog1.ShowDialog() == true) {

                foreach (GameExecutionInfo i in t) {
                    GameListView.SelectedItems.Add(i);
                }
                ExportXML(openFileDialog1.FileName, t);
                ExportTimeDict(System.IO.Path.ChangeExtension(openFileDialog1.FileName, ".time.xml"), t);
            }
        }
        /*
                private void ImportFromErogeTimer_Click(object sender, RoutedEventArgs e)
                {
                    OpenFileDialog openFileDialog1 = new OpenFileDialog();
                    openFileDialog1.Multiselect = false;
                    openFileDialog1.Filter = "ErogeTimerXMLFile(*.xml)|*.xml";
                    openFileDialog1.FilterIndex = 1;
                    if (openFileDialog1.ShowDialog() == true)
                    {
                        string dataPath = openFileDialog1.FileName;
                        ErogeNode[] erogeList = null;
                        try
                        {
                            System.Xml.Serialization.XmlSerializer serializer2 = new System.Xml.Serialization.XmlSerializer(typeof(ErogeNode[]));
                            System.IO.StreamReader sr = new System.IO.StreamReader(dataPath);
                            erogeList = (ErogeNode[])serializer2.Deserialize(sr);
                            sr.Close();
                        }
                        catch
                        {
                        }
                        if (erogeList != null)
                        {
                            foreach (ErogeNode i in erogeList)
                            {
                                GameExecutionInfo game = new GameExecutionInfo(i.Title, i.Path);
                                game.TotalPlayTime = i.Time;
                                items.Add(game);
                            }
                        }
                        UpdateStatus();
                        OnPropertyChanged("ItemCount");
                    }
                }*/

        private void PlayTimeStatistic_Click(object sender, RoutedEventArgs e) {
            try {
                Dialog.PlayTimeStatisticDialog pd = new Dialog.PlayTimeStatisticDialog(Utility.userDBPath);
                pd.Owner = this;
                if (pd.ShowDialog() == true) {
                }
            } catch (Exception a) {
                MessageBox.Show(a.Message);
            }

        }

        /*	private Task<byte[]> getDataAsync(string url)
            {
                return NetworkUtility.GetData(url);  
            }
            */
        private async void UpdateOfflineDatabase_Click(object sender, RoutedEventArgs e) {
            try {
                var controller = await this.ShowProgressAsync("更新しています", "Initializing...");
                controller.SetCancelable(true);
                await Task.Delay(1000);

                controller.SetMessage("Downloading...");
                string url = Properties.Settings.Default.databaseSyncServer;
                url = url.TrimEnd('/') + "/" + "esdata.gz";
                if (controller.IsCanceled) {
                    await controller.CloseAsync();
                    await this.ShowMessageAsync("データベースを更新する", "失敗しました");
                    return;
                }
                var data = await Task.Run(() => { return NetworkUtility.GetData(url); });
                if (controller.IsCanceled) {
                    await controller.CloseAsync();
                    await this.ShowMessageAsync("データベースを更新する", "失敗しました");
                    return;
                }

                controller.SetMessage("Decompressing...");
                var s = await Task.Run(() => { return Encoding.UTF8.GetString(Utility.Decompress(data)); });
                if (controller.IsCanceled) {
                    await controller.CloseAsync();
                    await this.ShowMessageAsync("データベースを更新する", "失敗しました");
                    return;
                }

                controller.SetMessage("Updating database...");
                bool re = await Task.Run(() => { return Utility.im.update(s.Split('\n')); });
                await controller.CloseAsync();
                if (re) {
                    await this.ShowMessageAsync("データベースを更新する", "成功しました");
                } else {
                    await this.ShowMessageAsync("データベースを更新する", "失敗しました");
                }
            } catch {
                return;
            }
        }

        private void QuitApp_Click(object sender, RoutedEventArgs e) {
            Application.Current.Shutdown();
        }

        private async void showMessage(string title, string str) {
            MessageDialogResult re = await this.ShowMessageAsync(title, str);

        }
        private void VersionInfo_Click(object sender, RoutedEventArgs e) {

            showMessage("バージョン情報", "Version " + FileVersionInfo.GetVersionInfo(System.Reflection.Assembly.GetExecutingAssembly().Location).ProductVersion +
                "\r\nOriginal Developed By Aixile (@namaniku0)."
                + "\r\nModified By Vibbit (@L8102259)."
                );

        }

        private void RefreshList_Click(object sender, RoutedEventArgs e) {
            loadGridData(false);
        }

        #endregion

        #region ContextMenuRegion
        private void GameProperty_Click(object sender, RoutedEventArgs e) {
            if (GameListView.SelectedItems.Count > 0) {
                GameExecutionInfo i = (GameExecutionInfo)GameListView.SelectedItem;
                Dialog.GamePropertyDialog gd = new Dialog.GamePropertyDialog(i);
                gd.Owner = this;
                if (gd.ShowDialog() == true) {
                    UpdateStatus();
                    db.UpdateGameInfoAndExec(i);
                }
            }
        }

        private void TimeSetting_Click(object sender, RoutedEventArgs e) {
            if (GameListView.SelectedItems.Count > 0) {
                GameExecutionInfo i = (GameExecutionInfo)GameListView.SelectedItem;
                Dialog.TimeSettingDialog td = new Dialog.TimeSettingDialog(i);

                td.Owner = this;
                if (td.ShowDialog() == true) {
                    UpdateStatus();
                    db.UpdateGameTimeInfo(i.UID, i.TotalPlayTime, i.FirstPlayTime, i.LastPlayTime);
                }
            }
        }

        private void OpenPlayStatistics_Click(object sender, RoutedEventArgs e) {
            if (GameListView.SelectedItems.Count > 0) {
                GameExecutionInfo i = (GameExecutionInfo)GameListView.SelectedItem;
                List<TimeSummary> t = db.QueryGamePlayTime(i.UID);
                Dialog.GameTimeStatisticDialog td = new Dialog.GameTimeStatisticDialog(t, i.Title);

                td.Owner = this;
                if (td.ShowDialog() == true) {
                }
            }
        }

        private async void DoDelete() {
            ArrayList a = new ArrayList(GameListView.SelectedItems);
            if (a.Count != 0) {
                MessageDialogResult re = await this.ShowMessageAsync("本当に削除しますか？", "", MessageDialogStyle.AffirmativeAndNegative);
                if (re != MessageDialogResult.Affirmative) {
                    return;
                }
            }
            foreach (GameExecutionInfo i in a) {
                db.DeleteGame(i.UID);
                (GameListView.ItemsSource as ObservableCollection<GameExecutionInfo>).Remove(i);
            }

            OnPropertyChanged("ItemCount");
        }

        private void Delete_Click(object sender, RoutedEventArgs e) {
            DoDelete();
        }

        private void DoOpenGame() {
            GameExecutionInfo g = (GameExecutionInfo)GameListView.SelectedItem;
            if (g != null) g.run();
            UpdateStatus();
        }

        private void OpenGame_Click(object sender, RoutedEventArgs e) {
            DoOpenGame();
        }

        private void OpenFolder_Click(object sender, RoutedEventArgs e) {
            if (GameListView.SelectedItems.Count > 0) {
                try {
                    GameExecutionInfo i = (GameExecutionInfo)GameListView.SelectedItem;
                    Process.Start(System.IO.Path.GetDirectoryName(i.ExecPath));
                } catch {

                }
            }
        }

        private void OpenComment_Click(object sender, RoutedEventArgs e) {
            if (GameListView.SelectedItems.Count > 0) {
                GameExecutionInfo i = (GameExecutionInfo)GameListView.SelectedItem;
                Dialog.GameComment gc = new Dialog.GameComment(i);
                gc.Owner = this;
                if (gc.ShowDialog() == true) {
                    db.UpdateGameComment(i);
                }
            }
        }

        private void ShowApp_Click(object sender, RoutedEventArgs e) {
            this.Show();
            this.WindowState = WindowState.Normal;
        }

        private void OpenES_Click(object sender, RoutedEventArgs e) {
            GameExecutionInfo g = (GameExecutionInfo)GameListView.SelectedItem;
            if (g != null && g.ErogameScapeID != 0) {
                System.Diagnostics.Process.Start("http://erogamescape.dyndns.org/~ap2/ero/toukei_kaiseki/game.php?game=" + g.ErogameScapeID);
            }
        }

        private void CopyTitle_Click(object sender, RoutedEventArgs e) {
            GameExecutionInfo g = (GameExecutionInfo)GameListView.SelectedItem;
            if (g != null) {
                Clipboard.SetText(g.Title);
            }

        }

        private void CopyBrand_Click(object sender, RoutedEventArgs e) {
            GameExecutionInfo g = (GameExecutionInfo)GameListView.SelectedItem;
            if (g != null) {
                Clipboard.SetText(g.Brand);
            }
        }

        private void TryGetGameInfo_Click(object sender, RoutedEventArgs e) {
            List<GameInfo> allGames = Utility.im.getAllEsInfo();
            GameExecutionInfo g = (GameExecutionInfo)GameListView.SelectedItem;
            Dictionary<GameInfo, int> dic = new Dictionary<GameInfo, int>();
            string title = g.Title;

            foreach (GameInfo i in allGames) {
                int dist = StringProcessing.LevenshteinDistance(title, i.Title);
                dic[i] = dist;
            }
            var ordered = dic.OrderBy(x => x.Value);
            GameInfo ans = ordered.ElementAt(0).Key;
            //g.ErogameScapeID = ans.ErogameScapeID;
            //g.Title = ans.Title;
            //g.Brand = ans.Brand;
            //g.SaleDay = ans.SaleDay;

            List<GameInfo> ans_l = new List<GameInfo>();
            for (int i = 0; i < 50; i++) {
                ans_l.Add(ordered.ElementAt(i).Key);
            }

            Dialog.GameInfoDialog td = new Dialog.GameInfoDialog(ans_l);

            td.Owner = this;
            if (td.ShowDialog() == true) {
                g.ErogameScapeID = td.SelectedGameInfo.ErogameScapeID;
                g.Title = td.SelectedGameInfo.Title;
                g.Brand = td.SelectedGameInfo.Brand;
                g.SaleDay = td.SelectedGameInfo.SaleDay;
                g.IsNukige = false;
                UpdateStatus();
                db.UpdateGameInfoAndExec(g);
            }

        }

        private delegate void RunOnUiThread();
        private async void TryGetGameInfoOnline_Click(object sender, RoutedEventArgs e) {

            GameExecutionInfo thisGame = (GameExecutionInfo)GameListView.SelectedItem;

            var progressDialog = await this.ShowProgressAsync("通信中", "ESからゲームデータを取得しています...", true);

            new Thread(() => {
                while (progressDialog.IsOpen) {
                    if (progressDialog.IsCanceled) {
                        RunOnUiThread runOnUiThread = new RunOnUiThread(async () => {
                            await progressDialog.CloseAsync();
                            await this.ShowMessageAsync("キャンセル", "通信がキャンセルされました");
                        });
                        this.Dispatcher.Invoke(runOnUiThread);
                        break;
                    }
                    Thread.Sleep(500);
                }
            }).Start();

            bool result1 = false;
            List<GameInfo> searchedList = new List<GameInfo>();
            try {
                searchedList = await searchGameFromES(thisGame);
                result1 = true;
            } catch (Exception ex) {
                Console.WriteLine(ex.Message);
            }

            if (progressDialog.IsCanceled) {
                return;
            }

            await progressDialog.CloseAsync();

            if (!result1) {
                await this.ShowMessageAsync("通信中", "データの取得が失敗しました");
                return;
            } 

            Dialog.GameInfoDialog td = new Dialog.GameInfoDialog(searchedList);

            td.Owner = this;
            if (td.ShowDialog() == true) {
                thisGame.ErogameScapeID = td.SelectedGameInfo.ErogameScapeID;

                var controller = await this.ShowProgressAsync("通信中", "ESからゲームデータを取得しています...", true);

                new Thread(() => {
                    while (controller.IsOpen) {
                        if (controller.IsCanceled) {
                            RunOnUiThread runOnUiThread = new RunOnUiThread(async () => {
                                await controller.CloseAsync();
                                await this.ShowMessageAsync("キャンセル", "通信がキャンセルされました");
                            });
                            this.Dispatcher.Invoke(runOnUiThread);
                            break;
                        }
                        Thread.Sleep(500);
                    }
                }).Start();

                bool result = false;

                try {
                    await thisGame.updateInfoFromES();
                    UpdateStatus();
                    db.UpdateGameInfoAndExec(thisGame);
                    result = true;
                } catch(Exception ex) {
                    Console.WriteLine(ex.Message);
                }

                if (!controller.IsCanceled) {
                    await controller.CloseAsync();
                    if (result) {
                        await this.ShowMessageAsync("通信中", "データの取得が成功しました");
                    } else {
                        await this.ShowMessageAsync("通信中", "データの取得が失敗しました");
                    }
                }
            }
        }

        #endregion
    }

    #region Command
    public class ShowAppCommand : ICommand
    {
        MainWindow m;
        public ShowAppCommand(MainWindow _m) {
            m = _m;
        }

        public void Execute(object parameter) {
            if (m != null) {
                m.Show();
                m.WindowState = WindowState.Normal;
            }
        }

        public bool CanExecute(object parameter) {
            return true;
        }
        public event EventHandler CanExecuteChanged;
    }
    #endregion

    #region Converter

    public class IndexConverter : IValueConverter
    {
        public object Convert(object value, Type TargetType, object parameter, CultureInfo culture) {
            var item = (ListViewItem)value;
            var listView = ItemsControl.ItemsControlFromItemContainer(item) as ListView;
            int index = listView.ItemContainerGenerator.IndexFromContainer(item) + 1;
            return index.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            throw new NotImplementedException();
        }
    }

    public class PathStyleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter,
            System.Globalization.CultureInfo culture) {
            if (Properties.Settings.Default.differExecuatableGame && !(bool)value) {
                return "#808080";
            } else {
                return "#FFFFFF";
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter,
            System.Globalization.CultureInfo culture) {
            throw new NotSupportedException();
        }
    }

    public class StatusStyleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter,
            System.Globalization.CultureInfo culture) {
            switch ((ProcStat)value) {
                case ProcStat.NotExist:
                    return "#808080";
                case ProcStat.Rest:
                    return "#f5f5f5";
                case ProcStat.Focused:
                    return "#05d3ff";
                case ProcStat.Unfocused:
                    return "#0e6c8f";
                default:
                    return "#f5f5f5";
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter,
            System.Globalization.CultureInfo culture) {
            throw new NotSupportedException();
        }
    }

    public class StatusBarStyleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter,
            System.Globalization.CultureInfo culture) {
            if ((StatusBarStatus)value == StatusBarStatus.watching) {
                return "#1585b5";
            } else if ((StatusBarStatus)value == StatusBarStatus.resting) {
                return "#d27e05";
            } else {
                return "#252525";
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter,
            System.Globalization.CultureInfo culture) {
            throw new NotSupportedException();
        }
    }

    #endregion
}
