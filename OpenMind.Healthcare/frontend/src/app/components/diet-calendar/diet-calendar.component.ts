import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { DietService } from '../../services/diet.service';
import { DaySummary, DietStatistics } from '../../models/diet.models';

interface CalendarMonth {
  label: string;
  year: number;
  month: number;
  /** Leading blanks so the first of the month falls under the right weekday. */
  leadingBlanks: number[];
  days: DaySummary[];
}

@Component({
  selector: 'app-diet-calendar',
  standalone: false,
  templateUrl: './diet-calendar.component.html',
  styleUrls: ['./diet-calendar.component.css']
})
export class DietCalendarComponent implements OnInit {
  view: 'month' | 'year' = 'month';
  anchor = new Date();

  months: CalendarMonth[] = [];
  stats: DietStatistics | null = null;

  loading = false;
  error: string | null = null;

  readonly weekdays = ['Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat', 'Sun'];

  constructor(private dietService: DietService, private router: Router) {}

  ngOnInit(): void {
    this.load();

    this.dietService.getStats().subscribe({
      next: stats => (this.stats = stats),
      error: () => (this.stats = null)
    });
  }

  setView(view: 'month' | 'year'): void {
    this.view = view;
    this.load();
  }

  step(direction: number): void {
    if (this.view === 'month') {
      this.anchor = new Date(this.anchor.getFullYear(), this.anchor.getMonth() + direction, 1);
    } else {
      this.anchor = new Date(this.anchor.getFullYear() + direction, 0, 1);
    }
    this.load();
  }

  get periodLabel(): string {
    return this.view === 'month'
      ? this.anchor.toLocaleDateString(undefined, { month: 'long', year: 'numeric' })
      : `${this.anchor.getFullYear()}`;
  }

  load(): void {
    const { from, to } = this.range();
    this.loading = true;
    this.error = null;

    this.dietService.getDayRange(from, to).subscribe({
      next: range => {
        this.months = this.groupIntoMonths(range.days);
        this.loading = false;
      },
      error: err => {
        this.loading = false;
        if (err?.status === 404) {
          this.router.navigate(['/diet/setup']);
          return;
        }
        this.error = err?.error?.message ?? 'Could not load your calendar.';
      }
    });
  }

  openDay(day: DaySummary): void {
    if (day.withinPlan) {
      this.router.navigate(['/diet/log', day.date]);
    }
  }

  /** One marking function shared by both views, so they can never disagree. */
  stateClass(day: DaySummary): string {
    if (!day.withinPlan) {
      return 'outside';
    }
    switch (day.state) {
      case 'OnTarget':
        return 'on-target';
      case 'OverTarget':
        return 'over-target';
      default:
        return 'not-logged';
    }
  }

  dayNumber(day: DaySummary): number {
    return Number(day.date.substring(8, 10));
  }

  private range(): { from: string; to: string } {
    const year = this.anchor.getFullYear();

    if (this.view === 'year') {
      return { from: `${year}-01-01`, to: `${year}-12-31` };
    }

    const month = this.anchor.getMonth();
    const first = new Date(year, month, 1);
    const last = new Date(year, month + 1, 0);
    return { from: this.iso(first), to: this.iso(last) };
  }

  private iso(date: Date): string {
    const month = `${date.getMonth() + 1}`.padStart(2, '0');
    const day = `${date.getDate()}`.padStart(2, '0');
    return `${date.getFullYear()}-${month}-${day}`;
  }

  private groupIntoMonths(days: DaySummary[]): CalendarMonth[] {
    const byMonth = new Map<string, DaySummary[]>();

    for (const day of days) {
      const key = day.date.substring(0, 7);
      const bucket = byMonth.get(key);
      if (bucket) {
        bucket.push(day);
      } else {
        byMonth.set(key, [day]);
      }
    }

    return [...byMonth.entries()].map(([key, monthDays]) => {
      const year = Number(key.substring(0, 4));
      const month = Number(key.substring(5, 7)) - 1;
      const first = new Date(year, month, 1);

      // JavaScript weeks start on Sunday; this calendar starts on Monday.
      const leading = (first.getDay() + 6) % 7;

      return {
        label: first.toLocaleDateString(undefined, { month: 'long' }),
        year,
        month,
        leadingBlanks: Array.from({ length: leading }, (_, i) => i),
        days: monthDays
      };
    });
  }
}
