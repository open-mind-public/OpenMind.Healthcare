import { Component, OnInit, OnDestroy } from '@angular/core';
import { Router } from '@angular/router';
import { interval, Subscription } from 'rxjs';
import { ApiService } from '../../services/api.service';
import { ProgressStats, DailyEncouragement, UserProgress, MoneySaved } from '../../models/models';

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
            icon="❤️"
            [value]="(stats.hoursSmokeFree | number) || '0'"
            label="Hours Smoke-Free"
            colorClass="pink">
          </app-stats-card>
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

    .stats-grid {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(250px, 1fr));
      gap: 20px;
      margin-bottom: 30px;
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
    }
  `]
})
export class DashboardComponent implements OnInit, OnDestroy {
  stats: ProgressStats | null = null;
  encouragement: DailyEncouragement | null = null;
  hasProgress = false;
  
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
    });
  }

  ngOnDestroy(): void {
    this.refreshSubscription?.unsubscribe();
  }

  loadData(): void {
    this.apiService.getProgress().subscribe({
      next: (progress) => {
        this.hasProgress = true;
        this.loadStats();
        this.loadEncouragement();
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
