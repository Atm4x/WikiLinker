using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using static System.Net.Mime.MediaTypeNames;

namespace WikiParser
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        static HttpClient httpClient = new HttpClient();
        public MainWindow()
        {
            InitializeComponent();
            //httpClient.DefaultRequestHeaders.Add("Content-Type", "text/html");
        }

        private void ChangeItems(object sender, RoutedEventArgs e)
        {
            var text = ArticleBox.Text;

            var language = text.Split('/')[2].Split('.')[0].ToString();

            var articleTitle = text.Split('/').Last();


            if (articleTitle.StartsWith(@"%"))
            {
                articleTitle = HttpUtility.UrlDecode(articleTitle).Replace('_', ' ');
            }

            var item = new TreeViewItem();
            item.Expanded += ItemExpand;
            item.Selected += ItemSelected;
            item.Header = articleTitle;
            item.Items.Add(new TreeViewItem());
            item.Tag = ArticleBox.Text;

            Article.Items.Add(item);
        }

        private void ItemSelected(object sender, RoutedEventArgs e)
        {
            var viewItem = (TreeViewItem)sender;
            var tagLink = (string)viewItem.Tag;


            var language = tagLink.Split('/')[2].Split('.')[0].ToString();

            var articleTitle = tagLink.Split('/').Last();

            var url = $"https://{language}.wikipedia.org/w/api.php?action=query&titles={articleTitle}&prop=extracts&redirects=true&format=json";



            var json = new WebClient().DownloadString(url);

            var data = JObject.Parse(json);

            var page = data["query"]["pages"].Values().ToArray()[0];

            if (page.Path.EndsWith("-1"))
            {
                return;
            }

            var extract = page["extract"].ToString();

            Regex rx = new Regex(@"\\[uU]([0-9A-F]{4})");
            extract = rx.Replace(extract, match => ((char)Int32.Parse(match.Value.Substring(2), NumberStyles.HexNumber)).ToString());

            extract = Regexity(extract);

            //$"<image src=\"http://latex.codecogs.com/gif.latex?{mathed}\">"
            var html = "<html><head><meta charset=\"Utf-8\"/></head><body>" + extract + "</body></html>";


            CurrentPage.NavigateToString(html);
            e.Handled = true;
        }

        public string Regexity(string expression)
        {
            string pattern_math = "<math[^>]*>";
            Regex regex_math = new Regex(pattern_math);
            MatchCollection matches_math = regex_math.Matches(expression);


            foreach (Match match in matches_math)
            {
                var value = match.Value;
                string pattern_alttext = "alttext=\"([^']*)\"";
                Regex regex_alttext = new Regex(pattern_alttext);
                MatchCollection matches_alttext = regex_alttext.Matches(expression);
                if (matches_alttext.Count > 0)
                {
                    var alttext = matches_alttext[0];
                    var equation = alttext.Value.Split('"')[1];
                    expression = expression.Replace(value, $"<image src=\"http://latex.codecogs.com/gif.latex?{equation}\">");
                }
            }

            string remove_semantics = "<semantics>(.*?)<\\/semantics>";
            Regex regex_semantics = new Regex(remove_semantics, RegexOptions.Singleline);

            MatchCollection matches = regex_semantics.Matches(expression);
            foreach (Match match in matches)
            {
                string content = match.Value;
                expression = expression.Replace(content, "");
            }

            return expression;
        }

        private void ItemExpand(object sender, RoutedEventArgs e)
        {
            var viewItem = (TreeViewItem)sender;
            
            viewItem.Items.Clear();

            var tagLink = (string)viewItem.Tag;

            var language = tagLink.Split('/')[2].Split('.')[0].ToString();

            var articleTitle = tagLink.Split('/').Last();

            if (articleTitle.StartsWith(@"%"))
            {
                articleTitle = HttpUtility.UrlDecode(articleTitle).Replace('_', ' ');
            }

            var url = $"https://{language}.wikipedia.org/w/api.php?action=query&titles={articleTitle}&prop=links&pllimit=max&format=json";

            var json = new WebClient().DownloadString(url);

            var data = JObject.Parse(json);

            var page = data["query"]["pages"].Values().ToArray()[0];
            if(page.Path.EndsWith("-1"))
            {
                return;
            }

            var links = page["links"].ToArray();

            foreach(var link in links)
            {
                var item = new TreeViewItem();
                item.Header = link["title"].ToString();
                item.Tag = $"https://{language}.wikipedia.org/wiki/{string.Join("_", link["title"].ToString().Split(' '))}";
                item.Expanded += ItemExpand;
                item.Selected += ItemSelected;
                item.Items.Add(new TreeViewItem());

                Dispatcher.Invoke(() =>
                viewItem.Items.Add(item));
            }
            e.Handled = true;
        }

        private void Articler(object sender, RoutedPropertyChangedEventArgs<object> e)
        {

        }
    }
}
