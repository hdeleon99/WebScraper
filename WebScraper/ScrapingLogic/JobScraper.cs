using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using SeleniumExtras.WaitHelpers;
using System.Data;
using System.Diagnostics;

namespace WebScraper.ScrapingLogic
{

    public class JobScraper
    {
        private static readonly Random random = new Random();

        /// <summary>
        /// Gets the jobs from the URL.
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public List<JobDetails> GetJobs(string url)
        {
            try
            {
                string command = @"C:\Program Files\Google\Chrome\Application\chrome.exe";
                string arguments = @"--remote-debugging-port=9222 --user-data-dir=""C:\Users\isaia\AppData\Local\Google\Chrome\User Data\Default""";

                Process chromeProcess = StartProcess(command, arguments);

                using (var driver = CreateChromeDriver())
                {
                    driver.Navigate().GoToUrl(url);
                    Thread.Sleep(3000);

                    var cardOutlines = driver.FindElements(By.CssSelector(".cardOutline"));
                    foreach (var cardOutline in cardOutlines)
                    {
                        JobDetails jobDetails = ExtractJobDetails(driver, cardOutline);
                        PrintJobDetails(jobDetails);
                        Thread.Sleep(random.Next(2000, 4000));
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex}");
            }

            return null;
        }

        /// <summary>
        /// Processes the location text
        /// </summary>
        /// <param name="locationText"></param>
        /// <returns></returns>
        private List<string> ProcessLocation(string locationText)
        {
            List<string> result = new List<string>() { "", "" };
            // Check if "•" is present
            if (locationText.Contains("•"))
            {
                // Split the string into an array using "•" as the delimiter
                string[] companyLocationElementTextArray = locationText.Split('•');

                // Assuming the split operation was successful
                if (companyLocationElementTextArray.Length >= 2)
                {
                    // Assign values
                    result[0] = companyLocationElementTextArray[0].Trim();
                    result[1] = companyLocationElementTextArray[1].Trim();

                    // Output the results
                 /* Console.WriteLine($"Location: {result[0]}");
                    Console.WriteLine($"Location Type: {result[1]}");*/
                }
                else
                {
                    Console.WriteLine("Invalid format: Unable to extract location and location type.");
                }
            }
            else
            {
                result[0] = locationText;
            }
            return result;
        }

        /// <summary>
        /// Creates a chrome driver used to scrape the website.
        /// </summary>
        /// <returns></returns>
        private IWebDriver CreateChromeDriver()
        {
            string chromeDriverPath = @"C:\chromedriver-win32\chromedriver.exe";
            var chromeOptions = new ChromeOptions();
            chromeOptions.DebuggerAddress = "localhost:9222";
            chromeOptions.AddArgument("--user-agent=Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36");

            return new ChromeDriver(chromeDriverPath, chromeOptions);
        }

        /// <summary>
        /// Starts a process to execute the chrome executable to aid in avoiding bot detection.
        /// </summary>
        /// <param name="command"></param>
        /// <param name="arguments"></param>
        /// <returns></returns>
        private static Process StartProcess(string command, string arguments)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = command,
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            Process process = new Process { StartInfo = startInfo };
            process.Start();

            return process;
        }

        /// <summary>
        /// Extracts the data from the job details panel and serializes it into a JobDetails object.
        /// </summary>
        /// <param name="driver"></param>
        /// <param name="cardOutline"></param>
        /// <returns></returns>
        private JobDetails ExtractJobDetails(IWebDriver driver, IWebElement cardOutline)
        {
            cardOutline.Click();
            try
            {
                var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(5));
                var titleElement = wait.Until(ExpectedConditions.ElementExists(By.CssSelector(".jobsearch-JobInfoHeader-title")));
                var title = titleElement.Text.Replace("\n- job post", "");

                var companyElement = driver.FindElement(By.CssSelector("div[data-company-name='true'] a"));
                var company = companyElement.Text;

                var locationElement = driver.FindElement(By.CssSelector("[data-testid='inlineHeader-companyLocation']"));
                var location = ProcessLocation(locationElement.Text);

                // Attempt to find the application link in #applyButtonLinkContainer button
                string applyLink = GetApplyLinkFromButton(driver);

                if (string.IsNullOrEmpty(applyLink))
                {
                    // If not found, try to get it from span#indeed-apply-widget
                    applyLink = GetApplyLinkFromIndeedApplyWidget(driver);
                }
                if (string.IsNullOrEmpty(applyLink))
                {
                    applyLink = string.Empty;
                }

                var payJobType = ExtractPayAndJobType(driver);
                string pay = payJobType.Item1;
                string jobType = payJobType.Item2;

                return new JobDetails
                {
                    Title = title,
                    Company = company,
                    Location = location[0],
                    Mode = location[1],
                    Link = applyLink,
                    Salary = pay,
                    Type = jobType
                };
            }
            catch (NoSuchElementException ex)
            {
                Console.WriteLine(ex.Message);
                return null;
            }
        }

        /// <summary>
        /// A very messy way to extract the pay and job type (contract, full-time, etc). There are no easy 
        /// identifiable id's or class names for these values.
        /// </summary>
        /// <param name="driver"></param>
        /// <returns></returns>
        private static Tuple<string, string> ExtractPayAndJobType(IWebDriver driver)
        {
            string pay = string.Empty;
            string jobType = string.Empty;
            try
            {
                var jobDetailsSection = driver.FindElement(By.CssSelector("#jobDetailsSection"));

                // Extract pay and job type from the primary job details section
                ExtractPayAndJobTypeFromSection(jobDetailsSection, ref pay, ref jobType);

                return Tuple.Create(pay, jobType);
            }
            catch (NoSuchElementException)
            {
                Console.WriteLine("Primary job details section not found. Trying alternative section.");

                // Handle the case where the primary job details section is not found
                try
                {
                    var otherJobDetailsSection = driver.FindElement(By.CssSelector("[data-testid='jobsearch-OtherJobDetailsContainer']"));

                    // Extract pay and job type from the alternative job details section
                    ExtractPayAndJobTypeFromSection(otherJobDetailsSection, ref pay, ref jobType);

                    return Tuple.Create(pay, jobType);
                }
                catch (NoSuchElementException ex)
                {
                    Console.WriteLine($"Error: {ex}");
                    Console.WriteLine("Both job details sections not found.");
                    return Tuple.Create(string.Empty, string.Empty);
                }
            }
        }

        /// <summary>
        /// A 
        /// </summary>
        /// <param name="section"></param>
        /// <param name="pay"></param>
        /// <param name="jobType"></param>
        private static void ExtractPayAndJobTypeFromSection(IWebElement section, ref string pay, ref string jobType)
        {
            foreach (var div in section.FindElements(By.CssSelector("div")))
            {
                if (div.Text == "Pay")
                {
                    IWebElement payElement = div.FindElement(By.XPath("following-sibling::*"));
                    pay = payElement.Text;
                }
                else if (div.Text == "Job Type")
                {
                    IWebElement jobTypeElement = div.FindElement(By.XPath("following-sibling::*"));
                    jobType = jobTypeElement.Text;
                }
            }
        }


        private void PrintJobDetails(JobDetails jobDetails)
        {
            if (jobDetails != null)
            {
                Console.WriteLine($"Title: {jobDetails.Title}");
                Console.WriteLine($"Company: {jobDetails.Company}");
                Console.WriteLine($"Location: {jobDetails.Location}");
                Console.WriteLine($"Mode: {jobDetails.Mode}");
                Console.WriteLine($"Link: {jobDetails.Link}");
                Console.WriteLine($"Pay: {jobDetails.Salary}");
                Console.WriteLine($"JobType: {jobDetails.Type}");
                Console.WriteLine("------------------------------");
            }
        }
        private string GetApplyLinkFromButton(IWebDriver driver)
        {
            try
            {
                var applyButton = driver.FindElement(By.CssSelector("#applyButtonLinkContainer button"));
                return applyButton.GetAttribute("href");
            }
            catch (NoSuchElementException)
            {
                return null;
            }
        }

        private string GetApplyLinkFromIndeedApplyWidget(IWebDriver driver)
        {
            try
            {
                var indeedApplyWidget = driver.FindElement(By.CssSelector("span#indeed-apply-widget"));
                return indeedApplyWidget.GetAttribute("data-indeed-apply-joburl");
            }
            catch (NoSuchElementException)
            {
                return null;
            }
        }
    }
}
