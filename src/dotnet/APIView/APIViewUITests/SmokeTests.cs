using Xunit;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using OpenQA.Selenium.Chrome;
using System.Collections.Generic;
using SeleniumExtras.WaitHelpers;
using System.Linq;
using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Azure.Cosmos;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace APIViewUITests
{
    public class SmokeTestsFixture : IDisposable
    {
        private readonly CosmosClient _cosmosClient;
        private readonly BlobContainerClient _blobCodeFileContainerClient;
        private readonly BlobContainerClient _blobOriginalContainerClient;
        private readonly BlobContainerClient _blobUsageSampleRepository;
        private readonly BlobContainerClient _blobCommentsRepository;

        public SmokeTestsFixture()
        {
            var config = new ConfigurationBuilder()
                .AddJsonFile("config.json")
                .Build();

            _cosmosClient = new CosmosClient(config["Cosmos:ConnectionString"]);
            var dataBaseResponse = _cosmosClient.CreateDatabaseIfNotExistsAsync("APIView").Result;
            _ = dataBaseResponse.Database.CreateContainerIfNotExistsAsync("Reviews", "/id");
            _ = dataBaseResponse.Database.CreateContainerIfNotExistsAsync("Comments", "/ReviewId");
            _ = dataBaseResponse.Database.CreateContainerIfNotExistsAsync("Profiles", "/id");
            _ = dataBaseResponse.Database.CreateContainerIfNotExistsAsync("PullRequests", "/PullRequestNumber");
            _ = dataBaseResponse.Database.CreateContainerIfNotExistsAsync("UsageSamples", "/ReviewId");
            _ = dataBaseResponse.Database.CreateContainerIfNotExistsAsync("UserPreference", "/ReviewId");

            _blobCodeFileContainerClient = new BlobContainerClient(config["Blob:ConnectionString"], "codefiles");
            _blobOriginalContainerClient = new BlobContainerClient(config["Blob:ConnectionString"], "originals");
            _blobUsageSampleRepository = new BlobContainerClient(config["Blob:ConnectionString"], "usagesamples");
            _blobCommentsRepository = new BlobContainerClient(config["Blob:ConnectionString"], "comments");
            _ = _blobCodeFileContainerClient.CreateIfNotExistsAsync(PublicAccessType.BlobContainer);
            _ = _blobOriginalContainerClient.CreateIfNotExistsAsync(PublicAccessType.BlobContainer);
            _ = _blobUsageSampleRepository.CreateIfNotExistsAsync(PublicAccessType.BlobContainer);
            _ = _blobCommentsRepository.CreateIfNotExistsAsync(PublicAccessType.BlobContainer);
        }

        public void Dispose()
        {
            _cosmosClient.GetDatabase("APIView").DeleteAsync().Wait();
            _cosmosClient.Dispose();

            _blobCodeFileContainerClient.DeleteIfExists();
            _blobOriginalContainerClient.DeleteIfExists();
            _blobUsageSampleRepository.DeleteIfExists();
            _blobCommentsRepository.DeleteIfExists();
        }
    }
    public class SmokeTests : IClassFixture<SmokeTestsFixture>
    {
        const int MaxWaitTime = 30;
        const int MinWaitTime = 10;

        [Fact]
        public void SmokeTest_CSharp()
        {
            using (IWebDriver driver = new ChromeDriver())
            {
                driver.Manage().Window.Maximize();
                driver.Manage().Timeouts().PageLoad = TimeSpan.FromSeconds(MinWaitTime);
                driver.Navigate().GoToUrl("http://localhost:5000");
                driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(MaxWaitTime);

                var createReviewBtn = driver.FindElement(By.XPath("//button[@data-target='#uploadModel']"));
                createReviewBtn.Click();
                driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(MaxWaitTime);
                var fileSelector = driver.FindElement(By.Id("Upload_Files"));
                fileSelector.SendKeys("C:\\Users\\chononiw\\Downloads\\azure.identity.1.9.0-beta.1.nupkg");
                var uploadBtn = driver.FindElement(By.XPath("//div[@class='modal-footer']/button[@type='submit']"));
                uploadBtn.Click();
                driver.Manage().Timeouts().PageLoad = TimeSpan.FromSeconds(MaxWaitTime);
            }
        }

        [Fact(Skip = "Test is too Flaky")]
        public void MostUsedPagesLoadsWithouErrors()
        {
            using (IWebDriver driver = new ChromeDriver())
            {
                driver.Manage().Window.Maximize();
                WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(50));
                // Index Page Loads Without Error
                driver.Navigate().GoToUrl("http://localhost:5000/");
                Assert.Equal("Reviews - apiview.dev", driver.Title);

                // Site Theme Changes Without Error
                var themeSelector = driver.FindElement(By.Id("theme-selector"));
                var themeSelectElement = new SelectElement(themeSelector);
                foreach (IWebElement option in themeSelectElement.Options)
                {
                    themeSelectElement.SelectByText(option.Text);
                    Assert.NotEqual("Error - apiview.dev", driver.Title);
                    Assert.NotEqual("Internal Server Error", driver.Title);
                }

                // Review Page Loads without Error
                var reviewNames = driver.FindElements(By.ClassName("review-name"));
                if (reviewNames.Any())
                {
                    wait.Until(ExpectedConditions.ElementToBeClickable(By.ClassName("review-name")));
                    reviewNames[0].Click();
                    Assert.NotEqual("Error - apiview.dev", driver.Title);
                    Assert.NotEqual("Internal Server Error", driver.Title);
                }

                // Conversiation and Revision Pages Loads without error
                var navLinks = driver.FindElements(By.ClassName("nav-link")).Select(c => c.Text).ToList();
                foreach (var navLink in navLinks)
                {
                    if (navLink.Equals("Conversations") || navLink.Equals("Revisions") || navLink.Equals("Usage Samples"))
                    {
                        var link = driver.FindElements(By.ClassName("nav-link")).Single(l => (l.Text.Equals(navLink)));
                        wait.Until(ExpectedConditions.ElementToBeClickable(By.ClassName("nav-link")));
                        link.Click();
                        Assert.NotEqual("Error - apiview.dev", driver.Title);
                        Assert.NotEqual("Internal Server Error", driver.Title);
                        driver.Navigate().Back();
                    }
                }

                // Review Options Changes without Errors
                driver.FindElement(By.CssSelector(".btn.btn-light.btn-sm.border.shadow-sm.dropdown-toggle")).Click();
                driver.FindElement(By.Id("show-comments-checkbox")).Click();
                Assert.NotEqual("Error - apiview.dev", driver.Title);
                Assert.NotEqual("Internal Server Error", driver.Title);

                driver.FindElement(By.Id("show-system-comments-checkbox")).Click();
                Assert.NotEqual("Error - apiview.dev", driver.Title);
                Assert.NotEqual("Internal Server Error", driver.Title);

                driver.FindElement(By.Id("hide-line-numbers")).Click();
                Assert.NotEqual("Error - apiview.dev", driver.Title);
                Assert.NotEqual("Internal Server Error", driver.Title);

                driver.FindElement(By.Id("hide-left-navigation")).Click();
                Assert.NotEqual("Error - apiview.dev", driver.Title);
                Assert.NotEqual("Internal Server Error", driver.Title);

                // Change Reviews and Revisions Withous Errors
                var revisionSelector = driver.FindElement(By.Id("revisions-bootstraps-select"));
                var revisionSelectElement = new SelectElement(revisionSelector);
                if (revisionSelectElement.Options.Count > 1)
                {
                    revisionSelectElement.SelectByText(revisionSelectElement.Options[1].Text);
                    Assert.NotEqual("Error - apiview.dev", driver.Title);
                    Assert.NotEqual("Internal Server Error", driver.Title);
                    driver.Navigate().Back();
                    Assert.NotEqual("Error - apiview.dev", driver.Title);
                    Assert.NotEqual("Internal Server Error", driver.Title);
                }
            }
        }

        [Fact(Skip = "Test is too Flaky")]
        public void ReviewFilterOptionsWorkWithoutErrors()
        {
            using (IWebDriver driver = new ChromeDriver())
            {
                driver.Manage().Window.Maximize();
                WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(50));
                driver.Navigate().GoToUrl("http://localhost:5000/");

                // Language Filters Work Without Errors
                var languageSelector = driver.FindElement(By.Id("language-filter-bootstraps-select"));
                var languageSelectElement = new SelectElement(languageSelector);
                List<string> languages = languageSelectElement.Options.Select(c => c.Text).ToList();
                foreach (var language in languages)
                {
                    languageSelector = driver.FindElement(By.Id("language-filter-bootstraps-select"));
                    languageSelectElement = new SelectElement(languageSelector);
                    languageSelectElement.SelectByText(language);
                    var reviewNames = driver.FindElements(By.ClassName("review-name"));
                    if (reviewNames.Any())
                    {
                        wait.Until(ExpectedConditions.ElementToBeClickable(By.ClassName("review-name")));
                        reviewNames[0].Click();
                        Assert.NotEqual("Error - apiview.dev", driver.Title);
                        Assert.NotEqual("Internal Server Error", driver.Title);
                        driver.Navigate().Back();
                        driver.FindElement(By.Id("reset-filter-button")).Click();
                    }
                }

                // State Filters Work Without Errors
                var stateSelector = driver.FindElement(By.Id("state-filter-bootstraps-select"));
                var stateSelectElement = new SelectElement(stateSelector);
                List<string> states = stateSelectElement.Options.Select(c => c.Text).ToList();
                foreach (var state in states)
                {
                    stateSelector = driver.FindElement(By.Id("state-filter-bootstraps-select"));
                    stateSelectElement = new SelectElement(stateSelector);
                    stateSelectElement.SelectByText(state);
                    var reviewNames = driver.FindElements(By.ClassName("review-name"));
                    if (reviewNames.Any())
                    {
                        wait.Until(ExpectedConditions.ElementToBeClickable(By.ClassName("review-name")));
                        reviewNames[0].Click();
                        Assert.NotEqual("Error - apiview.dev", driver.Title);
                        Assert.NotEqual("Internal Server Error", driver.Title);
                        driver.Navigate().Back();
                        driver.FindElement(By.Id("reset-filter-button")).Click();
                    }
                }

                // Status Filters Work Without Errors
                var statusSelector = driver.FindElement(By.Id("status-filter-bootstraps-select"));
                var statusSelectElement = new SelectElement(statusSelector);
                List<string> statuses = statusSelectElement.Options.Select(c => c.Text).ToList();
                foreach (var status in statuses)
                {
                    statusSelector = driver.FindElement(By.Id("status-filter-bootstraps-select"));
                    statusSelectElement = new SelectElement(statusSelector);
                    statusSelectElement.SelectByText(status);
                    var reviewNames = driver.FindElements(By.ClassName("review-name"));
                    if (reviewNames.Any())
                    {
                        wait.Until(ExpectedConditions.ElementToBeClickable(By.ClassName("review-name")));
                        reviewNames[0].Click();
                        Assert.NotEqual("Error - apiview.dev", driver.Title);
                        Assert.NotEqual("Internal Server Error", driver.Title);
                        driver.Navigate().Back();
                        driver.FindElement(By.Id("reset-filter-button")).Click();
                    }
                }

                // Type Filters Work Without Errors
                var typeSelector = driver.FindElement(By.Id("type-filter-bootstraps-select"));
                var typeSelectElement = new SelectElement(typeSelector);
                List<string> types = typeSelectElement.Options.Select(c => c.Text).ToList();
                foreach (var type in types)
                {
                    typeSelector = driver.FindElement(By.Id("type-filter-bootstraps-select"));
                    typeSelectElement = new SelectElement(typeSelector);
                    typeSelectElement.SelectByText(type);
                    var reviewNames = driver.FindElements(By.ClassName("review-name"));
                    if (reviewNames.Any())
                    {
                        wait.Until(ExpectedConditions.ElementToBeClickable(By.ClassName("review-name")));
                        reviewNames[0].Click();
                        Assert.NotEqual("Error - apiview.dev", driver.Title);
                        Assert.NotEqual("Internal Server Error", driver.Title);
                        driver.Navigate().Back();
                        driver.FindElement(By.Id("reset-filter-button")).Click();
                    }
                }
            }
        }
    }
}
