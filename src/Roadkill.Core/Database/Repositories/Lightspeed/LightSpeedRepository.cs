﻿using System;
using System.Collections.Generic;
using System.Linq;
using Mindscape.LightSpeed;
using Mindscape.LightSpeed.Caching;
using Mindscape.LightSpeed.Linq;
using Mindscape.LightSpeed.Querying;
using Roadkill.Core.Configuration;
using Roadkill.Core.Logging;
using Roadkill.Core.Plugins;
using PluginSettings = Roadkill.Core.Plugins.Settings;

namespace Roadkill.Core.Database.LightSpeed
{
	public class LightSpeedRepository : IRepository
	{
		[ThreadStatic]
		private static LightSpeedContext _context;

		internal readonly IUnitOfWork _unitOfWork;
		public DataProvider DataProvider { get; }
		public string ConnectionString { get; }

		internal IQueryable<PageEntity> Pages => UnitOfWork.Query<PageEntity>();
		internal IQueryable<PageContentEntity> PageContents => UnitOfWork.Query<PageContentEntity>();
		internal IQueryable<UserEntity> Users => UnitOfWork.Query<UserEntity>();

		public static LightSpeedContext Context
		{
			get
			{
				if (_context == null)
				{
					throw new DatabaseException("The LightSpeedContext for Lightspeed is null", null);
				}

				return _context;
			}
		}

		public virtual IUnitOfWork UnitOfWork
		{
			get
			{
				if (_unitOfWork == null)
				{
                    throw new DatabaseException("The IUnitOfWork for Lightspeed is null", null);
				}

				return _unitOfWork;
			}
		}

		public LightSpeedRepository(DataProvider dataDataProvider, string connectionString)
		{
			if (string.IsNullOrEmpty(connectionString))
				return;

			DataProvider = dataDataProvider;
			ConnectionString = connectionString;

			if (_context == null)
			{
				_context = CreateLightSpeedContext();
			}

			_unitOfWork = _context.CreateUnitOfWork();
		}

		private LightSpeedContext CreateLightSpeedContext()
		{
			LightSpeedContext context = new LightSpeedContext();
			context.Cache = new CacheBroker(new DefaultCache());
			context.ConnectionString = ConnectionString;
			context.DataProvider = DataProvider;
			context.IdentityMethod = IdentityMethod.GuidComb;
			context.CascadeDeletes = true;

			return context;
		}

		public void EnableVerboseLogging()
		{
			Context.VerboseLogging = true;
			Context.Logger = new DatabaseLogger();
		}

		#region ISettingsRepository
		public SiteSettings GetSiteSettings()
		{
			SiteSettings siteSettings = new SiteSettings();
			SiteConfigurationEntity entity = UnitOfWork.FindById<SiteConfigurationEntity>(SiteSettings.SiteSettingsId);

			if (entity != null)
			{
				siteSettings = SiteSettings.LoadFromJson(entity.Content);
			}
			else
			{
				Log.Warn("No site settings could be found in the database, using a default instance");
			}

			return siteSettings;
		}

		public PluginSettings GetTextPluginSettings(Guid databaseId)
		{
			PluginSettings pluginSettings = null;
			SiteConfigurationEntity entity = UnitOfWork.FindById<SiteConfigurationEntity>(databaseId);

			if (entity != null)
			{
				pluginSettings = PluginSettings.LoadFromJson(entity.Content);
			}

			return pluginSettings;
		}

		public void SaveSiteSettings(SiteSettings siteSettings)
		{
			SiteConfigurationEntity entity = UnitOfWork.FindById<SiteConfigurationEntity>(SiteSettings.SiteSettingsId);

			if (entity == null || entity.Id == Guid.Empty)
			{
				entity = new SiteConfigurationEntity();
				entity.Id = SiteSettings.SiteSettingsId;
				entity.Version = ApplicationSettings.ProductVersion.ToString();
				entity.Content = siteSettings.GetJson();
				UnitOfWork.Add(entity);
			}
			else
			{
				entity.Version = ApplicationSettings.ProductVersion.ToString();
				entity.Content = siteSettings.GetJson();
			}

			UnitOfWork.SaveChanges();
		}

		public void SaveTextPluginSettings(TextPlugin plugin)
		{
			string version = plugin.Version;
			if (string.IsNullOrEmpty(version))
				version = "1.0.0";

			SiteConfigurationEntity entity = UnitOfWork.FindById<SiteConfigurationEntity>(plugin.DatabaseId);

			if (entity == null || entity.Id == Guid.Empty)
			{
				entity = new SiteConfigurationEntity();
				entity.Id = plugin.DatabaseId;
				entity.Version = version;
				entity.Content = plugin.Settings.GetJson();
				UnitOfWork.Add(entity);
			}
			else
			{
				entity.Version = version;
				entity.Content = plugin.Settings.GetJson();
			}

			UnitOfWork.SaveChanges();
		}

		#endregion

		#region IPageRepository
		public PageContent AddNewPage(Page page, string text, string editedBy, DateTime editedOn)
		{
			PageEntity pageEntity = new PageEntity();
			ToEntity.FromPage(page, pageEntity);
			pageEntity.Id = 0;
			UnitOfWork.Add(pageEntity);
			UnitOfWork.SaveChanges();

			PageContentEntity pageContentEntity = new PageContentEntity()
			{
				Id = Guid.NewGuid(),
				Page = pageEntity,
				Text = text,
				EditedBy = editedBy,
				EditedOn = editedOn,
				VersionNumber = 1,
			};

			UnitOfWork.Add(pageContentEntity);
			UnitOfWork.SaveChanges();

			PageContent pageContent = FromEntity.ToPageContent(pageContentEntity);
			pageContent.Page = FromEntity.ToPage(pageEntity);
			return pageContent;
		}

		public PageContent AddNewPageContentVersion(Page page, string text, string editedBy, DateTime editedOn, int version)
		{
			if (version < 1)
				version = 1;

			PageEntity pageEntity = UnitOfWork.FindById<PageEntity>(page.Id);
			if (pageEntity != null)
			{
				// Update the content
				PageContentEntity pageContentEntity = new PageContentEntity()
				{
					Id = Guid.NewGuid(),
					Page = pageEntity,
					Text = text,
					EditedBy = editedBy,
					EditedOn = editedOn,
					VersionNumber = version,
				};

				UnitOfWork.Add(pageContentEntity);
				UnitOfWork.SaveChanges();

				// The page modified fields
				pageEntity.ModifiedOn = editedOn;
				pageEntity.ModifiedBy = editedBy;
				UnitOfWork.SaveChanges();

				// Turn the content database entity back into a domain object
				PageContent pageContent = FromEntity.ToPageContent(pageContentEntity);
				pageContent.Page = FromEntity.ToPage(pageEntity);

				return pageContent;
			}

			Log.Error("Unable to update page content for page id {0} (not found)", page.Id);
			return null;
		}

		public IEnumerable<Page> AllPages()
		{
			List<PageEntity> entities = Pages.ToList();
			return FromEntity.ToPageList(entities);
		}

		public IEnumerable<PageContent> AllPageContents()
		{
			List<PageContentEntity> entities = PageContents.ToList();
			return FromEntity.ToPageContentList(entities);
		}

		public IEnumerable<string> AllTags()
		{
			return new List<string>(Pages.Select(p => p.Tags));
		}

		public void DeleteAllPages()
		{
			UnitOfWork.Remove(new Query(typeof(PageEntity)));
			UnitOfWork.SaveChanges();

			UnitOfWork.Remove(new Query(typeof(PageContentEntity)));
			UnitOfWork.SaveChanges();
		}

		public void DeletePage(Page page)
		{
			PageEntity entity = UnitOfWork.FindById<PageEntity>(page.Id);
			UnitOfWork.Remove(entity);
			UnitOfWork.SaveChanges();
		}

		public void DeletePageContent(PageContent pageContent)
		{
			PageContentEntity entity = UnitOfWork.FindById<PageContentEntity>(pageContent.Id);
			UnitOfWork.Remove(entity);
			UnitOfWork.SaveChanges();
		}

		public IEnumerable<Page> FindPagesCreatedBy(string username)
		{
			List<PageEntity> entities = Pages.Where(p => p.CreatedBy == username).ToList();
			return FromEntity.ToPageList(entities);
		}

		public IEnumerable<Page> FindPagesModifiedBy(string username)
		{
			List<PageEntity> entities = Pages.Where(p => p.ModifiedBy == username).ToList();
			return FromEntity.ToPageList(entities);
		}

		public IEnumerable<Page> FindPagesContainingTag(string tag)
		{
			IEnumerable<PageEntity> entities = Pages.Where(p => p.Tags.ToLower().Contains(tag.ToLower())); // Lightspeed doesn't support ToLowerInvariant
			return FromEntity.ToPageList(entities);
		}

		public IEnumerable<PageContent> FindPageContentsByPageId(int pageId)
		{
			List<PageContentEntity> entities = PageContents.Where(p => p.Page.Id == pageId).ToList();
			return FromEntity.ToPageContentList(entities);
		}

		public IEnumerable<PageContent> FindPageContentsEditedBy(string username)
		{
			List<PageContentEntity> entities = PageContents.Where(p => p.EditedBy == username).ToList();
			return FromEntity.ToPageContentList(entities);
		}

		public Page GetPageById(int id)
		{
			PageEntity entity = Pages.FirstOrDefault(p => p.Id == id);
			return FromEntity.ToPage(entity);
		}

		public Page GetPageByTitle(string title)
		{
			PageEntity entity = Pages.FirstOrDefault(p => p.Title.ToLower() == title.ToLower());
			return FromEntity.ToPage(entity);
		}

		public PageContent GetLatestPageContent(int pageId)
		{
			PageContentEntity entity = PageContents.Where(x => x.Page.Id == pageId).OrderByDescending(x => x.EditedOn).FirstOrDefault();
			return FromEntity.ToPageContent(entity);
		}

		public PageContent GetPageContentById(Guid id)
		{
			PageContentEntity entity = PageContents.FirstOrDefault(p => p.Id == id);
			return FromEntity.ToPageContent(entity);
		}

		public PageContent GetPageContentByPageIdAndVersionNumber(int id, int versionNumber)
		{
			PageContentEntity entity = PageContents.FirstOrDefault(p => p.Page.Id == id && p.VersionNumber == versionNumber);
			return FromEntity.ToPageContent(entity);
		}

		public IEnumerable<PageContent> GetPageContentByEditedBy(string username)
		{
			List<PageContentEntity> entities = PageContents.Where(p => p.EditedBy == username).ToList();
			return FromEntity.ToPageContentList(entities);
		}

		public Page SaveOrUpdatePage(Page page)
		{
			PageEntity entity = UnitOfWork.FindById<PageEntity>(page.Id);
			if (entity == null)
			{
				entity = new PageEntity();
				ToEntity.FromPage(page, entity);
				UnitOfWork.Add(entity);
				UnitOfWork.SaveChanges();
				page = FromEntity.ToPage(entity);
			}
			else
			{
				ToEntity.FromPage(page, entity);
				UnitOfWork.SaveChanges();
				page = FromEntity.ToPage(entity);
			}

			return page;
		}

		/// <summary>
		/// This updates an existing set of text and is used for page rename updates.
		/// To add a new version of a page, use AddNewPageContentVersion
		/// </summary>
		/// <param name="content"></param>
		public void UpdatePageContent(PageContent content)
		{
			PageContentEntity entity = UnitOfWork.FindById<PageContentEntity>(content.Id);
			if (entity != null)
			{
				ToEntity.FromPageContent(content, entity);
				UnitOfWork.SaveChanges();
				content = FromEntity.ToPageContent(entity);
			}
		}

		#endregion

		#region IUserRepository
		public void DeleteUser(User user)
		{
			UserEntity entity = UnitOfWork.FindById<UserEntity>(user.Id);
			UnitOfWork.Remove(entity);
			UnitOfWork.SaveChanges();
		}

		public void DeleteAllUsers()
		{
			UnitOfWork.Remove(new Query(typeof(UserEntity)));
			UnitOfWork.SaveChanges();
		}

		public User GetAdminById(Guid id)
		{
			UserEntity entity = Users.FirstOrDefault(x => x.Id == id && x.IsAdmin);
			return FromEntity.ToUser(entity);
		}

		public User GetUserByActivationKey(string key)
		{
			UserEntity entity = Users.FirstOrDefault(x => x.ActivationKey == key && x.IsActivated == false);
			return FromEntity.ToUser(entity);
		}

		public User GetEditorById(Guid id)
		{
			UserEntity entity = Users.FirstOrDefault(x => x.Id == id && x.IsEditor);
			return FromEntity.ToUser(entity);
		}

		public User GetUserByEmail(string email, bool? isActivated = null)
		{
			UserEntity entity;

			if (isActivated.HasValue)
				entity = Users.FirstOrDefault(x => x.Email == email && x.IsActivated == isActivated);
			else
				entity = Users.FirstOrDefault(x => x.Email == email);

			return FromEntity.ToUser(entity);
		}

		public User GetUserById(Guid id, bool? isActivated = null)
		{
			UserEntity entity;

			if (isActivated.HasValue)
				entity = Users.FirstOrDefault(x => x.Id == id && x.IsActivated == isActivated);
			else
				entity = Users.FirstOrDefault(x => x.Id == id);

			return FromEntity.ToUser(entity);
		}

		public User GetUserByPasswordResetKey(string key)
		{
			UserEntity entity = Users.FirstOrDefault(x => x.PasswordResetKey == key);
			return FromEntity.ToUser(entity);
		}

		public User GetUserByUsername(string username)
		{
			UserEntity entity = Users.FirstOrDefault(x => x.Username == username);
			return FromEntity.ToUser(entity);
		}

		public User GetUserByUsernameOrEmail(string username, string email)
		{
			UserEntity entity = Users.FirstOrDefault(x => x.Username == username || x.Email == email);
			return FromEntity.ToUser(entity);
		}

		public IEnumerable<User> FindAllEditors()
		{
			List<UserEntity> entities = Users.Where(x => x.IsEditor).ToList();
			return FromEntity.ToUserList(entities);
		}

		public IEnumerable<User> FindAllAdmins()
		{
			List<UserEntity> entities = Users.Where(x => x.IsAdmin).ToList();
			return FromEntity.ToUserList(entities);
		}

		public User SaveOrUpdateUser(User user)
		{
			UserEntity entity = UnitOfWork.FindById<UserEntity>(user.Id);
			if (entity == null)
			{
				// Turn the domain object into a database entity
				entity = new UserEntity();
				ToEntity.FromUser(user, entity);
				UnitOfWork.Add(entity);
				UnitOfWork.SaveChanges();

				user = FromEntity.ToUser(entity);
			}
			else
			{
				ToEntity.FromUser(user, entity);
				UnitOfWork.SaveChanges();
			}

			return user;
		}

		#endregion

		#region IDisposable
		public void Dispose()
		{
			_unitOfWork.SaveChanges();
			_unitOfWork.Dispose();
		}
		#endregion
	}
}
