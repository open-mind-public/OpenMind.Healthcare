import { Component, OnInit, OnDestroy } from '@angular/core';
import { Router } from '@angular/router';
import { interval, Subscription } from 'rxjs';
import { ApiService } from '../../services/api.service';
import {
  ProgressStats,
  DailyEncouragement,
  UserProgress,
  MoneySaved,
  SmokedDay,
  RelapseTrigger,
  RELAPSE_TRIGGERS
} from '../../models/models';

@Component({
  selector: 'app-dashboard',
  standalone: false,
  template: `
    <div class="dashboard fade-in">
      <!-- Setup prompt if no progress -->
      <div *ngIf="!hasProgress" class="setup-prompt">
        <div class="card welcome-card">
          <h1>🚭 Welcome to Your Smoke-Free Journey!</h1>
          <p>Congratulations on taking the first step towards a healthier life. Let's set up your tracker to begin this amazing journey together.</p>
          <button class="btn btn-primary" (click)="goToSetup()">
            Start My Journey 🚀
          </button>
        </div>
      </div>

      <!-- Main dashboard when progress exists -->
      <div *ngIf="hasProgress && stats" class="dashboard-content">
        <!-- Hero Section -->
        <div class="hero-section">
          <div class="hero-left">
            <h1 class="hero-title">
              <span class="days-count pulse">{{ stats.daysSmokeFree }}</span>
              <span class="days-label">Days Smoke Free!</span>
              <span class="duration-breakdown" *ngIf="durationBreakdown">≈ {{ durationBreakdown }}</span>
            </h1>
            <p class="hero-subtitle">{{ stats.currentMilestone }} 🎉</p>
            <div class="next-milestone">
              <span>Next: {{ stats.nextMilestone }}</span>
              <span class="days-remaining" *ngIf="stats.daysToNextMilestone > 0">
                ({{ stats.daysToNextMilestone }} days to go)
              </span>
            </div>
            <div class="quit-date-line" *ngIf="quitDate">
              <span class="quit-date-label">Smoke-free since</span>
              <span class="quit-date-value">{{ quitDate | date:'d MMM yyyy, HH:mm' }}</span>
            </div>
            <div class="journey-integrity" *ngIf="stats.smokedDays > 0">
              🚬 {{ stats.smokedDays }} day{{ stats.smokedDays === 1 ? '' : 's' }} marked as smoked
              and excluded · {{ stats.smokeFreeRate | number:'1.0-1' }}% smoke-free
            </div>
          </div>
          <div class="hero-right">
            <div class="progress-ring">
              <svg viewBox="0 0 100 100">
                <circle class="progress-bg" cx="50" cy="50" r="45"/>
                <circle class="progress-fill" cx="50" cy="50" r="45"
                  [attr.stroke-dasharray]="circumference"
                  [attr.stroke-dashoffset]="progressOffset"/>
              </svg>
              <div class="progress-text">
                <span class="percentage">{{ stats.progressPercentage | number:'1.0-0' }}%</span>
                <span class="to-year">to 1 year</span>
              </div>
            </div>
          </div>
        </div>

        <!-- Stats Grid -->
        <div class="stats-grid">
          <app-stats-card
            icon="🚬"
            [value]="(stats.cigarettesNotSmoked | number) || '0'"
            label="Cigarettes Not Smoked"
            colorClass="primary">
          </app-stats-card>
          <app-stats-card
            icon="💰"
            [value]="formatMoney(stats.moneySaved)"
            label="Money Saved"
            colorClass="gold">
          </app-stats-card>
          <app-stats-card
            icon="⏰"
            [value]="stats.lifeRegainedFormatted"
            label="Life Regained"
            colorClass="blue">
          </app-stats-card>
          <app-stats-card
            icon="🔥"
            [value]="(stats.currentStreak | number) || '0'"
            label="Current Streak (days)"
            colorClass="pink">
          </app-stats-card>
        </div>

        <!-- Failed Days -->
        <div class="failed-days-section">
          <div class="card failed-card" [class.clean]="failedDayCount === 0">
            <div class="failed-header">
              <div class="failed-title">
                <span class="failed-icon">{{ failedDayCount === 0 ? '🌟' : '🚬' }}</span>
                <div>
                  <h2>Failed Days</h2>
                  <p class="failed-subtitle">
                    {{ failedDayCount === 0
                        ? 'Nothing marked so far - every day counts towards your total.'
                        : 'Days you marked as smoked. These are left out of your smoke-free total.' }}
                  </p>
                </div>
              </div>
              <span class="failed-count-badge">
                {{ failedDayCount }}
                <span class="badge-unit">{{ failedDayCount === 1 ? 'day' : 'days' }}</span>
              </span>
            </div>

            <ng-container *ngIf="failedDayCount > 0">
              <div class="failed-figures">
                <div class="figure">
                  <span class="figure-value">{{ failedDayCount | number }}</span>
                  <span class="figure-label">Failed days</span>
                </div>
                <div class="figure">
                  <span class="figure-value">{{ stats.cigarettesSmoked | number }}</span>
                  <span class="figure-label">Cigarettes smoked</span>
                </div>
                <div class="figure">
                  <span class="figure-value">{{ formatMoney(stats.moneySpentOnRelapses) }}</span>
                  <span class="figure-label">Spent on those days</span>
                </div>
                <div class="figure">
                  <span class="figure-value">{{ stats.smokeFreeRate | number:'1.0-1' }}%</span>
                  <span class="figure-label">Still smoke-free</span>
                </div>
              </div>

              <div class="latest-failed" *ngIf="latestFailedDay as latest">
                <div class="latest-info">
                  <span class="latest-label">Most recent</span>
                  <span class="latest-date">{{ latest.date | date:'EEEE, d MMMM yyyy' }}</span>
                  <span class="latest-detail">
                    {{ latest.cigarettesSmoked }} cigarette{{ latest.cigarettesSmoked === 1 ? '' : 's' }}
                    <span class="dot">·</span>
                    {{ triggerIcon(latest.trigger) }} {{ triggerLabel(latest.trigger) }}
                    <span class="dot">·</span>
                    {{ daysAgoLabel(latest.date) }}
                  </span>
                  <span class="latest-note" *ngIf="latest.note">“{{ latest.note }}”</span>
                </div>
                <button class="btn-calendar-link" (click)="viewFailedDayOnCalendar(latest)">
                  📅 View on calendar
                  <span class="arrow">→</span>
                </button>
              </div>
            </ng-container>

            <div class="failed-actions" *ngIf="failedDayCount === 0">
              <button class="btn-calendar-link" (click)="navigate('/calendar')">
                📅 Open the calendar
                <span class="arrow">→</span>
              </button>
            </div>
          </div>
        </div>

        <!-- Encouragement Section -->
        <div class="encouragement-section" *ngIf="encouragement">
          <div class="card encouragement-card">
            <div class="message-header">
              <span class="greeting-icon float">✨</span>
              <h2>Your Daily Encouragement</h2>
            </div>
            <p class="main-message">{{ encouragement.message }}</p>
            <div class="special-message" *ngIf="encouragement.specialMessage">
              {{ encouragement.specialMessage }}
            </div>
            <div class="quote-section" *ngIf="encouragement.quote">
              <blockquote>
                "{{ encouragement.quote.quote }}"
                <cite>— {{ encouragement.quote.author }}</cite>
              </blockquote>
            </div>
          </div>
        </div>

        <!-- Quick Actions -->
        <div class="quick-actions">
          <button class="action-btn health" (click)="navigate('/health')">
            <span class="action-icon">🫀</span>
            <span>Health Progress</span>
          </button>
          <button class="action-btn achievements" (click)="navigate('/achievements')">
            <span class="action-icon">🏆</span>
            <span>Achievements</span>
          </button>
          <button class="action-btn motivation" (click)="navigate('/motivation')">
            <span class="action-icon">💪</span>
            <span>Get Motivated</span>
          </button>
          <button class="action-btn analytics" (click)="navigate('/analytics')">
            <span class="action-icon">📈</span>
            <span>Relapse Analytics</span>
          </button>
          <button class="action-btn craving" (click)="navigate('/craving-help')">
            <span class="action-icon">🆘</span>
            <span>Craving Help</span>
          </button>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .dashboard {
      max-width: 1200px;
      margin: 0 auto;
      padding: 20px;
    }

    .setup-prompt {
      display: flex;
      justify-content: center;
      align-items: center;
      min-height: 60vh;
    }

    .welcome-card {
      text-align: center;
      max-width: 600px;
      padding: 50px;
      
      h1 {
        font-size: 32px;
        margin-bottom: 20px;
        background: linear-gradient(135deg, #10b981, #3b82f6);
        -webkit-background-clip: text;
        -webkit-text-fill-color: transparent;
      }
      
      p {
        color: rgba(255, 255, 255, 0.8);
        font-size: 18px;
        line-height: 1.6;
        margin-bottom: 30px;
      }
      
      button {
        font-size: 18px;
        padding: 15px 40px;
      }
    }

    .hero-section {
      display: flex;
      justify-content: space-between;
      align-items: center;
      background: linear-gradient(135deg, rgba(16, 185, 129, 0.2), rgba(59, 130, 246, 0.2));
      border-radius: 30px;
      padding: 50px;
      margin-bottom: 30px;
      border: 1px solid rgba(255, 255, 255, 0.1);
    }

    .hero-title {
      display: flex;
      flex-direction: column;
      gap: 10px;
    }

    .days-count {
      font-size: 100px;
      font-weight: 800;
      background: linear-gradient(135deg, #10b981, #34d399);
      -webkit-background-clip: text;
      -webkit-text-fill-color: transparent;
      line-height: 1;
    }

    .days-label {
      font-size: 28px;
      font-weight: 600;
    }

    .duration-breakdown {
      font-size: 22px;
      font-weight: 600;
      color: rgba(255, 255, 255, 0.75);
    }

    .hero-subtitle {
      font-size: 20px;
      color: #34d399;
      margin-top: 10px;
    }

    .next-milestone {
      margin-top: 15px;
      color: rgba(255, 255, 255, 0.7);
      font-size: 16px;
      
      .days-remaining {
        color: #f59e0b;
        margin-left: 5px;
      }
    }

    .progress-ring {
      position: relative;
      width: 200px;
      height: 200px;
      
      svg {
        transform: rotate(-90deg);
        width: 100%;
        height: 100%;
      }
      
      .progress-bg {
        fill: none;
        stroke: rgba(255, 255, 255, 0.1);
        stroke-width: 8;
      }
      
      .progress-fill {
        fill: none;
        stroke: url(#gradient);
        stroke-width: 8;
        stroke-linecap: round;
        transition: stroke-dashoffset 1s ease;
      }
    }

    .progress-text {
      position: absolute;
      top: 50%;
      left: 50%;
      transform: translate(-50%, -50%);
      text-align: center;
      
      .percentage {
        display: block;
        font-size: 36px;
        font-weight: 700;
        color: #10b981;
      }
      
      .to-year {
        font-size: 14px;
        color: rgba(255, 255, 255, 0.6);
      }
    }

    .quit-date-line {
      display: flex;
      align-items: center;
      flex-wrap: wrap;
      gap: 10px;
      margin-top: 14px;
      font-size: 13px;

      .quit-date-label {
        color: rgba(255, 255, 255, 0.5);
        text-transform: uppercase;
        letter-spacing: 0.06em;
        font-size: 11px;
      }

      .quit-date-value {
        color: rgba(255, 255, 255, 0.85);
        font-weight: 600;
      }
    }

    .journey-integrity {
      margin-top: 12px;
      font-size: 13px;
      color: rgba(255, 255, 255, 0.65);
      background: rgba(239, 68, 68, 0.12);
      border: 1px solid rgba(239, 68, 68, 0.25);
      border-radius: 10px;
      padding: 8px 12px;
      display: inline-block;
    }

    .stats-grid {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(250px, 1fr));
      gap: 20px;
      margin-bottom: 30px;
    }

    .failed-days-section {
      margin-bottom: 30px;
    }

    .failed-card {
      background: linear-gradient(135deg, rgba(239, 68, 68, 0.12), rgba(248, 113, 113, 0.05));
      border: 1px solid rgba(239, 68, 68, 0.28);

      &.clean {
        background: linear-gradient(135deg, rgba(16, 185, 129, 0.12), rgba(52, 211, 153, 0.05));
        border-color: rgba(16, 185, 129, 0.28);
      }
    }

    .failed-header {
      display: flex;
      align-items: flex-start;
      justify-content: space-between;
      gap: 20px;

      .failed-title {
        display: flex;
        align-items: flex-start;
        gap: 15px;
      }

      .failed-icon {
        font-size: 34px;
        line-height: 1;
      }

      h2 {
        font-size: 22px;
        color: white;
        margin-bottom: 4px;
      }

      .failed-subtitle {
        font-size: 13px;
        color: rgba(255, 255, 255, 0.6);
        max-width: 460px;
      }

      .failed-count-badge {
        display: flex;
        flex-direction: column;
        align-items: center;
        min-width: 74px;
        padding: 10px 14px;
        border-radius: 14px;
        background: rgba(239, 68, 68, 0.18);
        color: #f87171;
        font-size: 30px;
        font-weight: 700;
        line-height: 1;

        .badge-unit {
          font-size: 11px;
          font-weight: 600;
          text-transform: uppercase;
          letter-spacing: 0.06em;
          color: rgba(255, 255, 255, 0.55);
          margin-top: 5px;
        }
      }
    }

    .failed-card.clean .failed-count-badge {
      background: rgba(16, 185, 129, 0.18);
      color: #34d399;
    }

    .failed-figures {
      display: grid;
      grid-template-columns: repeat(4, 1fr);
      gap: 12px;
      margin-top: 22px;

      .figure {
        display: flex;
        flex-direction: column;
        align-items: center;
        text-align: center;
        padding: 14px 10px;
        background: rgba(255, 255, 255, 0.04);
        border-radius: 12px;
      }

      .figure-value {
        font-size: 20px;
        font-weight: 700;
        color: white;
      }

      .figure-label {
        font-size: 11px;
        color: rgba(255, 255, 255, 0.55);
        margin-top: 5px;
      }
    }

    .latest-failed {
      display: flex;
      align-items: center;
      justify-content: space-between;
      gap: 20px;
      flex-wrap: wrap;
      margin-top: 20px;
      padding-top: 20px;
      border-top: 1px solid rgba(255, 255, 255, 0.1);

      .latest-info {
        display: flex;
        flex-direction: column;
        gap: 3px;
      }

      .latest-label {
        font-size: 11px;
        text-transform: uppercase;
        letter-spacing: 0.06em;
        color: rgba(255, 255, 255, 0.45);
      }

      .latest-date {
        font-size: 17px;
        font-weight: 600;
        color: white;
      }

      .latest-detail {
        font-size: 13px;
        color: rgba(255, 255, 255, 0.65);

        .dot {
          margin: 0 6px;
          opacity: 0.5;
        }
      }

      .latest-note {
        font-size: 13px;
        font-style: italic;
        color: rgba(255, 255, 255, 0.5);
        margin-top: 2px;
      }
    }

    .failed-actions {
      margin-top: 20px;
    }

    .btn-calendar-link {
      display: inline-flex;
      align-items: center;
      gap: 8px;
      padding: 11px 20px;
      border-radius: 25px;
      border: 1px solid rgba(255, 255, 255, 0.2);
      background: rgba(255, 255, 255, 0.08);
      color: white;
      font-family: 'Poppins', sans-serif;
      font-size: 14px;
      font-weight: 600;
      cursor: pointer;
      transition: all 0.3s ease;
      white-space: nowrap;

      .arrow {
        transition: transform 0.3s ease;
      }

      &:hover {
        background: rgba(255, 255, 255, 0.16);
        transform: translateY(-2px);

        .arrow {
          transform: translateX(3px);
        }
      }
    }

    .encouragement-section {
      margin-bottom: 30px;
    }

    .encouragement-card {
      background: linear-gradient(135deg, rgba(245, 158, 11, 0.1), rgba(236, 72, 153, 0.1));
      border-color: rgba(245, 158, 11, 0.2);
      
      .message-header {
        display: flex;
        align-items: center;
        gap: 15px;
        margin-bottom: 20px;
        
        .greeting-icon {
          font-size: 40px;
        }
        
        h2 {
          font-size: 24px;
          background: linear-gradient(135deg, #f59e0b, #ec4899);
          -webkit-background-clip: text;
          -webkit-text-fill-color: transparent;
        }
      }
      
      .main-message {
        font-size: 18px;
        line-height: 1.8;
        color: rgba(255, 255, 255, 0.9);
      }
      
      .special-message {
        margin-top: 20px;
        padding: 15px 20px;
        background: linear-gradient(135deg, rgba(16, 185, 129, 0.3), rgba(52, 211, 153, 0.2));
        border-radius: 12px;
        font-weight: 600;
        font-size: 16px;
      }
      
      .quote-section {
        margin-top: 25px;
        padding-top: 25px;
        border-top: 1px solid rgba(255, 255, 255, 0.1);
        
        blockquote {
          font-style: italic;
          font-size: 16px;
          color: rgba(255, 255, 255, 0.8);
          
          cite {
            display: block;
            margin-top: 10px;
            font-size: 14px;
            color: rgba(255, 255, 255, 0.6);
            font-style: normal;
          }
        }
      }
    }

    .quick-actions {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
      gap: 15px;
    }

    .action-btn {
      display: flex;
      flex-direction: column;
      align-items: center;
      gap: 10px;
      padding: 25px;
      border-radius: 16px;
      border: none;
      cursor: pointer;
      transition: all 0.3s ease;
      font-family: 'Poppins', sans-serif;
      font-weight: 600;
      color: white;
      
      .action-icon {
        font-size: 36px;
      }
      
      &:hover {
        transform: translateY(-5px);
      }
      
      &.health {
        background: linear-gradient(135deg, #ec4899, #f472b6);
        &:hover { box-shadow: 0 10px 30px rgba(236, 72, 153, 0.4); }
      }
      
      &.achievements {
        background: linear-gradient(135deg, #f59e0b, #fbbf24);
        &:hover { box-shadow: 0 10px 30px rgba(245, 158, 11, 0.4); }
      }
      
      &.motivation {
        background: linear-gradient(135deg, #3b82f6, #60a5fa);
        &:hover { box-shadow: 0 10px 30px rgba(59, 130, 246, 0.4); }
      }
      
      &.analytics {
        background: linear-gradient(135deg, #6366f1, #818cf8);
        &:hover { box-shadow: 0 10px 30px rgba(99, 102, 241, 0.4); }
      }

      &.craving {
        background: linear-gradient(135deg, #ef4444, #f87171);
        &:hover { box-shadow: 0 10px 30px rgba(239, 68, 68, 0.4); }
      }
    }

    @media (max-width: 768px) {
      .hero-section {
        flex-direction: column;
        text-align: center;
        gap: 30px;
        padding: 30px;
      }
      
      .days-count {
        font-size: 60px;
      }
      
      .days-label {
        font-size: 20px;
      }

      .failed-header {
        flex-direction: column;
        align-items: stretch;
        gap: 15px;

        .failed-count-badge {
          flex-direction: row;
          align-items: baseline;
          justify-content: center;
          gap: 8px;
          font-size: 24px;

          .badge-unit {
            margin-top: 0;
          }
        }
      }

      .failed-figures {
        grid-template-columns: repeat(2, 1fr);
      }

      .latest-failed {
        flex-direction: column;
        align-items: stretch;

        .btn-calendar-link {
          justify-content: center;
        }
      }
    }
  `]
})
export class DashboardComponent implements OnInit, OnDestroy {
  stats: ProgressStats | null = null;
  encouragement: DailyEncouragement | null = null;
  hasProgress = false;

  /** Days marked as smoked, oldest first - drives the Failed Days section. */
  smokedDays: SmokedDay[] = [];

  /** Journey start, shown in the hero with a shortcut to change it. */
  quitDate: Date | null = null;

  circumference = 2 * Math.PI * 45;
  progressOffset = this.circumference;
  
  private refreshSubscription?: Subscription;

  constructor(
    private apiService: ApiService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.loadData();
    // Refresh stats every minute
    this.refreshSubscription = interval(60000).subscribe(() => {
      this.loadStats();
      this.loadSmokedDays();
    });
  }

  ngOnDestroy(): void {
    this.refreshSubscription?.unsubscribe();
  }

  loadData(): void {
    this.apiService.getProgress().subscribe({
      next: (progress) => {
        this.hasProgress = true;
        // the API sends UTC with no zone marker, so pin it before JS reads it as local
        this.quitDate = new Date(/[zZ]$/.test(progress.quitDate) ? progress.quitDate : `${progress.quitDate}Z`);
        this.loadStats();
        this.loadEncouragement();
        this.loadSmokedDays();
      },
      error: () => {
        this.hasProgress = false;
      }
    });
  }

  loadStats(): void {
    this.apiService.getStats().subscribe({
      next: (stats) => {
        this.stats = stats;
        this.updateProgressRing(stats.progressPercentage);
      }
    });
  }

  loadSmokedDays(): void {
    this.apiService.getSmokedDays().subscribe({
      next: (days) => {
        this.smokedDays = days;
      }
    });
  }

  get failedDayCount(): number {
    return this.smokedDays.length;
  }

  /** The API returns smoked days oldest first, so the most recent one is last. */
  get latestFailedDay(): SmokedDay | null {
    return this.smokedDays.length > 0 ? this.smokedDays[this.smokedDays.length - 1] : null;
  }

  viewFailedDayOnCalendar(day: SmokedDay): void {
    this.router.navigate(['/calendar'], { queryParams: { date: day.date } });
  }

  triggerLabel(trigger: RelapseTrigger): string {
    return RELAPSE_TRIGGERS.find(t => t.value === trigger)?.label ?? trigger;
  }

  triggerIcon(trigger: RelapseTrigger): string {
    return RELAPSE_TRIGGERS.find(t => t.value === trigger)?.icon ?? '📌';
  }

  /** "today" / "yesterday" / "12 days ago" for a yyyy-MM-dd date. */
  daysAgoLabel(date: string): string {
    const [year, month, day] = date.split('-').map(Number);
    const target = new Date(year, month - 1, day);
    const today = new Date();
    today.setHours(0, 0, 0, 0);

    const days = Math.round((today.getTime() - target.getTime()) / (1000 * 60 * 60 * 24));
    if (days <= 0) return 'today';
    if (days === 1) return 'yesterday';
    return `${days} days ago`;
  }

  get durationBreakdown(): string | null {
    const days = this.stats?.daysSmokeFree ?? 0;
    if (days < 30) return null;

    const years = Math.floor(days / 365);
    const months = Math.floor((days % 365) / 30);

    if (years > 0) {
      return months > 0
        ? `${years} year${years === 1 ? '' : 's'}, ${months} month${months === 1 ? '' : 's'}`
        : `${years} year${years === 1 ? '' : 's'}`;
    }
    return `${months} month${months === 1 ? '' : 's'}`;
  }

  loadEncouragement(): void {
    this.apiService.getDailyEncouragement().subscribe({
      next: (data) => {
        this.encouragement = data;
      }
    });
  }

  updateProgressRing(percentage: number): void {
    this.progressOffset = this.circumference - (percentage / 100) * this.circumference;
  }

  goToSetup(): void {
    this.router.navigate(['/setup']);
  }

  navigate(path: string): void {
    this.router.navigate([path]);
  }

  formatMoney(money: MoneySaved | number | undefined): string {
    if (!money) return '$0';
    
    let amount: number;
    let currency: string;
    
    if (typeof money === 'number') {
      amount = money;
      currency = 'USD';
    } else {
      amount = money.amount;
      currency = money.currency;
    }
    
    // Format large numbers with abbreviations
    if (currency === 'VND') {
      if (amount >= 1000000) {
        return `${(amount / 1000000).toFixed(1)}M \u20ab`;
      }
      if (amount >= 1000) {
        return `${(amount / 1000).toFixed(0)}K \u20ab`;
      }
      return `${amount.toLocaleString('vi-VN')} \u20ab`;
    }
    
    // USD formatting
    if (amount >= 1000000) {
      return `$${(amount / 1000000).toFixed(2)}M`;
    }
    if (amount >= 10000) {
      return `$${(amount / 1000).toFixed(1)}K`;
    }
    if (amount >= 1000) {
      return `$${amount.toLocaleString('en-US', { maximumFractionDigits: 0 })}`;
    }
    return `$${amount.toFixed(2)}`;
  }
}
