import { Component, Input, OnChanges, SimpleChanges } from '@angular/core';
import { Subject, debounceTime, distinctUntilChanged, switchMap } from 'rxjs';
import { DietService } from '../../services/diet.service';
import { ActivityTypeSummary, ExerciseDay, ExerciseEntry } from '../../models/diet.models';

/**
 * Recording and correcting a day's activity.
 *
 * Shows an estimate beside every session and says plainly that it is one. Deliberately shows no
 * calorie target, no remaining allowance and no verdict: exercise is a record of what a member
 * did, not a budget they can spend against (FR-016, FR-019).
 */
@Component({
  selector: 'app-exercise-log',
  standalone: false,
  templateUrl: './exercise-log.component.html',
  styleUrls: ['./exercise-log.component.css']
})
export class ExerciseLogComponent implements OnChanges {
  /** The date being shown, as `yyyy-MM-dd`. */
  @Input() date = '';

  day: ExerciseDay | null = null;
  loading = false;
  error = '';
  /** Set when a write was refused because the day changed elsewhere. */
  staleWarning = '';

  query = '';
  matches: ActivityTypeSummary[] = [];
  searched = false;
  searching = false;

  selected: ActivityTypeSummary | null = null;
  durationMinutes: number | null = null;
  saving = false;

  editingId: string | null = null;
  editDuration: number | null = null;

  private readonly terms = new Subject<string>();

  constructor(private dietService: DietService) {
    this.terms
      .pipe(
        debounceTime(250),
        distinctUntilChanged(),
        switchMap(term => {
          this.searching = true;
          return this.dietService.searchActivities(term);
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

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['date'] && this.date) {
      this.load();
    }
  }

  get entries(): ExerciseEntry[] {
    return this.day?.entries ?? [];
  }

  get hasActivity(): boolean {
    return this.entries.length > 0;
  }

  /**
   * The day total is only worth showing once there is something to total. With a single session
   * it repeats that session's own figures word for word, two lines apart.
   */
  get showDayTotal(): boolean {
    return this.entries.length > 1;
  }

  load(): void {
    this.loading = true;
    this.error = '';

    this.dietService.getExerciseDay(this.date).subscribe({
      next: day => {
        this.day = day;
        this.loading = false;
        this.staleWarning = '';
      },
      error: () => {
        this.day = null;
        this.loading = false;
        this.error = 'We could not load this day of activity.';
      }
    });
  }

  onQueryChanged(): void {
    this.selected = null;

    if (this.query.trim().length < 2) {
      this.matches = [];
      this.searched = false;
      return;
    }

    this.terms.next(this.query.trim());
  }

  select(activity: ActivityTypeSummary): void {
    this.selected = activity;
    this.durationMinutes = null;
  }

  resetSearch(): void {
    this.query = '';
    this.matches = [];
    this.searched = false;
    this.selected = null;
    this.durationMinutes = null;
  }

  add(): void {
    if (!this.selected || !this.durationMinutes || this.durationMinutes <= 0) {
      return;
    }

    this.saving = true;
    this.error = '';

    this.dietService
      .addExerciseEntry(this.date, {
        activityTypeId: this.selected.id,
        durationMinutes: Math.round(this.durationMinutes),
        version: this.day?.version ?? null
      })
      .subscribe({
        next: day => {
          this.day = day;
          this.saving = false;
          this.staleWarning = '';
          this.resetSearch();
        },
        error: response => {
          this.saving = false;
          this.handleWriteError(response);
        }
      });
  }

  startEdit(entry: ExerciseEntry): void {
    this.editingId = entry.id;
    this.editDuration = entry.durationMinutes;
  }

  cancelEdit(): void {
    this.editingId = null;
    this.editDuration = null;
  }

  saveEdit(entry: ExerciseEntry): void {
    if (!this.day?.version || !this.editDuration || this.editDuration <= 0) {
      return;
    }

    this.saving = true;
    this.error = '';

    this.dietService
      .updateExerciseEntry(entry.id, {
        activityTypeId: entry.activityTypeId,
        durationMinutes: Math.round(this.editDuration),
        version: this.day.version
      })
      .subscribe({
        next: day => {
          this.day = day;
          this.saving = false;
          this.staleWarning = '';
          this.cancelEdit();
        },
        error: response => {
          this.saving = false;
          this.handleWriteError(response);
        }
      });
  }

  remove(entry: ExerciseEntry): void {
    if (!this.day?.version) {
      return;
    }

    this.saving = true;
    this.error = '';

    this.dietService.deleteExerciseEntry(entry.id, this.day.version).subscribe({
      next: day => {
        // No day left once its last session goes - the date reverts to no exercise recorded,
        // not a zero-minute session.
        this.day = day ?? { date: this.date, version: null, totalMinutes: 0, totalKilocalories: 0, entries: [] };
        this.saving = false;
        this.staleWarning = '';
        this.cancelEdit();
      },
      error: response => {
        this.saving = false;
        this.handleWriteError(response);
      }
    });
  }

  /**
   * A 409 is not a failure the member caused - their copy is simply out of date. It gets a
   * reload prompt rather than an error, and nothing they entered elsewhere is lost.
   */
  private handleWriteError(response: { status?: number; error?: { message?: string } }): void {
    if (response?.status === 409) {
      this.staleWarning =
        response.error?.message ?? 'This day changed somewhere else. Reload to see the latest activity.';
      return;
    }

    this.error = response?.error?.message ?? 'We could not save that. Please try again.';
  }
}
