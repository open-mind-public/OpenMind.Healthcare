import { Component, HostListener, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { ApiService } from '../../services/api.service';
import {
  UserProgress,
  Achievement,
  ProgressStats,
  SmokedDay,
  RelapseTrigger,
  RELAPSE_TRIGGERS
} from '../../models/models';

type DayStatus = 'before-quit' | 'smoke-free' | 'smoked' | 'future';

interface CalendarDay {
  date: Date;
  dateKey: string;
  dayNumber: number;
  isCurrentMonth: boolean;
  isToday: boolean;
  isQuitDay: boolean;
  daysSinceQuit: number;
  status: DayStatus;
  achievements: Achievement[];
  moneySaved: number;
  cigarettesAvoided: number;
  smokedDay: SmokedDay | null;
}

interface CalendarWeek {
  days: CalendarDay[];
}

type CalendarViewMode = 'month' | 'year';

interface MiniMonth {
  name: string;
  monthIndex: number;
  /** Leading nulls pad the first week so weekday columns line up. */
  cells: (CalendarDay | null)[];
  smokeFreeDays: number;
  smokedDays: number;
}

const MONTH_NAMES = [
  'January', 'February', 'March', 'April', 'May', 'June',
  'July', 'August', 'September', 'October', 'November', 'December'
];

@Component({
  selector: 'app-progress-calendar',
  standalone: false,
  template: `
    <div class="calendar-container fade-in">
      <div class="calendar-header">
        <h1>📅 Progress Calendar</h1>
        <p class="subtitle">Track your smoke-free journey day by day</p>
      </div>

      <div class="quick-mark" *ngIf="progress">
        <button class="btn btn-danger" (click)="openMarkDialogForToday()">
          🚬 I smoked today
        </button>
        <button class="btn btn-ghost" routerLink="/analytics">
          📈 Relapse analytics
        </button>
      </div>

      <div class="view-toggle" *ngIf="progress">
        <button
          type="button"
          [class.active]="viewMode === 'month'"
          (click)="setViewMode('month')">
          Month
        </button>
        <button
          type="button"
          [class.active]="viewMode === 'year'"
          (click)="setViewMode('year')">
          Year
        </button>
      </div>

      <div class="calendar-card" *ngIf="progress && viewMode === 'month'">
        <!-- Month Navigation -->
        <div class="month-navigation">
          <button class="nav-btn" (click)="previousMonth()">
            <span>←</span>
          </button>
          <h2 class="current-month">{{ currentDate | date:'MMMM yyyy' }}</h2>
          <button class="nav-btn" (click)="nextMonth()">
            <span>→</span>
          </button>
        </div>

        <!-- Weekday Headers -->
        <div class="weekday-headers">
          <div class="weekday" *ngFor="let day of weekDays">{{ day }}</div>
        </div>

        <!-- Calendar Grid -->
        <div class="calendar-grid">
          <div
            *ngFor="let week of calendarWeeks"
            class="calendar-week">
            <div
              *ngFor="let day of week.days"
              class="calendar-day"
              [class.other-month]="!day.isCurrentMonth"
              [class.today]="day.isToday"
              [class.quit-day]="day.isQuitDay"
              [class.smoke-free]="day.status === 'smoke-free'"
              [class.smoked]="day.status === 'smoked'"
              [class.before-quit]="day.status === 'before-quit'"
              [class.future]="day.status === 'future'"
              [class.selected]="selectedDay?.dateKey === day.dateKey && day.isCurrentMonth"
              [class.highlighted]="highlightedKey === day.dateKey && day.isCurrentMonth"
              [attr.title]="dayTooltip(day)"
              (click)="selectDay(day)">
              <span class="day-number">{{ day.dayNumber }}</span>
              <div class="day-indicators" *ngIf="day.isCurrentMonth && day.status === 'smoke-free'">
                <span class="streak-badge" *ngIf="day.daysSinceQuit > 0">
                  Day {{ day.daysSinceQuit }}
                </span>
                <span class="achievement-indicator" *ngIf="day.achievements.length > 0">
                  🏆
                </span>
              </div>
              <div class="day-indicators" *ngIf="day.isCurrentMonth && day.status === 'smoked' && day.smokedDay as smoked">
                <span class="smoked-badge">Smoked</span>
                <span class="smoked-count">
                  {{ smoked.cigarettesSmoked }} {{ smoked.cigarettesSmoked === 1 ? 'cig' : 'cigs' }}
                </span>
              </div>
              <div class="quit-badge" *ngIf="day.isQuitDay">
                🚭
              </div>
              <div class="smoked-mark" *ngIf="day.status === 'smoked'">✖</div>
            </div>
          </div>
        </div>

        <!-- Legend -->
        <div class="calendar-legend">
          <div class="legend-item">
            <span class="legend-color quit-day"></span>
            <span>Quit Day</span>
          </div>
          <div class="legend-item">
            <span class="legend-color smoke-free"></span>
            <span>Smoke-Free</span>
          </div>
          <div class="legend-item">
            <span class="legend-color smoked"></span>
            <span>Smoked (excluded)</span>
          </div>
          <div class="legend-item">
            <span class="legend-icon">🏆</span>
            <span>Achievement</span>
          </div>
          <div class="legend-item">
            <span class="legend-color today"></span>
            <span>Today</span>
          </div>
        </div>
      </div>

      <!-- Year View -->
      <div class="calendar-card year-card" *ngIf="progress && viewMode === 'year'">
        <div class="month-navigation">
          <button class="nav-btn" (click)="previousYear()">
            <span>←</span>
          </button>
          <h2 class="current-month">{{ currentDate.getFullYear() }}</h2>
          <button class="nav-btn" (click)="nextYear()">
            <span>→</span>
          </button>
        </div>

        <div class="year-grid">
          <div class="mini-month" *ngFor="let m of yearMonths">
            <div class="mini-month-header">
              <span class="mini-month-name">{{ m.name }}</span>
              <span class="mini-month-stats">
                <span class="mm-free">{{ m.smokeFreeDays }}</span>
                <span class="mm-smoked" *ngIf="m.smokedDays > 0">· {{ m.smokedDays }} 🚬</span>
              </span>
            </div>
            <div class="mini-weekdays">
              <span *ngFor="let w of weekDayInitials">{{ w }}</span>
            </div>
            <div class="mini-grid">
              <ng-container *ngFor="let cell of m.cells">
                <span class="mini-cell empty" *ngIf="!cell"></span>
                <button
                  type="button"
                  *ngIf="cell as day"
                  class="mini-cell"
                  [class.smoke-free]="day.status === 'smoke-free'"
                  [class.smoked]="day.status === 'smoked'"
                  [class.before-quit]="day.status === 'before-quit'"
                  [class.future]="day.status === 'future'"
                  [class.quit-day]="day.isQuitDay"
                  [class.today]="day.isToday"
                  [class.highlighted]="highlightedKey === day.dateKey"
                  [disabled]="!isTrackable(day)"
                  [attr.title]="dayTooltip(day) || (day.date | date:'mediumDate')"
                  (click)="selectDay(day)">
                  {{ day.dayNumber }}
                </button>
              </ng-container>
            </div>
          </div>
        </div>

        <!-- Legend -->
        <div class="calendar-legend">
          <div class="legend-item">
            <span class="legend-color quit-day"></span>
            <span>Quit Day</span>
          </div>
          <div class="legend-item">
            <span class="legend-color smoke-free"></span>
            <span>Smoke-Free</span>
          </div>
          <div class="legend-item">
            <span class="legend-color smoked"></span>
            <span>Smoked (excluded)</span>
          </div>
          <div class="legend-item">
            <span class="legend-color today"></span>
            <span>Today</span>
          </div>
        </div>
      </div>

      <!-- Selected Day Details - opens as a popup when a day is clicked -->
      <div
        class="modal-backdrop"
        *ngIf="selectedDay && isTrackable(selectedDay) && !markDialogDay"
        (click)="closeDayDetails()">
        <div
          class="modal details-card"
          [class.relapse]="selectedDay.status === 'smoked'"
          role="dialog"
          aria-modal="true"
          [attr.aria-label]="'Details for ' + (selectedDay.date | date:'fullDate')"
          (click)="$event.stopPropagation()">
          <button class="modal-close" type="button" aria-label="Close" (click)="closeDayDetails()">✕</button>
          <h3>{{ selectedDay.date | date:'EEEE, MMMM d, yyyy' }}</h3>

          <ng-container *ngIf="selectedDay.status === 'smoke-free'">
            <div class="details-grid">
              <div class="detail-item">
                <span class="detail-icon">📆</span>
                <span class="detail-value">Day {{ selectedDay.daysSinceQuit }}</span>
                <span class="detail-label">of your journey</span>
              </div>
              <div class="detail-item">
                <span class="detail-icon">🚬</span>
                <span class="detail-value">{{ selectedDay.cigarettesAvoided }}</span>
                <span class="detail-label">cigarettes avoided</span>
              </div>
              <div class="detail-item">
                <span class="detail-icon">💰</span>
                <span class="detail-value">{{ formatDayMoney(selectedDay.moneySaved) }}</span>
                <span class="detail-label">money saved</span>
              </div>
            </div>
          </ng-container>

          <ng-container *ngIf="selectedDay.status === 'smoked' && selectedDay.smokedDay as smoked">
            <div class="relapse-banner">
              This day is marked as smoked and is excluded from your smoke-free total.
            </div>
            <div class="details-grid">
              <div class="detail-item">
                <span class="detail-icon">🚬</span>
                <span class="detail-value">{{ smoked.cigarettesSmoked }}</span>
                <span class="detail-label">cigarettes smoked</span>
              </div>
              <div class="detail-item">
                <span class="detail-icon">{{ triggerIcon(smoked.trigger) }}</span>
                <span class="detail-value">{{ triggerLabel(smoked.trigger) }}</span>
                <span class="detail-label">trigger</span>
              </div>
              <div class="detail-item">
                <span class="detail-icon">💸</span>
                <span class="detail-value">{{ formatDayMoney(smoked.moneySpent) }}</span>
                <span class="detail-label">money spent</span>
              </div>
            </div>
            <p class="relapse-note" *ngIf="smoked.note">“{{ smoked.note }}”</p>
          </ng-container>

          <div class="day-actions">
            <button class="btn btn-danger" (click)="openMarkDialog(selectedDay)">
              {{ selectedDay.status === 'smoked' ? '✏️ Edit relapse' : '🚬 Mark as smoked' }}
            </button>
            <button
              class="btn btn-ghost"
              *ngIf="selectedDay.status === 'smoked'"
              (click)="unmark(selectedDay)"
              [disabled]="saving">
              ↩️ Unmark this day
            </button>
          </div>

          <div class="achievements-section" *ngIf="selectedDay.achievements.length > 0">
            <h4>🏆 Achievements Unlocked</h4>
            <div class="achievement-list">
              <div class="achievement-item" *ngFor="let achievement of selectedDay.achievements">
                <span class="achievement-icon">{{ achievement.icon }}</span>
                <div class="achievement-info">
                  <span class="achievement-name">{{ achievement.name }}</span>
                  <span class="achievement-desc">{{ achievement.description }}</span>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>

      <!-- Stats Summary -->
      <div class="stats-summary" *ngIf="stats">
        <div class="summary-card">
          <h3>📊 {{ viewMode === 'year' ? 'Yearly' : 'Monthly' }} Summary</h3>
          <div class="summary-grid">
            <div class="summary-item">
              <span class="summary-value">{{ viewMode === 'year' ? getSmokeFreeThisYear() : getSmokeFreeThisMonth() }}</span>
              <span class="summary-label">Smoke-free days this {{ viewMode === 'year' ? 'year' : 'month' }}</span>
            </div>
            <div class="summary-item">
              <span class="summary-value danger">{{ viewMode === 'year' ? getSmokedThisYear() : getSmokedThisMonth() }}</span>
              <span class="summary-label">Smoked days this {{ viewMode === 'year' ? 'year' : 'month' }}</span>
            </div>
            <div class="summary-item">
              <span class="summary-value">{{ stats.daysSmokeFree }}</span>
              <span class="summary-label">Total days smoke-free</span>
            </div>
            <div class="summary-item">
              <span class="summary-value">{{ stats.currentStreak }}</span>
              <span class="summary-label">Current streak</span>
            </div>
          </div>
        </div>
      </div>

      <!-- No Progress Message -->
      <div class="no-progress" *ngIf="!progress">
        <div class="empty-state">
          <span class="empty-icon">📅</span>
          <h2>Start Your Journey</h2>
          <p>Set up your quit date to start tracking your progress on the calendar.</p>
          <button class="btn btn-primary" routerLink="/setup">Get Started</button>
        </div>
      </div>

      <!-- Mark-as-smoked dialog -->
      <div class="modal-backdrop" *ngIf="markDialogDay" (click)="closeMarkDialog()">
        <div class="modal" (click)="$event.stopPropagation()">
          <h3>🚬 Mark {{ markDialogDay.date | date:'MMM d, yyyy' }} as smoked</h3>
          <p class="modal-hint">
            Honesty keeps your numbers real - this day will be excluded from your smoke-free total.
          </p>

          <label class="field">
            <span class="field-label">How many cigarettes?</span>
            <input type="number" min="1" max="200" [(ngModel)]="form.cigarettesSmoked" class="field-input">
          </label>

          <div class="field">
            <span class="field-label">What triggered it?</span>
            <div class="trigger-grid">
              <button
                type="button"
                class="trigger-chip"
                *ngFor="let option of triggers"
                [class.active]="form.trigger === option.value"
                (click)="form.trigger = option.value">
                <span class="trigger-icon">{{ option.icon }}</span>
                <span>{{ option.label }}</span>
              </button>
            </div>
          </div>

          <label class="field">
            <span class="field-label">Note (optional)</span>
            <textarea
              rows="2"
              maxlength="500"
              class="field-input"
              placeholder="What happened? What will you do differently?"
              [(ngModel)]="form.note"></textarea>
          </label>

          <p class="modal-error" *ngIf="error">{{ error }}</p>

          <div class="modal-actions">
            <button class="btn btn-ghost" (click)="closeMarkDialog()" [disabled]="saving">Cancel</button>
            <button class="btn btn-danger" (click)="saveMark()" [disabled]="saving">
              {{ saving ? 'Saving...' : 'Save' }}
            </button>
          </div>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .calendar-container {
      padding: 20px;
      max-width: 900px;
      margin: 0 auto;
    }

    .calendar-header {
      text-align: center;
      margin-bottom: 20px;

      h1 {
        font-size: 2.5rem;
        margin-bottom: 10px;
        background: linear-gradient(135deg, #10b981, #34d399);
        -webkit-background-clip: text;
        -webkit-text-fill-color: transparent;
      }

      .subtitle {
        color: rgba(255, 255, 255, 0.7);
        font-size: 1.1rem;
      }
    }

    .quick-mark {
      display: flex;
      justify-content: center;
      gap: 12px;
      margin-bottom: 25px;
      flex-wrap: wrap;
    }

    .view-toggle {
      display: flex;
      justify-content: center;
      gap: 4px;
      margin-bottom: 20px;
      padding: 4px;
      background: rgba(255, 255, 255, 0.05);
      border: 1px solid rgba(255, 255, 255, 0.1);
      border-radius: 999px;
      width: fit-content;
      margin-left: auto;
      margin-right: auto;

      button {
        border: none;
        background: transparent;
        color: rgba(255, 255, 255, 0.7);
        padding: 8px 22px;
        border-radius: 999px;
        font-size: 0.9rem;
        font-weight: 600;
        cursor: pointer;
        transition: all 0.25s ease;

        &:hover {
          color: white;
        }

        &.active {
          background: linear-gradient(135deg, #10b981, #059669);
          color: white;
        }
      }
    }

    .year-grid {
      display: grid;
      grid-template-columns: repeat(auto-fill, minmax(220px, 1fr));
      gap: 18px;
    }

    .mini-month {
      background: rgba(255, 255, 255, 0.03);
      border: 1px solid rgba(255, 255, 255, 0.08);
      border-radius: 14px;
      padding: 12px;
    }

    .mini-month-header {
      display: flex;
      justify-content: space-between;
      align-items: baseline;
      margin-bottom: 8px;

      .mini-month-name {
        font-weight: 600;
        color: white;
        font-size: 0.95rem;
      }

      .mini-month-stats {
        font-size: 0.72rem;
        color: rgba(255, 255, 255, 0.55);

        .mm-free { color: #34d399; font-weight: 600; }
        .mm-smoked { color: #f87171; margin-left: 3px; }
      }
    }

    .mini-weekdays {
      display: grid;
      grid-template-columns: repeat(7, 1fr);
      gap: 2px;
      margin-bottom: 3px;

      span {
        text-align: center;
        font-size: 0.6rem;
        color: rgba(255, 255, 255, 0.4);
      }
    }

    .mini-grid {
      display: grid;
      grid-template-columns: repeat(7, 1fr);
      gap: 2px;
    }

    .mini-cell {
      aspect-ratio: 1;
      display: flex;
      align-items: center;
      justify-content: center;
      border: none;
      border-radius: 5px;
      background: rgba(255, 255, 255, 0.03);
      color: rgba(255, 255, 255, 0.55);
      font-size: 0.62rem;
      font-family: inherit;
      padding: 0;
      cursor: pointer;
      transition: transform 0.15s ease, background 0.15s ease;

      &.empty {
        background: transparent;
        cursor: default;
      }

      &:not(.empty):not(:disabled):hover {
        transform: scale(1.15);
        z-index: 1;
      }

      &:disabled {
        cursor: default;
      }

      &.before-quit,
      &.future {
        color: rgba(255, 255, 255, 0.25);
      }

      &.smoke-free {
        background: rgba(16, 185, 129, 0.28);
        color: #d1fae5;
      }

      &.smoked {
        background: rgba(239, 68, 68, 0.45);
        color: #fee2e2;
        font-weight: 700;
      }

      &.quit-day {
        background: linear-gradient(135deg, #10b981, #059669);
        color: white;
        font-weight: 700;
      }

      &.today {
        outline: 2px solid #10b981;
        outline-offset: 1px;
      }

      &.highlighted {
        outline: 2px solid #fbbf24;
        outline-offset: 2px;
      }
    }

    .calendar-card {
      background: rgba(255, 255, 255, 0.05);
      border-radius: 20px;
      padding: 25px;
      border: 1px solid rgba(255, 255, 255, 0.1);
    }

    .month-navigation {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-bottom: 25px;

      .nav-btn {
        background: rgba(255, 255, 255, 0.1);
        border: none;
        width: 40px;
        height: 40px;
        border-radius: 50%;
        cursor: pointer;
        transition: all 0.3s ease;
        color: white;
        font-size: 1.2rem;

        &:hover {
          background: rgba(16, 185, 129, 0.3);
          transform: scale(1.1);
        }
      }

      .current-month {
        font-size: 1.5rem;
        font-weight: 600;
        color: white;
      }
    }

    .weekday-headers {
      display: grid;
      grid-template-columns: repeat(7, 1fr);
      gap: 5px;
      margin-bottom: 10px;

      .weekday {
        text-align: center;
        padding: 10px;
        font-weight: 600;
        color: rgba(255, 255, 255, 0.6);
        font-size: 0.85rem;
      }
    }

    .calendar-grid {
      display: flex;
      flex-direction: column;
      gap: 5px;
    }

    .calendar-week {
      display: grid;
      grid-template-columns: repeat(7, 1fr);
      gap: 5px;
    }

    .calendar-day {
      aspect-ratio: 1;
      display: flex;
      flex-direction: column;
      align-items: center;
      justify-content: center;
      border-radius: 12px;
      cursor: pointer;
      transition: all 0.3s ease;
      position: relative;
      background: rgba(255, 255, 255, 0.03);
      min-height: 70px;

      .day-number {
        font-size: 1rem;
        font-weight: 500;
        color: white;
      }

      &.other-month {
        opacity: 0.3;

        .day-number {
          color: rgba(255, 255, 255, 0.4);
        }
      }

      &.today {
        border: 2px solid #10b981;
        box-shadow: 0 0 15px rgba(16, 185, 129, 0.3);
      }

      &.selected {
        outline: 2px solid rgba(255, 255, 255, 0.6);
        outline-offset: 2px;
      }

      &.highlighted {
        outline: 3px solid #fbbf24;
        outline-offset: 3px;
        position: relative;
        z-index: 2;
        animation: highlightPulse 1.4s ease-out 3;
      }

      &.quit-day {
        background: linear-gradient(135deg, #10b981, #059669);

        .day-number {
          font-weight: 700;
        }
      }

      &.smoke-free {
        background: rgba(16, 185, 129, 0.2);

        &:hover {
          background: rgba(16, 185, 129, 0.4);
          transform: scale(1.05);
        }
      }

      &.smoked {
        background: rgba(239, 68, 68, 0.25);
        border: 1px solid rgba(239, 68, 68, 0.45);

        &:hover {
          background: rgba(239, 68, 68, 0.4);
          transform: scale(1.05);
        }
      }

      &.before-quit {
        background: rgba(255, 255, 255, 0.02);
      }

      &.future {
        background: rgba(255, 255, 255, 0.02);
        opacity: 0.5;
        cursor: default;
      }

      .day-indicators {
        display: flex;
        flex-direction: column;
        align-items: center;
        gap: 2px;
        margin-top: 2px;
      }

      .streak-badge {
        font-size: 0.6rem;
        color: #10b981;
        font-weight: 600;
      }

      .smoked-badge {
        font-size: 0.58rem;
        color: #fca5a5;
        font-weight: 700;
        text-transform: uppercase;
        letter-spacing: 0.04em;
      }

      .smoked-count {
        font-size: 0.58rem;
        color: rgba(255, 255, 255, 0.6);
        font-weight: 500;
      }

      .achievement-indicator {
        font-size: 0.8rem;
      }

      .quit-badge {
        position: absolute;
        top: 5px;
        right: 5px;
        font-size: 0.8rem;
      }

      .smoked-mark {
        position: absolute;
        top: 4px;
        right: 6px;
        font-size: 0.7rem;
        color: #ef4444;
        font-weight: 700;
      }
    }

    .calendar-legend {
      display: flex;
      justify-content: center;
      gap: 25px;
      margin-top: 25px;
      padding-top: 20px;
      border-top: 1px solid rgba(255, 255, 255, 0.1);
      flex-wrap: wrap;

      .legend-item {
        display: flex;
        align-items: center;
        gap: 8px;
        font-size: 0.85rem;
        color: rgba(255, 255, 255, 0.7);
      }

      .legend-color {
        width: 16px;
        height: 16px;
        border-radius: 4px;

        &.quit-day {
          background: linear-gradient(135deg, #10b981, #059669);
        }

        &.smoke-free {
          background: rgba(16, 185, 129, 0.4);
        }

        &.smoked {
          background: rgba(239, 68, 68, 0.5);
        }

        &.today {
          border: 2px solid #10b981;
          background: transparent;
        }
      }

      .legend-icon {
        font-size: 1rem;
      }
    }

    .modal.details-card {
      position: relative;
      max-width: 560px;
      border-color: rgba(16, 185, 129, 0.35);

      &.relapse {
        border-color: rgba(239, 68, 68, 0.35);

        h3 {
          color: #fca5a5;
        }
      }

      h3 {
        text-align: center;
        margin: 0 30px 20px;
        color: #10b981;
        font-size: 1.15rem;
      }

      .modal-close {
        position: absolute;
        top: 16px;
        right: 16px;
        width: 30px;
        height: 30px;
        border-radius: 50%;
        border: none;
        background: rgba(255, 255, 255, 0.08);
        color: rgba(255, 255, 255, 0.7);
        font-size: 0.85rem;
        cursor: pointer;
        transition: all 0.2s ease;

        &:hover {
          background: rgba(255, 255, 255, 0.18);
          color: white;
        }
      }

      .relapse-banner {
        text-align: center;
        font-size: 0.9rem;
        color: rgba(255, 255, 255, 0.75);
        margin-bottom: 18px;
      }

      .relapse-note {
        margin-top: 15px;
        text-align: center;
        font-style: italic;
        color: rgba(255, 255, 255, 0.7);
      }

      .details-grid {
        display: grid;
        grid-template-columns: repeat(3, 1fr);
        gap: 20px;
        margin-bottom: 20px;
      }

      .detail-item {
        display: flex;
        flex-direction: column;
        align-items: center;
        text-align: center;
        padding: 15px;
        background: rgba(255, 255, 255, 0.03);
        border-radius: 12px;

        .detail-icon {
          font-size: 1.5rem;
          margin-bottom: 8px;
        }

        .detail-value {
          font-size: 1.3rem;
          font-weight: 700;
          color: white;
        }

        .detail-label {
          font-size: 0.8rem;
          color: rgba(255, 255, 255, 0.6);
          margin-top: 4px;
        }
      }

      .day-actions {
        display: flex;
        justify-content: center;
        gap: 12px;
        flex-wrap: wrap;
      }

      .achievements-section {
        margin-top: 20px;
        padding-top: 20px;
        border-top: 1px solid rgba(255, 255, 255, 0.1);

        h4 {
          margin-bottom: 15px;
          color: #fbbf24;
        }
      }

      .achievement-list {
        display: flex;
        flex-direction: column;
        gap: 12px;
      }

      .achievement-item {
        display: flex;
        align-items: center;
        gap: 15px;
        padding: 12px;
        background: rgba(251, 191, 36, 0.1);
        border-radius: 10px;
        border: 1px solid rgba(251, 191, 36, 0.2);

        .achievement-icon {
          font-size: 2rem;
        }

        .achievement-info {
          display: flex;
          flex-direction: column;

          .achievement-name {
            font-weight: 600;
            color: white;
          }

          .achievement-desc {
            font-size: 0.85rem;
            color: rgba(255, 255, 255, 0.6);
          }
        }
      }
    }

    .stats-summary {
      margin-top: 25px;

      .summary-card {
        background: rgba(255, 255, 255, 0.05);
        border-radius: 16px;
        padding: 25px;
        border: 1px solid rgba(255, 255, 255, 0.1);

        h3 {
          text-align: center;
          margin-bottom: 20px;
          color: white;
        }
      }

      .summary-grid {
        display: grid;
        grid-template-columns: repeat(4, 1fr);
        gap: 15px;
      }

      .summary-item {
        display: flex;
        flex-direction: column;
        align-items: center;
        text-align: center;
        padding: 15px;
        background: rgba(255, 255, 255, 0.03);
        border-radius: 12px;

        .summary-value {
          font-size: 1.5rem;
          font-weight: 700;
          color: #10b981;

          &.danger {
            color: #f87171;
          }
        }

        .summary-label {
          font-size: 0.8rem;
          color: rgba(255, 255, 255, 0.6);
          margin-top: 5px;
        }
      }
    }

    .no-progress {
      .empty-state {
        text-align: center;
        padding: 60px 20px;
        background: rgba(255, 255, 255, 0.05);
        border-radius: 20px;
        border: 1px solid rgba(255, 255, 255, 0.1);

        .empty-icon {
          font-size: 4rem;
          display: block;
          margin-bottom: 20px;
        }

        h2 {
          margin-bottom: 10px;
          color: white;
        }

        p {
          color: rgba(255, 255, 255, 0.6);
          margin-bottom: 25px;
        }
      }
    }

    .modal-backdrop {
      position: fixed;
      inset: 0;
      background: rgba(0, 0, 0, 0.6);
      backdrop-filter: blur(4px);
      display: flex;
      align-items: center;
      justify-content: center;
      padding: 20px;
      z-index: 100;
    }

    .modal {
      background: #111827;
      border: 1px solid rgba(239, 68, 68, 0.3);
      border-radius: 18px;
      padding: 25px;
      width: 100%;
      max-width: 460px;
      max-height: 90vh;
      overflow-y: auto;

      h3 {
        color: white;
        margin-bottom: 8px;
      }

      .modal-hint {
        color: rgba(255, 255, 255, 0.6);
        font-size: 0.85rem;
        margin-bottom: 20px;
      }

      .modal-error {
        color: #f87171;
        font-size: 0.85rem;
        margin-bottom: 12px;
      }

      .modal-actions {
        display: flex;
        justify-content: flex-end;
        gap: 10px;
        margin-top: 20px;
      }
    }

    .field {
      display: block;
      margin-bottom: 18px;

      .field-label {
        display: block;
        font-size: 0.85rem;
        color: rgba(255, 255, 255, 0.7);
        margin-bottom: 8px;
      }

      .field-input {
        width: 100%;
        padding: 10px 12px;
        border-radius: 10px;
        border: 1px solid rgba(255, 255, 255, 0.15);
        background: rgba(255, 255, 255, 0.05);
        color: white;
        font-size: 0.95rem;
        font-family: inherit;

        &:focus {
          outline: none;
          border-color: rgba(239, 68, 68, 0.6);
        }
      }
    }

    .trigger-grid {
      display: grid;
      grid-template-columns: repeat(3, 1fr);
      gap: 8px;
    }

    .trigger-chip {
      display: flex;
      flex-direction: column;
      align-items: center;
      gap: 4px;
      padding: 10px 6px;
      border-radius: 10px;
      border: 1px solid rgba(255, 255, 255, 0.12);
      background: rgba(255, 255, 255, 0.04);
      color: rgba(255, 255, 255, 0.8);
      font-size: 0.72rem;
      cursor: pointer;
      transition: all 0.2s ease;

      .trigger-icon {
        font-size: 1.2rem;
      }

      &:hover {
        background: rgba(255, 255, 255, 0.09);
      }

      &.active {
        border-color: #ef4444;
        background: rgba(239, 68, 68, 0.18);
        color: white;
      }
    }

    .btn {
      padding: 12px 24px;
      border: none;
      border-radius: 25px;
      font-size: 0.95rem;
      font-weight: 600;
      cursor: pointer;
      transition: all 0.3s ease;

      &:disabled {
        opacity: 0.5;
        cursor: not-allowed;
      }

      &.btn-primary {
        background: linear-gradient(135deg, #10b981, #059669);
        color: white;

        &:hover:not(:disabled) {
          transform: translateY(-2px);
          box-shadow: 0 10px 30px rgba(16, 185, 129, 0.4);
        }
      }

      &.btn-danger {
        background: linear-gradient(135deg, #ef4444, #dc2626);
        color: white;

        &:hover:not(:disabled) {
          transform: translateY(-2px);
          box-shadow: 0 10px 30px rgba(239, 68, 68, 0.4);
        }
      }

      &.btn-ghost {
        background: rgba(255, 255, 255, 0.08);
        color: white;
        border: 1px solid rgba(255, 255, 255, 0.15);

        &:hover:not(:disabled) {
          background: rgba(255, 255, 255, 0.15);
        }
      }
    }

    .fade-in {
      animation: fadeIn 0.5s ease-out;
    }

    @keyframes highlightPulse {
      0%   { box-shadow: 0 0 0 0 rgba(251, 191, 36, 0.6); }
      70%  { box-shadow: 0 0 0 16px rgba(251, 191, 36, 0); }
      100% { box-shadow: 0 0 0 0 rgba(251, 191, 36, 0); }
    }

    @keyframes fadeIn {
      from {
        opacity: 0;
        transform: translateY(20px);
      }
      to {
        opacity: 1;
        transform: translateY(0);
      }
    }

    @media (max-width: 768px) {
      .calendar-container {
        padding: 15px;
      }

      .calendar-header h1 {
        font-size: 1.8rem;
      }

      .calendar-day {
        min-height: 50px;

        .day-number {
          font-size: 0.85rem;
        }

        .streak-badge,
        .smoked-count {
          display: none;
        }
      }

      .calendar-legend {
        flex-wrap: wrap;
        gap: 15px;
      }

      .details-card .details-grid {
        grid-template-columns: 1fr;
        gap: 12px;
      }

      .stats-summary .summary-grid {
        grid-template-columns: repeat(2, 1fr);
      }

      .year-grid {
        grid-template-columns: repeat(2, 1fr);
        gap: 12px;
      }

      .mini-month {
        padding: 8px;
      }

      .mini-month-header .mini-month-stats {
        display: none;
      }
    }

    @media (max-width: 420px) {
      .year-grid {
        grid-template-columns: 1fr;
      }
    }
  `]
})
export class ProgressCalendarComponent implements OnInit {
  currentDate = new Date();
  progress: UserProgress | null = null;
  stats: ProgressStats | null = null;
  achievements: Achievement[] = [];
  unlockedAchievements: Achievement[] = [];
  calendarWeeks: CalendarWeek[] = [];
  selectedDay: CalendarDay | null = null;
  weekDays = ['Sun', 'Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat'];
  weekDayInitials = ['S', 'M', 'T', 'W', 'T', 'F', 'S'];

  viewMode: CalendarViewMode = 'month';
  yearMonths: MiniMonth[] = [];

  // Smoked ("failed") days keyed by yyyy-MM-dd
  smokedDays = new Map<string, SmokedDay>();

  triggers = RELAPSE_TRIGGERS;
  markDialogDay: CalendarDay | null = null;
  /** Day requested via ?date= - highlighted in the grid so the user can see where it landed. */
  highlightedKey: string | null = null;
  private hasScrolledToHighlight = false;
  form: { cigarettesSmoked: number; trigger: RelapseTrigger; note: string } = {
    cigarettesSmoked: 1,
    trigger: 'Bathroom',
    note: ''
  };
  saving = false;
  error = '';

  constructor(private apiService: ApiService, private route: ActivatedRoute) {}

  ngOnInit(): void {
    // ?date=yyyy-MM-dd deep-links a specific day (the dashboard links the latest failed day here)
    const requested = this.route.snapshot.queryParamMap.get('date');
    if (requested && /^\d{4}-\d{2}-\d{2}$/.test(requested)) {
      const [year, month, day] = requested.split('-').map(Number);
      this.currentDate = new Date(year, month - 1, 1);
      this.highlightedKey = this.toDateKey(new Date(year, month - 1, day));
    }

    this.loadData();
  }

  loadData(): void {
    this.apiService.getProgress().subscribe({
      next: (progress) => {
        this.progress = progress;
        this.loadStats();
        this.loadAchievements();
        this.loadSmokedDays();
      },
      error: () => {
        this.progress = null;
      }
    });
  }

  loadStats(): void {
    this.apiService.getStats().subscribe({
      next: (stats) => {
        this.stats = stats;
        this.generateCalendar();
      }
    });
  }

  loadAchievements(): void {
    this.apiService.getAchievements().subscribe({
      next: (achievements) => {
        this.achievements = achievements;
        this.unlockedAchievements = achievements.filter(a => a.isUnlocked);
        this.generateCalendar();
      }
    });
  }

  loadSmokedDays(): void {
    this.apiService.getSmokedDays().subscribe({
      next: (days) => {
        this.smokedDays = new Map(days.map(d => [d.date, d]));
        this.generateCalendar();
      }
    });
  }

  generateCalendar(): void {
    if (!this.progress) return;

    const year = this.currentDate.getFullYear();
    const month = this.currentDate.getMonth();
    const quitDate = new Date(this.progress.quitDate);
    const today = new Date();
    today.setHours(0, 0, 0, 0);

    const firstDayOfMonth = new Date(year, month, 1);
    const lastDayOfMonth = new Date(year, month + 1, 0);
    const startingDayOfWeek = firstDayOfMonth.getDay();
    const totalDaysInMonth = lastDayOfMonth.getDate();

    const weeks: CalendarWeek[] = [];
    let currentWeek: CalendarDay[] = [];

    // Fill in days from previous month
    const prevMonth = new Date(year, month, 0);
    const prevMonthDays = prevMonth.getDate();
    for (let i = startingDayOfWeek - 1; i >= 0; i--) {
      const dayNum = prevMonthDays - i;
      const date = new Date(year, month - 1, dayNum);
      currentWeek.push(this.createCalendarDay(date, dayNum, false, quitDate, today));
    }

    // Fill in current month days
    for (let day = 1; day <= totalDaysInMonth; day++) {
      const date = new Date(year, month, day);
      currentWeek.push(this.createCalendarDay(date, day, true, quitDate, today));

      if (currentWeek.length === 7) {
        weeks.push({ days: currentWeek });
        currentWeek = [];
      }
    }

    // Fill in days from next month
    if (currentWeek.length > 0) {
      let nextMonthDay = 1;
      while (currentWeek.length < 7) {
        const date = new Date(year, month + 1, nextMonthDay);
        currentWeek.push(this.createCalendarDay(date, nextMonthDay, false, quitDate, today));
        nextMonthDay++;
      }
      weeks.push({ days: currentWeek });
    }

    this.calendarWeeks = weeks;
    this.generateYear();

    // Bring a deep-linked day into view once the grid holding it has rendered
    if (this.highlightedKey && !this.hasScrolledToHighlight) {
      const target = weeks.flatMap(w => w.days)
        .find(d => d.dateKey === this.highlightedKey && d.isCurrentMonth);

      if (target) {
        this.hasScrolledToHighlight = true;
        this.scrollToHighlightedDay();
      }
    }

    // Keep the details panel in sync with freshly rebuilt day objects
    if (this.selectedDay) {
      const key = this.selectedDay.dateKey;
      const fromMonth = weeks.flatMap(w => w.days).find(d => d.dateKey === key && d.isCurrentMonth);
      const fromYear = this.yearMonths.flatMap(m => m.cells).find(c => c?.dateKey === key) ?? null;
      this.selectedDay = fromMonth ?? fromYear ?? null;
    }
  }

  private generateYear(): void {
    if (!this.progress) {
      this.yearMonths = [];
      return;
    }

    const year = this.currentDate.getFullYear();
    const quitDate = new Date(this.progress.quitDate);
    const today = new Date();
    today.setHours(0, 0, 0, 0);

    const months: MiniMonth[] = [];

    for (let m = 0; m < 12; m++) {
      const daysInMonth = new Date(year, m + 1, 0).getDate();
      const leadingBlanks = new Date(year, m, 1).getDay();

      const cells: (CalendarDay | null)[] = [];
      for (let i = 0; i < leadingBlanks; i++) cells.push(null);

      let smokeFreeDays = 0;
      let smokedDays = 0;

      for (let d = 1; d <= daysInMonth; d++) {
        const cell = this.createCalendarDay(new Date(year, m, d), d, true, quitDate, today);
        if (cell.status === 'smoke-free') smokeFreeDays++;
        if (cell.status === 'smoked') smokedDays++;
        cells.push(cell);
      }

      months.push({ name: MONTH_NAMES[m], monthIndex: m, cells, smokeFreeDays, smokedDays });
    }

    this.yearMonths = months;
  }

  setViewMode(mode: CalendarViewMode): void {
    if (this.viewMode === mode) return;
    this.viewMode = mode;
    this.selectedDay = null;
    this.clearHighlight();
    this.generateCalendar();
  }

  previousYear(): void {
    this.currentDate = new Date(this.currentDate.getFullYear() - 1, this.currentDate.getMonth(), 1);
    this.selectedDay = null;
    this.clearHighlight();
    this.generateCalendar();
  }

  nextYear(): void {
    this.currentDate = new Date(this.currentDate.getFullYear() + 1, this.currentDate.getMonth(), 1);
    this.selectedDay = null;
    this.clearHighlight();
    this.generateCalendar();
  }

  getSmokeFreeThisYear(): number {
    return this.yearMonths.reduce((total, m) => total + m.smokeFreeDays, 0);
  }

  getSmokedThisYear(): number {
    return this.yearMonths.reduce((total, m) => total + m.smokedDays, 0);
  }

  createCalendarDay(date: Date, dayNumber: number, isCurrentMonth: boolean, quitDate: Date, today: Date): CalendarDay {
    const dateOnly = new Date(date);
    dateOnly.setHours(0, 0, 0, 0);

    const quitDateOnly = new Date(quitDate);
    quitDateOnly.setHours(0, 0, 0, 0);

    const dateKey = this.toDateKey(dateOnly);
    const isQuitDay = dateOnly.getTime() === quitDateOnly.getTime();
    const isToday = dateOnly.getTime() === today.getTime();
    const smokedDay = this.smokedDays.get(dateKey) ?? null;

    let status: DayStatus;
    let daysSinceQuit = 0;

    if (dateOnly < quitDateOnly) {
      status = 'before-quit';
    } else if (dateOnly > today) {
      status = 'future';
    } else {
      status = smokedDay ? 'smoked' : 'smoke-free';
      daysSinceQuit = Math.floor((dateOnly.getTime() - quitDateOnly.getTime()) / (1000 * 60 * 60 * 24)) + 1;
    }

    // Find achievements for this day
    const achievementsForDay = this.unlockedAchievements.filter(a => {
      return a.requiredDays === daysSinceQuit - 1;
    });

    // Cumulative savings up to this day, with the smoked days deducted
    const cigarettesPerDay = this.progress?.cigarettesPerDay || 0;
    const pricePerPack = this.progress?.pricePerPack || 0;
    const cigarettesPerPack = this.progress?.cigarettesPerPack || 20;

    const smokeFreeDays = Math.max(0, daysSinceQuit - this.countSmokedUpTo(dateKey));
    const cigarettesAvoided = smokeFreeDays * cigarettesPerDay;
    const moneySaved = (cigarettesAvoided / cigarettesPerPack) * pricePerPack;

    return {
      date: dateOnly,
      dateKey,
      dayNumber,
      isCurrentMonth,
      isToday,
      isQuitDay,
      daysSinceQuit: status === 'before-quit' || status === 'future' ? 0 : daysSinceQuit,
      status,
      achievements: achievementsForDay,
      moneySaved: status === 'smoke-free' ? moneySaved : 0,
      cigarettesAvoided: status === 'smoke-free' ? cigarettesAvoided : 0,
      smokedDay
    };
  }

  /** Number of smoked days on or before the given day - used for cumulative savings. */
  private countSmokedUpTo(dateKey: string): number {
    let count = 0;
    this.smokedDays.forEach((_, key) => {
      if (key <= dateKey) count++;
    });
    return count;
  }

  previousMonth(): void {
    this.currentDate = new Date(this.currentDate.getFullYear(), this.currentDate.getMonth() - 1, 1);
    this.selectedDay = null;
    this.clearHighlight();
    this.generateCalendar();
  }

  nextMonth(): void {
    this.currentDate = new Date(this.currentDate.getFullYear(), this.currentDate.getMonth() + 1, 1);
    this.selectedDay = null;
    this.clearHighlight();
    this.generateCalendar();
  }

  /** Clicking a tracked day opens the details popup. */
  selectDay(day: CalendarDay): void {
    this.clearHighlight();

    if (day.isCurrentMonth && this.isTrackable(day)) {
      this.selectedDay = day;
    }
  }

  /** The deep-link highlight is an orientation cue, so any interaction retires it. */
  private clearHighlight(): void {
    this.highlightedKey = null;
  }

  private scrollToHighlightedDay(): void {
    setTimeout(() => {
      document.querySelector('.calendar-day.highlighted')
        ?.scrollIntoView({ behavior: 'smooth', block: 'center' });
    });
  }

  closeDayDetails(): void {
    this.selectedDay = null;
  }

  @HostListener('document:keydown.escape')
  onEscape(): void {
    // The mark dialog sits on top of the details popup, so it closes first
    if (this.markDialogDay) {
      this.closeMarkDialog();
      return;
    }
    this.closeDayDetails();
  }

  /** Native hover tooltip so a cell explains itself without needing a click. */
  dayTooltip(day: CalendarDay): string | null {
    if (!day.isCurrentMonth) return null;

    if (day.status === 'smoked' && day.smokedDay) {
      const count = day.smokedDay.cigarettesSmoked;
      return `Smoked: ${count} cigarette${count === 1 ? '' : 's'} · ${this.triggerLabel(day.smokedDay.trigger)}`
        + ' · excluded from your smoke-free total';
    }

    if (day.status === 'smoke-free') {
      return `Day ${day.daysSinceQuit} of your journey · smoke-free`;
    }

    return null;
  }

  isTrackable(day: CalendarDay): boolean {
    return day.status === 'smoke-free' || day.status === 'smoked';
  }

  openMarkDialog(day: CalendarDay): void {
    this.error = '';
    this.markDialogDay = day;
    this.form = {
      cigarettesSmoked: day.smokedDay?.cigarettesSmoked ?? 1,
      trigger: day.smokedDay?.trigger ?? 'Bathroom',
      note: day.smokedDay?.note ?? ''
    };
  }

  openMarkDialogForToday(): void {
    const todayKey = this.toDateKey(new Date());
    const todayCell = this.calendarWeeks
      .flatMap(w => w.days)
      .find(d => d.dateKey === todayKey && d.isCurrentMonth);

    if (todayCell) {
      this.openMarkDialog(todayCell);
      return;
    }

    // Today is outside the month being browsed - jump back to it first
    this.currentDate = new Date();
    this.generateCalendar();
    this.openMarkDialogForToday();
  }

  closeMarkDialog(): void {
    this.markDialogDay = null;
    this.error = '';
  }

  saveMark(): void {
    if (!this.markDialogDay) return;

    const cigarettes = Number(this.form.cigarettesSmoked);
    if (!Number.isFinite(cigarettes) || cigarettes < 1) {
      this.error = 'Enter at least one cigarette.';
      return;
    }

    this.saving = true;
    this.error = '';

    this.apiService.markSmokedDay({
      date: this.markDialogDay.dateKey,
      cigarettesSmoked: Math.floor(cigarettes),
      trigger: this.form.trigger,
      note: this.form.note?.trim() || null
    }).subscribe({
      next: (saved) => {
        this.smokedDays.set(saved.date, saved);
        this.saving = false;
        this.markDialogDay = null;
        this.selectedDay = null; // saving finishes the job - back to the calendar
        this.refreshAfterChange();
      },
      error: (err) => {
        this.saving = false;
        this.error = err?.error?.message || 'Could not save this day. Please try again.';
      }
    });
  }

  unmark(day: CalendarDay): void {
    this.saving = true;

    this.apiService.unmarkSmokedDay(day.dateKey).subscribe({
      next: () => {
        this.smokedDays.delete(day.dateKey);
        this.saving = false;
        this.refreshAfterChange();
      },
      error: () => {
        this.saving = false;
      }
    });
  }

  private refreshAfterChange(): void {
    this.generateCalendar();
    this.apiService.getStats().subscribe({
      next: (stats) => {
        this.stats = stats;
      }
    });
  }

  getSmokeFreeThisMonth(): number {
    return this.calendarWeeks.reduce((total, week) => {
      return total + week.days.filter(d => d.isCurrentMonth && d.status === 'smoke-free').length;
    }, 0);
  }

  getSmokedThisMonth(): number {
    return this.calendarWeeks.reduce((total, week) => {
      return total + week.days.filter(d => d.isCurrentMonth && d.status === 'smoked').length;
    }, 0);
  }

  triggerLabel(trigger: RelapseTrigger): string {
    return this.triggers.find(t => t.value === trigger)?.label ?? trigger;
  }

  triggerIcon(trigger: RelapseTrigger): string {
    return this.triggers.find(t => t.value === trigger)?.icon ?? '📌';
  }

  formatDayMoney(amount: number): string {
    const currency = this.progress?.currency || 'USD';
    if (currency === 'VND') {
      return `${amount.toLocaleString('vi-VN')} ₫`;
    }
    return `$${amount.toFixed(2)}`;
  }

  /** Local yyyy-MM-dd, so the date the user clicked is the date the API receives. */
  private toDateKey(date: Date): string {
    const year = date.getFullYear();
    const month = `${date.getMonth() + 1}`.padStart(2, '0');
    const day = `${date.getDate()}`.padStart(2, '0');
    return `${year}-${month}-${day}`;
  }
}
