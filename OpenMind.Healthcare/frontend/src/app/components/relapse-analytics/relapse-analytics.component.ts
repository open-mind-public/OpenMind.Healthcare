import { Component, OnInit } from '@angular/core';
import { ApiService } from '../../services/api.service';
import {
  RelapseAnalytics,
  RelapseTrigger,
  UserProgress,
  RELAPSE_TRIGGERS
} from '../../models/models';

@Component({
  selector: 'app-relapse-analytics',
  standalone: false,
  template: `
    <div class="analytics-container fade-in">
      <div class="page-header">
        <h1>📈 Relapse Analytics</h1>
        <p class="subtitle">The days you smoked, and what they tell you</p>
      </div>

      <div class="loading" *ngIf="loading">Loading your analytics...</div>

      <ng-container *ngIf="!loading && analytics as a">
        <!-- No journey yet -->
        <div class="empty-state" *ngIf="!hasJourney">
          <span class="empty-icon">📈</span>
          <h2>Nothing to analyse yet</h2>
          <p>Set your quit date first, then any day you mark as smoked will show up here.</p>
          <button class="btn btn-primary" routerLink="/quit-smoking/setup">Get Started</button>
        </div>

        <ng-container *ngIf="hasJourney">
          <!-- Hero -->
          <div class="hero-card">
            <div class="hero-main">
              <span class="hero-number">{{ a.smokeFreeRate | number:'1.0-1' }}%</span>
              <span class="hero-label">of your journey has been smoke-free</span>
              <span class="hero-sub">
                {{ a.smokeFreeDays | number }} smoke-free
                <span class="dot">·</span>
                {{ a.smokedDays | number }} smoked
                <span class="dot">·</span>
                {{ a.totalDaysInJourney | number }} days total
              </span>
            </div>
            <div class="hero-bar" role="img"
                 [attr.aria-label]="a.smokeFreeDays + ' smoke-free days and ' + a.smokedDays + ' smoked days'">
              <span class="seg smoke-free" [style.width.%]="barShare(a.smokeFreeDays, a.totalDaysInJourney)"></span>
              <span class="seg smoked" [style.width.%]="barShare(a.smokedDays, a.totalDaysInJourney)"></span>
            </div>
            <div class="legend">
              <span class="legend-item"><span class="swatch smoke-free"></span>✅ Smoke-free days</span>
              <span class="legend-item"><span class="swatch smoked"></span>🚬 Smoked days</span>
            </div>
          </div>

          <!-- Streaks -->
          <div class="tile-grid">
            <div class="tile">
              <span class="tile-icon">🔥</span>
              <span class="tile-value">{{ a.currentStreak | number }}</span>
              <span class="tile-label">Current streak (days)</span>
            </div>
            <div class="tile">
              <span class="tile-icon">🏅</span>
              <span class="tile-value">{{ a.longestStreak | number }}</span>
              <span class="tile-label">Longest streak (days)</span>
            </div>
            <div class="tile">
              <span class="tile-icon">📆</span>
              <span class="tile-value">{{ a.smokedDays > 0 ? (a.daysSinceLastRelapse | number) : '—' }}</span>
              <span class="tile-label">Days since last relapse</span>
            </div>
            <div class="tile">
              <span class="tile-icon">⏳</span>
              <span class="tile-value">{{ a.smokedDays > 0 ? (a.averageDaysBetweenRelapses | number:'1.0-1') : '—' }}</span>
              <span class="tile-label">Avg. days between relapses</span>
            </div>
          </div>

          <!-- Perfect record -->
          <div class="clean-card" *ngIf="a.smokedDays === 0">
            <span class="clean-icon">🌟</span>
            <h2>No smoked days recorded</h2>
            <p>
              Every single one of your {{ a.totalDaysInJourney | number }} days is counted as smoke-free.
              If you do slip, mark the day on the calendar - the numbers stay honest and the analytics
              here will show you the pattern behind it.
            </p>
            <button class="btn btn-ghost" routerLink="/quit-smoking/calendar">Open the calendar</button>
          </div>

          <ng-container *ngIf="a.smokedDays > 0">
            <!-- Cost of relapses -->
            <div class="tile-grid">
              <div class="tile danger">
                <span class="tile-icon">🚬</span>
                <span class="tile-value">{{ a.totalCigarettesSmoked | number }}</span>
                <span class="tile-label">Cigarettes smoked</span>
              </div>
              <div class="tile danger">
                <span class="tile-icon">💸</span>
                <span class="tile-value">{{ formatMoney(a.moneySpentOnRelapses) }}</span>
                <span class="tile-label">Spent on relapses</span>
              </div>
              <div class="tile danger">
                <span class="tile-icon">⏱️</span>
                <span class="tile-value">{{ a.lifeLostFormatted }}</span>
                <span class="tile-label">Life lost to relapses</span>
              </div>
              <div class="tile danger">
                <span class="tile-icon">📊</span>
                <span class="tile-value">{{ a.averageCigarettesPerRelapseDay | number:'1.0-1' }}</span>
                <span class="tile-label">Avg. cigarettes per relapse day</span>
              </div>
            </div>

            <!-- Trend -->
            <div class="card">
              <div class="card-head">
                <h3>Recent trend</h3>
                <span class="badge" [class]="trendClass(a.trend)">{{ trendIcon(a.trend) }} {{ trendLabel(a.trend) }}</span>
              </div>
              <div class="trend-grid">
                <div class="trend-item">
                  <span class="trend-value">{{ a.relapsesPrevious30Days }}</span>
                  <span class="trend-label">Days 31-60 ago</span>
                </div>
                <div class="trend-arrow">→</div>
                <div class="trend-item">
                  <span class="trend-value">{{ a.relapsesLast30Days }}</span>
                  <span class="trend-label">Last 30 days</span>
                </div>
              </div>
              <p class="card-note">{{ trendNote(a) }}</p>
            </div>

            <!-- Triggers -->
            <div class="card">
              <div class="card-head">
                <h3>What triggers your relapses</h3>
              </div>
              <div class="bar-list">
                <div class="bar-row" *ngFor="let t of a.triggerBreakdown">
                  <span class="bar-name">
                    <span class="bar-emoji">{{ triggerIcon(t.trigger) }}</span>
                    {{ triggerLabel(t.trigger) }}
                  </span>
                  <span class="bar-track">
                    <span
                      class="bar-fill smoked"
                      [style.width.%]="barShare(t.days, maxTriggerDays)"
                      [attr.title]="t.days + ' day(s) · ' + t.cigarettes + ' cigarettes · ' + t.sharePercentage + '% of relapses'"></span>
                  </span>
                  <span class="bar-value">{{ t.days }}<span class="bar-unit">d</span></span>
                </div>
              </div>
              <p class="card-note" *ngIf="a.mostCommonTrigger">
                {{ triggerIcon(a.mostCommonTrigger) }} <strong>{{ triggerLabel(a.mostCommonTrigger) }}</strong>
                is behind more of your relapses than anything else. Plan for it specifically.
              </p>
            </div>

            <!-- Weekdays -->
            <div class="card">
              <div class="card-head">
                <h3>Relapse rate by weekday</h3>
              </div>
              <div class="column-chart">
                <div class="column-group" *ngFor="let w of a.weekdayBreakdown">
                  <span class="column-value">{{ w.smokedDays }}</span>
                  <span class="column-track">
                    <span
                      class="column-fill"
                      [class.riskiest]="w.weekday === a.riskiestWeekday"
                      [style.height.%]="barShare(w.relapseRate, maxWeekdayRate)"
                      [attr.title]="w.weekday + ': ' + w.smokedDays + ' of ' + w.totalDays + ' days (' + w.relapseRate + '%)'"></span>
                  </span>
                  <span class="column-label">{{ w.weekday | slice:0:3 }}</span>
                </div>
              </div>
              <p class="card-note" *ngIf="a.riskiestWeekday">
                ⚠️ <strong>{{ a.riskiestWeekday }}</strong> is your riskiest day of the week.
              </p>
            </div>

            <!-- Monthly composition -->
            <div class="card">
              <div class="card-head">
                <h3>Month by month</h3>
                <button class="link-btn" (click)="showTable = !showTable">
                  {{ showTable ? 'Show chart' : 'Show data table' }}
                </button>
              </div>

              <div class="legend" *ngIf="!showTable">
                <span class="legend-item"><span class="swatch smoke-free"></span>✅ Smoke-free days</span>
                <span class="legend-item"><span class="swatch smoked"></span>🚬 Smoked days</span>
              </div>

              <div class="column-chart monthly" *ngIf="!showTable">
                <div class="column-group" *ngFor="let m of a.monthlyBreakdown">
                  <span class="column-value" [class.muted]="m.smokedDays === 0">{{ m.smokedDays }}</span>
                  <span class="column-track stacked">
                    <span
                      class="column-fill smoked"
                      [style.height.%]="barShare(m.smokedDays, maxMonthDays)"
                      [attr.title]="m.label + ': ' + m.smokedDays + ' smoked day(s), ' + m.cigarettes + ' cigarettes'"></span>
                    <span
                      class="column-fill smoke-free"
                      [style.height.%]="barShare(m.smokeFreeDays, maxMonthDays)"
                      [attr.title]="m.label + ': ' + m.smokeFreeDays + ' smoke-free day(s) (' + m.smokeFreeRate + '%)'"></span>
                  </span>
                  <span class="column-label">{{ m.label }}</span>
                </div>
              </div>

              <div class="table-wrap" *ngIf="showTable">
                <table>
                  <thead>
                    <tr>
                      <th>Month</th>
                      <th>Smoke-free</th>
                      <th>Smoked</th>
                      <th>Cigarettes</th>
                      <th>Smoke-free rate</th>
                    </tr>
                  </thead>
                  <tbody>
                    <tr *ngFor="let m of a.monthlyBreakdown">
                      <td>{{ m.label }}</td>
                      <td>{{ m.smokeFreeDays }}</td>
                      <td>{{ m.smokedDays }}</td>
                      <td>{{ m.cigarettes }}</td>
                      <td>{{ m.smokeFreeRate | number:'1.0-1' }}%</td>
                    </tr>
                  </tbody>
                </table>
              </div>
            </div>

            <div class="cta">
              <button class="btn btn-ghost" routerLink="/quit-smoking/calendar">📅 Back to calendar</button>
              <button class="btn btn-primary" routerLink="/quit-smoking/craving-help">🆘 Get craving help</button>
            </div>
          </ng-container>
        </ng-container>
      </ng-container>
    </div>
  `,
  styles: [`
    .analytics-container {
      padding: 20px;
      max-width: 900px;
      margin: 0 auto;
      display: flex;
      flex-direction: column;
      gap: 20px;
    }

    .page-header {
      text-align: center;

      h1 {
        font-size: 2.5rem;
        margin-bottom: 10px;
        color: var(--accent);
      }

      .subtitle {
        color: var(--text-secondary);
        font-size: 1.1rem;
      }
    }

    .loading {
      text-align: center;
      color: var(--text-muted);
      padding: 40px;
    }

    .card,
    .hero-card,
    .clean-card,
    .empty-state {
      background: var(--surface-sunken);
      border: 1px solid var(--border);
      border-radius: 18px;
      padding: 25px;
    }

    .card-head {
      display: flex;
      align-items: center;
      justify-content: space-between;
      gap: 12px;
      margin-bottom: 18px;

      h3 {
        color: var(--text);
        font-size: 1.1rem;
        font-weight: 600;
      }
    }

    .card-note {
      margin-top: 16px;
      font-size: 0.88rem;
      color: var(--text-secondary);
      line-height: 1.5;
    }

    .link-btn {
      background: none;
      border: none;
      color: var(--accent);
      font-size: 0.85rem;
      cursor: pointer;
      font-family: inherit;
      padding: 4px 0;

      &:hover {
        text-decoration: underline;
      }
    }

    /* Hero */
    .hero-card {
      text-align: center;
    }

    .hero-main {
      display: flex;
      flex-direction: column;
      align-items: center;
      gap: 6px;
      margin-bottom: 20px;
    }

    .hero-number {
      font-size: 3.6rem;
      font-weight: 700;
      line-height: 1;
      color: var(--accent);
    }

    .hero-label {
      color: var(--text);
      font-size: 1.05rem;
    }

    .hero-sub {
      color: var(--text-muted);
      font-size: 0.85rem;

      .dot {
        margin: 0 6px;
        opacity: 0.5;
      }
    }

    .hero-bar {
      display: flex;
      gap: 2px;
      height: 14px;
      width: 100%;

      .seg {
        display: block;
        height: 100%;

        &:first-child {
          border-radius: 4px 0 0 4px;
        }

        &:last-child {
          border-radius: 0 4px 4px 0;
        }

        &:only-child {
          border-radius: 4px;
        }
      }
    }

    .smoke-free {
      background: var(--accent);
    }

    .smoked {
      background: var(--danger);
    }

    .legend {
      display: flex;
      justify-content: center;
      flex-wrap: wrap;
      gap: 18px;
      margin-top: 14px;

      .legend-item {
        display: inline-flex;
        align-items: center;
        gap: 7px;
        font-size: 0.8rem;
        color: var(--text-secondary);
      }

      .swatch {
        width: 12px;
        height: 12px;
        border-radius: 3px;
      }
    }

    /* Stat tiles */
    .tile-grid {
      display: grid;
      grid-template-columns: repeat(4, 1fr);
      gap: 15px;
    }

    .tile {
      background: var(--surface-sunken);
      border: 1px solid var(--border);
      border-radius: 16px;
      padding: 20px 15px;
      display: flex;
      flex-direction: column;
      align-items: center;
      text-align: center;
      gap: 6px;

      .tile-icon {
        font-size: 1.5rem;
      }

      .tile-value {
        font-size: 1.5rem;
        font-weight: 700;
        color: var(--accent);
      }

      .tile-label {
        font-size: 0.78rem;
        color: var(--text-muted);
      }

      &.danger {
        border-color: var(--danger-border);
        background: var(--danger-subtle);

        .tile-value {
          color: var(--danger);
        }
      }
    }

    /* Clean record */
    .clean-card {
      text-align: center;

      .clean-icon {
        font-size: 3rem;
        display: block;
        margin-bottom: 12px;
      }

      h2 {
        color: var(--text);
        margin-bottom: 10px;
      }

      p {
        color: var(--text-secondary);
        max-width: 520px;
        margin: 0 auto 20px;
        line-height: 1.6;
      }
    }

    /* Trend */
    .badge {
      padding: 5px 12px;
      border-radius: 20px;
      font-size: 0.78rem;
      font-weight: 600;
      white-space: nowrap;

      &.good {
        background: var(--accent-subtle);
        color: var(--accent);
      }

      &.bad {
        background: var(--danger-subtle);
        color: var(--danger);
      }

      &.neutral {
        background: var(--surface-sunken);
        color: var(--text-secondary);
      }
    }

    .trend-grid {
      display: flex;
      align-items: center;
      justify-content: center;
      gap: 25px;

      .trend-item {
        display: flex;
        flex-direction: column;
        align-items: center;
        gap: 4px;
        min-width: 110px;
        padding: 15px;
        background: var(--surface-sunken);
        border-radius: 12px;
      }

      .trend-value {
        font-size: 1.8rem;
        font-weight: 700;
        color: var(--text);
      }

      .trend-label {
        font-size: 0.75rem;
        color: var(--text-muted);
      }

      .trend-arrow {
        font-size: 1.4rem;
        color: var(--text-muted);
      }
    }

    /* Horizontal bars */
    .bar-list {
      display: flex;
      flex-direction: column;
      gap: 10px;
    }

    .bar-row {
      display: grid;
      grid-template-columns: 140px 1fr 48px;
      align-items: center;
      gap: 12px;

      .bar-name {
        font-size: 0.85rem;
        color: var(--text-secondary);
        display: flex;
        align-items: center;
        gap: 7px;
        white-space: nowrap;
        overflow: hidden;
        text-overflow: ellipsis;
      }

      .bar-emoji {
        font-size: 1rem;
      }

      .bar-track {
        display: block;
        height: 12px;
        background: var(--surface-sunken);
        border-radius: 4px;
        overflow: hidden;
      }

      .bar-fill {
        display: block;
        height: 100%;
        border-radius: 0 4px 4px 0;
        min-width: 3px;
        transition: width 0.4s ease;
      }

      .bar-value {
        font-size: 0.85rem;
        font-weight: 600;
        color: var(--text);
        text-align: right;
      }

      .bar-unit {
        font-size: 0.7rem;
        color: var(--text-muted);
        margin-left: 1px;
      }
    }

    /* Column charts */
    .column-chart {
      display: flex;
      align-items: flex-end;
      justify-content: space-between;
      gap: 8px;
      padding-top: 6px;
    }

    .column-group {
      flex: 1;
      display: flex;
      flex-direction: column;
      align-items: center;
      gap: 6px;
      min-width: 0;

      .column-value {
        font-size: 0.78rem;
        font-weight: 600;
        color: var(--text);

        &.muted {
          color: var(--border-strong);
        }
      }

      .column-track {
        width: 100%;
        max-width: 46px;
        height: 130px;
        display: flex;
        flex-direction: column;
        justify-content: flex-end;
        background: var(--surface-sunken);
        border-radius: 4px;

        &.stacked {
          gap: 2px;
        }
      }

      .column-fill {
        display: block;
        width: 100%;
        background: var(--danger-border);
        border-radius: 4px 4px 0 0;
        min-height: 2px;
        transition: height 0.4s ease;

        &.riskiest {
          background: var(--danger);
        }

        &.smoked {
          background: var(--danger);
          border-radius: 4px 4px 0 0;
        }

        &.smoke-free {
          background: var(--accent);
          border-radius: 0 0 4px 4px;
        }
      }

      .column-label {
        font-size: 0.7rem;
        color: var(--text-muted);
        white-space: nowrap;
        overflow: hidden;
        text-overflow: ellipsis;
        max-width: 100%;
      }
    }

    .column-chart.monthly .column-label {
      font-size: 0.62rem;
    }

    /* Table view */
    .table-wrap {
      overflow-x: auto;

      table {
        width: 100%;
        border-collapse: collapse;
        font-size: 0.85rem;
      }

      th, td {
        padding: 10px 12px;
        text-align: left;
        border-bottom: 1px solid var(--border);
      }

      th {
        color: var(--text-muted);
        font-weight: 600;
        font-size: 0.78rem;
      }

      td {
        color: var(--text);
      }
    }

    /* Empty state */
    .empty-state {
      text-align: center;
      padding: 60px 20px;

      .empty-icon {
        font-size: 4rem;
        display: block;
        margin-bottom: 20px;
      }

      h2 {
        margin-bottom: 10px;
        color: var(--text);
      }

      p {
        color: var(--text-muted);
        margin-bottom: 25px;
      }
    }

    .cta {
      display: flex;
      justify-content: center;
      gap: 12px;
      flex-wrap: wrap;
    }

    .btn {
      padding: 12px 26px;
      border: none;
      border-radius: 25px;
      font-size: 0.95rem;
      font-weight: 600;
      cursor: pointer;
      transition: all 0.3s ease;

      &.btn-primary {
        background: var(--accent);
        color: var(--text-on-accent);

        &:hover {
          transform: translateY(-1px);
          box-shadow: var(--shadow-md);
        }
      }

      &.btn-ghost {
        background: var(--surface-sunken);
        color: var(--text);
        border: 1px solid var(--border);

        &:hover {
          background: var(--border);
        }
      }
    }

    .fade-in {
      animation: fadeIn 0.5s ease-out;
    }

    @keyframes fadeIn {
      from { opacity: 0; transform: translateY(20px); }
      to { opacity: 1; transform: translateY(0); }
    }

    @media (max-width: 768px) {
      .analytics-container { padding: 15px; }
      .page-header h1 { font-size: 1.8rem; }
      .tile-grid { grid-template-columns: repeat(2, 1fr); }
      .hero-number { font-size: 2.6rem; }
      .bar-row { grid-template-columns: 110px 1fr 42px; }
      .column-group .column-track { height: 100px; }
      .column-chart.monthly .column-label { font-size: 0.55rem; }
    }
  `]
})
export class RelapseAnalyticsComponent implements OnInit {
  analytics: RelapseAnalytics | null = null;
  progress: UserProgress | null = null;
  loading = true;
  showTable = false;

  private triggerOptions = RELAPSE_TRIGGERS;

  constructor(private apiService: ApiService) {}

  ngOnInit(): void {
    this.apiService.getProgress().subscribe({
      next: (progress) => {
        this.progress = progress;
        this.loadAnalytics();
      },
      error: () => {
        this.progress = null;
        this.loadAnalytics();
      }
    });
  }

  private loadAnalytics(): void {
    this.apiService.getRelapseAnalytics().subscribe({
      next: (analytics) => {
        this.analytics = analytics;
        this.loading = false;
      },
      error: () => {
        this.loading = false;
      }
    });
  }

  get hasJourney(): boolean {
    return !!this.progress;
  }

  /** Largest relapse-day count across triggers - the scale for the trigger bars. */
  get maxTriggerDays(): number {
    return Math.max(1, ...(this.analytics?.triggerBreakdown.map(t => t.days) ?? [1]));
  }

  /** Largest weekday relapse rate - the scale for the weekday columns. */
  get maxWeekdayRate(): number {
    return Math.max(1, ...(this.analytics?.weekdayBreakdown.map(w => w.relapseRate) ?? [1]));
  }

  /** Longest month in the window, so partial months read as shorter columns. */
  get maxMonthDays(): number {
    return Math.max(1, ...(this.analytics?.monthlyBreakdown.map(m => m.totalDays) ?? [1]));
  }

  barShare(value: number, total: number): number {
    if (!total || total <= 0) return 0;
    return Math.max(0, Math.min(100, (value / total) * 100));
  }

  triggerLabel(trigger: RelapseTrigger): string {
    return this.triggerOptions.find(t => t.value === trigger)?.label ?? trigger;
  }

  triggerIcon(trigger: RelapseTrigger): string {
    return this.triggerOptions.find(t => t.value === trigger)?.icon ?? '📌';
  }

  trendLabel(trend: string): string {
    switch (trend) {
      case 'Improving': return 'Improving';
      case 'Worsening': return 'Worsening';
      case 'Stable': return 'Holding steady';
      default: return 'Not enough data';
    }
  }

  trendIcon(trend: string): string {
    switch (trend) {
      case 'Improving': return '📉';
      case 'Worsening': return '📈';
      case 'Stable': return '➡️';
      default: return '⏳';
    }
  }

  trendClass(trend: string): string {
    switch (trend) {
      case 'Improving': return 'good';
      case 'Worsening': return 'bad';
      default: return 'neutral';
    }
  }

  trendNote(a: RelapseAnalytics): string {
    switch (a.trend) {
      case 'Improving':
        return `You slipped on ${a.relapsesLast30Days} day(s) in the last 30, down from ${a.relapsesPrevious30Days}. Whatever you changed, keep doing it.`;
      case 'Worsening':
        return `You slipped on ${a.relapsesLast30Days} day(s) in the last 30, up from ${a.relapsesPrevious30Days}. Worth looking at what changed recently.`;
      case 'Stable':
        return `Same number of slips as the previous 30 days (${a.relapsesLast30Days}). Steady, but there is room to push it down.`;
      default:
        return 'Once your journey passes 60 days there will be two full windows to compare.';
    }
  }

  formatMoney(amount: number): string {
    const currency = this.analytics?.currency || this.progress?.currency || 'USD';
    if (currency === 'VND') {
      return `${Math.round(amount).toLocaleString('vi-VN')} ₫`;
    }
    return `$${amount.toFixed(2)}`;
  }
}
