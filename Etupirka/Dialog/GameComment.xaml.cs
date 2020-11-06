using MahApps.Metro.Controls;
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

namespace Etupirka.Dialog
{
    /// <summary>
    /// Interaction logic for GameComment.xaml
    /// </summary>
    public partial class GameComment : MetroWindow
    {
        private GameExecutionInfo game;
        public GameComment() {
            InitializeComponent();
        }
        public GameComment(GameExecutionInfo g) {
            InitializeComponent();
            game = g;
            if (Properties.Settings.Default.disableGlowBrush) {
                this.GlowBrush = null;
            }
            this.DataContext = game;
        }

        private void btnSave_Click(object sender, RoutedEventArgs e) {
            game.Comment = commentBox.Text;
            this.DialogResult = true;
        }
    }
}
