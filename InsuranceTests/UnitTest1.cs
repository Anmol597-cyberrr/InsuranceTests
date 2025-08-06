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

        [Test]
        public void Test2_InsuranceDenied_4Accidents()
        {
            // Arrange
            string expectedMessage = "No Insurance for you!!  Too many accidents - go take a course!";

            // Act
            FillCompleteForm("25", "3", "4");
            string result = driver.FindElement(By.Id("finalQuote")).GetAttribute("value")!;

            // Assert
            Assert.That(result, Is.EqualTo(expectedMessage));
        }

        [Test]
        public void Test3_ValidWithDiscount_Quote3905()
        {
            // Arrange
            string expectedQuote = "$3905";

            // Act
            FillCompleteForm("35", "9", "2");
            string quote = driver.FindElement(By.Id("finalQuote")).GetAttribute("value")!;

            // Assert
            Assert.That(quote, Is.EqualTo(expectedQuote));
        }

        [Test]
        public void Test4_InvalidPhoneNumber_Error()
        {
            // Arrange
            FillCompleteForm("27", "3", "0", submitForm: false);
            driver.FindElement(By.Id("phone")).Clear();
            driver.FindElement(By.Id("phone")).SendKeys("123");

            // Act
            driver.FindElement(By.Id("btnSubmit")).Click();
            string errorMsg = GetValidationMessage("phone");

            // Assert
            Assert.That(errorMsg, Is.Not.Empty);
        }

        [Test]
        public void Test5_InvalidEmail_Error()
        {
            // Arrange
            FillCompleteForm("28", "3", "0", submitForm: false);
            driver.FindElement(By.Id("email")).Clear();
            driver.FindElement(By.Id("email")).SendKeys("test@");

            // Act
            driver.FindElement(By.Id("btnSubmit")).Click();
            string errorMsg = GetValidationMessage("email");

            // Assert
            Assert.That(errorMsg, Is.Not.Empty);
        }

        [Test]
        public void Test6_InvalidPostalCode_Error()
        {
            // Arrange
            FillCompleteForm("35", "15", "1", submitForm: false);
            driver.FindElement(By.Id("postalCode")).Clear();
            driver.FindElement(By.Id("postalCode")).SendKeys("12345");

            // Act
            driver.FindElement(By.Id("btnSubmit")).Click();
            string errorMsg = GetValidationMessage("postalCode");

            // Assert
            Assert.That(errorMsg, Is.Not.Empty);
        }

        [Test]
        public void Test7_AgeOmitted_Error()
        {
            // Arrange
            FillCompleteForm("30", "5", "0", submitForm: false);
            driver.FindElement(By.Id("age")).Clear();

            // Act
            driver.FindElement(By.Id("btnSubmit")).Click();
            string errorMsg = GetValidationMessage("age");

            // Assert
            Assert.That(errorMsg, Is.Not.Empty);
        }

        [Test]

        public void Test8_AccidentsOmitted_Error()
        {
            // Arrange
            FillCompleteForm("37", "8", "0", submitForm: false);
            driver.FindElement(By.Id("accidents")).Clear();

            // Act
            driver.FindElement(By.Id("btnSubmit")).Click();

            // Wait for validation to trigger
            System.Threading.Thread.Sleep(500); // Small delay for validation
            string errorMsg = GetValidationMessage("accidents");

            // Assert
            Assert.That(errorMsg, Is.Not.Empty.And.Contains("accident").IgnoreCase,
                $"Expected validation error for accidents field but got: '{errorMsg}'");
        }

        [Test]
        public void Test9_ExperienceOmitted_Error()
        {
            // Arrange
            FillCompleteForm("45", "10", "0", submitForm: false);
            driver.FindElement(By.Id("experience")).Clear();

            // Act
            driver.FindElement(By.Id("btnSubmit")).Click();

            // Wait for validation to trigger
            System.Threading.Thread.Sleep(500); // Small delay for validation
            string errorMsg = GetValidationMessage("experience");

            // Assert
            Assert.That(errorMsg, Is.Not.Empty.And.Contains("experience").IgnoreCase,
                $"Expected validation error for experience field but got: '{errorMsg}'");
        }

        [Test]
        public void Test10_MinimumAge_Quote7000()
        {
            // Arrange
            string expectedQuote = "$7000";

            // Act
            FillCompleteForm("16", "0", "0");
            string result = driver.FindElement(By.Id("finalQuote")).GetAttribute("value")!;

            // Assert
            Assert.That(result, Is.EqualTo(expectedQuote));
        }

        [Test]
        public void Test11_Age30_2YearsExp_Quote3905()
        {
            // Arrange
            string expected = "$3905";

            // Act
            FillCompleteForm("30", "2", "1");
            string actual = driver.FindElement(By.Id("finalQuote")).GetAttribute("value")!;

            // Assert
            Assert.That(actual, Is.EqualTo(expected));
        }

        [Test]
        public void Test12_MaxExperienceDiff_Quote2840()
        {
            // Arrange
            string expected = "$2840";

            // Act
            FillCompleteForm("45", "29", "1");
            string actual = driver.FindElement(By.Id("finalQuote")).GetAttribute("value")!;

            // Assert
            Assert.That(actual, Is.EqualTo(expected));
        }

        [Test]
        public void Test13_InvalidAge15_Error()
        {
            // Arrange
            FillCompleteForm("16", "0", "0", submitForm: false);
            driver.FindElement(By.Id("age")).Clear();
            driver.FindElement(By.Id("age")).SendKeys("15");

            // Act
            driver.FindElement(By.Id("btnSubmit")).Click();
            string errorMsg = GetValidationMessage("age");

            // Assert
            Assert.That(errorMsg, Is.Not.Empty);
        }

        [Test]
        public void Test14_InvalidExperience_Error()
        {
            // Arrange
            FillCompleteForm("20", "0", "0", submitForm: false);
            driver.FindElement(By.Id("experience")).Clear();
            driver.FindElement(By.Id("experience")).SendKeys("5");

            // Act
            driver.FindElement(By.Id("btnSubmit")).Click();
            string result = wait.Until(d => d.FindElement(By.Id("finalQuote"))).GetAttribute("value")!;

            // Assert
            Assert.That(result, Is.EqualTo("No Insurance for you!! Driver Age / Experience Not Correct"));
        }

        [Test]
        public void Test15_ValidData_Quote2840()
        {
            // Arrange
            string expected = "$2840";

            // Act
            FillCompleteForm("40", "10", "2");
            string actual = driver.FindElement(By.Id("finalQuote")).GetAttribute("value")!;

            // Assert
            Assert.That(actual, Is.EqualTo(expected));
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