using DietApi.Domain.Aggregates;
using DietApi.Domain.Entities;
using DietApi.Domain.ValueObjects;

namespace DietApi.Infrastructure.Data.Seeds;

/// <summary>
/// The curated food library shipped with the application.
/// </summary>
/// <remarks>
/// Members select from this rather than typing nutrition values, so the numbers behind every
/// statistic are trustworthy. The trade-off, accepted deliberately for this release, is that a
/// member cannot log a food the library lacks - so breadth matters. Nutrition is given per
/// serving, not per 100 g, because "1 medium banana" and "100 g of banana" are different numbers
/// and the member picks one of them.
/// </remarks>
public static class FoodLibrarySeed
{
    public static FoodLibraryItem[] Items() =>
    [
        // --- Staples -------------------------------------------------------
        F("Porridge oats", FoodCategory.Staple, S("1 bowl (60 g)", 60, 228, 8.4m, 36.0m, 4.8m), S("100 g", 100, 380, 14.0m, 60.0m, 8.0m)),
        F("White rice, cooked", FoodCategory.Staple, S("1 cup (160 g)", 160, 206, 4.3m, 45.0m, 0.4m), S("100 g", 100, 129, 2.7m, 28.0m, 0.3m)),
        F("Brown rice, cooked", FoodCategory.Staple, S("1 cup (160 g)", 160, 176, 4.0m, 36.0m, 1.4m), S("100 g", 100, 110, 2.5m, 22.5m, 0.9m)),
        F("Wholemeal bread", FoodCategory.Staple, S("1 slice (38 g)", 38, 92, 4.0m, 15.5m, 1.2m)),
        F("White bread", FoodCategory.Staple, S("1 slice (36 g)", 36, 96, 3.1m, 18.0m, 1.2m)),
        F("Spaghetti, cooked", FoodCategory.Staple, S("1 plate (200 g)", 200, 316, 11.6m, 62.0m, 1.8m), S("100 g", 100, 158, 5.8m, 31.0m, 0.9m)),
        F("Potato, boiled", FoodCategory.Staple, S("1 medium (150 g)", 150, 130, 2.9m, 30.0m, 0.2m)),
        F("Sweet potato, baked", FoodCategory.Staple, S("1 medium (130 g)", 130, 116, 2.1m, 27.0m, 0.1m)),
        F("Couscous, cooked", FoodCategory.Staple, S("1 cup (157 g)", 157, 176, 6.0m, 36.5m, 0.3m)),
        F("Quinoa, cooked", FoodCategory.Staple, S("1 cup (185 g)", 185, 222, 8.1m, 39.4m, 3.6m)),
        F("Bagel, plain", FoodCategory.Staple, S("1 bagel (98 g)", 98, 277, 11.0m, 55.0m, 1.7m)),
        F("Tortilla wrap", FoodCategory.Staple, S("1 wrap (49 g)", 49, 146, 4.0m, 25.0m, 3.5m)),
        F("Croissant", FoodCategory.Staple, S("1 croissant (57 g)", 57, 231, 4.7m, 26.0m, 12.0m)),
        F("Cornflakes", FoodCategory.Staple, S("1 bowl (30 g)", 30, 113, 2.1m, 25.0m, 0.3m)),
        F("Muesli", FoodCategory.Staple, S("1 bowl (45 g)", 45, 172, 4.5m, 30.0m, 3.5m)),
        F("Noodles, egg, cooked", FoodCategory.Staple, S("1 portion (180 g)", 180, 250, 8.2m, 47.0m, 2.8m)),
        F("Pita bread", FoodCategory.Staple, S("1 pita (60 g)", 60, 165, 5.5m, 33.0m, 0.7m)),
        F("Rye crispbread", FoodCategory.Staple, S("1 slice (10 g)", 10, 35, 1.0m, 7.0m, 0.2m)),
        F("Baguette", FoodCategory.Staple, S("1 piece (70 g)", 70, 190, 6.5m, 37.0m, 1.2m)),
        F("Naan bread", FoodCategory.Staple, S("1 naan (90 g)", 90, 285, 8.0m, 48.0m, 6.5m)),

        // --- Proteins ------------------------------------------------------
        F("Chicken breast, grilled", FoodCategory.Protein, S("1 breast (120 g)", 120, 198, 37.2m, 0m, 4.3m), S("100 g", 100, 165, 31.0m, 0m, 3.6m)),
        F("Chicken thigh, roasted", FoodCategory.Protein, S("1 thigh (90 g)", 90, 190, 22.5m, 0m, 11.0m)),
        F("Beef mince, 5% fat, cooked", FoodCategory.Protein, S("100 g", 100, 176, 26.0m, 0m, 8.0m)),
        F("Beef steak, sirloin", FoodCategory.Protein, S("1 steak (170 g)", 170, 340, 51.0m, 0m, 14.5m)),
        F("Pork chop, grilled", FoodCategory.Protein, S("1 chop (150 g)", 150, 291, 39.0m, 0m, 14.5m)),
        F("Salmon fillet, baked", FoodCategory.Protein, S("1 fillet (130 g)", 130, 271, 29.0m, 0m, 17.0m), S("100 g", 100, 208, 22.1m, 0m, 13.0m)),
        F("Cod fillet, baked", FoodCategory.Protein, S("1 fillet (150 g)", 150, 158, 34.5m, 0m, 1.3m)),
        F("Tuna, canned in water", FoodCategory.Protein, S("1 can (145 g)", 145, 179, 39.0m, 0m, 1.3m)),
        F("Prawns, cooked", FoodCategory.Protein, S("100 g", 100, 99, 24.0m, 0.2m, 0.3m)),
        F("Egg, boiled", FoodCategory.Protein, S("1 large egg (50 g)", 50, 78, 6.3m, 0.6m, 5.3m)),
        F("Egg, fried", FoodCategory.Protein, S("1 large egg (46 g)", 46, 90, 6.3m, 0.4m, 7.0m)),
        F("Tofu, firm", FoodCategory.Protein, S("100 g", 100, 144, 17.3m, 2.8m, 8.7m)),
        F("Lentils, cooked", FoodCategory.Protein, S("1 cup (198 g)", 198, 230, 17.9m, 39.9m, 0.8m)),
        F("Chickpeas, cooked", FoodCategory.Protein, S("1 cup (164 g)", 164, 269, 14.5m, 45.0m, 4.2m)),
        F("Black beans, cooked", FoodCategory.Protein, S("1 cup (172 g)", 172, 227, 15.2m, 40.8m, 0.9m)),
        F("Turkey breast, roasted", FoodCategory.Protein, S("100 g", 100, 135, 30.0m, 0m, 1.0m)),
        F("Bacon, grilled", FoodCategory.Protein, S("1 rasher (25 g)", 25, 108, 7.5m, 0.2m, 8.5m)),
        F("Sausage, pork, grilled", FoodCategory.Protein, S("1 sausage (57 g)", 57, 170, 8.5m, 2.0m, 14.0m)),
        F("Ham, sliced", FoodCategory.Protein, S("1 slice (28 g)", 28, 46, 5.5m, 1.0m, 2.2m)),
        F("Mackerel, smoked", FoodCategory.Protein, S("1 fillet (100 g)", 100, 305, 18.9m, 0m, 25.5m)),

        // --- Dairy ---------------------------------------------------------
        F("Whole milk", FoodCategory.Dairy, S("1 glass (250 ml)", 258, 155, 8.2m, 12.0m, 8.2m), S("100 ml", 103, 62, 3.3m, 4.8m, 3.3m)),
        F("Semi-skimmed milk", FoodCategory.Dairy, S("1 glass (250 ml)", 258, 118, 8.5m, 12.5m, 4.3m)),
        F("Skimmed milk", FoodCategory.Dairy, S("1 glass (250 ml)", 258, 88, 8.7m, 12.5m, 0.3m)),
        F("Greek yoghurt, natural", FoodCategory.Dairy, S("1 pot (170 g)", 170, 100, 17.3m, 6.1m, 0.7m), S("100 g", 100, 59, 10.2m, 3.6m, 0.4m)),
        F("Yoghurt, fruit", FoodCategory.Dairy, S("1 pot (125 g)", 125, 119, 4.5m, 19.0m, 2.6m)),
        F("Cheddar cheese", FoodCategory.Dairy, S("1 slice (28 g)", 28, 113, 7.0m, 0.4m, 9.3m), S("100 g", 100, 402, 25.0m, 1.3m, 33.1m)),
        F("Mozzarella", FoodCategory.Dairy, S("100 g", 100, 280, 28.0m, 3.1m, 17.1m)),
        F("Feta cheese", FoodCategory.Dairy, S("50 g", 50, 132, 7.1m, 2.0m, 10.7m)),
        F("Cottage cheese", FoodCategory.Dairy, S("1 pot (200 g)", 200, 196, 24.0m, 6.6m, 8.0m)),
        F("Cream cheese", FoodCategory.Dairy, S("1 tbsp (15 g)", 15, 51, 0.9m, 0.8m, 5.0m)),
        F("Butter", FoodCategory.Dairy, S("1 tsp (5 g)", 5, 36, 0m, 0m, 4.1m), S("1 tbsp (14 g)", 14, 102, 0.1m, 0m, 11.5m)),
        F("Parmesan, grated", FoodCategory.Dairy, S("1 tbsp (5 g)", 5, 22, 2.0m, 0.2m, 1.4m)),

        // --- Fruit ---------------------------------------------------------
        F("Banana", FoodCategory.Fruit, S("1 medium (118 g)", 118, 105, 1.3m, 27.0m, 0.4m)),
        F("Apple", FoodCategory.Fruit, S("1 medium (182 g)", 182, 95, 0.5m, 25.1m, 0.3m)),
        F("Orange", FoodCategory.Fruit, S("1 medium (131 g)", 131, 62, 1.2m, 15.4m, 0.2m)),
        F("Strawberries", FoodCategory.Fruit, S("1 cup (152 g)", 152, 49, 1.0m, 11.7m, 0.5m)),
        F("Blueberries", FoodCategory.Fruit, S("1 cup (148 g)", 148, 84, 1.1m, 21.4m, 0.5m)),
        F("Grapes", FoodCategory.Fruit, S("1 cup (151 g)", 151, 104, 1.1m, 27.3m, 0.2m)),
        F("Pear", FoodCategory.Fruit, S("1 medium (178 g)", 178, 101, 0.6m, 27.1m, 0.2m)),
        F("Mango", FoodCategory.Fruit, S("1 cup (165 g)", 165, 99, 1.4m, 24.7m, 0.6m)),
        F("Pineapple", FoodCategory.Fruit, S("1 cup (165 g)", 165, 82, 0.9m, 21.6m, 0.2m)),
        F("Watermelon", FoodCategory.Fruit, S("1 cup (152 g)", 152, 46, 0.9m, 11.5m, 0.2m)),
        F("Avocado", FoodCategory.Fruit, S("1 half (100 g)", 100, 160, 2.0m, 8.5m, 14.7m)),
        F("Peach", FoodCategory.Fruit, S("1 medium (150 g)", 150, 59, 1.4m, 14.3m, 0.4m)),
        F("Raspberries", FoodCategory.Fruit, S("1 cup (123 g)", 123, 64, 1.5m, 14.7m, 0.8m)),
        F("Kiwi", FoodCategory.Fruit, S("1 fruit (69 g)", 69, 42, 0.8m, 10.1m, 0.4m)),
        F("Melon, cantaloupe", FoodCategory.Fruit, S("1 cup (160 g)", 160, 54, 1.3m, 13.0m, 0.3m)),
        F("Dates, dried", FoodCategory.Fruit, S("1 date (24 g)", 24, 66, 0.4m, 18.0m, 0.1m)),

        // --- Vegetables ----------------------------------------------------
        F("Broccoli, steamed", FoodCategory.Vegetable, S("1 cup (156 g)", 156, 55, 3.7m, 11.2m, 0.6m)),
        F("Carrot, raw", FoodCategory.Vegetable, S("1 medium (61 g)", 61, 25, 0.6m, 5.8m, 0.1m)),
        F("Spinach, raw", FoodCategory.Vegetable, S("1 cup (30 g)", 30, 7, 0.9m, 1.1m, 0.1m)),
        F("Tomato", FoodCategory.Vegetable, S("1 medium (123 g)", 123, 22, 1.1m, 4.8m, 0.2m)),
        F("Cucumber", FoodCategory.Vegetable, S("100 g", 100, 15, 0.7m, 3.6m, 0.1m)),
        F("Bell pepper", FoodCategory.Vegetable, S("1 medium (119 g)", 119, 31, 1.2m, 7.2m, 0.4m)),
        F("Onion", FoodCategory.Vegetable, S("1 medium (110 g)", 110, 44, 1.2m, 10.3m, 0.1m)),
        F("Peas, cooked", FoodCategory.Vegetable, S("1 cup (160 g)", 160, 134, 8.6m, 25.0m, 0.4m)),
        F("Green beans, steamed", FoodCategory.Vegetable, S("1 cup (125 g)", 125, 44, 2.4m, 9.9m, 0.4m)),
        F("Mushrooms, fried", FoodCategory.Vegetable, S("100 g", 100, 63, 3.0m, 4.0m, 4.0m)),
        F("Cauliflower, steamed", FoodCategory.Vegetable, S("1 cup (124 g)", 124, 29, 2.3m, 5.1m, 0.6m)),
        F("Lettuce", FoodCategory.Vegetable, S("1 cup (36 g)", 36, 5, 0.5m, 1.0m, 0.1m)),
        F("Courgette, cooked", FoodCategory.Vegetable, S("1 cup (180 g)", 180, 27, 2.0m, 4.8m, 0.6m)),
        F("Sweetcorn", FoodCategory.Vegetable, S("1 cup (164 g)", 164, 143, 5.4m, 31.0m, 2.2m)),
        F("Aubergine, roasted", FoodCategory.Vegetable, S("1 cup (99 g)", 99, 35, 0.8m, 8.6m, 0.2m)),
        F("Cabbage, cooked", FoodCategory.Vegetable, S("1 cup (150 g)", 150, 34, 2.0m, 8.0m, 0.1m)),

        // --- Prepared meals ------------------------------------------------
        F("Chicken curry with rice", FoodCategory.PreparedMeal, S("1 portion (400 g)", 400, 560, 32.0m, 68.0m, 18.0m)),
        F("Margherita pizza", FoodCategory.PreparedMeal, S("1 slice (107 g)", 107, 250, 11.0m, 30.0m, 9.5m), S("Whole pizza (320 g)", 320, 750, 33.0m, 90.0m, 28.5m)),
        F("Spaghetti bolognese", FoodCategory.PreparedMeal, S("1 portion (400 g)", 400, 588, 28.0m, 70.0m, 22.0m)),
        F("Chicken salad", FoodCategory.PreparedMeal, S("1 bowl (300 g)", 300, 320, 30.0m, 12.0m, 17.0m)),
        F("Beef burger with bun", FoodCategory.PreparedMeal, S("1 burger (215 g)", 215, 540, 29.0m, 40.0m, 27.0m)),
        F("Fish and chips", FoodCategory.PreparedMeal, S("1 portion (400 g)", 400, 840, 34.0m, 88.0m, 40.0m)),
        F("Chicken sandwich", FoodCategory.PreparedMeal, S("1 sandwich (200 g)", 200, 400, 27.0m, 42.0m, 13.0m)),
        F("Vegetable stir fry", FoodCategory.PreparedMeal, S("1 portion (350 g)", 350, 285, 9.0m, 40.0m, 10.0m)),
        F("Sushi rolls", FoodCategory.PreparedMeal, S("6 pieces (170 g)", 170, 255, 9.0m, 44.0m, 4.5m)),
        F("Lasagne", FoodCategory.PreparedMeal, S("1 portion (350 g)", 350, 490, 26.0m, 43.0m, 24.0m)),
        F("Chicken noodle soup", FoodCategory.PreparedMeal, S("1 bowl (300 g)", 300, 180, 12.0m, 22.0m, 5.0m)),
        F("Jacket potato with beans", FoodCategory.PreparedMeal, S("1 portion (350 g)", 350, 390, 14.0m, 76.0m, 2.0m)),
        F("Omelette, three egg", FoodCategory.PreparedMeal, S("1 omelette (165 g)", 165, 300, 20.0m, 2.0m, 23.0m)),
        F("Falafel wrap", FoodCategory.PreparedMeal, S("1 wrap (250 g)", 250, 480, 15.0m, 58.0m, 21.0m)),
        F("Full English breakfast", FoodCategory.PreparedMeal, S("1 plate (450 g)", 450, 807, 38.0m, 50.0m, 48.0m)),
        F("Caesar salad", FoodCategory.PreparedMeal, S("1 bowl (250 g)", 250, 350, 12.0m, 14.0m, 27.0m)),

        // --- Snacks --------------------------------------------------------
        F("Almonds", FoodCategory.Snack, S("1 handful (28 g)", 28, 164, 6.0m, 6.1m, 14.2m)),
        F("Peanuts, salted", FoodCategory.Snack, S("1 handful (28 g)", 28, 161, 7.3m, 4.6m, 14.0m)),
        F("Walnuts", FoodCategory.Snack, S("1 handful (28 g)", 28, 185, 4.3m, 3.9m, 18.5m)),
        F("Peanut butter", FoodCategory.Snack, S("1 tbsp (16 g)", 16, 94, 3.5m, 3.2m, 8.0m)),
        F("Milk chocolate", FoodCategory.Snack, S("1 bar (45 g)", 45, 240, 3.4m, 26.0m, 13.5m), S("1 square (5 g)", 5, 27, 0.4m, 2.9m, 1.5m)),
        F("Dark chocolate", FoodCategory.Snack, S("1 square (10 g)", 10, 60, 0.8m, 4.6m, 4.3m)),
        F("Crisps, ready salted", FoodCategory.Snack, S("1 bag (25 g)", 25, 131, 1.6m, 12.5m, 8.2m)),
        F("Digestive biscuit", FoodCategory.Snack, S("1 biscuit (15 g)", 15, 71, 1.0m, 9.7m, 3.2m)),
        F("Granola bar", FoodCategory.Snack, S("1 bar (40 g)", 40, 180, 3.0m, 27.0m, 6.5m)),
        F("Popcorn, plain", FoodCategory.Snack, S("1 bowl (30 g)", 30, 116, 3.6m, 23.0m, 1.3m)),
        F("Hummus", FoodCategory.Snack, S("2 tbsp (60 g)", 60, 100, 4.8m, 8.0m, 5.5m)),
        F("Rice cake", FoodCategory.Snack, S("1 cake (9 g)", 9, 35, 0.7m, 7.3m, 0.3m)),
        F("Croissant, chocolate", FoodCategory.Snack, S("1 pastry (70 g)", 70, 290, 5.5m, 32.0m, 15.5m)),
        F("Ice cream, vanilla", FoodCategory.Snack, S("1 scoop (66 g)", 66, 137, 2.3m, 15.6m, 7.3m)),
        F("Cashews", FoodCategory.Snack, S("1 handful (28 g)", 28, 157, 5.2m, 8.6m, 12.4m)),
        F("Protein bar", FoodCategory.Snack, S("1 bar (60 g)", 60, 220, 20.0m, 22.0m, 7.0m)),

        // --- Drinks --------------------------------------------------------
        F("Coffee, black", FoodCategory.Drink, S("1 mug (240 ml)", 240, 2, 0.3m, 0m, 0m)),
        F("Latte", FoodCategory.Drink, S("1 regular (350 ml)", 350, 190, 12.0m, 18.0m, 7.0m)),
        F("Cappuccino", FoodCategory.Drink, S("1 regular (240 ml)", 240, 120, 8.0m, 12.0m, 4.0m)),
        F("Tea with milk", FoodCategory.Drink, S("1 mug (240 ml)", 240, 30, 1.5m, 2.5m, 1.5m)),
        F("Orange juice", FoodCategory.Drink, S("1 glass (250 ml)", 258, 112, 1.7m, 25.8m, 0.5m)),
        F("Apple juice", FoodCategory.Drink, S("1 glass (250 ml)", 258, 114, 0.3m, 28.0m, 0.3m)),
        F("Cola", FoodCategory.Drink, S("1 can (330 ml)", 330, 139, 0m, 35.0m, 0m)),
        F("Diet cola", FoodCategory.Drink, S("1 can (330 ml)", 330, 1, 0m, 0m, 0m)),
        F("Beer, lager", FoodCategory.Drink, S("1 pint (568 ml)", 568, 208, 1.7m, 17.0m, 0m)),
        F("Red wine", FoodCategory.Drink, S("1 glass (175 ml)", 175, 149, 0.1m, 4.6m, 0m)),
        F("White wine", FoodCategory.Drink, S("1 glass (175 ml)", 175, 145, 0.1m, 4.7m, 0m)),
        F("Smoothie, mixed berry", FoodCategory.Drink, S("1 bottle (250 ml)", 250, 130, 1.5m, 30.0m, 0.5m)),
        F("Water", FoodCategory.Drink, S("1 glass (250 ml)", 250, 0, 0m, 0m, 0m)),
        F("Hot chocolate", FoodCategory.Drink, S("1 mug (250 ml)", 250, 190, 8.0m, 27.0m, 6.0m)),
        F("Sports drink", FoodCategory.Drink, S("1 bottle (500 ml)", 500, 130, 0m, 32.0m, 0m)),
        F("Almond milk, unsweetened", FoodCategory.Drink, S("1 glass (250 ml)", 250, 33, 1.3m, 1.5m, 2.8m)),
        F("Oat milk", FoodCategory.Drink, S("1 glass (250 ml)", 250, 120, 3.0m, 16.0m, 5.0m)),
        F("Soy milk, unsweetened", FoodCategory.Drink, S("1 glass (250 ml)", 250, 80, 7.0m, 4.0m, 4.0m)),
        F("Energy drink", FoodCategory.Drink, S("1 can (250 ml)", 250, 110, 0m, 27.0m, 0m)),
        F("Lemonade", FoodCategory.Drink, S("1 glass (250 ml)", 250, 105, 0m, 26.0m, 0m)),

        // --- Condiments, fats and spreads: small, frequent, and easy to forget --
        F("Olive oil", FoodCategory.Staple, S("1 tbsp (14 g)", 14, 119, 0m, 0m, 13.5m), S("1 tsp (5 g)", 5, 42, 0m, 0m, 4.8m)),
        F("Vegetable oil", FoodCategory.Staple, S("1 tbsp (14 g)", 14, 124, 0m, 0m, 14.0m)),
        F("Mayonnaise", FoodCategory.Staple, S("1 tbsp (14 g)", 14, 94, 0.1m, 0.1m, 10.3m)),
        F("Tomato ketchup", FoodCategory.Staple, S("1 tbsp (17 g)", 17, 17, 0.2m, 4.5m, 0m)),
        F("Mustard", FoodCategory.Staple, S("1 tsp (5 g)", 5, 3, 0.2m, 0.3m, 0.2m)),
        F("Honey", FoodCategory.Staple, S("1 tbsp (21 g)", 21, 64, 0.1m, 17.3m, 0m)),
        F("Strawberry jam", FoodCategory.Staple, S("1 tbsp (20 g)", 20, 56, 0.1m, 13.8m, 0m)),
        F("Sugar, white", FoodCategory.Staple, S("1 tsp (4 g)", 4, 16, 0m, 4.0m, 0m)),
        F("Soy sauce", FoodCategory.Staple, S("1 tbsp (16 g)", 16, 9, 1.3m, 0.8m, 0m)),
        F("Salad dressing, vinaigrette", FoodCategory.Staple, S("1 tbsp (15 g)", 15, 72, 0.1m, 0.6m, 7.8m)),

        // --- Everyday additions -------------------------------------------
        F("Baked beans", FoodCategory.Protein, S("Half a tin (200 g)", 200, 162, 9.6m, 26.0m, 0.4m)),
        F("Scrambled eggs", FoodCategory.Protein, S("2 eggs (120 g)", 120, 200, 13.0m, 2.0m, 15.0m)),
        F("Porridge made with milk", FoodCategory.Staple, S("1 bowl (250 g)", 250, 240, 11.0m, 33.0m, 7.0m)),
        F("Chips, oven baked", FoodCategory.Staple, S("1 portion (150 g)", 150, 240, 3.5m, 40.0m, 7.5m)),
        F("Roast potatoes", FoodCategory.Staple, S("1 portion (150 g)", 150, 220, 3.0m, 33.0m, 8.5m)),
        F("Garlic bread", FoodCategory.Staple, S("1 slice (40 g)", 40, 145, 3.3m, 17.0m, 7.0m)),
        F("Blueberry muffin", FoodCategory.Snack, S("1 muffin (113 g)", 113, 426, 6.0m, 61.0m, 18.0m)),
        F("Doughnut, glazed", FoodCategory.Snack, S("1 doughnut (60 g)", 60, 240, 3.5m, 27.0m, 13.0m)),
        F("Cheesecake", FoodCategory.Snack, S("1 slice (125 g)", 125, 401, 7.0m, 32.0m, 27.0m)),
        F("Apple pie", FoodCategory.Snack, S("1 slice (125 g)", 125, 296, 2.4m, 43.0m, 13.0m)),
        F("Oatcakes", FoodCategory.Snack, S("1 oatcake (10 g)", 10, 44, 1.1m, 6.2m, 1.7m)),
        F("Trail mix", FoodCategory.Snack, S("1 handful (35 g)", 35, 173, 5.0m, 15.0m, 11.0m)),
        F("Chicken wrap", FoodCategory.PreparedMeal, S("1 wrap (250 g)", 250, 450, 30.0m, 45.0m, 15.0m)),
        F("Tomato soup", FoodCategory.PreparedMeal, S("1 bowl (300 g)", 300, 165, 3.0m, 24.0m, 6.0m)),
        F("Macaroni cheese", FoodCategory.PreparedMeal, S("1 portion (350 g)", 350, 595, 24.0m, 60.0m, 28.0m)),
        F("Shepherd's pie", FoodCategory.PreparedMeal, S("1 portion (350 g)", 350, 462, 24.0m, 42.0m, 21.0m))
    ];

    private static FoodLibraryItem F(string name, FoodCategory category, params ServingSize[] servings) =>
        FoodLibraryItem.Create(name, category, servings);

    private static ServingSize S(string label, decimal grams, int kcal, decimal protein, decimal carbs, decimal fat) =>
        ServingSize.Create(label, grams, NutritionValues.Create(kcal, protein, carbs, fat));
}
