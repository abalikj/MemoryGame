using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace MemoryGameWPF
{
    
    public partial class MainMenu : Window
    {

        // listata so site skorovi so nivo
        private static List<(string Difficulty, double Score)> highScores = new List<(string, double)>();
        public MainMenu()
        {
            InitializeComponent();
        }

        private void Easy_Click(object sender, RoutedEventArgs e)
        {
            var game = new MainWindow(2, 4, "Easy"); // 4x2
            game.Show();
            this.Close();
        }

        private void Medium_Click(object sender, RoutedEventArgs e)
        {
            var game = new MainWindow(4, 4, "Medium"); // 4x4
            game.Show();
            this.Close();
        }

        private void Hard_Click(object sender, RoutedEventArgs e)
        {
            var game = new MainWindow(6, 6, "Hard"); // 6x6
            game.Show();
            this.Close();
        }

        public static void AddHighScore(string difficulty, double timeSeconds, int moves)
        {
            double score = timeSeconds * moves;
            highScores.Add((difficulty, score));
        }

        private void HighScore_Click(object sender, RoutedEventArgs e)
        {
            MainMenuPanel.Visibility = Visibility.Collapsed;
            HighScoresPanel.Visibility = Visibility.Visible;

            HighScoresListBox.Items.Clear();

            if (highScores.Count == 0)
            {
                HighScoresListBox.Items.Add("No scores yet.");
                return;
            }

            // najdobriot rezultat e toj so najmal score
            var best = highScores.OrderBy(s => s.Score).First();

            
            HighScoresListBox.Items.Add($"Best score: {best.Score:F2} ({best.Difficulty})");
        }

        private void Back_Click(object sender, RoutedEventArgs e)
        {
            HighScoresPanel.Visibility = Visibility.Collapsed;
            MainMenuPanel.Visibility = Visibility.Visible;
        }
    }
}
