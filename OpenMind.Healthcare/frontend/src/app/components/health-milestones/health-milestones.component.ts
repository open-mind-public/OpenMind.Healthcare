import { Component, OnInit } from '@angular/core';
import { ApiService } from '../../services/api.service';
import { HealthMilestone } from '../../models/models';

@Component({
  selector: 'app-health-milestones',
  standalone: false,
  template: `
    <div class="health-page fade-in">
      <div class="page-header">
        <h1>❤️ Your Health Recovery</h1>
        <p>Watch your body heal and recover from smoking</p>
      </div>

      <div class="health-summary">
        <div class="summary-item achieved">
          <span class="icon">✅</span>
          <span class="count">{{ achievedCount }}</span>
          <span class="label">Milestones Achieved</span>
        </div>
        <div class="summary-item upcoming">
          <span class="icon">🎯</span>
          <span class="count">{{ upcomingCount }}</span>
          <span class="label">More to Go</span>
        </div>
      </div>

      <div class="timeline">
        <div 
          *ngFor="let milestone of milestones; let i = index" 
          class="timeline-item"
          [class.achieved]="milestone.isAchieved"
          [class.upcoming]="!milestone.isAchieved">
          
          <div class="timeline-marker">
            <div class="marker-icon" [class.pulse]="milestone.isAchieved">
              {{ milestone.icon }}
            </div>
            <div class="timeline-line" *ngIf="i < milestones.length - 1"></div>
          </div>
          
          <div class="timeline-content">
            <div class="milestone-header">
              <h3>{{ milestone.title }}</h3>
              <span class="time-badge">{{ milestone.timeDisplay }}</span>
            </div>
            <p>{{ milestone.description }}</p>
            
            <div class="progress-section" *ngIf="!milestone.isAchieved">
              <div class="progress-bar">
                <div class="progress-fill" [style.width.%]="milestone.progressPercentage"></div>
              </div>
              <span class="progress-text">{{ milestone.progressPercentage | number:'1.0-1' }}% complete</span>
            </div>
            
            <div class="achieved-badge" *ngIf="milestone.isAchieved">
              <span>✨ Achieved!</span>
            </div>
          </div>
        </div>
      </div>

      <div class="health-tips card">
        <h2>💡 Health Tips for Your Journey</h2>
        <ul>
          <li>
            <span class="tip-icon">💧</span>
            <div>
              <strong>Stay Hydrated</strong>
              <p>Drinking water helps flush toxins from your body faster</p>
            </div>
          </li>
          <li>
            <span class="tip-icon">🏃</span>
            <div>
              <strong>Exercise Regularly</strong>
              <p>Physical activity speeds up lung recovery and reduces cravings</p>
            </div>
          </li>
          <li>
            <span class="tip-icon">🥗</span>
            <div>
              <strong>Eat Antioxidant-Rich Foods</strong>
              <p>Fruits and vegetables help repair cellular damage</p>
            </div>
          </li>
          <li>
            <span class="tip-icon">😴</span>
            <div>
              <strong>Get Quality Sleep</strong>
              <p>Your body heals faster when you're well-rested</p>
            </div>
          </li>
        </ul>
      </div>
    </div>
  `,
  styles: [`
    .health-page {
      max-width: 800px;
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

    .health-summary {
      display: flex;
      justify-content: center;
      gap: 40px;
      margin-bottom: 50px;
      
      .summary-item {
        display: flex;
        flex-direction: column;
        align-items: center;
        padding: 25px 40px;
        border-radius: 20px;
        
        .icon {
          font-size: 32px;
          margin-bottom: 10px;
        }
        
        .count {
          font-size: 42px;
          font-weight: 700;
        }
        
        .label {
          font-size: 14px;
          color: var(--text-muted);
          margin-top: 5px;
        }
        
        &.achieved {
          background: var(--accent-subtle);
          border: 1px solid var(--accent-border);
          
          .count { color: var(--accent); }
        }
        
        &.upcoming {
          background: var(--info-subtle);
          border: 1px solid var(--info-border);
          
          .count { color: var(--info); }
        }
      }
    }

    .timeline {
      position: relative;
      margin-bottom: 50px;
    }

    .timeline-item {
      display: flex;
      gap: 25px;
      margin-bottom: 0;
      padding-bottom: 30px;
      
      &.achieved {
        .timeline-content {
          background: var(--accent-subtle);
          border-color: var(--accent-border);
        }
        
        .timeline-line {
          background: var(--accent);
        }
      }
      
      &.upcoming {
        opacity: 0.7;
        
        .marker-icon {
          filter: grayscale(50%);
        }
      }
    }

    .timeline-marker {
      display: flex;
      flex-direction: column;
      align-items: center;
      flex-shrink: 0;
      width: 60px;
    }

    .marker-icon {
      width: 60px;
      height: 60px;
      background: var(--surface-sunken);
      border-radius: 50%;
      display: flex;
      align-items: center;
      justify-content: center;
      font-size: 28px;
      
      &.pulse {
        animation: pulse 2s infinite;
        background: var(--accent-border);
      }
    }

    @keyframes pulse {
      0%, 100% { transform: scale(1); }
      50% { transform: scale(1.1); }
    }

    .timeline-line {
      width: 3px;
      flex: 1;
      background: var(--surface-sunken);
      margin-top: 10px;
      min-height: 50px;
    }

    .timeline-content {
      flex: 1;
      background: var(--surface-sunken);
      border-radius: 16px;
      padding: 20px 25px;
      border: 1px solid var(--border);
      transition: all 0.3s ease;
      
      &:hover {
        transform: translateX(5px);
      }
    }

    .milestone-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-bottom: 10px;
      
      h3 {
        font-size: 18px;
        color: var(--info);
      }
      
      .time-badge {
        background: var(--surface-sunken);
        padding: 5px 12px;
        border-radius: 20px;
        font-size: 12px;
        font-weight: 600;
      }
    }

    .timeline-content p {
      color: var(--text-secondary);
      font-size: 14px;
      line-height: 1.6;
    }

    .progress-section {
      margin-top: 15px;
      
      .progress-bar {
        height: 8px;
        background: var(--surface-sunken);
        border-radius: 4px;
        overflow: hidden;
        margin-bottom: 8px;
        
        .progress-fill {
          height: 100%;
          background: var(--info);
          border-radius: 4px;
          transition: width 1s ease;
        }
      }
      
      .progress-text {
        font-size: 12px;
        color: var(--text-muted);
      }
    }

    .achieved-badge {
      margin-top: 15px;
      
      span {
        background: var(--accent);
        color: var(--text-on-accent);
        padding: 6px 14px;
        border-radius: 20px;
        font-size: 13px;
        font-weight: 600;
      }
    }

    .health-tips {
      h2 {
        margin-bottom: 25px;
        font-size: 22px;
        color: var(--warning);
      }
      
      ul {
        list-style: none;
        
        li {
          display: flex;
          gap: 15px;
          padding: 15px;
          background: var(--surface-sunken);
          border-radius: 12px;
          margin-bottom: 12px;
          transition: all 0.3s ease;
          
          &:hover {
            background: var(--surface-sunken);
            transform: translateX(5px);
          }
          
          .tip-icon {
            font-size: 28px;
            flex-shrink: 0;
          }
          
          strong {
            display: block;
            margin-bottom: 5px;
            color: var(--warning);
          }
          
          p {
            font-size: 14px;
            color: var(--text-muted);
            margin: 0;
          }
        }
      }
    }

    @media (max-width: 600px) {
      .health-summary {
        flex-direction: column;
        gap: 15px;
      }
      
      .timeline-item {
        gap: 15px;
      }
      
      .timeline-marker {
        width: 50px;
      }
      
      .marker-icon {
        width: 50px;
        height: 50px;
        font-size: 24px;
      }
    }
  `]
})
export class HealthMilestonesComponent implements OnInit {
  milestones: HealthMilestone[] = [];
  achievedCount = 0;
  upcomingCount = 0;

  constructor(private apiService: ApiService) {}

  ngOnInit(): void {
    this.loadMilestones();
  }

  loadMilestones(): void {
    this.apiService.getHealthMilestones().subscribe({
      next: (milestones) => {
        this.milestones = milestones;
        this.achievedCount = milestones.filter(m => m.isAchieved).length;
        this.upcomingCount = milestones.filter(m => !m.isAchieved).length;
      }
    });
  }
}
