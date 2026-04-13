using Schema.NET;

internal sealed class A2UIRecipeRenderer : IRecipeRenderer
{
      /// <summary>
      /// Builds the A2UI operations that render a recipe card populated with
      /// data extracted from the supplied <see cref="Recipe"/> object.
      /// </summary>
      public object[] RenderRecipe(Recipe recipe)
      {
            var title            = recipe.Name.FirstOrDefault() ?? "Recipe";
            var image            = GetImageUrl(recipe);
            var (rating, reviewCountLabel) = GetRatingInfo(recipe);
            var prepTime         = FormatDuration(recipe.PrepTime.FirstOrDefault());
            var cookTime         = FormatDuration(recipe.CookTime.FirstOrDefault());
            var servings         = GetServings(recipe);

            object[] ingredients = recipe.RecipeIngredient
                  .Select(text => (object)new { text })
                  .ToArray();

            object[] instructions = GetInstructions(recipe);

            // v0.8 format: component is a discriminated-union object { TypeName: { ...props } }
            // StringValue = { path: "..." } or { literalString: "..." }
            // children (Column/Row/List) = { explicitList: [...] } or { template: { componentId, dataBinding } }
            object[] components =
            [
            new { id = "root",
                  component = new { Card = new { child = "tabs-container" } } },

            new { id = "tabs-container",
                  component = new { Tabs = new { tabItems = new object[]
                  {
                        new { title = new { literalString = "Overview"     }, child = "overview-col"       },
                        new { title = new { literalString = "Ingredients"  }, child = "ingredients-list"   },
                        new { title = new { literalString = "Instructions" }, child = "instructions-list"  },
                  }}}},

            new { id = "overview-col",
                  component = new { Column = new { children = new { explicitList = new[] { "recipe-image", "overview-content" } } } } },

            new { id = "recipe-image",
                  component = new { Image = new { url = new { path = "/image" }, usageHint = "mediumFeature", fit = "cover" } } },

            new { id = "overview-content",
                  component = new { Column = new { children = new { explicitList = new[] { "title", "rating-row", "times-row", "servings" } } } } },

            new { id = "title",
                  component = new { Text = new { text = new { path = "/title" }, usageHint = "h3" } } },

            new { id = "rating-row",
                  component = new { Row = new { children = new { explicitList = new[] { "star-icon", "rating", "review-count" } } } } },

            new { id = "star-icon",
                  component = new { Icon = new { name = new { literalString = "star" } } } },

            new { id = "rating",
                  component = new { Text = new { text = new { path = "/rating" }, usageHint = "body" } } },

            new { id = "review-count",
                  component = new { Text = new { text = new { path = "/reviewCountLabel" }, usageHint = "caption" } } },

            new { id = "times-row",
                  component = new { Row = new { children = new { explicitList = new[] { "prep-time", "cook-time" } } } } },

            new { id = "prep-time",
                  component = new { Row = new { children = new { explicitList = new[] { "prep-icon", "prep-text" } } } } },

            new { id = "prep-icon",
                  component = new { Icon = new { name = new { literalString = "calendarToday" } } } },

            new { id = "prep-text",
                  component = new { Text = new { text = new { path = "/prepTime" }, usageHint = "caption" } } },

            new { id = "cook-time",
                  component = new { Row = new { children = new { explicitList = new[] { "cook-icon", "cook-text" } } } } },

            new { id = "cook-icon",
                  component = new { Icon = new { name = new { literalString = "timer" } } } },

            new { id = "cook-text",
                  component = new { Text = new { text = new { path = "/cookTime" }, usageHint = "caption" } } },

            new { id = "servings",
                  component = new { Text = new { text = new { path = "/servings" }, usageHint = "caption" } } },

            new { id = "ingredients-list",
                  component = new { Column = new { children = new { template = new { componentId = "item-template", dataBinding = "/ingredients" } } } } },

            new { id = "instructions-list",
                  component = new { Column = new { children = new { template = new { componentId = "item-template", dataBinding = "/instructions" } } } } },

            new { id = "item-template",
                  component = new { Text = new { text = new { path = "text" }, usageHint = "body" } } },
            ];

            object[] operations =
            [
            new { surfaceUpdate   = new { surfaceId = "recipe-surface", components } },
            new { dataModelUpdate = new { surfaceId = "recipe-surface", path = "/", contents = new { image, title, rating, reviewCountLabel, prepTime, cookTime, servings, ingredients, instructions } } },
            new { beginRendering  = new { surfaceId = "recipe-surface", root = "root", styles = new { } } },
            ];

            return operations;
      }

      // ── helpers ────────────────────────────────────────────────────────────

      private static string GetImageUrl(Recipe recipe)
      {
            foreach (var item in recipe.Image)
            {
                  if (item is IImageObject img)
                  {
                        var url = img.ContentUrl.FirstOrDefault()?.ToString()
                                  ?? img.Url.FirstOrDefault()?.ToString();
                        if (!string.IsNullOrEmpty(url)) return url;
                  }
                  else if (item is Uri uri)
                        return uri.ToString();
            }
            return "";
      }

      private static (string rating, string reviewCountLabel) GetRatingInfo(Recipe recipe)
      {
            var ar = recipe.AggregateRating.FirstOrDefault();
            if (ar is null) return ("", "");

            var rating = ar.RatingValue.OfType<double?>().FirstOrDefault()?.ToString("F1")
                         ?? ar.RatingValue.OfType<string>().FirstOrDefault()
                         ?? "";

            var count = ar.ReviewCount.FirstOrDefault() ?? ar.RatingCount.FirstOrDefault();
            var label = count.HasValue ? $"({count.Value:N0} reviews)" : "";

            return (rating, label);
      }

      private static string FormatDuration(TimeSpan? ts)
      {
            if (ts is null) return "";
            if (ts.Value.TotalHours >= 1)
                  return $"{(int)ts.Value.TotalHours} h {ts.Value.Minutes} min";
            return $"{(int)ts.Value.TotalMinutes} min";
      }

      private static string GetServings(Recipe recipe)
      {
            return recipe.RecipeYield.OfType<string>().FirstOrDefault()
                   ?? recipe.RecipeYield.OfType<IQuantitativeValue>()
                         .Select(q => q.Value.OfType<string>().FirstOrDefault()
                                      ?? q.Value.OfType<double?>().FirstOrDefault()?.ToString())
                         .FirstOrDefault(s => s is not null)
                   ?? "";
      }

      private static object[] GetInstructions(Recipe recipe)
      {
            var steps = new List<object>();
            int n = 1;

            // Preferred: ItemList or HowToSection (both implement IItemList) containing HowToStep / IListItem elements
            var itemList = recipe.RecipeInstructions.OfType<IItemList>().FirstOrDefault();
            if (itemList is not null)
            {
                  foreach (var hs in itemList.ItemListElement.OfType<HowToStep>())
                  {
                        var t = hs.Text.FirstOrDefault() ?? hs.Name.FirstOrDefault() ?? "";
                        if (!string.IsNullOrWhiteSpace(t))
                              steps.Add(new { text = $"{n++}. {t}" });
                  }

                  if (steps.Count == 0)
                  {
                        // Fallback within ItemList: use IListItem.Name
                        foreach (var li in itemList.ItemListElement.OfType<IListItem>())
                        {
                              var t = li.Name.FirstOrDefault() ?? "";
                              if (!string.IsNullOrWhiteSpace(t))
                                    steps.Add(new { text = $"{n++}. {t}" });
                        }
                  }

                  if (steps.Count > 0) return steps.ToArray();
            }

            // Fallback: bare HowToStep values (no wrapping ItemList)
            foreach (var hs in recipe.RecipeInstructions.OfType<HowToStep>())
            {
                  var t = hs.Text.FirstOrDefault() ?? hs.Name.FirstOrDefault() ?? "";
                  if (!string.IsNullOrWhiteSpace(t))
                        steps.Add(new { text = $"{n++}. {t}" });
            }
            if (steps.Count > 0) return steps.ToArray();

            // Last resort: plain strings
            foreach (var s in recipe.RecipeInstructions.OfType<string>())
            {
                  if (!string.IsNullOrWhiteSpace(s))
                        steps.Add(new { text = $"{n++}. {s}" });
            }

            return steps.ToArray();
      }
}