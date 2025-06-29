using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
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
            TurnLabel.Text = "Turn: You";

            // spored br.na parovi set na progres bart
            ProgressBarMatches.Maximum = totalPairs;
            ProgressBarMatches.Value = 0;

            StartTimer();

            Random rnd = new Random();

            List<string> imageFiles = Directory.GetFiles($"Images/{difficulty}")
                                   .Where(f => f.EndsWith(".png"))
                                   .OrderBy(x => rnd.Next())
                                   .Take(totalPairs)
                                   .ToList();

            // dupliraj za da ima par
            List<string> allImages = imageFiles.SelectMany(f => new[] { f, f }).ToList();

            // meshanje
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
                btn.Padding = new Thickness(0); // vaznooo
                btn.Margin = new Thickness(5);
                btn.BorderThickness = new Thickness(0);


                // centriranje
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
                ProgressBarMatches.Value = totalMatches;
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

            // turn na kompjuterot
            isPlayerTurn = false;
            TurnLabel.Text = "Turn: Computer";

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
                ProgressBarMatches.Value = totalMatches;
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
            TurnLabel.Text = "Turn: You";
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
            ProgressBarMatches.Value = totalMatches;
            tries++;
            UpdateStatusText();

            if (totalMatches >= totalPairs)
            {
                EndGame();
                return;
            }

            isPlayerTurn = true;
            TurnLabel.Text = "Turn: You";
        }

        private void EndGame()
        {
            timer.Stop();
            MainMenu.AddHighScore(difficulty, seconds, tries);

            // MessageBox za info samo
            MessageBox.Show($"🎉 Finished in {seconds} seconds with {tries} moves!", "Congratulations!");

            // sega panelot
            EndGamePanel.Visibility = Visibility.Visible;
            EndGameMessage.Text = "What would you like \nto do next?";
            EndGameStats.Text = $"Level: {difficulty}\nTime: {seconds} seconds\nMoves: {tries}";


        }

        private void UpdateStatusText()
        {
            TimerText.Text = $"Time: {seconds} sec\nMoves: {tries}";
        }

        private void PlayAgain_Click(object sender, RoutedEventArgs e)
        {
            var newGame = new MainWindow(CardGrid.Rows, CardGrid.Columns, difficulty);
            newGame.Show();
            this.Close();
        }

        private void BackToMenu_Click(object sender, RoutedEventArgs e)
        {
            MainMenu menu = new MainMenu();
            menu.Show();
            this.Close();
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void GoToMenu_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Do you want to return to the main menu? The current game will be lost.",
                                         "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                timer?.Stop();
                MainMenu menu = new MainMenu();
                menu.Show();
                this.Close();
            }
        }

        private string saveFilePath = "savedgame.dat";

        public void SaveGame()
        {
            GameState state = new GameState
            {
                Cards = new List<Card>(),
                TotalMatches = totalMatches,
                Tries = tries,
                Seconds = seconds,
                IsPlayerTurn = isPlayerTurn,
                Difficulty = difficulty,
                BotMemoryIndices = new Dictionary<string, List<int>>()
            };

            // listata ja popolnuvame so sostjbata na kartickite
            for (int i = 0; i < CardGrid.Children.Count; i++)
            {
                Button btn = (Button)CardGrid.Children[i];
                Card card = btn.Tag as Card;

                state.Cards.Add(new Card
                {
                    Id = i,
                    ImagePath = card.ImagePath,
                    IsFlipped = card.IsFlipped,
                    IsMatched = card.IsMatched
                });
            }

            // botMemory da gi pameti indsite na kopcinjata
            foreach (var kvp in botMemory)
            {
                List<int> indices = new List<int>();
                foreach (var btn in kvp.Value)
                {
                    int index = CardGrid.Children.IndexOf(btn);
                    if (index >= 0)
                        indices.Add(index);
                }
                state.BotMemoryIndices[kvp.Key] = indices;
            }

            using (FileStream fs = new FileStream(saveFilePath, FileMode.Create))
            {
                BinaryFormatter formatter = new BinaryFormatter();
                formatter.Serialize(fs, state);
            }
        }

        public void LoadGame()
        {
            if (!File.Exists(saveFilePath))
            {
                MessageBox.Show("No saved game found.");
                return;
            }

            GameState state;

            using (FileStream fs = new FileStream(saveFilePath, FileMode.Open))
            {
                BinaryFormatter formatter = new BinaryFormatter();
                state = (GameState)formatter.Deserialize(fs);
            }

            // proveri nivo tezinata
            if (state.Difficulty != difficulty)
            {
                MessageBox.Show($"No saved game found for difficulty: {difficulty}.", "Load error");
                return;
            }

            // zemija ja sostojbata 
            totalMatches = state.TotalMatches;
            tries = state.Tries;
            seconds = state.Seconds;
            isPlayerTurn = state.IsPlayerTurn;
            difficulty = state.Difficulty;

            cards.Clear();
            CardGrid.Children.Clear();
            botMemory.Clear();

            int totalCards = state.Cards.Count;

            // dinamicki del ama ne znam
            int rows = (int)Math.Sqrt(totalCards);
            int cols = totalCards / rows;
            if (rows * cols < totalCards)
                cols++; 

            CardGrid.Rows = rows;
            CardGrid.Columns = cols;

            totalPairs = totalCards / 2; 

            for (int i = 0; i < totalCards; i++)
            {
                var cs = state.Cards[i];
                Card c = new Card
                {
                    ImagePath = cs.ImagePath,
                    IsFlipped = cs.IsFlipped,
                    IsMatched = cs.IsMatched
                };
                cards.Add(c);

                Button btn = new Button();
                btn.Tag = c;
                btn.Width = 100;
                btn.Height = 100;
                btn.Padding = new Thickness(0);
                btn.Margin = new Thickness(5);
                btn.BorderThickness = new Thickness(0);
                btn.HorizontalContentAlignment = HorizontalAlignment.Center;
                btn.VerticalContentAlignment = VerticalAlignment.Center;
                btn.Click += Card_Click;

                if (c.IsFlipped || c.IsMatched)
                {
                    btn.Content = new Image
                    {
                        Source = new BitmapImage(new Uri(c.ImagePath, UriKind.Relative)),
                        Stretch = Stretch.UniformToFill
                    };
                }
                else
                {
                    btn.Content = new Image
                    {
                        Source = new BitmapImage(new Uri("Images/square.png", UriKind.Relative)),
                        Stretch = Stretch.UniformToFill
                    };
                }

                btn.Background = Brushes.LightGray;
                CardGrid.Children.Add(btn);
            }

            // nazad memorijata na botot
            foreach (var kvp in state.BotMemoryIndices)
            {
                List<Button> btns = new List<Button>();
                foreach (int idx in kvp.Value)
                {
                    if (idx >= 0 && idx < CardGrid.Children.Count)
                    {
                        Button b = (Button)CardGrid.Children[idx];
                        btns.Add(b);
                    }
                }
                botMemory[kvp.Key] = btns;
            }

            ProgressBarMatches.Maximum = totalPairs;
            ProgressBarMatches.Value = totalMatches;
            UpdateStatusText();

            TurnLabel.Text = isPlayerTurn ? "Turn: You" : "Turn: Computer";

            timer.Stop();
            timer.Start();

            if (!isPlayerTurn)
            {
                _ = BotTurn();
            }
        }

        private void SaveGame_Click(object sender, RoutedEventArgs e)
        {
            SaveGame();
            MessageBox.Show("Game has been saved.");
        }

        private void LoadGame_Click(object sender, RoutedEventArgs e)
        {
            LoadGame();
        }

    }
}
