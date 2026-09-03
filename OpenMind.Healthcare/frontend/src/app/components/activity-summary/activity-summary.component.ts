import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { DietService } from '../../services/diet.service';
import { ActivitySummary } from '../../models/diet.models';

/**
 * How active a member has been this week, against last week.
 *
 * Shows time and active days. It deliberately offers no calorie target, no allowance and no
 * verdict: this page reports what happened, and nothing here is spendable (FR-016, FR-019).
 */
@Component({
  selector: 'app-activity-summary',
  standalone: false,
  templateUrl: './activity-summary.component.html',
  styleUrls: ['./activity-summary.component.css']
})
export class ActivitySummaryComponent implements OnInit {
  summary: ActivitySummary | null = null;
  loading = false;
  error: string | null = null;

  constructor(private dietService: DietService, private router: Router) {}

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading = true;
    this.error = null;

    this.dietService.getActivitySummary().subscribe({
      next: summary => {
        this.summary = summary;
        this.loading = false;
      },
      error: err => {
        this.loading = false;
        if (err?.status === 404) {
          this.router.navigate(['/diet/setup']);
          return;
        }
        this.error = err?.error?.message ?? 'Could not load your activity.';
      }
    });
  }

  /** A week with nothing in it is a plain zero state, not an error and not a blank panel. */
  get hasActivity(): boolean {
    return (this.summary?.activeDays ?? 0) > 0;
  }

  get minutesChange(): number {
    if (!this.summary) {
      return 0;
    }
    return this.summary.totalMinutes - this.summary.previousWindowMinutes;
  }

  get activeDaysChange(): number {
    if (!this.summary) {
      return 0;
    }
    return this.summary.activeDays - this.summary.previousWindowActiveDays;
  }

  /**
   * The comparison in words. Deliberately neutral - a quieter week is a fact about the week, not
   * a failure, and the copy does not scold.
   */
  get comparison(): string {
    if (!this.summary) {
      return '';
    }

    if (this.summary.previousWindowMinutes === 0 && this.summary.totalMinutes === 0) {
      return 'Nothing recorded in either of the last two weeks.';
    }

    if (this.summary.previousWindowMinutes === 0) {
      return 'This is your first week with activity recorded.';
    }

    const change = this.minutesChange;

    if (change === 0) {
      return 'The same amount of time as last week.';
    }

    return change > 0
      ? `${change} minutes more than last week.`
      : `${Math.abs(change)} minutes less than last week.`;
  }

  formatDuration(minutes: number): string {
    if (minutes < 60) {
      return `${minutes} min`;
    }

    const hours = Math.floor(minutes / 60);
    const rest = minutes % 60;

    return rest === 0 ? `${hours} h` : `${hours} h ${rest} min`;
  }
}
