using FluentAssertions;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;

namespace DatesAndStuff.Web.Tests;

[TestFixture]
public class BlazeDemoTests
{
    private IWebDriver driver;

    [SetUp]
    public void SetupTest()
    {
        driver = new ChromeDriver();
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
            // Ignore browser shutdown errors
        }
    }

    [Test]
    public void BlazeDemo_MexicoCity_To_Dublin_ShouldHaveAtLeastThreeFlights()
    {
        // Arrange
        driver.Navigate().GoToUrl("https://blazedemo.com");

        var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));
        wait.Until(d => d.FindElement(By.Name("fromPort")));
        wait.Until(d => d.FindElement(By.Name("toPort")));

        ((IJavaScriptExecutor)driver).ExecuteScript(
            """
            document.querySelector("select[name='fromPort']").value = "Mexico City";
            document.querySelector("select[name='toPort']").value = "Dublin";
            """);

        // Act
        var findFlightsButton = wait.Until(d => d.FindElement(By.CssSelector("input[type='submit']")));
        findFlightsButton.Click();

        // Assert
        var flights = wait.Until(d =>
        {
            var rows = d.FindElements(By.CssSelector("table tbody tr"));
            return rows.Count > 0 ? rows : null;
        });

        flights.Should().NotBeNull();
        flights!.Count.Should().BeGreaterThanOrEqualTo(3);
    }
}
