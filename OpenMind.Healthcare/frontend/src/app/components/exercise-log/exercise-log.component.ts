import { Component, EventEmitter, Input, OnChanges, Output, SimpleChanges } from '@angular/core';
import { Subject, debounceTime, distinctUntilChanged, switchMap } from 'rxjs';
import { DietService } from '../../services/diet.service';
import {
  ActivityTypeSummary,
  ExerciseDay,
  ExerciseEntry,
  ExerciseShortcut,
  ExerciseShortcutList
} from '../../models/diet.models';

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

  // --- Shortcuts --------------------------------------------------------

  shortcuts: ExerciseShortcutList | null = null;
  tappingId: string | null = null;
  managingShortcuts = false;
  renamingId: string | null = null;
  renameValue = '';
  shortcutNotice = '';

  /**
   * True when the day being viewed cannot accept a session at all.
   *
   * The client already knows the date it is showing, so it disables the row rather than offering a
   * tap it knows the server will refuse (FR-013). The server still enforces every rule.
   */
  @Input() canRecord = true;

  /**
   * Fires after any change to the day's activity (a session added, edited, removed, or tapped in
   * from a shortcut). A host that shows this alongside other views of the same day - the calendar,
   * for one - listens so it can refresh its own marking.
   */
  @Output() changed = new EventEmitter<void>();

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
      this.loadShortcuts();
    }
  }

  get entries(): ExerciseEntry[] {
    return this.day?.entries ?? [];
  }

  get hasActivity(): boolean {
    return this.entries.length > 0;
  }

  /** True when there is a valid, not-yet-saved session or edit waiting in the form. */
  get hasPendingInput(): boolean {
    const pendingEdit = !!this.editingId && !!this.editDuration && this.editDuration > 0;
    const pendingAdd = !!this.selected && !!this.durationMinutes && this.durationMinutes > 0;
    return pendingEdit || pendingAdd;
  }

  /**
   * Commit whatever is in the form - the half-filled "add a session" row, or an open edit - as if
   * the member had clicked its own button. A host with its own "save and close" affordance calls
   * this so nothing typed is silently dropped. A `changed` event follows on success.
   */
  commitPending(): void {
    if (this.editingId && this.editDuration && this.editDuration > 0) {
      const entry = this.entries.find(e => e.id === this.editingId);
      if (entry) {
        this.saveEdit(entry);
      }
      return;
    }

    if (this.selected && this.durationMinutes && this.durationMinutes > 0) {
      this.add();
    }
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
          this.changed.emit();
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
          this.changed.emit();
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
        this.changed.emit();
      },
      error: response => {
        this.saving = false;
        this.handleWriteError(response);
      }
    });
  }

  // --- Shortcuts --------------------------------------------------------

  get shortcutList(): ExerciseShortcut[] {
    return this.shortcuts?.shortcuts ?? [];
  }

  get canSaveMoreShortcuts(): boolean {
    return (this.shortcuts?.remainingSlots ?? 0) > 0;
  }

  loadShortcuts(): void {
    this.dietService.getExerciseShortcuts().subscribe({
      next: list => (this.shortcuts = list),
      error: () => (this.shortcuts = null)
    });
  }

  /** The one tap. */
  tap(shortcut: ExerciseShortcut): void {
    if (!shortcut.available || !this.canRecord || this.saving) {
      return;
    }

    this.tappingId = shortcut.id;
    this.error = '';
    this.shortcutNotice = '';

    this.dietService
      .addExerciseEntryFromShortcut(this.date, {
        shortcutId: shortcut.id,
        version: this.day?.version ?? null
      })
      .subscribe({
        next: day => {
          this.day = day;
          this.tappingId = null;
          this.staleWarning = '';
          this.changed.emit();
        },
        error: response => {
          this.tappingId = null;
          this.handleWriteError(response);
        }
      });
  }

  /** Saves a session the member has already recorded as a shortcut, in one action (FR-001). */
  saveAsShortcut(entry: ExerciseEntry): void {
    this.shortcutNotice = '';

    this.dietService
      .createExerciseShortcut({
        activityTypeId: entry.activityTypeId,
        durationMinutes: entry.durationMinutes,
        name: null
      })
      .subscribe({
        next: list => {
          this.shortcuts = list;
          this.shortcutNotice = 'Saved as a shortcut.';
        },
        error: response => {
          // The duplicate and limit messages are the useful part, so they are shown as they came.
          this.shortcutNotice = response?.error?.message ?? 'We could not save that shortcut.';
        }
      });
  }

  toggleManage(): void {
    this.managingShortcuts = !this.managingShortcuts;
    this.renamingId = null;
    this.shortcutNotice = '';
  }

  startRename(shortcut: ExerciseShortcut): void {
    this.renamingId = shortcut.id;
    this.renameValue = shortcut.name;
  }

  cancelRename(): void {
    this.renamingId = null;
    this.renameValue = '';
  }

  saveRename(shortcut: ExerciseShortcut): void {
    if (!this.renameValue.trim()) {
      return;
    }

    this.dietService.renameExerciseShortcut(shortcut.id, this.renameValue.trim()).subscribe({
      next: list => {
        this.shortcuts = list;
        this.cancelRename();
      },
      error: response => {
        this.shortcutNotice = response?.error?.message ?? 'We could not rename that.';
      }
    });
  }

  /** Sends the complete ordered list rather than a move, so two clients cannot interleave. */
  move(shortcut: ExerciseShortcut, direction: -1 | 1): void {
    const ids = this.shortcutList.map(s => s.id);
    const from = ids.indexOf(shortcut.id);
    const to = from + direction;

    if (from < 0 || to < 0 || to >= ids.length) {
      return;
    }

    [ids[from], ids[to]] = [ids[to], ids[from]];

    this.dietService.reorderExerciseShortcuts(ids).subscribe({
      next: list => (this.shortcuts = list),
      error: response => {
        this.shortcutNotice = response?.error?.message ?? 'We could not reorder those.';
      }
    });
  }

  removeShortcut(shortcut: ExerciseShortcut): void {
    this.dietService.deleteExerciseShortcut(shortcut.id).subscribe({
      next: list => {
        this.shortcuts = list;
        this.shortcutNotice = '';
      },
      error: response => {
        this.shortcutNotice = response?.error?.message ?? 'We could not remove that.';
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
