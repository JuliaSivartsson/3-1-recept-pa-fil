using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FiledRecipes.Domain
{
    /// <summary>
    /// Holder for recipes.
    /// </summary>
    public class RecipeRepository : IRecipeRepository
    {
        /// <summary>
        /// Represents the recipe section.
        /// </summary>
        private const string SectionRecipe = "[Recept]";

        /// <summary>
        /// Represents the ingredients section.
        /// </summary>
        private const string SectionIngredients = "[Ingredienser]";

        /// <summary>
        /// Represents the instructions section.
        /// </summary>
        private const string SectionInstructions = "[Instruktioner]";

        /// <summary>
        /// Occurs after changes to the underlying collection of recipes.
        /// </summary>
        public event EventHandler RecipesChangedEvent;

        /// <summary>
        /// Specifies how the next line read from the file will be interpreted.
        /// </summary>
        private enum RecipeReadStatus { Indefinite, New, Ingredient, Instruction };

        /// <summary>
        /// Collection of recipes.
        /// </summary>
        private List<IRecipe> _recipes;

        /// <summary>
        /// The fully qualified path and name of the file with recipes.
        /// </summary>
        private string _path;

        /// <summary>
        /// Indicates whether the collection of recipes has been modified since it was last saved.
        /// </summary>
        public bool IsModified { get; protected set; }

        /// <summary>
        /// Initializes a new instance of the RecipeRepository class.
        /// </summary>
        /// <param name="path">The path and name of the file with recipes.</param>
        public RecipeRepository(string path)
        {
            // Throws an exception if the path is invalid.
            _path = Path.GetFullPath(path);

            _recipes = new List<IRecipe>();
        }

        /// <summary>
        /// Returns a collection of recipes.
        /// </summary>
        /// <returns>A IEnumerable&lt;Recipe&gt; containing all the recipes.</returns>
        public virtual IEnumerable<IRecipe> GetAll()
        {
            // Deep copy the objects to avoid privacy leaks.
            return _recipes.Select(r => (IRecipe)r.Clone());
        }

        /// <summary>
        /// Returns a recipe.
        /// </summary>
        /// <param name="index">The zero-based index of the recipe to get.</param>
        /// <returns>The recipe at the specified index.</returns>
        public virtual IRecipe GetAt(int index)
        {
            // Deep copy the object to avoid privacy leak.
            return (IRecipe)_recipes[index].Clone();
        }

        /// <summary>
        /// Deletes a recipe.
        /// </summary>
        /// <param name="recipe">The recipe to delete. The value can be null.</param>
        public virtual void Delete(IRecipe recipe)
        {
            // If it's a copy of a recipe...
            if (!_recipes.Contains(recipe))
            {
                // ...try to find the original!
                recipe = _recipes.Find(r => r.Equals(recipe));
            }
            _recipes.Remove(recipe);
            IsModified = true;
            OnRecipesChanged(EventArgs.Empty);
        }

        /// <summary>
        /// Deletes a recipe.
        /// </summary>
        /// <param name="index">The zero-based index of the recipe to delete.</param>
        public virtual void Delete(int index)
        {
            Delete(_recipes[index]);
        }

        /// <summary>
        /// Raises the RecipesChanged event.
        /// </summary>
        /// <param name="e">The EventArgs that contains the event data.</param>
        protected virtual void OnRecipesChanged(EventArgs e)
        {
            // Make a temporary copy of the event to avoid possibility of 
            // a race condition if the last subscriber unsubscribes 
            // immediately after the null check and before the event is raised.
            EventHandler handler = RecipesChangedEvent;

            // Event will be null if there are no subscribers. 
            if (handler != null)
            {
                // Use the () operator to raise the event.vi 
                handler(this, e);
            }
        }

        public virtual void Load()
        {
            RecipeReadStatus recipeStatus = 0;
            Recipe receptObject = null;

            //dynamic array
            List<string> readInRecipes = new List<string>();
            List<IRecipe> addedRecipes = new List<IRecipe>();


            try
            {
                using (StreamReader reader = new StreamReader(_path))
                {
                    string line;

                    // read all lines in file
                    while ((line = reader.ReadLine()) != null)
                    {
                        readInRecipes.Add(line);

                        // determine what next line should have as status
                       if(line == "")
                       {
                           continue;
                       }
                       else if (line == "[Recept]")
                        {
                            recipeStatus = RecipeReadStatus.Indefinite;
                        }

                       else if (line == "[Ingredienser]")
                       {
                           recipeStatus = RecipeReadStatus.Ingredient;
                       }
                       
                        else if (line == "[Instruktioner]")
                       {
                           recipeStatus = RecipeReadStatus.Instruction;
                       }

                        // determine what to do with different status.
                       switch (recipeStatus)
                       {
                           // if it is the title
                           case RecipeReadStatus.Indefinite:
                               if(line != "[Recept]")
                               {
                                   if (receptObject != null)
                                   {
                                       addedRecipes.Add(receptObject);
                                   }
                                   receptObject = new Recipe(line);
                               }
                               break;

                           // if it is the ingredients
                           case RecipeReadStatus.Ingredient:
                               	string[] ingredientSplit = line.Split(';');
                               if(line != "[Ingredienser]")
                               {
                                   if (ingredientSplit.Length != 3)
                                   {
                                       throw new FileFormatException();
                                   }
                                       // skapa ingrediensobjekt, skicka in mängd, mått och namn
                                       Ingredient ingredient = new Ingredient();
                                       
                                       ingredient.Amount = ingredientSplit[0];
                                       ingredient.Measure = ingredientSplit[1];
                                       ingredient.Name = ingredientSplit[2];
                                   
                                       // add the ingredient to the recipe-object
                                       receptObject.Add(ingredient);  
                               }
                               break;

                           // if it is the instructions
                           case RecipeReadStatus.Instruction:
                               if (line != "[Instruktioner]")
                               {
                                   // add the instruction to the recipe-object
                                   receptObject.Add(line);
                               }
                               break;

                           default:
                               throw new FileFormatException();
                       }
                    }

                    // We need to add the last recipe.
                    if (receptObject != null)
                    {
                        addedRecipes.Add(receptObject);
                    }

                    IEnumerable<IRecipe> sortRecipes = addedRecipes.OrderBy(r => r.Name);
                    
                    //Add the sorted list to _recipes 
                    _recipes = sortRecipes.ToList(); 
                }
                IsModified = false;
                OnRecipesChanged(EventArgs.Empty);
            }
                
            catch (Exception)
            {
                throw new FileNotFoundException();
            }
        }

        public virtual void Save()
        {
            try
            {
                using (StreamWriter writeToFile = File.AppendText(_path))
                {
                    writeToFile.WriteLine(_recipes);
                    //test to save recipe
                    //writeToFile.WriteLine("[Recept]");
                    //Console.Write("Receptets namn: ");
                    //writeToFile.WriteLine(Console.ReadLine());
                    //writeToFile.WriteLine("[Ingredienser]");
                    //Console.Write("Ingredienser: ");
                    //writeToFile.WriteLine(Console.ReadLine());
                    //writeToFile.WriteLine("[Instruktioner]");
                    //Console.Write("Instruktioner: ");
                    //writeToFile.WriteLine(Console.ReadLine());
                    IsModified = true;
                    OnRecipesChanged(EventArgs.Empty);
                }
            }
            catch (Exception)
            {
                throw new FileNotFoundException();
            }
            IsModified = false;
            OnRecipesChanged(EventArgs.Empty);
        }

    }
}

