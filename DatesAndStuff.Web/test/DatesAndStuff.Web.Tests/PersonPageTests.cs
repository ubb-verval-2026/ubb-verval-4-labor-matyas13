using System;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Globalization;
using FluentAssertions;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using SeleniumExtras.WaitHelpers;

namespace DatesAndStuff.Web.Tests;

[TestFixture]
public class PersonPageTests
{
    private IWebDriver driver;
    private StringBuilder verificationErrors;
    private const string BaseURL = "http://localhost:5091";
    private bool acceptNextAlert = true;

    private Process? _blazorProcess;

    [OneTimeSetUp]
    public void StartBlazorServer()
    {
        var webProjectPath = Path.GetFullPath(Path.Combine(
            Assembly.GetExecutingAssembly().Location,
            "../../../../../../src/DatesAndStuff.Web/DatesAndStuff.Web.csproj"
            ));

        var webProjFolderPath = Path.GetDirectoryName(webProjectPath);

        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            //Arguments = $"run --project \"{webProjectPath}\"",
            Arguments = "dotnet run --no-build",
            WorkingDirectory = webProjFolderPath,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };

        _blazorProcess = Process.Start(startInfo);

        // Wait for the app to become available
        var client = new HttpClient();
        var timeout = TimeSpan.FromSeconds(30);
        var start = DateTime.Now;

        while (DateTime.Now - start < timeout)
        {
            try
            {
                var result = client.GetAsync(BaseURL).Result;
                if (result.IsSuccessStatusCode)
                {
                    break;
                }
            }
            catch (Exception e)
            {
                Thread.Sleep(1000);
            }
        }
    }

    [OneTimeTearDown]
    public void StopBlazorServer()
    {
        if (_blazorProcess != null && !_blazorProcess.HasExited)
        {
            _blazorProcess.Kill(true);
            _blazorProcess.Dispose();
        }
    }

    [SetUp]
    public void SetupTest()
    {
        driver = new ChromeDriver();
        verificationErrors = new StringBuilder();
    }

    [TearDown]
    public void TeardownTest()
    {
        try
        {
            driver.Quit();
            driver.Dispose();
        }
        catch (Exception)
        {
            // Ignore errors if unable to close the browser
        }
        Assert.That(verificationErrors.ToString(), Is.EqualTo(""));
    }

    [TestCase(5, 5250)]
    [TestCase(10, 5500)]
    [TestCase(2.5, 5125)]
    public void Person_SalaryIncrease_ShouldIncrease(double increasePercentage, double expectedSalary)
    {
        // Arrange
        driver.Navigate().GoToUrl(BaseURL);
        driver.FindElement(By.XPath("//*[@data-test='PersonPageNavigation']")).Click();

        var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(5));
        var inputSelector = "//*[@data-test='SalaryIncreasePercentageInput']";

        wait.Until(ExpectedConditions.ElementExists(By.XPath(inputSelector)));
        var inputValue = increasePercentage.ToString(System.Globalization.CultureInfo.InvariantCulture);
        ((IJavaScriptExecutor)driver).ExecuteScript(
            """
            const input = document.querySelector("[data-test='SalaryIncreasePercentageInput']");
            input.value = arguments[0];
            input.dispatchEvent(new Event('change', { bubbles: true }));
            input.dispatchEvent(new Event('blur', { bubbles: true }));
            """,
            inputValue);

        // Act
        var submitButton = wait.Until(ExpectedConditions.ElementExists(By.XPath("//*[@data-test='SalaryIncreaseSubmitButton']")));
        submitButton.Click();


        // Assert
        var salaryLabel = wait.Until(ExpectedConditions.ElementExists(By.XPath("//*[@data-test='DisplayedSalary']")));
        var salaryAfterSubmission = double.Parse(salaryLabel.Text);
        salaryAfterSubmission.Should().BeApproximately(expectedSalary, 0.00001);
    }

    [TestCase(-10)]
    [TestCase(-11)]
    public void Person_SalaryIncrease_MinusTenOrLess_ShouldDisplayValidationMessages(double increasePercentage)
    {
        // Arrange
        driver.Navigate().GoToUrl(BaseURL);
        driver.FindElement(By.XPath("//*[@data-test='PersonPageNavigation']")).Click();

        var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(5));
        const string validationMessage = "The specified percentage should be greater than -10.";
        wait.Until(ExpectedConditions.ElementExists(By.XPath("//*[@data-test='SalaryIncreasePercentageInput']")));

        ((IJavaScriptExecutor)driver).ExecuteScript(
            """
            const input = document.querySelector("[data-test='SalaryIncreasePercentageInput']");
            input.value = arguments[0];
            input.dispatchEvent(new Event('change', { bubbles: true }));
            input.dispatchEvent(new Event('blur', { bubbles: true }));
            """,
            increasePercentage.ToString(CultureInfo.InvariantCulture));

        // Act
        var submitButton = wait.Until(ExpectedConditions.ElementExists(By.XPath("//*[@data-test='SalaryIncreaseSubmitButton']")));
        submitButton.Click();

        // Assert
        var summaryError = wait.Until(ExpectedConditions.ElementExists(
            By.XPath($"//ul[contains(@class,'validation-errors')]//li[normalize-space()='{validationMessage}']")));

        var fieldError = wait.Until(ExpectedConditions.ElementExists(
            By.XPath($"//*[@data-test='SalaryIncreasePercentageInput']/parent::*//*[contains(@class,'validation-message') and normalize-space()='{validationMessage}']")));

        summaryError.Text.Should().Be(validationMessage);
        fieldError.Text.Should().Be(validationMessage);
    }

    private bool IsElementPresent(By by)
    {
        try
        {
            driver.FindElement(by);
            return true;
        }
        catch (NoSuchElementException)
        {
            return false;
        }
    }

    private bool IsAlertPresent()
    {
        try
        {
            driver.SwitchTo().Alert();
            return true;
        }
        catch (NoAlertPresentException)
        {
            return false;
        }
    }

    private string CloseAlertAndGetItsText()
    {
        try
        {
            IAlert alert = driver.SwitchTo().Alert();
            string alertText = alert.Text;
            if (acceptNextAlert)
            {
                alert.Accept();
            }
            else
            {
                alert.Dismiss();
            }
            return alertText;
        }
        finally
        {
            acceptNextAlert = true;
        }
    }
}
