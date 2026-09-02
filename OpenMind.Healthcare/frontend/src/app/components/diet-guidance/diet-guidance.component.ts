import { Component, OnInit } from '@angular/core';
import { DietService } from '../../services/diet.service';
import { DailyEncouragement, EatingTip, TipCategory } from '../../models/diet.models';

@Component({
  selector: 'app-diet-guidance',
  standalone: false,
  templateUrl: './diet-guidance.component.html',
  styleUrls: ['./diet-guidance.component.css']
})
export class DietGuidanceComponent implements OnInit {
  tips: EatingTip[] = [];
  encouragement: DailyEncouragement | null = null;

  category: TipCategory | '' = '';
  loading = false;
  error: string | null = null;

  readonly categories: { value: TipCategory | ''; label: string }[] = [
    { value: '', label: 'Everything' },
    { value: 'Craving', label: 'Cravings' },
    { value: 'Planning', label: 'Planning ahead' },
    { value: 'PortionControl', label: 'Portions' },
    { value: 'EatingOut', label: 'Eating out' },
    { value: 'Mindset', label: 'Mindset' }
  ];

  constructor(private dietService: DietService) {}

  ngOnInit(): void {
    this.loadTips();

    this.dietService.getEncouragement().subscribe({
      next: encouragement => (this.encouragement = encouragement),
      // Guidance is still worth showing to someone who has not set up a plan yet.
      error: () => (this.encouragement = null)
    });
  }

  filter(category: TipCategory | ''): void {
    this.category = category;
    this.loadTips();
  }

  private loadTips(): void {
    this.loading = true;
    this.error = null;

    this.dietService.getTips(this.category || undefined).subscribe({
      next: tips => {
        this.tips = tips;
        this.loading = false;
      },
      error: err => {
        this.loading = false;
        this.error = err?.error?.message ?? 'Could not load guidance.';
      }
    });
  }
}
