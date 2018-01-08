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

        [HttpGet]
        public IActionResult Comment()
        {
            return PartialView("_CommentPartial");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Comment([Bind("Id,Content,ArticleId")] Comment item)
        {
            if (ModelState.IsValid)
            {
                context.AddCommentToArticle(item);

                return RedirectToAction(nameof(Index));
            }

            return PartialView("_CommentPartial");
        }

        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
