import { Component, Input } from '@angular/core';

@Component({
  selector: 'app-stats-card',
  standalone: false,
  template: `
    <div class="stats-card" [class]="colorClass">
      <div class="icon">{{ icon }}</div>
      <div class="content">
        <div class="value">{{ value }}</div>
        <div class="label">{{ label }}</div>
      </div>
    </div>
  `,
  styles: [`
    .stats-card {
      background: rgba(255, 255, 255, 0.1);
      backdrop-filter: blur(10px);
      border-radius: 20px;
      padding: 25px;
      display: flex;
      align-items: center;
      gap: 20px;
      border: 1px solid rgba(255, 255, 255, 0.1);
      transition: all 0.3s ease;
      
      &:hover {
        transform: translateY(-5px);
        box-shadow: 0 15px 40px rgba(0, 0, 0, 0.2);
      }
      
      &.primary {
        background: linear-gradient(135deg, rgba(16, 185, 129, 0.2), rgba(52, 211, 153, 0.1));
        border-color: rgba(16, 185, 129, 0.3);
      }
      
      &.gold {
        background: linear-gradient(135deg, rgba(245, 158, 11, 0.2), rgba(251, 191, 36, 0.1));
        border-color: rgba(245, 158, 11, 0.3);
      }
      
      &.blue {
        background: linear-gradient(135deg, rgba(59, 130, 246, 0.2), rgba(96, 165, 250, 0.1));
        border-color: rgba(59, 130, 246, 0.3);
      }
      
      &.pink {
        background: linear-gradient(135deg, rgba(236, 72, 153, 0.2), rgba(244, 114, 182, 0.1));
        border-color: rgba(236, 72, 153, 0.3);
      }

      &.danger {
        background: linear-gradient(135deg, rgba(239, 68, 68, 0.2), rgba(248, 113, 113, 0.1));
        border-color: rgba(239, 68, 68, 0.3);
      }
    }
    
    .icon {
      font-size: 48px;
      animation: float 3s ease-in-out infinite;
    }
    
    @keyframes float {
      0%, 100% { transform: translateY(0); }
      50% { transform: translateY(-5px); }
    }
    
    .content {
      flex: 1;
    }
    
    .value {
      font-size: 32px;
      font-weight: 700;
      line-height: 1.2;
    }
    
    .label {
      font-size: 14px;
      color: rgba(255, 255, 255, 0.7);
      margin-top: 5px;
    }
  `]
})
export class StatsCardComponent {
  @Input() icon: string = '';
  @Input() value: string = '';
  @Input() label: string = '';
  @Input() colorClass: string = '';
}
