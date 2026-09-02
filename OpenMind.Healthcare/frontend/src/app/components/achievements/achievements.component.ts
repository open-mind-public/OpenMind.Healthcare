import { Component, OnInit } from '@angular/core';
import { ApiService } from '../../services/api.service';
import { Achievement } from '../../models/models';

@Component({
  selector: 'app-achievements',
  standalone: false,
  template: `
    <div class="achievements-page fade-in">
      <div class="page-header">
        <h1>🏆 Your Achievements</h1>
        <p>Celebrate every milestone on your smoke-free journey</p>
      </div>

      <div class="achievements-stats">
        <div class="stat-item">
          <span class="stat-value">{{ unlockedCount }}</span>
          <span class="stat-label">Unlocked</span>
        </div>
        <div class="stat-divider"></div>
        <div class="stat-item">
          <span class="stat-value">{{ totalCount }}</span>
          <span class="stat-label">Total</span>
        </div>
      </div>

      <div class="achievements-grid">
        <div 
          *ngFor="let achievement of achievements" 
          class="achievement-card"
          [class.unlocked]="achievement.isUnlocked"
          [class.locked]="!achievement.isUnlocked">
          <div class="achievement-icon" [class.celebrate]="achievement.isUnlocked">
            {{ achievement.icon }}
          </div>
          <div class="achievement-content">
            <h3>{{ achievement.name }}</h3>
            <p>{{ achievement.description }}</p>
            <div class="achievement-meta">
              <span class="days-required">{{ achievement.requiredDays }} days</span>
              <span *ngIf="achievement.isUnlocked && achievement.unlockedAt" class="unlocked-date">
                ✓ Unlocked
              </span>
              <span *ngIf="!achievement.isUnlocked" class="locked-status">
                🔒 Keep going!
              </span>
            </div>
          </div>
        </div>
      </div>

      <div class="encouragement-banner" *ngIf="nextAchievement">
        <span class="banner-icon">🎯</span>
        <div class="banner-content">
          <h3>Next Achievement: {{ nextAchievement.name }}</h3>
          <p>{{ daysToNext }} more days to unlock!</p>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .achievements-page {
      max-width: 1000px;
      margin: 0 auto;
      padding: 20px;
    }

    .page-header {
      text-align: center;
      margin-bottom: 40px;
      
      h1 {
        font-size: 36px;
        margin-bottom: 10px;
        color: var(--warning);
      }
      
      p {
        color: var(--text-secondary);
        font-size: 18px;
      }
    }

    .achievements-stats {
      display: flex;
      justify-content: center;
      align-items: center;
      gap: 40px;
      margin-bottom: 40px;
      padding: 30px;
      background: var(--surface-sunken);
      border-radius: 20px;
      
      .stat-item {
        text-align: center;
        
        .stat-value {
          display: block;
          font-size: 48px;
          font-weight: 700;
          color: var(--warning);
        }
        
        .stat-label {
          font-size: 16px;
          color: var(--text-muted);
        }
      }
      
      .stat-divider {
        width: 2px;
        height: 60px;
        background: var(--border);
      }
    }

    .achievements-grid {
      display: grid;
      grid-template-columns: repeat(auto-fill, minmax(300px, 1fr));
      gap: 20px;
      margin-bottom: 40px;
    }

    .achievement-card {
      background: var(--surface-sunken);
      border-radius: 20px;
      padding: 25px;
      display: flex;
      gap: 20px;
      border: 2px solid transparent;
      transition: all 0.3s ease;
      
      &.unlocked {
        background: var(--warning-subtle);
        border-color: var(--warning-border);
        
        &:hover {
          transform: translateY(-1px);
          box-shadow: var(--shadow-md);
        }
      }
      
      &.locked {
        opacity: 0.6;
        filter: grayscale(50%);
        
        .achievement-icon {
          filter: grayscale(100%);
        }
      }
    }

    .achievement-icon {
      font-size: 48px;
      flex-shrink: 0;
      
      &.celebrate {
        animation: celebrate 1s ease-in-out;
      }
    }

    @keyframes celebrate {
      0% { transform: scale(1) rotate(0deg); }
      25% { transform: scale(1.2) rotate(-10deg); }
      50% { transform: scale(1.3) rotate(10deg); }
      75% { transform: scale(1.2) rotate(-10deg); }
      100% { transform: scale(1) rotate(0deg); }
    }

    .achievement-content {
      flex: 1;
      
      h3 {
        font-size: 18px;
        margin-bottom: 8px;
        color: var(--warning);
      }
      
      p {
        font-size: 14px;
        color: var(--text-secondary);
        line-height: 1.5;
        margin-bottom: 12px;
      }
    }

    .achievement-meta {
      display: flex;
      justify-content: space-between;
      align-items: center;
      font-size: 12px;
      
      .days-required {
        background: var(--surface-sunken);
        padding: 4px 10px;
        border-radius: 12px;
      }
      
      .unlocked-date {
        color: var(--accent);
        font-weight: 600;
      }
      
      .locked-status {
        color: var(--text-muted);
      }
    }

    .encouragement-banner {
      display: flex;
      align-items: center;
      gap: 20px;
      padding: 30px;
      background: var(--info-subtle);
      border-radius: 20px;
      border: 1px solid var(--info-border);
      
      .banner-icon {
        font-size: 48px;
        animation: float 3s ease-in-out infinite;
      }
      
      .banner-content {
        h3 {
          font-size: 20px;
          margin-bottom: 5px;
          color: var(--info);
        }
        
        p {
          color: var(--text-secondary);
        }
      }
    }

    @keyframes float {
      0%, 100% { transform: translateY(0); }
      50% { transform: translateY(-1px); }
    }
  `]
})
export class AchievementsComponent implements OnInit {
  achievements: Achievement[] = [];
  unlockedCount = 0;
  totalCount = 0;
  nextAchievement: Achievement | null = null;
  daysToNext = 0;

  constructor(private apiService: ApiService) {}

  ngOnInit(): void {
    this.loadAchievements();
  }

  loadAchievements(): void {
    this.apiService.getAchievements().subscribe({
      next: (achievements) => {
        this.achievements = achievements;
        this.totalCount = achievements.length;
        this.unlockedCount = achievements.filter(a => a.isUnlocked).length;
        
        // Find next achievement
        this.nextAchievement = achievements.find(a => !a.isUnlocked) || null;
        
        if (this.nextAchievement) {
          this.apiService.getStats().subscribe({
            next: (stats) => {
              this.daysToNext = this.nextAchievement!.requiredDays - stats.daysSmokeFree;
            }
          });
        }
      }
    });
  }
}
