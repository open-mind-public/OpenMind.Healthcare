import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { DietService } from '../../services/diet.service';
import { WeightReading, WeightTrend } from '../../models/diet.models';

interface ChartPoint {
  reading: WeightReading;
  x: number;
  y: number;
}

@Component({
  selector: 'app-weight-tracker',
  standalone: false,
  templateUrl: './weight-tracker.component.html',
  styleUrls: ['./weight-tracker.component.css']
})
export class WeightTrackerComponent implements OnInit {
  trend: WeightTrend | null = null;

  date = new Date().toISOString().substring(0, 10);
  weightKg: number | null = null;

  loading = false;
  error: string | null = null;

  readonly chartWidth = 600;
  readonly chartHeight = 180;

  constructor(private dietService: DietService, private router: Router) {}

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading = true;
    this.error = null;

    this.dietService.getWeightTrend().subscribe({
      next: trend => {
        this.trend = trend;
        this.loading = false;
      },
      error: err => {
        this.loading = false;
        if (err?.status === 404) {
          this.router.navigate(['/diet/setup']);
          return;
        }
        this.error = err?.error?.message ?? 'Could not load your weight history.';
      }
    });
  }

  record(): void {
    if (this.weightKg === null || this.weightKg <= 0) {
      return;
    }

    this.error = null;

    this.dietService.recordWeight(this.date, this.weightKg).subscribe({
      next: trend => {
        this.trend = trend;
        this.weightKg = null;
      },
      error: err => (this.error = err?.error?.message ?? 'That weight did not save.')
    });
  }

  remove(reading: WeightReading): void {
    this.error = null;

    this.dietService.deleteWeightReading(reading.date).subscribe({
      next: () => this.load(),
      // Includes the refusal to delete a plan's only remaining reading, which carries an
      // explanation worth showing verbatim.
      error: err => (this.error = err?.error?.message ?? 'That reading could not be removed.')
    });
  }

  /** A plain polyline - enough to show a direction without pulling in a charting library. */
  get points(): ChartPoint[] {
    const readings = this.trend?.readings ?? [];
    if (readings.length === 0) {
      return [];
    }

    const weights = readings.map(r => r.weightKg);
    const min = Math.min(...weights);
    const max = Math.max(...weights);
    const spread = max - min || 1;

    return readings.map((reading, index) => ({
      reading,
      x: readings.length === 1
        ? this.chartWidth / 2
        : (index / (readings.length - 1)) * this.chartWidth,
      y: this.chartHeight - ((reading.weightKg - min) / spread) * (this.chartHeight - 20) - 10
    }));
  }

  get polyline(): string {
    return this.points.map(p => `${p.x},${p.y}`).join(' ');
  }

  get changeLabel(): string {
    const change = this.trend?.changeKg;
    if (change === null || change === undefined) {
      return '';
    }
    if (change === 0) {
      return 'No change yet';
    }
    return change < 0 ? `${Math.abs(change)} kg lost` : `${change} kg gained`;
  }
}
