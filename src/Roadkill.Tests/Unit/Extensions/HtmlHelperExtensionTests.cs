﻿using System.IO;
using System.Web.Mvc;
using Moq;
using NUnit.Framework;
using Roadkill.Core;
using Roadkill.Core.Configuration;
using Roadkill.Core.Extensions;
using Roadkill.Core.Mvc.Controllers;
using Roadkill.Core.Mvc.ViewModels;
using Roadkill.Core.Services;
using Roadkill.Tests.Unit.StubsAndMocks;
using Roadkill.Tests.Unit.StubsAndMocks.Mvc;

namespace Roadkill.Tests.Unit.Extensions
{
	[TestFixture]
	[Category("Unit")]
	public class HtmlHelperExtensionTests
	{
		// Objects for the HtmlHelper
		private MocksAndStubsContainer _container;
		private ApplicationSettings _applicationSettings;
		private IUserContext _context;
		private RepositoryMock _repository;
		private UserServiceMock _userService;
		private PageService _pageService;
		private PageHistoryService _historyService;
		private SettingsService _settingsService;
		private PluginFactoryMock _pluginFactory;
		private WikiController _wikiController;
		private HtmlHelper _htmlHelper;
		private ViewContext _viewContext;

		[SetUp]
		public void Setup()
		{
			// WikiController setup (use WikiController as it's the one typically used by views)
			_container = new MocksAndStubsContainer();

			_applicationSettings = _container.ApplicationSettings;
			_context = _container.UserContext;
			_repository = _container.Repository;
			_pluginFactory = _container.PluginFactory;
			_settingsService = _container.SettingsService;
			_userService = _container.UserService;
			_historyService = _container.HistoryService;
			_pageService = _container.PageService;

			_wikiController = new WikiController(_applicationSettings, _userService, _pageService, _context, _settingsService);
			_wikiController.SetFakeControllerContext("~/wiki/index/1");

			// HtmlHelper setup
			var viewDataDictionary = new ViewDataDictionary();
			_viewContext = new ViewContext(_wikiController.ControllerContext, new Mock<IView>().Object, viewDataDictionary, new TempDataDictionary(), new StringWriter());
			var mockViewDataContainer = new Mock<IViewDataContainer>();
			mockViewDataContainer.Setup(v => v.ViewData).Returns(viewDataDictionary);

			_htmlHelper = new HtmlHelper(_viewContext, mockViewDataContainer.Object);
		}

		[Test]
		public void renderpagebytag_should_return_rendered_html_locked_page_when_multiple_pages_exist_for_tag()
		{
			// Arrange
			_pageService.AddPage( new PageViewModel() { Title = "Page1", RawTags = "software,tag2,tag3", IsLocked = true, Content = "page 1 content"});
			_pageService.AddPage(new PageViewModel() { Title = "Page2", RawTags = "software,page2", Content = "page 2 content" });

			// Act
			MvcHtmlString htmlString = _htmlHelper.RenderPageByTag("software");

			// Assert
			Assert.That(htmlString.ToHtmlString(), Is.EqualTo("<p>page 1 content\n</p>"));
		}

		[Test]
		public void renderpagebytag_should_return_rendered_html_for_known_tag()
		{
			// Arrange
			_pageService.AddPage(new PageViewModel() { Title = "Page1", RawTags = "software, tag2, tag3", IsLocked = true, Content = "page 1 content" });
			_pageService.AddPage(new PageViewModel() { Title = "Page2", RawTags = "software, page2", Content = "page 2 content" });

			// Act
			MvcHtmlString htmlString = _htmlHelper.RenderPageByTag("page2");

			// Assert
			Assert.That(htmlString.ToHtmlString(), Is.EqualTo("<p>page 2 content\n</p>"));
		}

		[Test]
		public void renderpagebytag_should_return_empty_string_when_tag_does_not_exist()
		{
			// Arrange
			_pageService.AddPage(new PageViewModel() { Title = "Page1", RawTags = "software, tag2, tag3", Content = "page 1 content" });
			_pageService.AddPage(new PageViewModel() { Title = "Page2", RawTags = "software, page2", Content = "page 2 content" });

			// Act
			MvcHtmlString htmlString = _htmlHelper.RenderPageByTag("no-tag");

			// Assert
			Assert.That(htmlString.ToHtmlString(), Is.EqualTo(""));
		}

		[Test]
		public void renderpagebytag_should_return_empty_string_when_controller_is_not_wikicontroller()
		{
			// Arrange
			_viewContext.Controller = new Mock<System.Web.Mvc.ControllerBase>().Object;

			// Act
			MvcHtmlString htmlString = _htmlHelper.RenderPageByTag("tag1");

			// Assert
			Assert.That(htmlString.ToHtmlString(), Is.EqualTo(""));
		}

		[Test]
		[Ignore]
		public void DropDownBox1_Should()
		{
			// Can't mock DropDownList as it relies on private/internal implementations in System.Web.Mvc
		}

		[Test]
		[Ignore]
		public void DropDownBox2_Should()
		{
			// Can't mock DropDownList as it relies on private/internal implementations in System.Web.Mvc

		}

		[Test]
		[Ignore]
		public void DialogPartial_Should_()
		{
			// RenderPartials can't be tested without ridiculous amounts of setup
		}

		[Test]
		[Ignore]
		public void DialogPartial2_Should_()
		{
			// RenderPartialscan't be tested without ridiculous amounts of setup
		}

		[Ignore]
		[Test]
		public void sitesettingsnavigation_should_()
		{
			// RenderPartials can't be tested without ridiculous amounts of setup
		}
		
		// All Bootstrap Helpers are the same as above
	}
}
