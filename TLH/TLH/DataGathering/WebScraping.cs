using HtmlAgilityPack;
using OpenQA.Selenium.Edge;

namespace TLH
{
    public static class WebScraper
    {
        public static void StartScraping()
        {
            var loginUrl = "https://sms.schoolsoft.se/nti/html/redirect_login.htm";
            var targetUrl = "https://sms.schoolsoft.se/nti/jsp/teacher/right_teacher_startpage.jsp";

            var edgeOptions = new EdgeOptions();
            // Uncomment the following line if you want to use headless mode
            //edgeOptions.AddArgument("headless");

            using var driver = new EdgeDriver(edgeOptions);

            // Navigate to the login page
            driver.Navigate().GoToUrl(loginUrl);

            // Wait for the user to log in and navigate to the target page
            Console.WriteLine("Please log in and navigate to the target page...");

            while (driver.Url != targetUrl)
            {
                System.Threading.Thread.Sleep(20000);
            }

            // The user has navigated to the target page
            Console.WriteLine("The target page has been reached. Start scraping...");

            var targetHtml = driver.PageSource;

            var htmlDocument = new HtmlDocument();
            htmlDocument.LoadHtml(targetHtml);

            // Replace the XPath with the appropriate one for the data you want to scrape
            var nodes = htmlDocument.DocumentNode.SelectNodes("//tag[@attribute='value']");

            if (nodes != null)
            {
                foreach (var node in nodes)
                {
                    Console.WriteLine(node.InnerText);
                }
            }

            driver.Quit();
        }
    }
}