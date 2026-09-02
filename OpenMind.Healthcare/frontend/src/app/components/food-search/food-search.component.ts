import { Component, EventEmitter, Output } from '@angular/core';
import { Subject, debounceTime, distinctUntilChanged, switchMap } from 'rxjs';
import { DietService } from '../../services/diet.service';
import { FoodLibraryItem, MealType, ServingSize } from '../../models/diet.models';

export interface FoodChoice {
  foodLibraryItemId: string;
  servingSizeId: string;
  quantity: number;
  mealType: MealType;
}

@Component({
  selector: 'app-food-search',
  standalone: false,
  templateUrl: './food-search.component.html',
  styleUrls: ['./food-search.component.css']
})
export class FoodSearchComponent {
  @Output() chosen = new EventEmitter<FoodChoice>();

  query = '';
  matches: FoodLibraryItem[] = [];
  searched = false;
  searching = false;

  selectedFood: FoodLibraryItem | null = null;
  selectedServing: ServingSize | null = null;
  quantity = 1;
  mealType: MealType = 'Breakfast';

  readonly meals: MealType[] = ['Breakfast', 'Lunch', 'Dinner', 'Snack'];

  private readonly terms = new Subject<string>();

  constructor(private dietService: DietService) {
    this.terms
      .pipe(
        debounceTime(250),
        distinctUntilChanged(),
        switchMap(term => {
          this.searching = true;
          return this.dietService.searchFoods(term);
        })
      )
      .subscribe({
        next: result => {
          this.matches = result.matches;
          this.searched = true;
          this.searching = false;
        },
        error: () => {
          this.matches = [];
          this.searched = true;
          this.searching = false;
        }
      });
  }

  onQueryChanged(): void {
    this.selectedFood = null;
    this.selectedServing = null;

    if (this.query.trim().length < 2) {
      this.matches = [];
      this.searched = false;
      return;
    }

    this.terms.next(this.query.trim());
  }

  select(food: FoodLibraryItem): void {
    this.selectedFood = food;
    this.selectedServing = food.servingSizes[0] ?? null;
    this.quantity = 1;
  }

  chooseServing(serving: ServingSize): void {
    this.selectedServing = serving;
  }

  get previewCalories(): number {
    if (!this.selectedServing || this.quantity <= 0) {
      return 0;
    }
    return Math.round(this.selectedServing.nutrition.calories * this.quantity);
  }

  add(): void {
    if (!this.selectedFood || !this.selectedServing || this.quantity <= 0) {
      return;
    }

    this.chosen.emit({
      foodLibraryItemId: this.selectedFood.id,
      servingSizeId: this.selectedServing.id,
      quantity: this.quantity,
      mealType: this.mealType
    });

    this.reset();
  }

  reset(): void {
    this.query = '';
    this.matches = [];
    this.searched = false;
    this.selectedFood = null;
    this.selectedServing = null;
    this.quantity = 1;
  }
}
