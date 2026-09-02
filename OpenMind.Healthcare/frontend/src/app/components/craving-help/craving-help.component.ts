import { Component, OnInit } from '@angular/core';
import { ApiService } from '../../services/api.service';
import { CravingTip } from '../../models/models';

@Component({
  selector: 'app-craving-help',
  standalone: false,
  template: `
    <div class="craving-page fade-in">
      <div class="emergency-header">
        <h1>🆘 Craving Help</h1>
        <p>You've got this! Here are some ways to beat this craving.</p>
      </div>

      <!-- Emergency breathing exercise -->
      <div class="breathing-section card">
        <h2>🌬️ Quick Breathing Exercise</h2>
        <p>Follow this breathing pattern to calm your craving:</p>
        
        <div class="breathing-exercise">
          <div class="breath-circle" [class]="breathingPhase">
            <span class="breath-text">{{ breathingText }}</span>
            <span class="breath-timer">{{ breathingTimer }}s</span>
          </div>
        </div>
        
        <button 
          class="btn btn-primary" 
          (click)="toggleBreathing()">
          {{ isBreathing ? 'Stop' : 'Start Breathing Exercise' }}
        </button>
      </div>

      <!-- Quick tips -->
      <div class="quick-tips">
        <h2>⚡ Quick Tips</h2>
        <div class="tips-grid">
          <div 
            *ngFor="let tip of tips" 
            class="tip-card"
            (click)="selectTip(tip)">
            <span class="tip-icon">{{ tip.icon }}</span>
            <h3>{{ tip.title }}</h3>
            <p>{{ tip.description }}</p>
          </div>
        </div>
      </div>

      <!-- Expanded tip -->
      <div class="selected-tip card" *ngIf="selectedTip">
        <button class="close-btn" (click)="selectedTip = null">×</button>
        <div class="tip-detail">
          <span class="big-icon float">{{ selectedTip.icon }}</span>
          <h2>{{ selectedTip.title }}</h2>
          <p>{{ selectedTip.description }}</p>
          <div class="tip-action">
            <p>Try this technique right now! Most cravings pass within 10 minutes.</p>
          </div>
        </div>
      </div>

      <!-- Craving countdown -->
      <div class="countdown-section card">
        <h2>⏱️ Craving Countdown</h2>
        <p>Most cravings only last 3-5 minutes. Start the countdown and wait it out!</p>
        
        <div class="countdown-display">
          <span class="countdown-number">{{ countdownMinutes }}:{{ countdownSeconds.toString().padStart(2, '0') }}</span>
        </div>
        
        <div class="countdown-controls">
          <button class="btn btn-primary" (click)="startCountdown()" *ngIf="!countdownActive">
            Start 5 Minute Countdown
          </button>
          <button class="btn btn-danger" (click)="stopCountdown()" *ngIf="countdownActive">
            Stop
          </button>
        </div>
        
        <div class="countdown-message" *ngIf="countdownComplete">
          🎉 You did it! The craving has passed. You're amazing!
        </div>
      </div>

      <!-- Emergency contacts / distractions -->
      <div class="distractions-section">
        <h2>🎮 Distraction Ideas</h2>
        <div class="distraction-list">
          <div class="distraction-item" *ngFor="let distraction of distractions">
            <span class="distraction-icon">{{ distraction.icon }}</span>
            <span>{{ distraction.text }}</span>
          </div>
        </div>
      </div>

      <!-- Motivational reminder -->
      <div class="reminder-section card">
        <h2>💪 Remember...</h2>
        <ul class="reminder-list">
          <li>This craving WILL pass - usually in just a few minutes</li>
          <li>You've already made it this far - don't give up now!</li>
          <li>Every craving you overcome makes you stronger</li>
          <li>Think about how proud you'll feel tomorrow morning</li>
          <li>Your body is healing right now - don't interrupt the process</li>
        </ul>
      </div>
    </div>
  `,
  styles: [`
    .craving-page {
      max-width: 900px;
      margin: 0 auto;
      padding: 20px;
    }

    .emergency-header {
      text-align: center;
      margin-bottom: 40px;
      padding: 30px;
      background: var(--danger-subtle);
      border-radius: 20px;
      border: 1px solid var(--danger-border);
      
      h1 {
        font-size: 42px;
        margin-bottom: 10px;
        color: var(--danger);
      }
      
      p {
        font-size: 18px;
        color: var(--text-secondary);
      }
    }

    .breathing-section {
      text-align: center;
      margin-bottom: 30px;
      
      h2 {
        margin-bottom: 15px;
        color: var(--info);
      }
      
      > p {
        color: var(--text-secondary);
        margin-bottom: 30px;
      }
    }

    .breathing-exercise {
      display: flex;
      justify-content: center;
      margin-bottom: 30px;
    }

    .breath-circle {
      width: 200px;
      height: 200px;
      border-radius: 50%;
      display: flex;
      flex-direction: column;
      align-items: center;
      justify-content: center;
      background: var(--info-subtle);
      border: 3px solid var(--info-border);
      transition: all 1s ease;
      
      &.inhale {
        transform: scale(1.2);
        background: var(--accent-subtle);
        border-color: var(--accent-border);
      }
      
      &.hold {
        transform: scale(1.2);
        background: var(--warning-subtle);
        border-color: var(--warning-border);
      }
      
      &.exhale {
        transform: scale(1);
        background: var(--info-subtle);
        border-color: var(--info-border);
      }
      
      .breath-text {
        font-size: 24px;
        font-weight: 600;
        margin-bottom: 5px;
      }
      
      .breath-timer {
        font-size: 36px;
        font-weight: 700;
        color: var(--info);
      }
    }

    .quick-tips {
      margin-bottom: 30px;
      
      h2 {
        text-align: center;
        margin-bottom: 25px;
      }
    }

    .tips-grid {
      display: grid;
      grid-template-columns: repeat(auto-fill, minmax(200px, 1fr));
      gap: 15px;
    }

    .tip-card {
      background: var(--surface-sunken);
      border-radius: 16px;
      padding: 20px;
      text-align: center;
      cursor: pointer;
      transition: all 0.3s ease;
      border: 1px solid transparent;
      
      &:hover {
        background: var(--surface-sunken);
        transform: translateY(-1px);
        border-color: var(--accent-border);
      }
      
      .tip-icon {
        font-size: 36px;
        display: block;
        margin-bottom: 10px;
      }
      
      h3 {
        font-size: 16px;
        margin-bottom: 8px;
        color: var(--accent);
      }
      
      p {
        font-size: 13px;
        color: var(--text-muted);
        line-height: 1.4;
      }
    }

    .selected-tip {
      position: relative;
      text-align: center;
      margin-bottom: 30px;
      background: var(--accent-subtle);
      border-color: var(--accent-border);
      
      .close-btn {
        position: absolute;
        top: 15px;
        right: 15px;
        background: none;
        border: none;
        color: var(--text);
        font-size: 28px;
        cursor: pointer;
        opacity: 0.7;
        
        &:hover {
          opacity: 1;
        }
      }
      
      .big-icon {
        font-size: 64px;
        display: block;
        margin-bottom: 20px;
      }
      
      h2 {
        font-size: 28px;
        margin-bottom: 15px;
        color: var(--accent);
      }
      
      p {
        font-size: 18px;
        color: var(--text-secondary);
        line-height: 1.6;
      }
      
      .tip-action {
        margin-top: 25px;
        padding: 15px 20px;
        background: var(--surface-sunken);
        border-radius: 12px;
        
        p {
          font-size: 14px;
          margin: 0;
          color: var(--text-secondary);
        }
      }
    }

    .countdown-section {
      text-align: center;
      margin-bottom: 30px;
      
      h2 {
        margin-bottom: 10px;
        color: var(--warning);
      }
      
      > p {
        color: var(--text-secondary);
        margin-bottom: 25px;
      }
    }

    .countdown-display {
      margin-bottom: 25px;
      
      .countdown-number {
        font-size: 72px;
        font-weight: 700;
        color: var(--warning);
      }
    }

    .countdown-controls {
      margin-bottom: 20px;
    }

    .countdown-message {
      padding: 20px;
      background: var(--accent-subtle);
      border-radius: 12px;
      font-size: 18px;
      font-weight: 600;
      color: var(--accent);
      animation: celebrate 0.5s ease;
    }

    @keyframes celebrate {
      0% { transform: scale(0.8); opacity: 0; }
      50% { transform: none; }
      100% { transform: scale(1); opacity: 1; }
    }

    .distractions-section {
      margin-bottom: 30px;
      
      h2 {
        text-align: center;
        margin-bottom: 20px;
      }
    }

    .distraction-list {
      display: grid;
      grid-template-columns: repeat(auto-fill, minmax(200px, 1fr));
      gap: 12px;
    }

    .distraction-item {
      display: flex;
      align-items: center;
      gap: 12px;
      padding: 15px 20px;
      background: var(--surface-sunken);
      border-radius: 12px;
      transition: all 0.3s ease;
      
      &:hover {
        background: var(--surface-sunken);
        transform: translateX(5px);
      }
      
      .distraction-icon {
        font-size: 24px;
      }
    }

    .reminder-section {
      background: var(--info-subtle);
      border-color: var(--info-border);
      
      h2 {
        margin-bottom: 20px;
        color: var(--info);
      }
    }

    .reminder-list {
      list-style: none;
      
      li {
        padding: 12px 0;
        padding-left: 30px;
        position: relative;
        color: var(--text-secondary);
        line-height: 1.5;
        
        &:before {
          content: '💜';
          position: absolute;
          left: 0;
        }
        
        &:not(:last-child) {
          border-bottom: 1px solid var(--border);
        }
      }
    }

    @media (max-width: 600px) {
      .breath-circle {
        width: 150px;
        height: 150px;
      }
      
      .countdown-display .countdown-number {
        font-size: 48px;
      }
    }
  `]
})
export class CravingHelpComponent implements OnInit {
  tips: CravingTip[] = [];
  selectedTip: CravingTip | null = null;
  
  // Breathing exercise
  isBreathing = false;
  breathingPhase = '';
  breathingText = 'Ready';
  breathingTimer = 0;
  breathingInterval: any;

  // Countdown
  countdownActive = false;
  countdownMinutes = 5;
  countdownSeconds = 0;
  countdownComplete = false;
  countdownInterval: any;

  distractions = [
    { icon: '🚶', text: 'Take a short walk' },
    { icon: '🎵', text: 'Listen to music' },
    { icon: '📱', text: 'Call a friend' },
    { icon: '🧩', text: 'Play a quick game' },
    { icon: '🍎', text: 'Eat a healthy snack' },
    { icon: '💧', text: 'Drink a glass of water' },
    { icon: '📺', text: 'Watch a funny video' },
    { icon: '🪥', text: 'Brush your teeth' }
  ];

  constructor(private apiService: ApiService) {}

  ngOnInit(): void {
    this.loadTips();
  }

  loadTips(): void {
    this.apiService.getCravingTips().subscribe({
      next: (tips) => {
        this.tips = tips;
      }
    });
  }

  selectTip(tip: CravingTip): void {
    this.selectedTip = tip;
  }

  toggleBreathing(): void {
    if (this.isBreathing) {
      this.stopBreathing();
    } else {
      this.startBreathing();
    }
  }

  startBreathing(): void {
    this.isBreathing = true;
    this.runBreathingCycle();
  }

  stopBreathing(): void {
    this.isBreathing = false;
    clearInterval(this.breathingInterval);
    this.breathingPhase = '';
    this.breathingText = 'Ready';
    this.breathingTimer = 0;
  }

  runBreathingCycle(): void {
    const phases = [
      { name: 'inhale', text: 'Breathe In', duration: 4 },
      { name: 'hold', text: 'Hold', duration: 4 },
      { name: 'exhale', text: 'Breathe Out', duration: 4 }
    ];
    
    let phaseIndex = 0;
    
    const runPhase = () => {
      if (!this.isBreathing) return;
      
      const phase = phases[phaseIndex];
      this.breathingPhase = phase.name;
      this.breathingText = phase.text;
      this.breathingTimer = phase.duration;
      
      this.breathingInterval = setInterval(() => {
        this.breathingTimer--;
        
        if (this.breathingTimer <= 0) {
          clearInterval(this.breathingInterval);
          phaseIndex = (phaseIndex + 1) % phases.length;
          runPhase();
        }
      }, 1000);
    };
    
    runPhase();
  }

  startCountdown(): void {
    this.countdownActive = true;
    this.countdownComplete = false;
    this.countdownMinutes = 5;
    this.countdownSeconds = 0;
    
    this.countdownInterval = setInterval(() => {
      if (this.countdownSeconds === 0) {
        if (this.countdownMinutes === 0) {
          this.completeCountdown();
          return;
        }
        this.countdownMinutes--;
        this.countdownSeconds = 59;
      } else {
        this.countdownSeconds--;
      }
    }, 1000);
  }

  stopCountdown(): void {
    this.countdownActive = false;
    clearInterval(this.countdownInterval);
    this.countdownMinutes = 5;
    this.countdownSeconds = 0;
  }

  completeCountdown(): void {
    this.countdownActive = false;
    this.countdownComplete = true;
    clearInterval(this.countdownInterval);
  }
}
