﻿using System;
using NUnit.Framework;
using Roadkill.Core.Cache;
using Roadkill.Core.Configuration;
using Roadkill.Core.Converters;
using Roadkill.Core.Database;
using Roadkill.Core.Text;
using Roadkill.Tests.Unit.StubsAndMocks;

namespace Roadkill.Tests.Unit.Text
{
	[TestFixture]
	[Category("Unit")]
	public class MenuParserTests
	{
		private PluginFactoryMock _pluginFactory;

		[SetUp]
		public void Setup()
		{
			_pluginFactory = new PluginFactoryMock();
		}

		[Test]
		public void should_replace_known_tokens_when_logged_in_as_admin()
		{
			// Arrange
			string menuMarkup = "* %categories%\r\n\r\n%allpages%\r\n%mainpage%\r\n%newpage%\r\n%managefiles%\r\n%sitesettings%\r\n";
			string expectedHtml = "<ul><li><a href=\"/pages/alltags\">Categories</a></li></ul>" +
								  "<a href=\"/pages/allpages\">All pages</a>" +
								  "<a href=\"/\">Main Page</a>" +
								  "<a href=\"/pages/new\">New page</a>" +
								  "<a href=\"/filemanager\">Manage files</a>" +
								  "<a href=\"/settings\">Site settings</a>";

			RepositoryMock repository = new RepositoryMock();
			repository.SiteSettings = new SiteSettings();
			repository.SiteSettings.MarkupType = "Markdown";
			repository.SiteSettings.MenuMarkup = menuMarkup;

			UserContextStub userContext = new UserContextStub();
			userContext.IsAdmin = true;
			userContext.IsLoggedIn = true;

			ApplicationSettings applicationSettings = new ApplicationSettings();
			applicationSettings.Installed = true;
			CacheMock cache = new CacheMock();
			SiteCache siteCache = new SiteCache(cache);

			MarkupConverter converter = new MarkupConverter(applicationSettings, repository, _pluginFactory);
			MenuParser parser = new MenuParser(converter, repository, siteCache, userContext);

			// Act
			string actualHtml = parser.GetMenu();

			// Assert
			Assert.That(actualHtml, Is.EqualTo(expectedHtml), actualHtml);
		}

		[Test]
		public void should_replace_known_tokens_when_logged_in_as_editor()
		{
			// Arrange
			string menuMarkup = "* %categories%\r\n\r\n%allpages%\r\n%mainpage%\r\n%newpage%\r\n%managefiles%\r\n%sitesettings%\r\n";
			string expectedHtml = "<ul><li><a href=\"/pages/alltags\">Categories</a></li></ul>" +
								  "<a href=\"/pages/allpages\">All pages</a>" +
								  "<a href=\"/\">Main Page</a>" +
								  "<a href=\"/pages/new\">New page</a><a href=\"/filemanager\">Manage files</a>";

			RepositoryMock repository = new RepositoryMock();
			repository.SiteSettings = new SiteSettings();
			repository.SiteSettings.MarkupType = "Markdown";
			repository.SiteSettings.MenuMarkup = menuMarkup;

			UserContextStub userContext = new UserContextStub();
			userContext.IsAdmin = false;
			userContext.IsLoggedIn = true;

			ApplicationSettings applicationSettings = new ApplicationSettings();
			applicationSettings.Installed = true;
			CacheMock cache = new CacheMock();
			SiteCache siteCache = new SiteCache(cache);

			MarkupConverter converter = new MarkupConverter(applicationSettings, repository, _pluginFactory);
			MenuParser parser = new MenuParser(converter, repository, siteCache, userContext);

			// Act
			string actualHtml = parser.GetMenu();

			// Assert
			Assert.That(actualHtml, Is.EqualTo(expectedHtml), actualHtml);
		}

		[Test]
		public void should_replace_known_tokens_when_not_logged()
		{
			// Arrange
			string menuMarkup = "* %categories%\r\n\r\n%allpages%\r\n%mainpage%\r\n%newpage%\r\n%managefiles%\r\n%sitesettings%\r\n";
			string expectedHtml = "<ul><li><a href=\"/pages/alltags\">Categories</a></li></ul>" +
								  "<a href=\"/pages/allpages\">All pages</a>" +
								  "<a href=\"/\">Main Page</a>";

			RepositoryMock repository = new RepositoryMock();
			repository.SiteSettings = new SiteSettings();
			repository.SiteSettings.MarkupType = "Markdown";
			repository.SiteSettings.MenuMarkup = menuMarkup;

			UserContextStub userContext = new UserContextStub();
			userContext.IsLoggedIn = false;

			ApplicationSettings applicationSettings = new ApplicationSettings();
			applicationSettings.Installed = true;
			CacheMock cache = new CacheMock();
			SiteCache siteCache = new SiteCache(cache);

			MarkupConverter converter = new MarkupConverter(applicationSettings, repository, _pluginFactory);
			MenuParser parser = new MenuParser(converter, repository, siteCache, userContext);

			// Act
			string actualHtml = parser.GetMenu();

			// Assert
			Assert.That(actualHtml, Is.EqualTo(expectedHtml), actualHtml);
		}

		[Test]
		[TestCase("Creole", "<a href=\"/\">Main Page</a>")]
		[TestCase("Markdown", "<a href=\"/\">Main Page</a>")]
		public void Should_Remove_Empty_UL_Tags_For_Logged_In_Tokens_When_Not_Logged_In(string markupType, string expectedHtml)
		{
			// Arrange - \r\n is important so the markdown is valid
			string menuMarkup = "%mainpage%\r\n\r\n* %newpage%\r\n* %managefiles%\r\n* %sitesettings%\r\n";

			RepositoryMock repository = new RepositoryMock();
			repository.SiteSettings = new SiteSettings();
			repository.SiteSettings.MarkupType = markupType;
			repository.SiteSettings.MenuMarkup = menuMarkup;

			UserContextStub userContext = new UserContextStub();
			userContext.IsLoggedIn = false;

			ApplicationSettings applicationSettings = new ApplicationSettings();
			applicationSettings.Installed = true;
			CacheMock cache = new CacheMock();
			SiteCache siteCache = new SiteCache(cache);

			MarkupConverter converter = new MarkupConverter(applicationSettings, repository, _pluginFactory);
			MenuParser parser = new MenuParser(converter, repository, siteCache, userContext);

			// Act
			string actualHtml = parser.GetMenu();

			// Assert
			Assert.That(actualHtml, Is.EqualTo(expectedHtml));
		}

		[Test]
		public void should_cache_menu_html_for_admin_and_editor_and_guest_user()
		{
			// Arrange
			string menuMarkup = "My menu %newpage% %sitesettings%";

			RepositoryMock repository = new RepositoryMock();
			repository.SiteSettings = new SiteSettings();
			repository.SiteSettings.MarkupType = "Markdown";
			repository.SiteSettings.MenuMarkup = menuMarkup;

			UserContextStub userContext = new UserContextStub();
			ApplicationSettings applicationSettings = new ApplicationSettings();
			applicationSettings.Installed = true;

			CacheMock cache = new CacheMock();
			SiteCache siteCache = new SiteCache(cache);

			MarkupConverter converter = new MarkupConverter(applicationSettings, repository, _pluginFactory);
			MenuParser parser = new MenuParser(converter, repository, siteCache, userContext);

			// Act
			userContext.IsLoggedIn = false;
			userContext.IsAdmin = false;
			parser.GetMenu();

			userContext.IsLoggedIn = true;
			userContext.IsAdmin = false;
			parser.GetMenu();

			userContext.IsLoggedIn = true;
			userContext.IsAdmin = true;
			parser.GetMenu();

			// Assert
			Assert.That(cache.CacheItems.Count, Is.EqualTo(3));
		}

		[Test]
		public void should_return_different_menu_html_for_admin_and_editor_and_guest_user()
		{
			// Arrange
			string menuMarkup = "My menu %newpage% %sitesettings%";

			RepositoryMock repository = new RepositoryMock();
			repository.SiteSettings = new SiteSettings();
			repository.SiteSettings.MarkupType = "Markdown";
			repository.SiteSettings.MenuMarkup = menuMarkup;

			UserContextStub userContext = new UserContextStub();
			ApplicationSettings applicationSettings = new ApplicationSettings();
			applicationSettings.Installed = true;

			CacheMock cache = new CacheMock();
			SiteCache siteCache = new SiteCache(cache);

			MarkupConverter converter = new MarkupConverter(applicationSettings, repository, _pluginFactory);
			MenuParser parser = new MenuParser(converter, repository, siteCache, userContext);

			// Act
			userContext.IsLoggedIn = false;
			userContext.IsAdmin = false;
			string guestHtml = parser.GetMenu();

			userContext.IsLoggedIn = true;
			userContext.IsAdmin = false;
			string editorHtml = parser.GetMenu();

			userContext.IsLoggedIn = true;
			userContext.IsAdmin = true;
			string adminHtml = parser.GetMenu();

			// Assert
			Assert.That(guestHtml, Is.EqualTo("My menu"));
			Assert.That(editorHtml, Is.EqualTo("My menu <a href=\"/pages/new\">New page</a>"));
			Assert.That(adminHtml, Is.EqualTo("My menu <a href=\"/pages/new\">New page</a> <a href=\"/settings\">Site settings</a>"));
		}
		
		[Test]
		public void should_replace_markdown_with_external_link()
		{
			// Arrange
			string menuMarkup = "* [First link](http://www.google.com)\r\n";
			string expectedHtml = "<ul><li><a href=\"http://www.google.com\" rel=\"nofollow\" class=\"external-link\">First link</a></li></ul>";

			RepositoryMock repository = new RepositoryMock();
			repository.SiteSettings = new SiteSettings();
			repository.SiteSettings.MarkupType = "Markdown";
			repository.SiteSettings.MenuMarkup = menuMarkup;

			UserContextStub userContext = new UserContextStub();
			ApplicationSettings applicationSettings = new ApplicationSettings();
			applicationSettings.Installed = true;

			CacheMock cache = new CacheMock();
			SiteCache siteCache = new SiteCache(cache);

			MarkupConverter converter = new MarkupConverter(applicationSettings, repository, _pluginFactory);
			MenuParser parser = new MenuParser(converter, repository, siteCache, userContext);

			// Act
			string actualHtml = parser.GetMenu();

			// Assert
			Assert.That(actualHtml, Is.EqualTo(expectedHtml));
		}

		[Test]
		public void should_replace_markdown_with_internal_link()
		{
			// Arrange
			string menuMarkup = "* [First link](my-page)\r\n";
			string expectedHtml = "<ul><li><a href=\"/wiki/1/my-page\">First link</a></li></ul>";

			RepositoryMock repository = new RepositoryMock();
			repository.AddNewPage(new Page() { Title = "my page", Id = 1 }, "text", "user", DateTime.Now);

			repository.SiteSettings = new SiteSettings();
			repository.SiteSettings.MarkupType = "Markdown";
			repository.SiteSettings.MenuMarkup = menuMarkup;

			UserContextStub userContext = new UserContextStub();
			ApplicationSettings applicationSettings = new ApplicationSettings();
			applicationSettings.Installed = true;

			CacheMock cache = new CacheMock();
			SiteCache siteCache = new SiteCache(cache);

			MarkupConverter converter = new MarkupConverter(applicationSettings, repository, _pluginFactory);
			MenuParser parser = new MenuParser(converter, repository, siteCache, userContext);

			// Act
			string actualHtml = parser.GetMenu();

			// Assert
			Assert.That(actualHtml, Is.EqualTo(expectedHtml));
		}
	}
}
