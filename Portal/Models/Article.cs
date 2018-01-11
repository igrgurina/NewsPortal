using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;

using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
//using MongoDB.Driver.Builders;

namespace Portal.Models
{
    public class Article
    {
        public ObjectId Id { get; set; }

        //[BsonElement("ArticleId")]
        //public int ArticleId { get; set; }

        [BsonElement("Title")]
        public string Title { get; set; }

        [BsonElement("Content")]
        public string Content { get; set; }

        [BsonElement("Author")]
        public string Author { get; set; }

        [BsonElement("Image")]
        public string Picture { get; set; } // base 64

        // date created
        [BsonElement("_date")]
        //[BsonDateTimeOptions(DateOnly = true)]
        public DateTime DateCreated { get; set; }

        [BsonElement("Comments")]
        public ICollection<Comment> Comments { get; set; }
    }

    public class Comment
    {
        public ObjectId Id { get; set; }

        [BsonElement("Content")]
        public string Content { get; set; }

        [BsonElement("ArticleId")]
        public ObjectId ArticleId { get; set; }

        [BsonElement("_date")]
        public DateTime DateCreated { get; set; }
    }

    public class CommentViewModel
    {
        public string Comment { get; set; }
        public string ArticleId { get; set; }

        public CommentViewModel()
        { }
    }

    public class ArticleViewModel
    {
        public string Title { get; set; }
        public string Content { get; set; }
        public string Author { get; set; }
        public IFormFile Picture { get; set; }
    }

    public class DataAccess
    {
        private static string ARTICLE_COLLECTION_NAME = "Articles";
        IMongoCollection<Article> collection => _db.GetCollection<Article>(ARTICLE_COLLECTION_NAME);

        MongoClient _client;
        //MongoServer _server;
        IMongoDatabase _db;

        public DataAccess()
        {
            _client = new MongoClient("mongodb://localhost:27017");
            //_server = _client.GetServer();
            _db = _client.GetDatabase("PortalDB");
        }

        public IEnumerable<Article> GetArticles()
        {
            //return _db.GetCollection<Product>("Products").FindAll();
            //var collection = _db.GetCollection<Article>(ARTICLE_COLLECTION_NAME);
            var documents = collection.Find(FilterDefinition<Article>.Empty).ToList();
            return documents;
        }

        public IQueryable<Article> GetNLatestArticles(int n = 10)
        {
            var query = collection.AsQueryable()
                .OrderByDescending(a => a.DateCreated)
                .Take(n);

            return query;
        }

        public Article GetArticle(ObjectId id)
        {
            var filter = Builders<Article>.Filter.Eq(p => p.Id, id);
            //var res = Query<Product>.EQ(p => p.Id, id);
            //var collection = _db.GetCollection<Article>(ARTICLE_COLLECTION_NAME);
            var document = collection.Find(filter).First();

            return document;
            //return _db.GetCollection<Article>(ARTICLE_COLLECTION_NAME).FindOne(res);
        }

        public Article CreateArticle(Article p)
        {
            //var document = new BsonDocument
            //{
            //    { "title", "News" },
            //    { "content", "News" },
            //    { "author", "Ivan Grgurina" }
            //};

            // InsertOne
            //IMongoCollection<Article> collection = _db.GetCollection<Article>(ARTICLE_COLLECTION_NAME);
            collection.InsertOne(p);

            //_db.GetCollection<Article>("Articles").Save(p);
            return p;
        }

        public Comment AddCommentToArticle(Comment comment)
        {
            var update = Builders<Article>.Update.Push(article => article.Comments, comment);
            collection.FindOneAndUpdate(article => article.Id == comment.ArticleId, update);

            return comment;
        }

        public long Count => collection.Count(article => article != null);

        public void UpdateArticle(ObjectId id, Article p)
        {
            p.Id = id;
            //var filter = Builders<Article>.Filter.Eq(pd => pd.Id, id);
            //var update = Builders<Article>.Update.
            //_db.GetCollection<Article>(ARTICLE_COLLECTION_NAME).Update(filter, operation);
            collection.ReplaceOne(b => b.Id == id, p);
        }
        public void RemoveArticle(ObjectId id)
        {
            collection.DeleteOne(b => b.Id == id);
            //var res = Query<Article>.EQ(e => e.Id, id);
            //var operation = _db.GetCollection<Article>(ARTICLE_COLLECTION_NAME).Remove(res);
        }
    }
}


public static class Extensions
{
    public static string TimeAgo(this DateTime dt)
    {
        TimeSpan span = DateTime.Now - dt;
        if (span.Days > 365)
        {
            int years = (span.Days / 365);
            if (span.Days % 365 != 0)
                years += 1;
            return String.Format("about {0} {1} ago",
                years, years == 1 ? "year" : "years");
        }
        if (span.Days > 30)
        {
            int months = (span.Days / 30);
            if (span.Days % 31 != 0)
                months += 1;
            return String.Format("about {0} {1} ago",
                months, months == 1 ? "month" : "months");
        }
        if (span.Days > 1)
            return String.Format("about {0} days ago",
                span.Days);
        
        if (span.Days == 1)
        {
            return String.Format("yesterday");//,

        }
        if (span.Hours > 3)
        {
            return String.Format("today");//,

        }    //span.Hours, span.Hours == 1 ? "hour" : "hours");
        if (span.Minutes > 0)
            return String.Format("about {0} {1} ago",
                span.Minutes, span.Minutes == 1 ? "minute" : "minutes");
        if (span.Seconds > 5)
            return String.Format("about {0} seconds ago", span.Seconds);
        if (span.Seconds <= 5)
            return "just now";
        return string.Empty;
    }
}
