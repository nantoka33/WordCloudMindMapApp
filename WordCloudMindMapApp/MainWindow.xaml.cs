using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using WordCloudSharp;

namespace WordCloudMindMapApp
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            InitializeComponent();
        }
        private void LoadFile_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "テキストファイル (*.txt)|*.txt|すべてのファイル (*.*)|*.*"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    var content = File.ReadAllText(openFileDialog.FileName, Encoding.UTF8);
                    InputTextBox.Text = content;
                }
                catch (Exception ex)
                {
                    MessageBox.Show("ファイル読み込みエラー: " + ex.Message);
                }
            }
        }

        private void GenerateWordCloud_Click(object sender, RoutedEventArgs e)
        {
            var text = InputTextBox.Text?.Trim();
            if (string.IsNullOrEmpty(text))
            {
                MessageBox.Show("テキストを入力してください。");
                return;
            }

            var freq = AnalyzeText(text);
            if (freq.Count == 0)
            {
                MessageBox.Show("単語が検出されませんでした。\n名詞のみ抽出しています。");
                return;
            }

            var wordList = freq.Keys.ToList();
            var freqList = freq.Values.ToList();

            var wordCloud = new WordCloud(800, 400);
            Bitmap bitmap = (Bitmap)wordCloud.Draw(wordList, freqList);
            WordCloudImage.Source = BitmapToImageSource(bitmap);
        }

        private Dictionary<string, int> AnalyzeText(string text)
        {
            var wordCounts = new Dictionary<string, int>();
            var words = TokenizeWithMeCab(text);
            foreach (var word in words)
            {
                if (wordCounts.ContainsKey(word)) wordCounts[word]++;
                else wordCounts[word] = 1;
            }
            return wordCounts;
        }

        private List<string> TokenizeWithMeCab(string text)
        {
            var tokens = new List<string>();
            var psi = new ProcessStartInfo
            {
                FileName = @"C:\Program Files\MeCab\bin\mecab.exe",
                Arguments = "",
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.GetEncoding("shift_jis")
            };

            try
            {
                using (var process = Process.Start(psi))
                {
                    if (process == null) return tokens;

                    process.StandardInput.WriteLine(text);
                    process.StandardInput.Close();

                    string? line;
                    while ((line = process.StandardOutput.ReadLine()) != null)
                    {
                        if (line == "EOS") break;

                        var parts = line.Split('\t');
                        if (parts.Length < 2) continue;

                        var surface = parts[0];
                        var features = parts[1].Split(',');
                        if (features.Length > 0 && features[0] == "名詞")
                        {
                            if (!string.IsNullOrWhiteSpace(surface) && surface.Length > 1)
                                tokens.Add(surface);
                        }
                    }

                    process.WaitForExit();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("MeCabエラー: " + ex.Message);
            }

            return tokens;
        }

        private BitmapImage BitmapToImageSource(Bitmap bitmap)
        {
            using var memory = new MemoryStream();
            bitmap.Save(memory, ImageFormat.Png);
            memory.Position = 0;
            var bitmapImage = new BitmapImage();
            bitmapImage.BeginInit();
            bitmapImage.StreamSource = memory;
            bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
            bitmapImage.EndInit();
            return bitmapImage;
        }
    }
}
