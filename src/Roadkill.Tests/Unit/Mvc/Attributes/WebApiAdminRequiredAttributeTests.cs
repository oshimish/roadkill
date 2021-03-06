﻿using System;
using System.Threading;
using System.Web.Http.Controllers;
using NUnit.Framework;
using Roadkill.Core;
using Roadkill.Core.Configuration;
using Roadkill.Core.Mvc.Attributes;
using Roadkill.Tests.Unit.StubsAndMocks;

namespace Roadkill.Tests.Unit.Mvc.Attributes
{
	/// <summary>
	/// Setup-heavy tests for the AdminRequired attribute.
	/// </summary>
	[TestFixture]
	[Category("Unit")]
	public class WebApiAdminRequiredAttributeTests
	{
		private MocksAndStubsContainer _container;

		private ApplicationSettings _applicationSettings;
		private IUserContext _context;
		private UserServiceMock _userService;

		[SetUp]
		public void Setup()
		{
			_container = new MocksAndStubsContainer();

			_applicationSettings = _container.ApplicationSettings;
			_context = _container.UserContext;
			_userService = _container.UserService;

			_applicationSettings.AdminRoleName = "Admin";
			_applicationSettings.EditorRoleName = "Editor";
		}

		[Test]
		public void should_use_authorizationprovider()
		{
			// Arrange
			WebApiAdminRequiredAttributeMock attribute = new WebApiAdminRequiredAttributeMock();
			attribute.AuthorizationProvider = new AuthorizationProviderMock() { IsAdminResult = true };
			attribute.ApplicationSettings = _applicationSettings;
			attribute.UserService = _userService;

			IdentityStub identity = new IdentityStub() { Name = Guid.NewGuid().ToString(), IsAuthenticated = true };
			PrincipalStub principal = new PrincipalStub() { Identity = identity };
			Thread.CurrentPrincipal = principal;

			// Act
			bool isAuthorized = attribute.CallAuthorize(new HttpActionContext());

			// Assert
			Assert.That(isAuthorized, Is.True);
		}

		[Test]
		[ExpectedException(typeof(SecurityException))]
		public void Should_Throw_SecurityException_When_AuthorizationProvider_Is_Null()
		{
			// Arrange
			WebApiAdminRequiredAttributeMock attribute = new WebApiAdminRequiredAttributeMock();
			attribute.AuthorizationProvider = null;

			IdentityStub identity = new IdentityStub() { Name = Guid.NewGuid().ToString(), IsAuthenticated = true };
			PrincipalStub principal = new PrincipalStub() { Identity = identity };
			Thread.CurrentPrincipal = principal;

			// Act + Assert
			attribute.CallAuthorize(new HttpActionContext());
		}
	}

	internal class WebApiAdminRequiredAttributeMock : WebApiAdminRequiredAttribute
	{
		public bool CallAuthorize(HttpActionContext actionContext)
		{
			return base.IsAuthorized(actionContext);
		}
	}
}