using System;
using System.Collections.Generic;
using System.IO;
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
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace MemoryGameWPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private List<Card> cards = new List<Card>();
        private Button firstCardBtn = null;
        private Card firstCard = null;
        private bool isPlayerTurn = true;
        private Dictionary<string, List<Button>> botMemory = new Dictionary<string, List<Button>>();
        private int totalMatches = 0;
        private int totalPairs = 8;
        private int tries = 0;
        private DispatcherTimer timer;
        private int seconds = 0;
        private string difficulty = "Easy";

        public MainWindow(int rows, int cols, string difficultyLevel)
        {
            InitializeComponent();
            difficulty = difficultyLevel;
            totalPairs = (rows * cols) / 2;
            SetupGrid(rows, cols);
            StartGame();
        }

        private void SetupGrid(int rows, int cols)
        {
            CardGrid.Rows = rows;
            CardGrid.Columns = cols;
        }

        private void StartGame()
        {
            CardGrid.Children.Clear();
            cards.Clear();
            botMemory.Clear();
            totalMatches = 0;
            seconds = 0;
            firstCard = null;
            firstCardBtn = null;
            isPlayerTurn = true;
            TurnLabel.Text = "На потег е: Ти";
            StartTimer();

            Random rnd = new Random();

            List<string> imageFiles = Directory.GetFiles($"Images/{difficulty}")
                                   .Where(f => f.EndsWith(".png"))
                                   .OrderBy(x => rnd.Next())
                                   .Take(totalPairs)
                                   .ToList();

            // Дуплирај ги за да има по два од секоја (за парови)
            List<string> allImages = imageFiles.SelectMany(f => new[] { f, f }).ToList();

            // Промешај ги сите
            allImages = allImages.OrderBy(f => rnd.Next()).ToList();
            for (int i = 0; i < allImages.Count; i++)
            {
                Card c = new Card
                {
                    ImagePath = allImages[i],
                    IsFlipped = false,
                    IsMatched = false
                };

                cards.Add(c);

                Button btn = new Button();
                ImageBrush backImage = new ImageBrush();
                backImage.ImageSource = new BitmapImage(new Uri("pack://application:,,,/Images/square.png"));
                backImage.Stretch = Stretch.UniformToFill;
                btn.Background = backImage;

                btn.Width = 100;
                btn.Height = 100;
                btn.Padding = new Thickness(0); // важно
                btn.Margin = new Thickness(5);
                btn.BorderThickness = new Thickness(0); 


                // Центрирање на содржина (ако има)
                btn.HorizontalContentAlignment = HorizontalAlignment.Center;
                btn.VerticalContentAlignment = VerticalAlignment.Center;
                btn.Tag = c;
                btn.Click += Card_Click;
                btn.Content = new Image
                {
                    Source = new BitmapImage(new Uri("Images/square.png", UriKind.Relative)),
                    Stretch = Stretch.Uniform
                };
                btn.Background = Brushes.LightGray;
                CardGrid.Children.Add(btn);
            }
        }

        private void StartTimer()
        {
            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(1);
            timer.Tick += (s, e) =>
            {
                seconds++;
                UpdateStatusText();
            };
            timer.Start();
        }

        private async void Card_Click(object sender, RoutedEventArgs e)
        {
            if (!isPlayerTurn) return;

            Button btn = sender as Button;
            Card card = btn.Tag as Card;

            if (card.IsFlipped || card.IsMatched || btn == firstCardBtn)
                return;

            FlipCard(btn, card);

            if (firstCard == null)
            {
                firstCard = card;
                firstCardBtn = btn;
                return; // Важно!
            }

            await Task.Delay(800);

            if (card.ImagePath == firstCard.ImagePath)
            {
                card.IsMatched = true;
                firstCard.IsMatched = true;
                totalMatches++;
                tries++;
                UpdateStatusText();
                if (botMemory.ContainsKey(card.ImagePath)) botMemory.Remove(card.ImagePath);

                if (totalMatches >= totalPairs)
                {
                    EndGame();
                    return;
                }
            }
            else
            {
                tries++;
                UpdateStatusText();
                UnflipCard(btn, card);
                UnflipCard(firstCardBtn, firstCard);
                AddToBotMemory(card.ImagePath, btn);
                AddToBotMemory(firstCard.ImagePath, firstCardBtn);
            }

            firstCard = null;
            firstCardBtn = null;

            // Префрли потег на компјутерот
            isPlayerTurn = false;
            TurnLabel.Text = "На потег е: Компјутерот";

            await Task.Delay(1000);
            await BotTurn();
        }

        private void FlipCard(Button btn, Card card)
        {
            card.IsFlipped = true;
            btn.Content = new Image
            {
                Source = new BitmapImage(new Uri(card.ImagePath, UriKind.Relative)),
                Stretch = Stretch.UniformToFill,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            };
        }

        private void UnflipCard(Button btn, Card card)
        {
            card.IsFlipped = false;
            btn.Content = new Image
            {
                Source = new BitmapImage(new Uri("Images/square.png", UriKind.Relative)),
                Stretch = Stretch.UniformToFill,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            };
        }

        private void AddToBotMemory(string path, Button btn)
        {
            if (!botMemory.ContainsKey(path))
                botMemory[path] = new List<Button>();
            if (!botMemory[path].Contains(btn))
                botMemory[path].Add(btn);
        }

        private async Task BotTurn()
        {
            foreach (var kvp in botMemory)
            {
                if (kvp.Value.Count >= 2)
                {
                    await BotClickPair(kvp.Value[0], kvp.Value[1]);
                    botMemory.Remove(kvp.Key);
                    return;
                }
            }

            var unknowns = CardGrid.Children.OfType<Button>()
                              .Where(b => ((b.Tag as Card).IsMatched == false && (b.Tag as Card).IsFlipped == false))
                              .ToList();
            if (unknowns.Count < 2)
            {
                EndGame();
                return;
            }

            Random rnd = new Random();
            Button b1 = unknowns[rnd.Next(unknowns.Count)];
            Button b2;
            do { b2 = unknowns[rnd.Next(unknowns.Count)]; } while (b1 == b2);

            Card c1 = b1.Tag as Card;
            Card c2 = b2.Tag as Card;

            FlipCard(b1, c1);
            FlipCard(b2, c2);
            await Task.Delay(800);

            if (c1.ImagePath == c2.ImagePath)
            {
                c1.IsMatched = true;
                c2.IsMatched = true;
                totalMatches++;
                tries++;
                UpdateStatusText();

                if (totalMatches >= totalPairs)
                {
                    EndGame();
                    return;
                }
            }
            else
            {
                tries++;
                UpdateStatusText();

                UnflipCard(b1, c1);
                UnflipCard(b2, c2);
                AddToBotMemory(c1.ImagePath, b1);
                AddToBotMemory(c2.ImagePath, b2);
            }

            isPlayerTurn = true;
            TurnLabel.Text = "На потег е: Ти";
        }

        private async Task BotClickPair(Button b1, Button b2)
        {
            Card c1 = b1.Tag as Card;
            Card c2 = b2.Tag as Card;

            FlipCard(b1, c1);
            FlipCard(b2, c2);
            await Task.Delay(800);

            c1.IsMatched = true;
            c2.IsMatched = true;
            totalMatches++;
            tries++;
            UpdateStatusText();

            if (totalMatches >= totalPairs)
            {
                EndGame();
                return;
            }

            isPlayerTurn = true;
            TurnLabel.Text = "На потег е: Ти";
        }

        private void EndGame()
        {
            timer.Stop();
            var result = MessageBox.Show(
                $"Играта заврши за {seconds} секунди со {tries} потези!\nДали сакате да играте повторно?",
                "Честитки",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                // Show the main menu
                MainMenu menu = new MainMenu();
                menu.Show();
                this.Close();
            }
            else
            {
                Application.Current.Shutdown();
            }
        }

        private void UpdateStatusText()
        {
            TimerText.Text = $"Време: {seconds} сек\nПотези: {tries}";
        }

    }
}


