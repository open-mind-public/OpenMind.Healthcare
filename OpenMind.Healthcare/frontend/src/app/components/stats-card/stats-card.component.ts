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
      background: var(--surface-sunken);
      border-radius: 20px;
      padding: 25px;
      display: flex;
      align-items: center;
      gap: 20px;
      border: 1px solid var(--border);
      transition: all 0.3s ease;
      
      &:hover {
        transform: translateY(-1px);
        box-shadow: var(--shadow-md);
      }
      
      &.primary {
        background: var(--accent-subtle);
        border-color: var(--accent-border);
      }
      
      &.gold {
        background: var(--warning-subtle);
        border-color: var(--warning-border);
      }
      
      &.blue {
        background: var(--info-subtle);
        border-color: var(--info-border);
      }
      
      &.pink {
        background: var(--info-subtle);
        border-color: var(--info-border);
      }

      &.danger {
        background: var(--danger-subtle);
        border-color: var(--danger-border);
      }
    }
    
    .icon {
      font-size: 48px;
      animation: float 3s ease-in-out infinite;
    }
    
    @keyframes float {
      0%, 100% { transform: translateY(0); }
      50% { transform: translateY(-1px); }
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
      color: var(--text-secondary);
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
