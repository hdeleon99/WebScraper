using WebScraper.ScrapingLogic;

namespace WebScraper;
class Program
{
    static void Main()
    {

        JobScraper scraper = new JobScraper();

        // string query = "apple"
        // string url = $"https://ww.indeed.com/jobs?q={query}"

        string url = "https://www.indeed.com/jobs?q=software+engineer&l=Oakland%2C+CA&from=searchOnHP&vjk=893e17986a3f2da2";
        var jobs = scraper.GetJobs(url);
        // Output the job titles

    }


}
