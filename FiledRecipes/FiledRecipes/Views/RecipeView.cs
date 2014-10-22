using FiledRecipes.Domain;
using FiledRecipes.App.Mvp;
using FiledRecipes.Properties;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FiledRecipes.Views
{
    /// <summary>
    /// 
    /// </summary>
    public class RecipeView : ViewBase, IRecipeView
    {
        public void Show(IRecipe recipe)
        {
            
            // Get the name to the header
            Header = recipe.Name;
            ShowHeaderPanel();

            Console.WriteLine();
            Console.WriteLine("Ingredienser");
            Console.WriteLine("============");
            foreach (var ingredients in recipe.Ingredients)
            {
                Console.WriteLine(ingredients);
            }
            Console.WriteLine();
            Console.WriteLine("Gör såhär:");
            Console.WriteLine("============");
            int number = 0;
            foreach (var instructions in recipe.Instructions)
            {
                number++;
                Console.Write(number + " ");
                Console.WriteLine(instructions);
            }
        }

        public void Show(IEnumerable<IRecipe> recipes)
        {
                //show one recipe at a time.
                foreach (var recipeToShow in recipes)
                {
                    Show(recipeToShow);
                    ContinueOnKeyPressed();
                }
        }

    }                                                                                                                                                                                                                                                                                                      
}
