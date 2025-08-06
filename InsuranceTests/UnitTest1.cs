using NUnit.Framework;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using System;

namespace InsuranceTesters
{
    [TestFixture]
    public class InsuranceQuoteTests : IDisposable
    {
        private IWebDriver driver;
        private WebDriverWait wait;
        private bool disposed = false;

        [SetUp]
        public void Setup()
        {
            var options = new ChromeOptions();
            options.AddArguments("--start-maximized", "--disable-notifications");
            driver = new ChromeDriver(options);

            driver.Manage().Timeouts().PageLoad = TimeSpan.FromSeconds(30);
            driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(0);
            wait = new WebDriverWait(driver, TimeSpan.FromSeconds(20));

            driver.Navigate().GoToUrl("http://localhost/prog8170a04/getQuote.html");
            wait.Until(d => d.FindElement(By.Id("btnSubmit")).Displayed);
        }

        private void FillCompleteForm(string age, string experience, string accidents, bool submitForm = true)
        {
            driver.FindElement(By.Id("firstName")).SendKeys("Anmol");
            driver.FindElement(By.Id("lastName")).SendKeys("Kaur");
            driver.FindElement(By.Id("address")).SendKeys("456 King Street");
            driver.FindElement(By.Id("city")).SendKeys("Kitchener");
            driver.FindElement(By.Id("postalCode")).SendKeys("N2H 5G1");
            driver.FindElement(By.Id("phone")).SendKeys("647-111-2222");
            driver.FindElement(By.Id("email")).SendKeys("anmol@example.com");

            driver.FindElement(By.Id("age")).SendKeys(age);
            driver.FindElement(By.Id("experience")).SendKeys(experience);
            driver.FindElement(By.Id("accidents")).SendKeys(accidents);

            if (submitForm)
            {
                var submitBtn = driver.FindElement(By.Id("btnSubmit"));
                ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].scrollIntoView(true);", submitBtn);
                submitBtn.Click();
                wait.Until(d => !string.IsNullOrEmpty(d.FindElement(By.Id("finalQuote")).GetAttribute("value")));
            }
        }

        private string GetValidationMessage(string fieldId)
        {
            try
            {
                var field = driver.FindElement(By.Id(fieldId));
                var html5Message = field!.GetAttribute("validationMessage") ?? string.Empty;
                if (!string.IsNullOrEmpty(html5Message)) return html5Message;

                string[] selectors = {
                    $"[data-valmsg-for='{fieldId}']",
                    $"#{fieldId}-error",
                    $"#{fieldId} + .validation-message",
                    $"#{fieldId} ~ .error-message"
                };

                foreach (var selector in selectors)
                {
                    try
                    {
                        var element = driver.FindElement(By.CssSelector(selector));
                        if (element != null && !string.IsNullOrEmpty(element.Text))
                            return element.Text;
                    }
                    catch (NoSuchElementException) { }
                }

                var classAttr = field!.GetAttribute("class") ?? string.Empty;
                if (classAttr.Contains("error") || classAttr.Contains("invalid"))
                    return "Field is flagged as invalid";

                return string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        //TEST CASES 

        [Test]
        public void Test1_ValidData_Quote5500()
        {
            // Arrange
            string expectedQuote = "$5500";

            // Act
            FillCompleteForm("24", "3", "0");
            string actualQuote = driver.FindElement(By.Id("finalQuote")).GetAttribute("value")!;

            // Assert
            Assert.That(actualQuote, Is.EqualTo(expectedQuote));
        }

        

        [TearDown]
        public void Teardown()
        {
            Dispose();
        }

        public void Dispose()
        {
            if (!disposed)
            {
                try
                {
                    driver?.Quit();
                    driver?.Dispose();
                }
                catch { }
                disposed = true;
            }
        }
    }
}
