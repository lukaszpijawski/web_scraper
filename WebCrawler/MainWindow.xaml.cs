using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.IO;
using System.Text.RegularExpressions;
using CrawlerLibrary;


namespace WebCrawler
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        #region Variables
        public List<string> SearchAboutList = null;
        public List<string> PagesFromWhichDownloadImagesList = null;              
        public List<string> PagesFromWhichDownloadHtmlCodeList = null;                
        public string InfoFilePath = AppDomain.CurrentDomain.BaseDirectory;       //directory of the .exe file
        public string ImagesFilePath = AppDomain.CurrentDomain.BaseDirectory;        
        public string HtmlFilePath = AppDomain.CurrentDomain.BaseDirectory;
        #endregion

        public MainWindow()
        {
            InitializeComponent();
            FilePathTextBlock1.Text = InfoFilePath;
            FilePathTextBlock2.Text = ImagesFilePath;            
            FilePathTextBlock4.Text = HtmlFilePath;
        }

        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);            
        }

        #region Getting Data

        private string getTextFromRichTextBox(CheckBox checkBox, RichTextBox richTextBox)
        {
            string SearchAboutString = String.Empty;
            if (checkBox.IsChecked == true)
            {
                SearchAboutString = new TextRange(richTextBox.Document.ContentStart, richTextBox.Document.ContentEnd).Text;                
            }
            return SearchAboutString.Trim();        
        }

        private int getNumberFromTextBox(CheckBox checkBox, TextBox textBox)
        {
            int numberFromTextBox = 0;            
            if (checkBox.IsEnabled == true)
            {
                if (Int32.TryParse(textBox.Text, out numberFromTextBox) == false)
                {
                    numberFromTextBox = 0;
                }
            }            
            return numberFromTextBox;
        }

        private void getDataFromSearchAboutField()
        {
            string textFromInput;
            textFromInput = getTextFromRichTextBox(SearchAboutCheckBox, SearchAboutRichTextBox);
            if (textFromInput != String.Empty)
            {
                SearchAboutList = new List<string>(textFromInput.Split('\n').Where(s => !String.IsNullOrWhiteSpace(s)));                
            }
        }

        private void getDataFromDownloadImagesFromPagesField()
        {
            string textFromInput;
            textFromInput = getTextFromRichTextBox(DownloadImagesFromPagesCheckBox, DownloadImagesFromPagesRichTextbox);
            if (textFromInput != String.Empty)
            {
                PagesFromWhichDownloadImagesList = new List<string>(textFromInput.Split('\n').Where(s => !String.IsNullOrWhiteSpace(s)));
            }
        }        

        private void getDataFromDownloadHtmlCodeFromPagesField()
        {
            string textFromInput;
            textFromInput = getTextFromRichTextBox(DownloadHtmlCodeFromPagesCheckBox, DownloadHtmlCodeFromPagesRichTextBox);
            if (textFromInput != String.Empty)
            {
                PagesFromWhichDownloadHtmlCodeList = new List<string>(textFromInput.Split('\n').Where(s => !String.IsNullOrWhiteSpace(s)));
            }
        }

        #endregion

        private string CreateDirectory(string folderPath, string folderName)
        {
            folderPath = folderPath.Trim();
            folderName = folderName.Trim();
            string path = folderPath + folderName;
            try
            {
                int i = 1;
                while (Directory.Exists(path))                
                {
                    Console.WriteLine("That path exists already.");
                    path += i.ToString();
                    i++;
                }
                DirectoryInfo directory = Directory.CreateDirectory(path);
                Console.WriteLine("The directory {1} was created successfully at {0}.", Directory.GetCreationTime(path), path);
            }
            catch (Exception e)
            {
                Console.WriteLine("The process failed: {0}", e.ToString());
            }
            return path + "\\";
        }

        private async void SearchButtonWiki_ClickAsync(object sender, RoutedEventArgs e)
        {
            InfoStatusTextBlock.Text = "Trwa pobieranie danych...";
            getDataFromSearchAboutField();
            await Crawler.GetInfoFromWikipediaAndSaveToFilePathAsync(SearchAboutList, InfoFilePath);            
            InfoStatusTextBlock.Text = "Pobieranie ukończone";            
        }

        private async void SearchButtonImages_ClickAsync(object sender, RoutedEventArgs e)
        {
            ImagesStatusTextBlock.Text = "Trwa pobieranie danych...";
            getDataFromDownloadImagesFromPagesField();
            await Task.Run(() =>
            {
               foreach (string url in PagesFromWhichDownloadImagesList)
               {
                    string trimmedUrl = url.Trim();
                    string folderName = MakeProperFileNameFromUrlWithEnding(trimmedUrl, "_obrazki");                                    
                    string path = CreateDirectory(ImagesFilePath, folderName);
                    Crawler.DownloadImagesFromUrlAndSaveToFolder(url, path);
               }                              
            });            
            ImagesStatusTextBlock.Text = "Pobieranie ukończone";
        }

        private string MakeProperFileNameFromUrlWithEnding(string url, string ending)
        {
            string fileNameToReturn = url.Trim();
            fileNameToReturn = Regex.Replace(fileNameToReturn, "(http|https)://", String.Empty).Trim();
            fileNameToReturn = Crawler.ReplaceAllIllegalCharactersInPathWith(fileNameToReturn, "_");
            fileNameToReturn += ending;
            return fileNameToReturn;
        }

        private async Task<string> GetHtmlAsync()
        {
            List<string> listOfLinks;
            await Task.Run(async () =>
            {
                foreach (string page in PagesFromWhichDownloadHtmlCodeList)
                {
                    string trimmedPage = page.Trim();
                    string folderName = MakeProperFileNameFromUrlWithEnding(trimmedPage, "_kody_html");
                    string path = CreateDirectory(HtmlFilePath, folderName);                    
                    Crawler.GetHtmlCodeFromPageAndSaveToFileAsync(trimmedPage, path + folderName + ".html");
                    listOfLinks = await Crawler.FindLinksInPageAsync(trimmedPage);                                        
                    Parallel.ForEach(listOfLinks, (link) =>
                    {
                        string element = Regex.Replace(link, "///+", "//");
                        string filename = MakeProperFileNameFromUrlWithEnding(element, ".html");
                        Crawler.GetHtmlCodeFromPageAndSaveToFileAsync(element, path + filename);
                    });
                }
            });
            return "Pobieranie ukończone";
        }

        private async void SearchButtonGetHtml_ClickAsync(object sender, RoutedEventArgs e)
        {
            HtmlStatusTextBlock.Text = "Trwa pobieranie danych...";
            getDataFromDownloadHtmlCodeFromPagesField();
            string message = String.Empty;
            await Task.Run(async () => message = await GetHtmlAsync());                
            HtmlStatusTextBlock.Text = message;
        }

        #region Folder choosers region
        private void FolderChooser1_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.FolderBrowserDialog folderBrowserDialog = new System.Windows.Forms.FolderBrowserDialog();

            folderBrowserDialog.RootFolder = Environment.SpecialFolder.Desktop;
            folderBrowserDialog.Description = "Wybierz folder, w którym chcesz zapisać wyniki poszukiwań";
            if (folderBrowserDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                InfoFilePath = folderBrowserDialog.SelectedPath + @"\";
                FilePathTextBlock1.Text = InfoFilePath;
            }
        }

        private void FolderChooser2_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.FolderBrowserDialog folderBrowserDialog = new System.Windows.Forms.FolderBrowserDialog();

            folderBrowserDialog.RootFolder = Environment.SpecialFolder.Desktop;
            folderBrowserDialog.Description = "Wybierz folder, w którym chcesz zapisać wyniki poszukiwań";
            if (folderBrowserDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                ImagesFilePath = folderBrowserDialog.SelectedPath + @"\";
                FilePathTextBlock2.Text = ImagesFilePath;
            }
        }       

        private void FolderChooser4_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.FolderBrowserDialog folderBrowserDialog = new System.Windows.Forms.FolderBrowserDialog();

            folderBrowserDialog.RootFolder = Environment.SpecialFolder.Desktop;
            folderBrowserDialog.Description = "Wybierz folder, w którym chcesz zapisać wyniki poszukiwań";
            if (folderBrowserDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                HtmlFilePath = folderBrowserDialog.SelectedPath + @"\";
                FilePathTextBlock4.Text = HtmlFilePath;
            }
        }
        #endregion

        #region Controls' events
        private void RichTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {

        }

        private void SearchAboutCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {            
            SearchAboutRichTextBox.IsEnabled = false;
            SearchButtonWiki.IsEnabled = false;
        }

        private void SearchAboutCheckBox_Checked(object sender, RoutedEventArgs e)
        {            
            SearchAboutRichTextBox.IsEnabled = true;
            SearchButtonWiki.IsEnabled = true;
        }       

        private void DownloadImagesFromPagesCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            DownloadImagesFromPagesRichTextbox.IsEnabled = true;
            SearchButtonImages.IsEnabled = true;
        }

        private void DownloadImagesFromPagesCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            DownloadImagesFromPagesRichTextbox.IsEnabled = false;
            SearchButtonImages.IsEnabled = false;
        }                

        private void DownloadHtmlCodeFromPagesCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            DownloadHtmlCodeFromPagesRichTextBox.IsEnabled = true;
            SearchButtonGetHtml.IsEnabled = true;
        }

        private void DownloadHtmlCodeFromPagesCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            DownloadHtmlCodeFromPagesRichTextBox.IsEnabled = false;
            SearchButtonGetHtml.IsEnabled = false;
        }

        private void IntegerNumberTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            string[] codesOfDigits = { "D1", "D2", "D3", "D4", "D5", "D6", "D7", "D8", "D9", "D0" };
            if (!codesOfDigits.Contains(e.Key.ToString()))
            {
                e.Handled = true;
            }
        }

#endregion

        #region SearchAboutRichTextBox focus changed
        private bool isSearchAboutDefaultTextChanged = false;
        private void SearchAboutRichTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (isSearchAboutDefaultTextChanged == false)
            {
                SearchAboutRichTextBox.Document.Blocks.Clear();
                isSearchAboutDefaultTextChanged = true;
            }
        }
        private string defaultSearchAboutText = "Wpisz rzeczy do wyszukania, rozdzielając je enterem";
        private void SearchAboutRichTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (SearchAboutRichTextBox.Document.Blocks.Count == 0)
            {
                SearchAboutRichTextBox.Document.Blocks.Add(new Paragraph(new Run(defaultSearchAboutText)));
                isSearchAboutDefaultTextChanged = false;
            }
            else
            {
                string myText = getTextFromRichTextBox(SearchAboutCheckBox, SearchAboutRichTextBox);
                if (myText == String.Empty)
                {
                    SearchAboutRichTextBox.AppendText(defaultSearchAboutText);
                    isSearchAboutDefaultTextChanged = false;
                }
            }
        }
        #endregion

        #region DownloadImagesRichTextBox focus changed
        private bool isDownloadImagesDefaultTextChanged = false;
        private void DownloadImagesFromPagesRichTextbox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (isDownloadImagesDefaultTextChanged == false)
            {
                DownloadImagesFromPagesRichTextbox.Document.Blocks.Clear();
                isDownloadImagesDefaultTextChanged = true;
            }
        }

        private string downloadImagesDefaultText = "Wpisz strony, które chcesz przeszukać, rozdzielając ich adresy enterem";
        private void DownloadImagesFromPagesRichTextbox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (DownloadImagesFromPagesRichTextbox.Document.Blocks.Count == 0)
            {
                DownloadImagesFromPagesRichTextbox.Document.Blocks.Add(new Paragraph(new Run(downloadImagesDefaultText)));
                isDownloadImagesDefaultTextChanged = false;
            }
            else
            {
                string myText = getTextFromRichTextBox(DownloadImagesFromPagesCheckBox, DownloadImagesFromPagesRichTextbox);
                if (myText == String.Empty)
                {
                    DownloadImagesFromPagesRichTextbox.AppendText(downloadImagesDefaultText);
                    isDownloadImagesDefaultTextChanged = false;
                }
            }
        }

        #endregion

        #region DownloadHtmlRichTextBox focus Changed

        private bool isDownloadHtmlCodeFromPagesTextChanged = false;
        private void DownloadHtmlCodeFromPagesRichTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (isDownloadHtmlCodeFromPagesTextChanged == false)
            {
                DownloadHtmlCodeFromPagesRichTextBox.Document.Blocks.Clear();
                isDownloadHtmlCodeFromPagesTextChanged = true;
            }
        }

        private string DownloadHtmlCodeFromPagesDefaultText = "Wpisz strony, które chcesz przeszukać, rozdzielając ich adresy enterem";
        private void DownloadHtmlCodeFromPagesRichTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (DownloadHtmlCodeFromPagesRichTextBox.Document.Blocks.Count == 0)
            {
                DownloadHtmlCodeFromPagesRichTextBox.Document.Blocks.Add(new Paragraph(new Run(DownloadHtmlCodeFromPagesDefaultText)));
                isDownloadHtmlCodeFromPagesTextChanged = false;
            }
            else
            {
                string myText = getTextFromRichTextBox(DownloadHtmlCodeFromPagesCheckBox, DownloadHtmlCodeFromPagesRichTextBox);
                if (myText == String.Empty)
                {
                    DownloadHtmlCodeFromPagesRichTextBox.AppendText(DownloadHtmlCodeFromPagesDefaultText);
                    isDownloadHtmlCodeFromPagesTextChanged = false;
                }
            }
        }
#endregion
    }
}
