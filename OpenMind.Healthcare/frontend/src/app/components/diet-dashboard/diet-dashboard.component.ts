import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { DietService } from '../../services/diet.service';
import { FoodChoice } from '../food-search/food-search.component';
import { FoodEntry, LoggedDay, MealType } from '../../models/diet.models';

@Component({
  selector: 'app-diet-dashboard',
  standalone: false,
  templateUrl: './diet-dashboard.component.html',
  styleUrls: ['./diet-dashboard.component.css']
})
export class DietDashboardComponent implements OnInit {
  day: LoggedDay | null = null;
  date = new Date().toISOString().substring(0, 10);

  loading = false;
  error: string | null = null;

  /** Set when another session changed the day first - the member reloads rather than overwrites. */
  conflict = false;

  readonly meals: MealType[] = ['Breakfast', 'Lunch', 'Dinner', 'Snack'];

  constructor(
    private dietService: DietService,
    private route: ActivatedRoute,
    private router: Router
  ) {}

  ngOnInit(): void {
    const routeDate = this.route.snapshot.paramMap.get('date');
    if (routeDate) {
      this.date = routeDate;
    }
    this.load();
  }

  load(): void {
    this.loading = true;
    this.error = null;
    this.conflict = false;

    this.dietService.getDay(this.date).subscribe({
      next: day => {
        this.day = day;
        this.loading = false;
      },
      error: err => {
        this.loading = false;
        if (err?.status === 404) {
          // No plan yet - setup is the only sensible destination.
          this.router.navigate(['/diet/setup']);
          return;
        }
        this.error = err?.error?.message ?? 'Could not load this day.';
      }
    });
  }

  changeDate(date: string): void {
    this.date = date;
    this.load();
  }

  entriesFor(meal: MealType): FoodEntry[] {
    return this.day?.entries.filter(e => e.mealType === meal) ?? [];
  }

  caloriesFor(meal: MealType): number {
    return this.entriesFor(meal).reduce((total, e) => total + e.nutrition.calories, 0);
  }

  add(choice: FoodChoice): void {
    this.error = null;

    this.dietService
      .addEntry(this.date, { ...choice, version: this.day?.version ?? null })
      .subscribe({
        next: day => (this.day = day),
        error: err => this.handleWriteError(err)
      });
  }

  changeQuantity(entry: FoodEntry, quantity: number): void {
    if (!this.day?.version || quantity <= 0) {
      return;
    }

    this.dietService
      .updateEntry(entry.id, {
        servingSizeId: entry.servingSizeId,
        quantity,
        mealType: entry.mealType,
        version: this.day.version
      })
      .subscribe({
        next: day => (this.day = day),
        error: err => this.handleWriteError(err)
      });
  }

  remove(entry: FoodEntry): void {
    if (!this.day?.version) {
      return;
    }

    this.dietService.deleteEntry(entry.id, this.day.version).subscribe({
      next: day => {
        // A null body means that was the day's last entry: the date reverts to not logged.
        if (day) {
          this.day = day;
        } else {
          this.load();
        }
      },
      error: err => this.handleWriteError(err)
    });
  }

  private handleWriteError(err: any): void {
    if (err?.status === 409) {
      this.conflict = true;
      this.error = err?.error?.message ?? 'This day changed somewhere else. Reload to see the latest.';
      return;
    }
    this.error = err?.error?.message ?? 'That did not save.';
  }
}
