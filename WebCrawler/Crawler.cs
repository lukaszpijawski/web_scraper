using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.IO;
using System.Net;
using HtmlAgilityPack;

namespace CrawlerLibrary
{
    public class Crawler
    {
        public static async Task<bool> GetInfoFromWikipediaAndSaveToFilePathAsync(List<string> list, string filePath)
        {
            foreach (string keyword in list)
            {
                Console.Write(keyword);
                string buffer = await GetInfoFromWikipediaAboutAsync(keyword);
                if (String.IsNullOrEmpty(buffer))
                {
                    await Task.Run(() => SaveTextToFileAsync("", filePath, keyword + "_nie_udało_się_pobrać_informacji"));
                }
                else
                {
                    await Task.Run(() => SaveTextToFileAsync(buffer, filePath, keyword));
                }
            }
            return true;
        }

        public static async void SaveTextToFileAsync(string text, string filePath, string filename)
        {
            filename = filename.Trim();
            filename = Regex.Replace(filename, "\\s", String.Empty);
            filename = ReplaceAllIllegalCharactersInPathWith(filename, "_");

            string path = filePath + filename + ".txt";
            using (StreamWriter outputFile = new StreamWriter(path))
            {
                await outputFile.WriteAsync(text);
            }
        }

        public static async Task<string> GetInfoFromWikipediaAboutAsync(string keyword)
        {
            StringBuilder wikipediaUrl = new StringBuilder("https://pl.wikipedia.org/wiki/");
            HtmlDocument htmlDocument = new HtmlDocument();
            string wikipediaPage;
            keyword = keyword.Trim();
            keyword = Regex.Replace(keyword, @"\s+", "_");
            wikipediaUrl.Append(keyword);
            wikipediaPage = await LoadPageAsStringAsync(wikipediaUrl.ToString());
            if (String.IsNullOrEmpty(wikipediaPage))
            {
                return String.Empty;
            }
            htmlDocument.LoadHtml(wikipediaPage);
            List<HtmlNode> divs = htmlDocument.DocumentNode.Descendants("div")?
                                    .Where(node => node.GetAttributeValue("id", "")
                                    .Equals("mw-content-text")).FirstOrDefault()?.Descendants()
                                        .Where(node => (node.Name == "p" || node.Name == "h2" || node.Name == "ul"))
                                            .ToList();

            StringBuilder stringBuilder1 = new StringBuilder("");
            StringBuilder stringBuilder2 = new StringBuilder("");
            StringBuilder stringBuilder3 = new StringBuilder("");
            StringBuilder stringBuilder4 = new StringBuilder("");
            string stringhelper1 = String.Empty;
            string stringhelper2 = String.Empty;
            string stringhelper3 = String.Empty;
            string stringhelper4 = String.Empty;

            string regexPattern = "\\[\\S*\\]";
            int length = divs.Count;
            Task task1 = Task.Run
            (
                () =>
                {
                    for (int i = 0; i < length / 4; i++)
                    {
                        stringBuilder1.Append(divs[i].InnerText);
                        stringBuilder1.Append("\n");
                    }
                    stringhelper1 = stringBuilder1.ToString();
                    stringhelper1 = Regex.Replace(stringhelper1, regexPattern, String.Empty);
                }
            );
            Task task2 = Task.Run
            (
                () =>
                {
                    for (int i = length / 4; i < length / 2; i++)
                    {
                        stringBuilder2.Append(divs[i].InnerText);
                        stringBuilder2.Append("\n");
                    }
                    stringhelper2 = stringBuilder2.ToString();
                    stringhelper2 = Regex.Replace(stringhelper2, regexPattern, String.Empty);
                }
            );
            Task task3 = Task.Run
            (
                () =>
                {
                    for (int i = length / 2; i < length * 3 / 4; i++)
                    {
                        stringBuilder3.Append(divs[i].InnerText);
                        stringBuilder3.Append("\n");
                    }
                    stringhelper3 = stringBuilder3.ToString();
                    stringhelper3 = Regex.Replace(stringhelper3, regexPattern, String.Empty);
                }
            );
            Task task4 = Task.Run
            (
               () =>
               {
                   for (int i = length * 3 / 4; i < length; i++)
                   {
                       stringBuilder4.Append(divs[i].InnerText);
                       stringBuilder4.Append("\n");
                   }
                   stringhelper4 = stringBuilder4.ToString();
                   stringhelper4 = Regex.Replace(stringhelper4, regexPattern, String.Empty);
               }
            );
            Task.WaitAll(task1, task2, task3, task4);
            return stringhelper1 + stringhelper2 + stringhelper3 + stringhelper4;
        }

        public static string LoadPageAsString(string url)
        {
            WebClient webClient = new WebClient();
            string html = String.Empty;
            try
            {
                UriBuilder uriBuilder = new UriBuilder(url);
                html = webClient.DownloadString(uriBuilder.Uri);
            }
            catch
            {
                html = String.Empty;
            }
            webClient.Dispose();
            return html;
        }

        public static async Task<string> LoadPageAsStringAsync(string url)
        {
            WebClient webClient = new WebClient();
            HttpClient httpClient = new HttpClient();
            string html = String.Empty;
            try
            {
                UriBuilder uriBuilder = new UriBuilder(url);
                html = await webClient.DownloadStringTaskAsync(uriBuilder.Uri);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                html = String.Empty;
            }
            webClient.Dispose();
            return html;
        }

        public static void DownloadImagesFromUrlAndSaveToFolder(string url, string folderPath)
        {
            WebClient webClient = new WebClient();
            url = url.Trim();
            string html = LoadPageAsString(url);
            HtmlDocument htmlDocument = new HtmlDocument();
            htmlDocument.LoadHtml(html);
            List<HtmlNode> imageUrls = htmlDocument.DocumentNode.Descendants("img").ToList();
            int i = 0;
            foreach (var imageUrl in imageUrls)
            {
                string address = imageUrl?.ChildAttributes("src")?.FirstOrDefault()?.Value;
                folderPath = folderPath.Trim();
                try
                {
                    string path = (folderPath + @"\obrazek" + i + ".png").Trim();
                    webClient.DownloadFile(address, path);
                    i++;
                }
                catch
                { }
            }
            webClient.Dispose();
        }

        public static string ReplaceAllIllegalCharactersInPathWith(string path, string replacement)
        {
            path = Regex.Replace(path, "(/|<|>|\\\\|:|\\?|\\*|\")", replacement);
            return path;
        }

        public static void GetHtmlCodeFromPageAndSaveToFileAsync(string url, string path)
        {
            url = url.Trim();
            string html = LoadPageAsString(url);
            HtmlDocument htmlDocument = new HtmlDocument();
            htmlDocument.LoadHtml(html);
            path = Regex.Replace(path, @"/", @"_");
            try
            {
                htmlDocument.Save(path);
            }
            catch (Exception)
            {
                Console.WriteLine(path);
            }
        }

        public static async Task<List<string>> FindLinksInPageAsync(string url)
        {
            url = url.Trim();
            UriBuilder uriBuilder = new UriBuilder(url);
            string absoluteUrl = Regex.Match(uriBuilder.Uri.ToString(), "(http|https)://\\S+?/").ToString();
            string html = await LoadPageAsStringAsync(url);
            List<string> list = new List<string>();
            HtmlDocument htmlDocument = new HtmlDocument();
            htmlDocument.LoadHtml(html);
            List<HtmlNode> links = htmlDocument.DocumentNode.Descendants("a").ToList();
            foreach (HtmlNode link in links)
            {
                string address = link.GetAttributeValue("href", "").Trim();
                address = address.TrimStart('/');
                if (Uri.IsWellFormedUriString(address, UriKind.Absolute))
                {
                    list.Add(address);
                }
                else
                {
                    list.Add(absoluteUrl + address);
                }
            }
            list = list.Distinct().ToList();
            return list;
        }

        public static async Task<List<string>> FindUrlsFromPageUsingRegexAsync(string url, string regex, char[] charactersToTrimFromUrl)
        {
            List<string> listOfUrls = new List<string>();
            string html = await LoadPageAsStringAsync(url);
            string[] matchedUrls = Regex.Matches(html, regex)
                                        .Cast<Match>()
                                        .Select(m => m.Value)
                                        .ToArray();
            Parallel.ForEach(matchedUrls, (match) =>
            {
                string urlToReturn = match;
                urlToReturn = urlToReturn.Trim(charactersToTrimFromUrl);
                if (Uri.IsWellFormedUriString(urlToReturn, UriKind.Absolute))
                {
                    listOfUrls.Add(urlToReturn);
                }
                else if (Uri.IsWellFormedUriString(urlToReturn, UriKind.Relative))
                {
                    listOfUrls.Add(url + "/" + urlToReturn);
                }
            });
            listOfUrls = listOfUrls.Distinct().ToList();
            return listOfUrls;
        }

        public static async Task<List<string>> FindTextFromPageUsingRegexAsync(string url, string regex)
        {
            List<string> text = new List<string>();
            string html = await LoadPageAsStringAsync(url);
            string[] matchedLines = Regex.Matches(html, regex)
                                        .Cast<Match>()
                                        .Select(m => m.Value)
                                        .ToArray();
            Parallel.ForEach(matchedLines, (match) =>
            {
                text.Add(match);
            });
            return text;
        }
    }
}
