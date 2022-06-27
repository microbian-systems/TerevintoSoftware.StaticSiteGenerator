﻿using Moq;
using TerevintoSoftware.StaticSiteGenerator.AspNetCoreInternal;
using TerevintoSoftware.StaticSiteGenerator.Configuration;
using TerevintoSoftware.StaticSiteGenerator.Services;

namespace TerevintoSoftware.StaticSiteGenerator.Tests.Services;

public class ViewCompilerServiceTests
{
    private readonly Mock<IViewRenderService> _viewRenderServiceMock = new();
    private readonly Mock<IHtmlFormatter> _htmlFormatterMock = new();
    private readonly StaticSiteGenerationOptions _staticSiteGenerationOptions = new("test", "test", "test", "Home", "", RouteCasing.LowerCase, "en", false, false);

    private ViewCompilerService _viewCompilerService;

    [SetUp]
    public void Setup()
    {
        _viewCompilerService = new(_viewRenderServiceMock.Object, _htmlFormatterMock.Object, _staticSiteGenerationOptions);
    }

    [Test]
    public async Task CompileViews_ShouldReturnSameAmountOfElements()
    {
        var viewsToRender = new List<CultureBasedView>
        {
            new CultureBasedView("Home/Index", new[] { "en" }),
            new CultureBasedView("Home/Blog", new[] { "en" }),
        };

        _viewRenderServiceMock.Setup(x => x.GetCompiledView(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(() => "html");
        _htmlFormatterMock
            .Setup(x => x.FixRelativeLinks(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns((string html, string defaultCulture, string culture) => html);

        var result = await _viewCompilerService.CompileViews(viewsToRender);

        Assert.Multiple(() =>
        {
            Assert.That(result.Count(), Is.EqualTo(viewsToRender.Count));
            Assert.That(result.Any(x => x.Failed), Is.EqualTo(false));
        });
    }

    [Test]
    public async Task CompileViews_WhenRenderingFails_ViewsAreReturnedAsFailures()
    {
        var viewsToRender = new List<CultureBasedView>
        {
            new CultureBasedView("Home/Index", new[] { "en" }),
            new CultureBasedView("Home/Blog", new[] { "en" }),
        };

        _viewRenderServiceMock.Setup(x => x.GetCompiledView(It.IsAny<string>(), It.IsAny<string>())).ThrowsAsync(new Exception("test"));

        var result = await _viewCompilerService.CompileViews(viewsToRender);

        Assert.Multiple(() =>
        {
            Assert.That(result.Count(), Is.EqualTo(viewsToRender.Count));
            Assert.That(result.Any(x => x.Failed), Is.EqualTo(true));
        });
    }

    [Test]
    public async Task CompileViews_WhenTheCultureIsDifferent_TheViewNameShouldBeUpdated()
    {
        var viewsToRender = new List<CultureBasedView>
        {
            new CultureBasedView("Home/Index", new[] { "en" }),
            new CultureBasedView("Home/Blog", new[] { "es" }),
        };

        _viewRenderServiceMock.Setup(x => x.GetCompiledView(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(() => "html");
        _htmlFormatterMock
            .Setup(x => x.FixRelativeLinks(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns((string html, string defaultCulture, string culture) => html);

        var result = await _viewCompilerService.CompileViews(viewsToRender);

        var firstView = result.First(x => x.OriginalViewName.StartsWith("Home/Index"));
        var secondView = result.First(x => x.OriginalViewName.StartsWith("Home/Blog"));

        Assert.Multiple(() =>
        {
            Assert.That(firstView.OriginalViewName, Is.EqualTo("Home/Index"));
            Assert.That(secondView.OriginalViewName, Is.EqualTo("Home/Blog.es"));
        });
    }
}
