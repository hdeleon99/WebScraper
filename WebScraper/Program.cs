using WebScraper.ScrapingLogic;

namespace WebScraper;
class Program
{
    static void Main()
    {
        // Set up the Dependency Injection container
/*        var serviceProvider = new ServiceCollection()
            .AddLogging(builder =>
            {
                builder.AddConsole(); // You can customize this based on your needs
            })
            .BuildServiceProvider();*/

        JobScraper scraper = new JobScraper();


        string url = "https://www.indeed.com/jobs?q=software+engineer&l=Oakland%2C+CA&from=searchOnHP&vjk=893e17986a3f2da2";
        var jobs = scraper.GetJobs(url);
        // Output the job titles

    }


}
