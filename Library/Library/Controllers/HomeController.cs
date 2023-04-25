﻿using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Library.Models;

namespace Library.Controllers
{
    public class HomeController : Controller
    {
        private LibraryContext context;
        public HomeController(LibraryContext ctx) => context = ctx;

        public IActionResult Index(string id)
        {
            // load current filters and data needed for filter drop downs in ViewBag
            var filters = new Filters(id);
            ViewBag.Filters = filters;
            ViewBag.Genres = context.Genres.ToList();
            ViewBag.Statuses = context.Statuses.ToList();
            ViewBag.DueFilters = Filters.DueFilterValues;

            IQueryable<Book> query = context.Books
                .Include(t => t.Genre).Include(t => t.Status);
            if (filters.HasGenre) {
                query = query.Where(t => t.GenreId == filters.GenreId);
            }
            if (filters.HasStatus) {
                query = query.Where(t => t.StatusId == filters.StatusId);
            }
            if (filters.HasDue) {
                var today = DateTime.Today;
                if (filters.IsPast)
                    query = query.Where(t => t.DueDate < today);
                else if (filters.IsFuture)
                    query = query.Where(t => t.DueDate > today);
                else if (filters.IsToday)
                    query = query.Where(t => t.DueDate == today);
            }
            var tasks = query.OrderBy(t => t.DueDate).ToList();
            return View(tasks);
        }

        public IActionResult Add()
        {
            ViewBag.Genres = context.Genres.ToList();
            ViewBag.Statuses = context.Statuses.ToList();
            return View();
        }

        [HttpPost]
        public IActionResult Add(Book book)
        {
            if (ModelState.IsValid)
            {
                var isbnLargest = context.Books.Max(x => x.ISBN);
				book.ISBN = isbnLargest + 15;				
				context.Books.Add(book);                
                context.SaveChanges();
                return RedirectToAction("Index");
            }
            else
            {
                ViewBag.Genres = context.Genres.ToList();
                ViewBag.Statuses = context.Statuses.ToList();
                return View(book);
            }
        }

        [HttpPost]
        public IActionResult Filter(string[] filter)
        {
            string id = string.Join('-', filter);
            return RedirectToAction("Index", new { ID = id });
        }

        [HttpPost]
        public IActionResult Edit([FromRoute]string id, Book selected)
        {
            if (selected.StatusId == null) {
                context.Books.Remove(selected);
            }
            else {
                string newStatusId = selected.StatusId;
                selected = context.Books.Find(selected.Id);
                selected.StatusId = newStatusId;
                context.Books.Update(selected);
            }
            context.SaveChanges();

            return RedirectToAction("Index", new { ID = id });
        }
    }
}