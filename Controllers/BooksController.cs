﻿using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using BooksCatalogue.Models;
using Microsoft.AspNetCore.Http;
using System.Net.Http;
using System.Net;
using System.Text.Json;
using System;
using Microsoft.AspNetCore.Routing;

// For more information on enabling MVC for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace BooksCatalogue.Controllers
{
    public class BooksController : Controller
    {
        private readonly string bookEndpoint = "https://indramahkota-api.azurewebsites.net/api/books/";
        private readonly string reviewEndpoint = "https://indramahkota-api.azurewebsites.net/api/reviews/";

        private readonly HttpClient _client;

        public BooksController()
        {
            // Use this client handler to bypass ssl policy errors
            // clientHandler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => { return true; };
            _client = new HttpClient();
        }

        // GET: Books
        public async Task<IActionResult> Index()
        {
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, bookEndpoint);
            HttpResponseMessage response = await _client.SendAsync(request);

            switch (response.StatusCode)
            {
                case HttpStatusCode.OK:
                    string responseString = await response.Content.ReadAsStringAsync();
                    List<Book> books = JsonSerializer.Deserialize<Book[]>(responseString).ToList();
                    return View(books);
                default:
                    return ErrorAction("Error. Status code = " + response.StatusCode + ": " + response.ReasonPhrase);
            }
        }

        // GET: Books/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            HttpRequestMessage bookRequest = new HttpRequestMessage(HttpMethod.Get, bookEndpoint + id);
            HttpResponseMessage bookResponse = await _client.SendAsync(bookRequest);

            HttpRequestMessage reviewRequest = new HttpRequestMessage(HttpMethod.Get, reviewEndpoint + id);
            HttpResponseMessage reviewResponse = await _client.SendAsync(reviewRequest);

            if(bookResponse.StatusCode == HttpStatusCode.OK &&
                reviewResponse.StatusCode == HttpStatusCode.OK)
            {
                string bookString = await bookResponse.Content.ReadAsStringAsync();
                var book = JsonSerializer.Deserialize<Book>(bookString);

                string reviewString = await reviewResponse.Content.ReadAsStringAsync();
                var reviews = JsonSerializer.Deserialize<ICollection<Review>>(reviewString);

                book.Reviews = reviews;

                return View(book);
            }
            else
            {
                if(bookResponse.StatusCode != HttpStatusCode.OK)
                {
                    return ErrorAction("Error. Status code = " + bookResponse.StatusCode + "; " + bookResponse.ReasonPhrase);
                } else
                {
                    return ErrorAction("Error. Status code = " + reviewResponse.StatusCode + "; " + reviewResponse.ReasonPhrase);
                }
            }
        }

        // GET: Books/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Books/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Title,Author,Synopsis,ReleaseYear,CoverURL")][FromForm] Book book, ICollection<IFormFile> CoverURL)
        {
            var image = CoverURL.First();

            if (IsImage(image))
            {
                MultipartFormDataContent content = new MultipartFormDataContent
                {
                    { new StringContent(book.Title), "title" },
                    { new StringContent(book.Author), "author" },
                    { new StringContent(book.Synopsis), "synopsis" },
                    { new StringContent(book.ReleaseYear.ToString()), "releaseYear" },
                    { new StreamContent(image.OpenReadStream(), (int)image.Length), "coverURL", image.FileName }
                };

                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, bookEndpoint)
                {
                    Content = content
                };
                HttpResponseMessage response = await _client.SendAsync(request);

                switch (response.StatusCode)
                {
                    case HttpStatusCode.OK:
                    case HttpStatusCode.NoContent:
                    case HttpStatusCode.Created:
                        return RedirectToAction(nameof(Index));
                    default:
                        return ErrorAction("Error. Status code = " + response.StatusCode + "; " + response.ReasonPhrase);
                }
            }
            else
            {
                return ErrorAction("Error. Status code = " + (new UnsupportedMediaTypeResult().StatusCode) + "; File is not an image.");
            }
        }

        // GET: Books/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, bookEndpoint + id);
            HttpResponseMessage response = await _client.SendAsync(request);

            switch (response.StatusCode)
            {
                case HttpStatusCode.OK:
                    string responseString = await response.Content.ReadAsStringAsync();
                    var book = JsonSerializer.Deserialize<Book>(responseString);
                    return View(book);
                default:
                    return ErrorAction("Error. Status code = " + response.StatusCode + ": " + response.ReasonPhrase);
            }
        }

        // PUT: Books/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Title,Author,Synopsis,ReleaseYear,CoverURL")][FromForm] Book book)
        {
            if (id != book.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                var httpContent = new[] {
                    new KeyValuePair<string, string>("id", book.Id.ToString()),
                    new KeyValuePair<string, string>("title", book.Title),
                    new KeyValuePair<string, string>("author", book.Author),
                    new KeyValuePair<string, string>("synopsis", book.Synopsis),
                    new KeyValuePair<string, string>("releaseYear", book.ReleaseYear.ToString()),
                    new KeyValuePair<string, string>("coverURL", book.CoverURL)
                };

                HttpContent content = new FormUrlEncodedContent(httpContent);
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Put, bookEndpoint + id)
                {
                    Content = content
                };
                HttpResponseMessage response = await _client.SendAsync(request);

                switch (response.StatusCode)
                {
                    case HttpStatusCode.OK:
                    case HttpStatusCode.NoContent:
                    case HttpStatusCode.Created:
                        //return RedirectToAction(nameof(Details));
                        return RedirectToAction("Details", new RouteValueDictionary(
                            new { controller = "Books", action = "Details", Id = id }));
                    default:
                        return ErrorAction("Error. Status code = " + response.StatusCode);
                }
            }
            return View(book);
        }

        // GET: Books/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, bookEndpoint + id);
            HttpResponseMessage response = await _client.SendAsync(request);

            switch (response.StatusCode)
            {
                case HttpStatusCode.OK:
                    string responseString = await response.Content.ReadAsStringAsync();
                    var book = JsonSerializer.Deserialize<Book>(responseString);
                    return View(book);
                default:
                    return ErrorAction("Error. Status code = " + response.StatusCode + ": " + response.ReasonPhrase);
            }
        }

        // POST: Books/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Delete, bookEndpoint + id);
            HttpResponseMessage response = await _client.SendAsync(request);

            switch (response.StatusCode)
            {
                case HttpStatusCode.OK:
                case HttpStatusCode.NoContent:
                    return RedirectToAction(nameof(Index));
                case HttpStatusCode.Unauthorized:
                    return ErrorAction("Please sign in again. " + response.ReasonPhrase);
                default:
                    return ErrorAction("Error. Status code = " + response.StatusCode);
            }
        }

        private bool IsImage(IFormFile file)
        {
            if (file.ContentType.Contains("image"))
            {
                return true;
            }

            string[] formats = new string[] { ".jpg", ".png", ".gif", ".jpeg" };

            return formats.Any(item => file.FileName.EndsWith(item, StringComparison.OrdinalIgnoreCase));
        }

        private ActionResult ErrorAction(string message)
        {
            return new RedirectResult("/Home/Error?message=" + message);
        }
    }
}
