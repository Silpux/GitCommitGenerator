using GitCommitGenerator.Classes;
using Microsoft.Win32;
using System.Diagnostics;
using System.Globalization;
using System.IO;
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

namespace GitCommitGenerator
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        private Border[,] calendar;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            var folderDialog = new OpenFolderDialog
            {
                Title = "Choose repository location",
            };

            if (folderDialog.ShowDialog() is true)
            {
                var folderName = folderDialog.FolderName;
                RepositoryPathTextBox.Text = folderName;
                return;
            }
            //RepositoryPathTextBox.Text = "None";
        }

        private async void GenerateCommitsButton_Click(object sender, RoutedEventArgs e)
        {
            if (!IsGitInstalled())
            {
                return;
            }

            try
            {
                System.IO.Path.GetFullPath(RepositoryPathTextBox.Text);
            }
            catch
            {
                CommandOutputTextBox.Text = "Not valid path";
                return;
            }

            try
            {
                GenerateCommitsButton.IsEnabled = false;
                ExecuteCommands(RepositoryPathTextBox.Text);
            }
            catch (Exception ex)
            {
                CommandOutputTextBox.Text = $"Error: {ex.Message}";
            }
            finally
            {
                GenerateCommitsButton.IsEnabled = true;
            }


        }



        public void ExecuteCommands(string folderPath)
        {

            string authorName = $" && set GIT_AUTHOR_NAME='{AuthorTextBox.Text}'";
            if (string.IsNullOrEmpty(AuthorTextBox.Text))
            {
                authorName = string.Empty;
            }

            string authorEmail = $" && set GIT_AUTHOR_EMAIL='{EmailTextBox.Text}'";
            if (string.IsNullOrEmpty(EmailTextBox.Text))
            {
                authorEmail = string.Empty;
            }

            string committerName = $" && set GIT_COMMITTER_NAME='{AuthorTextBox.Text}'";
            if (string.IsNullOrEmpty(AuthorTextBox.Text))
            {
                committerName = string.Empty;
            }

            string committerEmail = $" && set GIT_COMMITTER_EMAIL='{EmailTextBox.Text}'";
            if (string.IsNullOrEmpty(EmailTextBox.Text))
            {
                committerEmail = string.Empty;
            }

            using (ProcessManager processManager = new ProcessManager(folderPath))
            {
                if (!IsGitRepository(folderPath))
                {
                    processManager.RunCommand("git init");
                }

                int totalCommits = GetTotalCommitsCount();

                bool rewriteFirstCommit = false;

                string filePath = System.IO.Path.Combine(folderPath, "CommitCounter.txt");
                if (File.Exists(filePath) && File.ReadAllText(filePath) == "1")
                {
                    rewriteFirstCommit = true;
                }
                File.Create(filePath).Dispose();

                int commitCount = 1;
                
                for(int i = 0; i < calendar.GetLength(0); i++)
                {
                    for(int j = 0; j < calendar.GetLength(1); j++)
                    {

                        if (TryGetCommitsInBorder(calendar[i, j], out int commitsOnDay))
                        {

                            for(int k = 0;k < commitsOnDay; k++)
                            {

                                if (rewriteFirstCommit)
                                {
                                    File.WriteAllText(filePath, string.Empty);
                                    rewriteFirstCommit = false;
                                }
                                else
                                {
                                    File.WriteAllText(filePath, commitCount.ToString());
                                }

                                processManager.RunCommand("git add CommitCounter.txt");

                                if (calendar[i, j].Tag is not DateTime)
                                {
                                    throw new InvalidOperationException(calendar[i, j].Tag.ToString());
                                }

                                DateTime commitDateTime = (DateTime)calendar[i, j].Tag;

                                string commitDate = commitDateTime.ToString("yyyy-MM-dd HH:mm:ss");

                                string committerDate = $" && set GIT_COMMITTER_DATE='{commitDate}'";
                                string authorDate = $" && set GIT_AUTHOR_DATE='{commitDate}'";

                                string envParams = $"{authorName}{authorEmail}{authorDate}{committerName}{committerEmail}{committerDate}";
                                if (envParams.StartsWith(" && "))
                                {
                                    envParams = envParams.Substring(4);
                                }

                                processManager.RunCommand($"{envParams} && git commit -m \"{commitCount}\"");


                                commitCount++;

                            }

                        }

                    }
                }

            }


        }

        private bool TryGetCommitsInBorder(Border border, out int commitsCount)
        {
            commitsCount = -1;
            if (border.Child is null)
            {
                return false;
            }

            TextBlock? commitsCountTextBlock = border.Child as TextBlock;

            if (commitsCountTextBlock is null)
            {
                throw new NullReferenceException(border.Child.ToString());
            }

            if (commitsCountTextBlock.Text.Length == 0)
            {
                return false;
            }

            commitsCount = int.Parse(commitsCountTextBlock.Text);
            return true;

        }

        private int GetTotalCommitsCount()
        {
            int result = 0;
            for (int i = 0; i < calendar.GetLength(0); i++)
            {
                for (int j = 0; j < calendar.GetLength(1); j++)
                {
                    if(TryGetCommitsInBorder(calendar[i, j], out int commitsOnDay))
                    {
                        result += commitsOnDay;
                    }
                }
            }
            return result;
        }


        private void GenerateTable_Click(object sender, RoutedEventArgs e)
        {
            CalendarGrid.Children.Clear();
            calendar = new Border[0, 0];

            if (int.TryParse(YearInput.Text, out int year) && year >= 1000 && year <= 9999)
            {
                DateTime firstDay = new DateTime(year, 1, 1);
                DateTime lastDay = new DateTime(year, 12, 31);
                int daysInYear = (lastDay - firstDay).Days + 1;

                int firstDayOffset = (int)firstDay.DayOfWeek;
                int totalWeeks = (int)Math.Ceiling((daysInYear + firstDayOffset) / 7.0);

                CalendarGrid.Rows = 7;
                CalendarGrid.Columns = totalWeeks;

                calendar = new Border[totalWeeks, 7];

                CornerRadius cornerRadius = new CornerRadius()
                {
                    TopRight = 3,
                    BottomRight = 3,
                    BottomLeft = 3,
                    TopLeft = 3,
                };

                DateTime currentDay = firstDay;
                for (int week = 0; week < totalWeeks; week++)
                {
                    for (int day = 0; day < 7; day++)
                    {
                        Border dayRect;
                        if ((week is 0 && day < firstDayOffset) || currentDay > lastDay)
                        {
                            dayRect = new Border
                            {
                                Width = 25,
                                Height = 25,
                                Margin = new Thickness(2),
                                Background = Brushes.Transparent
                            };
                        }
                        else
                        {
                            TextBlock counterText = new TextBlock
                            {
                                Text = string.Empty,
                                HorizontalAlignment = HorizontalAlignment.Center,
                                VerticalAlignment = VerticalAlignment.Center,
                                FontSize = 14,
                                FontWeight = FontWeights.Bold
                            };
                            dayRect = new Border
                            {
                                Width = 25,
                                Height = 25,
                                CornerRadius = cornerRadius,
                                Margin = new Thickness(2),
                                Background = Brushes.DarkGray,
                                ToolTip = currentDay.ToString("dddd, MMMM d, yyyy", new CultureInfo("en-us")),
                                Tag = currentDay
                            };

                            dayRect.Child = counterText;
                            currentDay = currentDay.AddDays(1);
                        }
                        dayRect.MouseLeftButtonDown += (s, ev) =>
                        {
                            AddCommitsToDay(s, multiplier: 1);
                        };
                        dayRect.MouseRightButtonDown += (s, ev) =>
                        {
                            AddCommitsToDay(s, multiplier: -1);
                        };

                        calendar[week, day] = dayRect;
                    }
                }

                for (int day = 0; day < 7; day++)
                {
                    for (int week = 0; week < totalWeeks; week++)
                    {
                        CalendarGrid.Children.Add(calendar[week, day]);
                    }
                }
            }
            else
            {
                MessageBox.Show("Not a valid year. Must be 1000-9999.");
            }
        }

        public void AddCommitsToDay(object s, int multiplier)
        {

            if (s is Border border && border.Child is TextBlock textBlock)
            {
                int currentCount;

                if (int.TryParse(textBlock.Text, out currentCount))
                {
                    ;
                }
                else
                {
                    currentCount = 0;
                }

                if (int.TryParse(CommitIncrementTextBox.Text, out int commitIncrement))
                {
                    int resultCommits = currentCount + commitIncrement * multiplier;
                    if (resultCommits <= 0)
                    {
                        textBlock.Text = string.Empty;
                        return;
                    }
                    textBlock.Text = resultCommits.ToString();
                }
                else
                {
                    MessageBox.Show("Invalid commit increment value");
                }
            }
        }


        public bool IsGitRepository(string folderPath)
        {
            string gitFolderPath = System.IO.Path.Combine(folderPath, ".git");
            return Directory.Exists(gitFolderPath);
        }

        public bool IsGitInstalled()
        {
            try
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "git",
                        Arguments = "--version",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };

                process.Start();
                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();
                process.WaitForExit();

                if (process.ExitCode == 0 && !string.IsNullOrEmpty(output))
                {
                    // CommandOutputTextBox.Text = $"Output: {output}";
                    return true;
                }

                if (!string.IsNullOrEmpty(error))
                {
                    CommandOutputTextBox.Text = $"Error: {error}";
                }
            }
            catch (Exception ex)
            {
                CommandOutputTextBox.Text = $"Git is not installed";
            }

            return false;
        }
    }
}