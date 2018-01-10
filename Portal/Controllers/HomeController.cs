using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Portal.Models;
using MongoDB.Bson;

namespace Portal.Controllers
{
    public class HomeController : Controller
    {
        DataAccess context;

        public HomeController()
        {
            context = new DataAccess();
        }

        public IActionResult Index()
        {
            return View(context.GetNLatestArticles().ToList());
        }

        //[HttpGet]
        //public IActionResult Comment()
        //{
        //    return PartialView("_CommentPartial");
        //}

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Comment([FromForm] CommentViewModel item)
        {
            var comment = new Comment()
            {
                ArticleId = ObjectId.Parse(item.ArticleId),
                Content = item.Comment
            };

            //item.ArticleId = ObjectId.Parse(item.ArticleId);
            if (ModelState.IsValid)
            {
                context.AddCommentToArticle(comment);
            }

            return RedirectToAction(nameof(Index));
        }

        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
