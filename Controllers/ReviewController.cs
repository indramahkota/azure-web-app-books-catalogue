using BooksCatalogue.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

// For more information on enabling MVC for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace BooksCatalogue.Controllers
{
    public class ReviewController : Controller
    {
        private readonly string apiEndpoint = "https://indramahkota-api.azurewebsites.net/api/";
        private readonly HttpClient _client;


        public ReviewController()
        {
            _client = new HttpClient();
        }

        // GET: Review/AddReview/2
        public async Task<IActionResult> AddReview(int? bookId)
        {
            if (bookId == null)
            {
                return NotFound();
            }

            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, apiEndpoint + "books/" + bookId);
            HttpResponseMessage response = await _client.SendAsync(request);

            switch (response.StatusCode)
            {
                case HttpStatusCode.OK:
                    string responseString = await response.Content.ReadAsStringAsync();
                    _ = JsonSerializer.Deserialize<Book>(responseString);

                    ViewData["BookId"] = bookId;
                    return View("Add");
                case HttpStatusCode.NotFound:
                    return NotFound();
                default:
                    return ErrorAction("Error. Status code = " + response.StatusCode + ": " + response.ReasonPhrase);
            }
        }

        // TODO: Tambahkan fungsi ini untuk mengirimkan atau POST data review menuju API
        // POST: Review/AddReview
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddReview([Bind("Id,BookId,ReviewerName,Rating,Comment")] Review tReview)
        {
            MultipartFormDataContent content = new MultipartFormDataContent
            {
                { new StringContent(tReview.BookId.ToString()), "bookId" },
                { new StringContent(tReview.ReviewerName), "reviewerName" },
                { new StringContent(tReview.Rating.ToString()), "rating" },
                { new StringContent(tReview.Comment), "comment" }
            };

            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, apiEndpoint + "reviews")
            {
                Content = content
            };
            HttpResponseMessage response = await _client.SendAsync(request);

            switch (response.StatusCode)
            {
                case HttpStatusCode.OK:
                case HttpStatusCode.NoContent:
                case HttpStatusCode.Created:
                    return RedirectToAction("Details", new RouteValueDictionary(
                            new { controller = "Books", action = "Details", Id = tReview.BookId }));
                default:
                    return ErrorAction("Error. Status code = " + response.StatusCode);
            }
        }

        private ActionResult ErrorAction(string message)
        {
            return new RedirectResult("/Home/Error?message=" + message);
        }
    }
}
