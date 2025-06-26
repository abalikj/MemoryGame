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
    /// <summary>
    /// Interaction logic for MainMenu.xaml
    /// </summary>
    public partial class MainMenu : Window
    {
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
    }
}
