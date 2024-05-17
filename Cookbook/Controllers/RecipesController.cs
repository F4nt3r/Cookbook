using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Cookbook.Models;
using Cookbook.Data;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using System.Globalization;

namespace Cookbook.Controllers
{
    public class RecipesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public RecipesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Recipes
        public async Task<IActionResult> Index(string searchString, string sortBy, string sortOrder)
        {
            var recipes = _context.Recipe
              .Include(r => r.Ratings)
              .AsQueryable();

           

            if (!String.IsNullOrEmpty(searchString))
            {
                recipes = recipes.Where(r => r.Title.Contains(searchString));
            }
            switch (sortBy)
            {
                case "Title":
                    recipes = sortOrder == "asc" ? recipes.OrderBy(r => r.Title) : recipes.OrderByDescending(r => r.Title);
                    break;
                case "Ratings":
                    recipes = sortOrder == "asc" ? recipes.OrderBy(r => r.Ratings.Average(r => r.Score)) : recipes.OrderByDescending(r => r.Ratings.Average(r => r.Score));
                    break;
                default:
                    recipes = recipes.OrderBy(r => r.Title);
                    break;
            }
            var myRecipes = await recipes.ToListAsync();

            return _context.Recipe != null ?
                        View(myRecipes) :
                        Problem("Entity set 'ApplicationDbContext.Recipe' is null.");
        }
        // GET: Recipes/MyRecipes
        [HttpPost]
        public async Task<IActionResult> AddToFavorites(int recipeId)
        {
            var username = User.FindFirstValue(ClaimTypes.Name);
            var favorite = new Favorite { RecipeId = recipeId, UserName = username };
            _context.Favorites.Add(favorite);
            await _context.SaveChangesAsync();
            return RedirectToAction("Details", new { id = recipeId });
        }
        [HttpPost]
        public async Task<IActionResult> RemoveFromFavorites(int recipeId)
        {
            var username = User.FindFirstValue(ClaimTypes.Name);
            var favorite = await _context.Favorites
                .FirstOrDefaultAsync(f => f.RecipeId == recipeId && f.UserName == username);
            if (favorite != null)
            {
                _context.Favorites.Remove(favorite);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Details", new { id = recipeId });
        }

        // Action to view user's favorites
        public async Task<IActionResult> Favorites()
        {
            var username = User.FindFirstValue(ClaimTypes.Name);
            var favorites = await _context.Favorites
                .Where(f => f.UserName == username)
                .Include(f => f.Recipe)
                .ToListAsync();
            return View(favorites);
        }

        [Authorize]
        public async Task<IActionResult> MyRecipes()
        {
            if (User.Identity.IsAuthenticated)
            {
                var userName = User.FindFirstValue(ClaimTypes.Name);
                if (string.IsNullOrEmpty(userName))
                {
                    return Problem("User name is null or empty");
                }

                var myRecipes = await _context.Recipe
                    .Where(r => r.CreatedBy == userName)
                    .ToListAsync();

                return View(myRecipes);
            }
            else
            {
                return Forbid(); // lub RedirectToAction("Login", "Account");
            }
        }

        // GET: Recipes/Details/5
        public async Task<IActionResult> Details(int? id, string returnUrl = null)
        {
            if (id == null || _context.Recipe == null)
            {
                return NotFound();
            }

              var recipe = await _context.Recipe
            .Include(r => r.Ingredients)
            .Include(r => r.Comments)
            .Include(r => r.Ratings)
            .FirstOrDefaultAsync(m => m.Id == id);
            if (recipe == null)
            {
                return NotFound();
            }
            if (string.IsNullOrEmpty(returnUrl))
            {
                returnUrl = Request.Headers["Referer"].ToString();
            }

            ViewBag.ReturnUrl = returnUrl;

            var username = User.FindFirstValue(ClaimTypes.Name);
            var isFavorite = await _context.Favorites
                .AnyAsync(f => f.RecipeId == id && f.UserName == username);

            var viewModel = new RecipeDetails
            {
                Recipe = recipe,
                NewComment = new Comment(),
                IsFavorite = isFavorite
            };
            if (viewModel.Recipe.Ratings == null)
            {
                viewModel.Recipe.Ratings = new List<Rating>();
            }

            return View(viewModel);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddComment(int id, [Bind("Content")] Comment newComment)
        {
            var recipe = await _context.Recipe.Include(r => r.Comments).FirstOrDefaultAsync(r => r.Id == id);
            if (recipe == null)
            {
                return NotFound();
            }
            if(newComment.Content != null) { 
           
                if (User.Identity.IsAuthenticated)
                {
                    var userName = User.FindFirstValue(ClaimTypes.Name);

                    if (!string.IsNullOrEmpty(userName))
                    {
                        newComment.UserName = userName;
                        newComment.UserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                    }
                }
            else
            {
                string anonym = "Anonim" + Guid.NewGuid().ToString().Substring(0, 4);
                newComment.UserName = anonym;
                newComment.UserId = Guid.NewGuid().ToString(); // Ustaw losowy identyfikator dla anonimowego użytkownika
            }


            newComment.CreatedAt = DateTime.Now; 
                recipe.Comments.Add(newComment);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Details), new { id = id });
            }
             return RedirectToAction(nameof(Details), new { id = id });

        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteComment(int recipeId, int commentId)
        {
            var recipe = await _context.Recipe.Include(r => r.Comments).FirstOrDefaultAsync(r => r.Id == recipeId);
            if (recipe == null)
            {
                return NotFound();
            }

            var comment = recipe.Comments.FirstOrDefault(c => c.Id == commentId);
            if (comment == null)
            {
                return NotFound();
            }

            var userId = User.FindFirstValue(ClaimTypes.Name);
            if (recipe.CreatedBy != userId)
            {
                return Forbid(); // Użytkownik nie jest autorem przepisu
            }

            recipe.Comments.Remove(comment);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Details), new { id = recipeId });
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddRating(int id, int score)
        {
            if (score < 1 || score > 5)
            {
                return BadRequest("Invalid rating value.");
            }

            var recipe = await _context.Recipe.Include(r => r.Ratings).FirstOrDefaultAsync(r => r.Id == id);
            if (recipe == null)
            {
                return NotFound();
            }

            var username = User.FindFirstValue(ClaimTypes.Name);
            if (username == null)
            {
                return Unauthorized();
            }

            var existingRating = recipe.Ratings.FirstOrDefault(r => r.UserName == username);
            if (existingRating != null)
            {
                existingRating.Score = score;
                existingRating.CreatedAt = DateTime.Now;
            }
            else
            {
                var newRating = new Rating
                {
                    RecipeId = id,
                    Score = score,
                    UserName = username,
                    CreatedAt = DateTime.Now
                };
                recipe.Ratings.Add(newRating);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Details), new { id = id });
        }

        // GET: Recipes/Create
        [Authorize]
        public IActionResult Create()
        {
            return View();
        }

        // POST: Recipes/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Recipe model,string returnUrl = null)
        {
            if (ModelState.IsValid)
            {
               
                    var recipe = new Recipe
                    {
                        Title = model.Title,
                        Description = model.Description,
                        ImageUrl = model.ImageUrl,
                        VideoId = model.VideoId,
                        Ingredients = model.Ingredients.Select(i => new Ingredient
                        {
                            Name = i.Name,
                            Quantity = i.Quantity
                        }).ToList(),
                        CreatedBy = model.CreatedBy
                    };
                if (string.IsNullOrEmpty(returnUrl))
                {
                    returnUrl = Request.Headers["Referer"].ToString();
                }

                ViewBag.ReturnUrl = returnUrl;
                _context.Add(recipe);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                
            }
            else
            {
                // Collect error messages
                var errorMessages = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                foreach (var error in errorMessages)
                {
                    Console.WriteLine(error);
                }
                ViewBag.ErrorMessages = errorMessages;
            }

            ViewBag.ReturnUrl = Request.Headers["Referer"].ToString();
            return View(model);
        }


        // GET: Recipes/Edit/5
        [Authorize]
        public async Task<IActionResult> Edit(int? id, string returnUrl = null)
        {
            if (id == null || _context.Recipe == null)
            {
                return NotFound();
            }

            var recipe = await _context.Recipe
                .Include(r => r.Ingredients)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (recipe == null)
            {
                return NotFound();
            }
            var userId = User.FindFirstValue(ClaimTypes.Name);
            if (recipe.CreatedBy != userId)
            {
                return Forbid();
            }
            if (string.IsNullOrEmpty(returnUrl))
            {
                returnUrl = Request.Headers["Referer"].ToString();
            }
            ViewBag.ReturnUrl = returnUrl;
            return View(recipe);
        }


        // POST: Recipes/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Edit(int id, Recipe recipe)
        {
            if (id != recipe.Id)
            {
                return NotFound();
            }
            var userId = User.FindFirstValue(ClaimTypes.Name);
            if (recipe.CreatedBy != userId)
            {
                return Forbid();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Pobierz oryginalny przepis z bazy danych
                    var originalRecipe = await _context.Recipe
                        .Include(r => r.Ingredients)
                        .FirstOrDefaultAsync(r => r.Id == id);

                    if (originalRecipe == null)
                    {
                        return NotFound();
                    }

                    // Zaktualizuj właściwości oryginalnego przepisu
                    originalRecipe.Title = recipe.Title;
                    originalRecipe.Description = recipe.Description;
                    originalRecipe.ImageUrl = recipe.ImageUrl;
                    originalRecipe.VideoId = recipe.VideoId;
                    originalRecipe.CreatedBy = recipe.CreatedBy;
                    originalRecipe.Comments = recipe.Comments;
                    originalRecipe.Ratings = recipe.Ratings;
                    // Usuń usunięte składniki
                    var removedIngredients = originalRecipe.Ingredients
                        .Where(i => !recipe.Ingredients.Any(ri => ri.Id == i.Id))
                        .ToList();
                    foreach (var ingredient in removedIngredients)
                    {
                        originalRecipe.Ingredients.Remove(ingredient);
                        _context.Ingredients.Remove(ingredient); 
                    }

                    // Zaktualizuj istniejące składniki i dodaj nowe
                    foreach (var ingredient in recipe.Ingredients)
                    {
                        var existingIngredient = originalRecipe.Ingredients
                            .FirstOrDefault(i => i.Id == ingredient.Id);
                        if (existingIngredient != null)
                        {
                            existingIngredient.Name = ingredient.Name;
                            existingIngredient.Quantity = ingredient.Quantity;
                        }
                        else
                        {
                            originalRecipe.Ingredients.Add(new Ingredient
                            {
                                Name = ingredient.Name,
                                Quantity = ingredient.Quantity
                            });
                        }
                    }

                    _context.Update(originalRecipe);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!RecipeExists(recipe.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(recipe);
        }


        // GET: Recipes/Delete/5+
        [Authorize]
        public async Task<IActionResult> Delete(int? id, string returnUrl = null)
        {
            if (id == null || _context.Recipe == null)
            {
                return NotFound();
            }

            var recipe = await _context.Recipe
                .Include(r => r.Ingredients)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (recipe == null)
            {
                return NotFound();
            }
            var userId = User.FindFirstValue(ClaimTypes.Name);
            if (recipe.CreatedBy != userId)
            {
                return Forbid();
            }
            if (string.IsNullOrEmpty(returnUrl))
            {
                returnUrl = Request.Headers["Referer"].ToString();
            }
            ViewBag.ReturnUrl = returnUrl;
            return View(recipe);
        }

        // POST: Recipes/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (_context.Recipe == null)
            {
                return Problem("Entity set 'ApplicationDbContext.Recipe' is null.");
            }

            var recipe = await _context.Recipe
                .Include(r => r.Ingredients)
                .FirstOrDefaultAsync(r => r.Id == id);

            var userId = User.FindFirstValue(ClaimTypes.Name);
            if (recipe.CreatedBy != userId)
            {
                return Forbid();
            }
            if (recipe != null)
            {
                _context.Recipe.Remove(recipe);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        private bool RecipeExists(int id)
        {
            return (_context.Recipe?.Any(e => e.Id == id)).GetValueOrDefault();
        }
    }
}
