using System;
using System.Web;
using System.Collections;

using NUnit.Framework;
using Gentle.Framework;

using Cuyahoga.Core;
using Cuyahoga.Core.DAL;
using Cuyahoga.Modules;
using Cuyahoga.Modules.Articles;

namespace Cuyahoga.Test
{
	/// <summary>
	/// Summary description for ArticleTest.
	/// </summary>
	[TestFixture]
	public class ArticleTest
	{
		private Node _node;
		private Section _section;
		private User _admin;
		private User _author;
		private Guid _testGuid;

		[TestFixtureSetUp]
		public void Init()
		{
			// Setup a Cuyahoga context for the tests.	
			this._admin = new User();
			this._admin.UserName = "testadmin";
			this._admin.Password = "fhjkd838";
			this._admin.Email = "testadmin@cuyahoga.org";
			CmsDataFactory.GetInstance().InsertUser(this._admin);
			
			this._author = new User();
			this._author.UserName = "testauthor";
			this._author.Password = "fhj38nd8";
			this._author.Email = "testauthor@cuyahoga.org";
			CmsDataFactory.GetInstance().InsertUser(this._author);
			
			this._node = new Node();
			this._node.Title = "testnode";
			this._node.ShortDescription = "testnode";
			this._node.Position = 9999;
			CmsDataFactory.GetInstance().InsertNode(this._node);

			this._section = new Section();
			this._section.Module = new ArticleModule();
			this._section.Module.ModuleId = 2; // HACK, Module.Id hardcoded
			this._section.Node = this._node;
			this._section.Title = "testsection";
			this._section.Position = 9999;
			CmsDataFactory.GetInstance().InsertSection(this._section);

			// Guid to be used in the test to identify objects generated by this test.
			this._testGuid = Guid.NewGuid();
		}

		[TestFixtureTearDown]
		public void Clear()
		{
			// Delete all records that have not been removed already by the test.

			// Delete the Cuyahoga context.
			// We're assuming that all tests went well and no Articles are related.
			CmsDataFactory.GetInstance().DeleteSection(this._section);
			CmsDataFactory.GetInstance().DeleteNode(this._node);
			CmsDataFactory.GetInstance().DeleteUser(this._author);
			CmsDataFactory.GetInstance().DeleteUser(this._admin);
		}

		[Test]
		public void TestCRUD()
		{
			// New Article without category
			Article article1 = CreateArticle("Test article");
			Assert.IsTrue(article1.Id == 0, "Invalid id for newly created Article" );
			Broker.Persist(article1);
			Assert.IsTrue(article1.Id != 0, "No id generated for the Article just inserted!" );

			// Retieve the an Article
			Key key = new Key(typeof(Article), true, "_id", article1.Id);
			Article article2 = Broker.RetrieveInstance(typeof(Article), key) as Article;
			Assert.AreEqual(article1.Id, article2.Id);

			// Update an Article
			article2.Title = "Changed";
			Broker.Persist(article2);
			// Verify update
			key = new Key(typeof(Article), true, "_id", article2.Id);
			article1 = Broker.RetrieveInstance(typeof(Article), key) as Article;
			Assert.AreEqual(article1.Title, article2.Title);			
			
			// Delete the Article
			Broker.Remove(article1);
			// Verify delete
			key = new Key(typeof(Article), true, "_id", article2.Id);
			IList articleList = Broker.RetrieveList(typeof(Article), key);
			Assert.IsTrue(articleList.Count == 0);
		}

		public void TestRelations()
		{
			Article article1 = CreateArticle("article 1");
			article1.Persist();
			Comment comment1 = new Comment(article1.Id, this._admin.Id, "Testcomment 1");
			Comment comment2 = new Comment(article1.Id, this._author.Id, "Reply to Testcomment 1");
			Category cat = new Category("Test", "", true);
			try 
			{
				// Comments in article
				article1.Comments.Add(comment1);
				Assert.AreEqual(article1.Comments.Count, 1);
				article1.Comments.Add(comment2);
				Assert.AreEqual(article1.Comments.Count, 2);
				// Article propery of comment
				Assert.AreEqual(comment1.Article.Id, article1.Id);
				// Category
				article1.Category = cat;
				Assert.IsTrue(article1.Category.Id > 0, "Category not persisted during assignment");
				article1.Persist();
				Article article2 = new Article(article1.Id);
				Assert.AreEqual(article1.Category.Title, article2.Category.Title);
				article2.Category = new Category("Test", "", true);
				Assert.AreEqual(article1.Category.Id, article2.Category.Id, "Created a new category with an already existing title");
			}
			finally
			{
				article1.Comments.Remove(comment1);
				article1.Comments.Remove(comment2);		
				Broker.Remove(article1);
				Broker.Remove(cat);
			}
		}

		private Article CreateArticle(string title)
		{
			Article article = new Article();
			article.Title = title;
			article.Content = "testcontent";
			article.Syndicate = false;
			article.DateOnline = DateTime.Now.AddDays(1);
			article.DateOffline = DateTime.Now.AddDays(50);
			article.CreatedBy = this._author;
			article.Section = this._section;

			return article;
		}
	}
}
