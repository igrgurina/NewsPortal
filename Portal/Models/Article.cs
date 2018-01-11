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

        [BsonElement("ArticleId")]
        public int ArticleId { get; set; }

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

        [BsonIgnore]
        public ObjectId ArticleId { get; set; }
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



        #region MAP REDUCE
        public void MapReduce()
        {
            var collection = _db.GetCollection<Article>("articles");

            var options = new MapReduceOptions<Article, int>
            {
                Finalize = finalize,
                OutputOptions = MapReduceOutputOptions.Inline
            };

            var results = collection.MapReduce(map, reduce, options);

            foreach (var result in results.ToEnumerable())
            {
                Console.WriteLine(result.ToJson());
            }
        }

        private string map = @"
            function() {
                var movie = this;
                emit(movie.Category, { count: 1, totalMinutes: movie.Minutes }); 
            }";

        // emit: key = how we want to group the values (by category)
        //      value = object containing the count of movies (always 1) 
        //              and total length of each individual movie

        // mongo groups the items you emit and pass them as an array to the reduce function you provide
        private string reduce = @"
            function(key, values) {
                var result = { count: 0, totalMinutes: 0};

                values.forEach(function(value) {
                    result.count += value.count;
                    result.totalMinutes += value.totalMinutes;
                });

                return result;
            }";

        // reduce function returns single result
        // return value must have the same shape as the emitted values!

        // finalize is optional, but you can perform some final calculation
        // based on fully reduced set of data --- calculate the average length of all movies in a category
        private string finalize = @"
            function(key, value) {
                value.average = value.totalMinutes / value.count;
                return value;
            }";
        #endregion
    }




    public interface IPortalContext
    {
        DbSet<Article> Articles { get; set; }
        DbSet<Comment> Comments { get; set; }
    }

    public class PortalContext : DbContext, IPortalContext
    {
        public PortalContext(DbContextOptions options) : base(options)
        {

        }

        public DbSet<Article> Articles { get; set; }
        public DbSet<Comment> Comments { get; set; }
    }

    public class PortalContextFactory : IDesignTimeDbContextFactory<PortalContext>
    {
        public PortalContext CreateDbContext(string[] args)
        {
            var environmentName = Environment.GetEnvironmentVariable("Hosting:Environment");
            var basePath = AppContext.BaseDirectory;
            return Create(basePath, environmentName);
        }

        private PortalContext Create(string basePath, string environmentName)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(basePath)
                .AddJsonFile("appsetings.json")
                .AddJsonFile($"appsetings.{environmentName}.json", true)
                .AddEnvironmentVariables();

            var config = builder.Build();
            var connstr = config.GetConnectionString("DefaultConnection");

            if (string.IsNullOrWhiteSpace(connstr) == true)
            {
                throw new InvalidOperationException("Could not find a connection string named '(DefaultConnection').");
            }
            else
            {
                return Create(connstr);
            }
        }

        private PortalContext Create(string connectionString)
        {
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new ArgumentException($"{nameof(connectionString)} is null or empty.",
                    nameof(connectionString));
            }

            var optionsBuilder = new DbContextOptionsBuilder();
            optionsBuilder.UseSqlServer(connectionString);
            return new PortalContext(optionsBuilder.Options);
        }

        public PortalContext Create(DbContextFactoryOptions options)
        {
            return Create(Directory.GetCurrentDirectory(), Environment.GetEnvironmentVariable("Hosting:Environment"));
            //return Create(options.ContentRootPath, options.EnvironmentName);
        }
    }

    public class PortalMR
    {
        private string map = @"
            function() {
                var movie = this;
                emit(movie.Category, { count: 1, totalMinutes: movie.Minutes }); 
            }";

        // emit: key = how we want to group the values (by category)
        //      value = object containing the count of movies (always 1) 
        //              and total length of each individual movie

        // mongo groups the items you emit and pass them as an array to the reduce function you provide
        private string reduce = @"
            function(key, values) {
                var result = { count: 0, totalMinutes: 0};

                values.forEach(function(value) {
                    result.count += value.count;
                    result.totalMinutes += value.totalMinutes;
                });

                return result;
            }";

        // reduce function returns single result
        // return value must have the same shape as the emitted values!

        // finalize is optional, but you can perform some final calculation
        // based on fully reduced set of data --- calculate the average length of all movies in a category
        private string finalize = @"
            function(key, value) {
                value.average = value.totalMinutes / value.count;
                return value;
            }";

        //IMongoDatabase _db;

        //public void MapReduce()
        //{
        //    var collection = _db.GetCollection<Article>("articles");

        //    var options = new MapReduceOptions<Article, int>
        //    {
        //        Finalize = finalize,
        //        OutputOptions = MapReduceOutputOptions.Inline
        //    };

        //    var results = collection.MapReduce(map, reduce, options);

        //    foreach (var result in results.ToEnumerable())
        //    {
        //        Console.WriteLine(result.ToJson());
        //    }
        //}
    }

}
