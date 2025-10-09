using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MovieApp.Interfaces;
using MovieApp.Models;
using Newtonsoft.Json;
using System.Net.Http.Headers;

namespace MovieApp.Controllers
{
    [Produces("application/json")]
    [Route("api/[controller]")]
    [ApiController]
    public class MovieController : ControllerBase
    {
        private readonly IWebHostEnvironment _hostingEnvironment;
        private readonly IMovie _movieService;
        private readonly IConfiguration _config;
        private readonly string _posterFolderPath;

        public MovieController(IConfiguration config, IWebHostEnvironment hostingEnvironment, IMovie movieService)
        {
            _config = config;
            _movieService = movieService;
            _hostingEnvironment = hostingEnvironment;
            _posterFolderPath = Path.Combine(_hostingEnvironment.WebRootPath, "Poster");
        }

        #region GET Endpoints

        [HttpGet]
        [ProducesResponseType(typeof(List<Movie>), 200)]
        public async Task<ActionResult<List<Movie>>> Get(CancellationToken cancellationToken)
        {
            var movies = await _movieService.GetAllMovies();
            return Ok(movies);
        }

        [HttpGet("GetGenreList")]
        [ProducesResponseType(typeof(IEnumerable<Genre>), 200)]
        public async Task<ActionResult<IEnumerable<Genre>>> GenreList(CancellationToken cancellationToken)
        {
            var genres = await _movieService.GetGenre();
            return Ok(genres);
        }

        [HttpGet("GetSimilarMovies/{movieId}")]
        [ProducesResponseType(typeof(List<Movie>), 200)]
        public async Task<ActionResult<List<Movie>>> SimilarMovies(int movieId, CancellationToken cancellationToken)
        {
            var similarMovies = await _movieService.GetSimilarMovies(movieId);
            return Ok(similarMovies);
        }

        #endregion

        #region POST Endpoint

        [HttpPost, DisableRequestSizeLimit]
        [Authorize(Policy = UserRoles.Admin)]
        [ProducesResponseType(typeof(Movie), 200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> Post(CancellationToken cancellationToken)
        {
            var movieFormData = Request.Form["movieFormData"].ToString();
            if (string.IsNullOrWhiteSpace(movieFormData))
                return BadRequest("Movie data is required.");

            var movie = JsonConvert.DeserializeObject<Movie>(movieFormData);
            if (movie == null)
                return BadRequest("Invalid movie data.");

            movie.PosterPath = await HandleFileUploadAsync(Request.Form.Files);

            await _movieService.AddMovie(movie);
            return Ok(movie);
        }

        #endregion

        #region PUT Endpoint

        [HttpPut]
        [Authorize(Policy = UserRoles.Admin)]
        [ProducesResponseType(typeof(Movie), 200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> Put(CancellationToken cancellationToken)
        {
            var movieFormData = Request.Form["movieFormData"].ToString();
            if (string.IsNullOrWhiteSpace(movieFormData))
                return BadRequest("Movie data is required.");

            var movie = JsonConvert.DeserializeObject<Movie>(movieFormData);
            if (movie == null)
                return BadRequest("Invalid movie data.");

            var uploadedFile = await HandleFileUploadAsync(Request.Form.Files);
            if (!string.IsNullOrEmpty(uploadedFile))
                movie.PosterPath = uploadedFile;

            await _movieService.UpdateMovie(movie);
            return Ok(movie);
        }

        #endregion

        #region DELETE Endpoint

        [HttpDelete("{movieId}")]
        [Authorize(Policy = UserRoles.Admin)]
        [ProducesResponseType(200)]
        public async Task<IActionResult> Delete(int movieId, CancellationToken cancellationToken)
        {
            var coverFileName = await _movieService.DeleteMovie(movieId);

            if (!string.IsNullOrEmpty(coverFileName) && coverFileName != _config["DefaultPoster"])
            {
                var fullPath = Path.Combine(_posterFolderPath, coverFileName);
                if (System.IO.File.Exists(fullPath))
                {
                    try
                    {
                        System.IO.File.Delete(fullPath);
                    }
                    catch (Exception ex)
                    {
                        // Optional: log the exception
                    }
                }
            }

            return Ok(new { Message = "Movie deleted successfully." });
        }

        #endregion

        #region Private Helpers

        private async Task<string> HandleFileUploadAsync(IFormFileCollection files)
        {
            if (files.Count == 0) return _config["DefaultPoster"];

            var file = files[0];
            if (file.Length == 0) return _config["DefaultPoster"];

            string extension = Path.GetExtension(file.FileName);
            string fileName = $"{Guid.NewGuid()}{extension}";
            string fullPath = Path.Combine(_posterFolderPath, fileName);

            // Ensure folder exists
            if (!Directory.Exists(_posterFolderPath))
                Directory.CreateDirectory(_posterFolderPath);

            using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return fileName;
        }

        #endregion
    }
}
