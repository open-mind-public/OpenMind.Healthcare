import { Component, OnInit, ViewChild } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, forkJoin, of } from 'rxjs';
import { DietService } from '../../services/diet.service';
import { DaySummary, DietStatistics, ExerciseDaySummary } from '../../models/diet.models';
import { ExerciseLogComponent } from '../exercise-log/exercise-log.component';

type BeerRange = { from: string; to: string; days: string[] };

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

  /**
   * Exercise, keyed by date. Fetched separately from the eating range and merged here: the two
   * are independent records of the same day, and neither endpoint knows about the other
   * (research.md R-005).
   */
  private exerciseByDate = new Map<string, ExerciseDaySummary>();

  /**
   * The dates the member has marked as beer days, fetched as its own range and merged here - the
   * eating and exercise endpoints know nothing about beer (research.md R-003). A beer day is a
   * plain marker: it never changes the day's eating state (FR-004).
   */
  private beerByDate = new Set<string>();

  /** The day whose popover is open, where a member marks or unmarks a beer day (FR-001). */
  selectedDay: DaySummary | null = null;

  /** True while a mark/unmark round-trips, so the toggle cannot be double-fired. */
  savingBeer = false;

  /** Set when the embedded activity log reported a change, so closing the popover refreshes the grid. */
  private activityDirty = false;

  /** Set by "Save & close" while a pending session is being committed, so the close waits for it. */
  private closeAfterSave = false;

  @ViewChild(ExerciseLogComponent) private activityLog?: ExerciseLogComponent;

  loading = false;
  error: string | null = null;

  readonly weekdays = ['Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat', 'Sun'];

  /** The year view has ~21px columns, so the headers shrink to initials. */
  readonly weekdayInitials = ['M', 'T', 'W', 'T', 'F', 'S', 'S'];

  /** Initials repeat (T, S), so the repeater must track by position, not value. */
  trackByIndex(index: number): number {
    return index;
  }

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

    forkJoin({
      eating: this.dietService.getDayRange(from, to),

      // A failure in either of these must not cost the member their calendar. Exercise and beer
      // markings are additional information about days that are drawn either way, so they degrade
      // to nothing.
      exercise: this.dietService
        .getExerciseRange(from, to)
        .pipe(catchError(() => of({ from, to, days: [] as ExerciseDaySummary[] }))),

      beer: this.dietService
        .getBeerRange(from, to)
        .pipe(catchError(() => of({ from, to, days: [] as string[] } as BeerRange)))
    }).subscribe({
      next: ({ eating, exercise, beer }) => {
        this.exerciseByDate = new Map(exercise.days.map(day => [day.date, day]));
        this.beerByDate = new Set(beer.days);
        this.months = this.groupIntoMonths(eating.days);
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

  /**
   * Whether the date has recorded activity. Deliberately independent of the eating state: a day
   * can be on target and active, over target and active, or not logged and active, and the
   * calendar shows both facts rather than letting one hide the other (FR-021, R-009).
   */
  hasExercise(day: DaySummary): boolean {
    return this.exerciseByDate.has(day.date);
  }

  /** Whether the date is marked as a beer day. Independent of the eating state (FR-004). */
  isBeer(day: DaySummary): boolean {
    return this.beerByDate.has(day.date);
  }

  /** The tooltip, so a marking is readable and not only visible. */
  dayTitle(day: DaySummary): string {
    if (!day.withinPlan) {
      return `${day.date} - before your plan started`;
    }

    const exercise = this.exerciseByDate.get(day.date);
    const activity = exercise ? ` - ${exercise.totalMinutes} min of exercise` : '';
    const beer = this.isBeer(day) ? ' - beer day' : '';

    return `${day.date} - ${day.state}${activity}${beer}`;
  }

  /** A within-plan day opens the popover; out-of-plan days are inert, as before. */
  selectDay(day: DaySummary): void {
    if (day.withinPlan) {
      this.selectedDay = day;
      this.activityDirty = false;
    }
  }

  /** The embedded activity log logged, edited or removed a session on the open day. */
  onActivityChanged(): void {
    this.activityDirty = true;

    // "Save & close" was waiting on this commit to land before closing.
    if (this.closeAfterSave) {
      this.closeAfterSave = false;
      this.closePopover();
    }
  }

  /**
   * Commit anything still sitting unsaved in the activity form, then close. If there is nothing
   * pending, this is just a close; if a commit is in flight, the close waits for it (and stays
   * open, showing the error, if it is refused).
   */
  saveAndClose(): void {
    if (this.savingBeer) {
      return;
    }

    if (this.activityLog?.hasPendingInput) {
      this.closeAfterSave = true;
      this.activityLog.commitPending();
      return;
    }

    this.closePopover();
  }

  closePopover(): void {
    this.selectedDay = null;
    this.closeAfterSave = false;

    // A session was added or removed while the popover was open - refresh so the day's exercise
    // bar reflects it. Beer is already kept in step locally by toggleBeer.
    if (this.activityDirty) {
      this.activityDirty = false;
      this.load();
    }
  }

  openDay(day: DaySummary): void {
    if (day.withinPlan) {
      this.router.navigate(['/diet/log', day.date]);
    }
  }

  /** Mark or unmark the open day as a beer day, then reflect it in the merged set (FR-002, SC-001). */
  toggleBeer(day: DaySummary): void {
    if (this.savingBeer) {
      return;
    }

    this.savingBeer = true;
    const wasBeer = this.isBeer(day);
    const request = wasBeer
      ? this.dietService.unmarkBeerDay(day.date)
      : this.dietService.markBeerDay(day.date);

    request.subscribe({
      next: () => {
        if (wasBeer) {
          this.beerByDate.delete(day.date);
        } else {
          this.beerByDate.add(day.date);
        }
        this.savingBeer = false;
      },
      error: err => {
        this.savingBeer = false;
        this.error = err?.error?.message ?? 'Could not update this beer day.';
      }
    });
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
