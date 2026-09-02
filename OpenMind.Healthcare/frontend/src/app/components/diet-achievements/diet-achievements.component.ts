import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { DietService } from '../../services/diet.service';
import { DietAchievement } from '../../models/diet.models';

@Component({
  selector: 'app-diet-achievements',
  standalone: false,
  templateUrl: './diet-achievements.component.html',
  styleUrls: ['./diet-achievements.component.css']
})
export class DietAchievementsComponent implements OnInit {
  achievements: DietAchievement[] = [];
  newlyUnlocked: DietAchievement[] = [];

  loading = false;
  error: string | null = null;

  constructor(private dietService: DietService, private router: Router) {}

  ngOnInit(): void {
    // Check first, so anything earned since the last visit is celebrated rather than
    // silently appearing in the list.
    this.dietService.checkAchievements().subscribe({
      next: result => {
        this.newlyUnlocked = result.newlyUnlocked;
        this.load();
      },
      error: () => this.load()
    });
  }

  load(): void {
    this.loading = true;
    this.error = null;

    this.dietService.getAchievements().subscribe({
      next: result => {
        this.achievements = result.achievements;
        this.loading = false;
      },
      error: err => {
        this.loading = false;
        if (err?.status === 404) {
          this.router.navigate(['/diet/setup']);
          return;
        }
        this.error = err?.error?.message ?? 'Could not load your achievements.';
      }
    });
  }

  get unlocked(): DietAchievement[] {
    return this.achievements.filter(a => a.unlocked);
  }

  get locked(): DietAchievement[] {
    return this.achievements.filter(a => !a.unlocked);
  }

  remainingLabel(achievement: DietAchievement): string {
    const unit = achievement.remaining === 1 ? 'day' : 'days';
    switch (achievement.criterion) {
      case 'ConsecutiveOnTargetDays':
        return `${achievement.remaining} more ${unit} on target`;
      case 'TotalDaysLogged':
        return `${achievement.remaining} more ${unit} logged`;
      default:
        return `${achievement.remaining} more ${unit} on plan`;
    }
  }
}
