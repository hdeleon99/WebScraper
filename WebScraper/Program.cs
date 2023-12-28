using WebScraper.ScrapingLogic;

namespace WebScraper;
class Program
{
    static void Main()
    {

        JobScraper scraper = new JobScraper();
        /*
         * Here we can take advantage of the query parameter. 
         * We will sift through the CSV file, grab all company names
         * Get the jobs for that company name (start with 50 jobs)
         * Serialize the jobs list into an xml file for that specific company 
         * It will need to be in https://free-college-jobs.web.app/amazon.xml this format 
         * 
         */
        // string query = "apple"
        // string url = $"https://ww.indeed.com/jobs?q={companyName}"

        string url = "https://www.indeed.com/jobs?q=software+engineer&l=Oakland%2C+CA&from=searchOnHP&vjk=893e17986a3f2da2";
        var jobs = scraper.GetJobs(url);
        // Output the job titles

    }


}
