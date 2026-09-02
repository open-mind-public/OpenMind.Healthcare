import { Component, OnInit } from '@angular/core';
import { ApiService } from '../../services/api.service';
import { MotivationalQuote, ProgressStats, MoneySaved } from '../../models/models';

@Component({
  selector: 'app-motivation',
  standalone: false,
  template: `
    <div class="motivation-page fade-in">
      <div class="page-header">
        <h1>💪 Stay Motivated</h1>
        <p>Remember why you started this journey</p>
      </div>

      <!-- Quote of the day -->
      <div class="quote-section card">
        <div class="quote-icon float">💬</div>
        <blockquote *ngIf="currentQuote">
          "{{ currentQuote.quote }}"
          <cite>— {{ currentQuote.author }}</cite>
        </blockquote>
        <button class="btn btn-secondary" (click)="getNewQuote()">
          🔄 New Quote
        </button>
      </div>

      <!-- Stats reminder -->
      <div class="stats-reminder" *ngIf="stats">
        <h2>Look How Far You've Come! 🌟</h2>
        <div class="reminder-stats">
          <div class="reminder-stat">
            <span class="big-number">{{ stats.daysSmokeFree }}</span>
            <span class="stat-text">Days of Freedom</span>
          </div>
          <div class="reminder-stat gold">
            <span class="big-number">{{ formatMoney(stats.moneySaved) }}</span>
            <span class="stat-text">Money Saved</span>
          </div>
          <div class="reminder-stat blue">
            <span class="big-number">{{ stats.cigarettesNotSmoked | number }}</span>
            <span class="stat-text">Cigarettes NOT Smoked</span>
          </div>
        </div>
      </div>

      <!-- Reasons to stay quit -->
      <div class="reasons-section card">
        <h2>🎯 Remember Your Reasons</h2>
        <div class="reasons-grid">
          <div class="reason-card">
            <span class="reason-icon">🫀</span>
            <h4>Better Health</h4>
            <p>Your heart, lungs, and every organ is healing right now</p>
          </div>
          <div class="reason-card">
            <span class="reason-icon">💰</span>
            <h4>Save Money</h4>
            <p>Think of all the things you can do with the money saved</p>
          </div>
          <div class="reason-card">
            <span class="reason-icon">👨‍👩‍👧‍👦</span>
            <h4>For Your Loved Ones</h4>
            <p>Be there for the people who care about you</p>
          </div>
          <div class="reason-card">
            <span class="reason-icon">🏃</span>
            <h4>More Energy</h4>
            <p>Feel alive, active, and full of energy</p>
          </div>
          <div class="reason-card">
            <span class="reason-icon">😤</span>
            <h4>Freedom</h4>
            <p>No more being controlled by nicotine addiction</p>
          </div>
          <div class="reason-card">
            <span class="reason-icon">✨</span>
            <h4>Self-Pride</h4>
            <p>Prove to yourself that you can overcome anything</p>
          </div>
        </div>
      </div>

      <!-- Affirmations -->
      <div class="affirmations-section">
        <h2>🌈 Daily Affirmations</h2>
        <div class="affirmations-list">
          <div class="affirmation" *ngFor="let affirmation of affirmations">
            <span class="check">✓</span>
            {{ affirmation }}
          </div>
        </div>
      </div>

      <!-- Milestones reminder -->
      <div class="milestones-preview card">
        <h2>🏆 Upcoming Milestones</h2>
        <div class="milestone-list">
          <div class="milestone-item" *ngFor="let milestone of upcomingMilestones">
            <span class="milestone-icon">{{ milestone.icon }}</span>
            <div class="milestone-info">
              <strong>{{ milestone.name }}</strong>
              <span>{{ milestone.daysAway }} days away</span>
            </div>
          </div>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .motivation-page {
      max-width: 900px;
      margin: 0 auto;
      padding: 20px;
    }

    .page-header {
      text-align: center;
      margin-bottom: 40px;
      
      h1 {
        font-size: 36px;
        margin-bottom: 10px;
        color: var(--text);
      }
      
      p {
        color: var(--text-secondary);
        font-size: 18px;
      }
    }

    .quote-section {
      text-align: center;
      padding: 50px;
      margin-bottom: 30px;
      background: var(--info-subtle);
      border-color: var(--info-border);
      
      .quote-icon {
        font-size: 48px;
        margin-bottom: 20px;
        display: block;
      }
      
      blockquote {
        font-size: 24px;
        font-style: italic;
        line-height: 1.6;
        color: var(--text);
        margin-bottom: 25px;
        
        cite {
          display: block;
          margin-top: 15px;
          font-size: 16px;
          font-style: normal;
          color: var(--text-muted);
        }
      }
    }

    .stats-reminder {
      text-align: center;
      margin-bottom: 30px;
      
      h2 {
        font-size: 24px;
        margin-bottom: 25px;
        color: var(--accent);
      }
    }

    .reminder-stats {
      display: flex;
      justify-content: center;
      gap: 30px;
      flex-wrap: wrap;
    }

    .reminder-stat {
      background: var(--accent-subtle);
      border: 1px solid var(--accent-border);
      border-radius: 20px;
      padding: 30px 40px;
      text-align: center;
      min-width: 200px;
      
      .big-number {
        display: block;
        font-size: 42px;
        font-weight: 700;
        color: var(--accent);
        line-height: 1.2;
      }
      
      .stat-text {
        font-size: 14px;
        color: var(--text-muted);
        margin-top: 5px;
      }
      
      &.gold {
        background: var(--warning-subtle);
        border-color: var(--warning-border);
        .big-number { color: var(--warning); }
      }
      
      &.blue {
        background: var(--info-subtle);
        border-color: var(--info-border);
        .big-number { color: var(--info); }
      }
    }

    .reasons-section {
      margin-bottom: 30px;
      
      h2 {
        text-align: center;
        margin-bottom: 30px;
        font-size: 24px;
      }
    }

    .reasons-grid {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(250px, 1fr));
      gap: 20px;
    }

    .reason-card {
      background: var(--surface-sunken);
      border-radius: 16px;
      padding: 25px;
      text-align: center;
      transition: all 0.3s ease;
      
      &:hover {
        transform: translateY(-1px);
        background: var(--surface-sunken);
      }
      
      .reason-icon {
        font-size: 40px;
        display: block;
        margin-bottom: 15px;
      }
      
      h4 {
        font-size: 18px;
        margin-bottom: 10px;
        color: var(--info);
      }
      
      p {
        font-size: 14px;
        color: var(--text-muted);
        line-height: 1.5;
      }
    }

    .affirmations-section {
      margin-bottom: 30px;
      
      h2 {
        text-align: center;
        margin-bottom: 25px;
        font-size: 24px;
      }
    }

    .affirmations-list {
      display: grid;
      gap: 12px;
    }

    .affirmation {
      display: flex;
      align-items: center;
      gap: 15px;
      padding: 18px 25px;
      background: var(--surface-sunken);
      border-radius: 12px;
      font-size: 16px;
      transition: all 0.3s ease;
      
      &:hover {
        background: var(--surface-sunken);
        transform: translateX(10px);
      }
      
      .check {
        color: var(--accent);
        font-weight: bold;
        font-size: 20px;
      }
    }

    .milestones-preview {
      h2 {
        margin-bottom: 25px;
        font-size: 22px;
        text-align: center;
      }
    }

    .milestone-list {
      display: grid;
      gap: 15px;
    }

    .milestone-item {
      display: flex;
      align-items: center;
      gap: 20px;
      padding: 20px;
      background: var(--surface-sunken);
      border-radius: 12px;
      
      .milestone-icon {
        font-size: 36px;
      }
      
      .milestone-info {
        strong {
          display: block;
          font-size: 16px;
          margin-bottom: 4px;
          color: var(--warning);
        }
        
        span {
          font-size: 13px;
          color: var(--text-muted);
        }
      }
    }

    @media (max-width: 600px) {
      .reminder-stats {
        flex-direction: column;
        align-items: center;
      }
      
      .quote-section blockquote {
        font-size: 18px;
      }
    }
  `]
})
export class MotivationComponent implements OnInit {
  currentQuote: MotivationalQuote | null = null;
  stats: ProgressStats | null = null;
  
  affirmations = [
    "I am stronger than my cravings",
    "Every smoke-free day makes me healthier",
    "I choose life and health over addiction",
    "I am proud of how far I've come",
    "My lungs are healing with every breath",
    "I deserve a smoke-free, healthy life",
    "I am in control of my choices",
    "Each day smoke-free is a victory"
  ];

  upcomingMilestones: any[] = [];

  constructor(private apiService: ApiService) {}

  ngOnInit(): void {
    this.getNewQuote();
    this.loadStats();
    this.loadMilestones();
  }

  getNewQuote(): void {
    this.apiService.getRandomQuote().subscribe({
      next: (quote) => {
        this.currentQuote = quote;
      }
    });
  }

  loadStats(): void {
    this.apiService.getStats().subscribe({
      next: (stats) => {
        this.stats = stats;
      }
    });
  }

  loadMilestones(): void {
    this.apiService.getAchievements().subscribe({
      next: (achievements) => {
        this.apiService.getStats().subscribe({
          next: (stats) => {
            this.upcomingMilestones = achievements
              .filter(a => !a.isUnlocked)
              .slice(0, 3)
              .map(a => ({
                ...a,
                daysAway: a.requiredDays - stats.daysSmokeFree
              }));
          }
        });
      }
    });
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
    return `$${Math.round(amount)}`;
  }
}
