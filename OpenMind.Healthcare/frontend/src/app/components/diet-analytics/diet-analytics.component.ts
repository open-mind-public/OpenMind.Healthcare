import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { DietService } from '../../services/diet.service';
import {
  DailyIntakePoint,
  EatingPatterns,
  HabitInsights,
  IntakeAnalysis,
  IntakeTrend,
  MacroAnalysis,
  Observations,
  PeriodPreset
} from '../../models/diet.models';

/** Which figure the trend line is plotting. */
type TrendSeries = 'calories' | 'protein' | 'carbs' | 'fat';

interface SeriesChoice {
  value: TrendSeries;
  label: string;
  unit: string;
}

interface PeriodChoice {
  value: PeriodPreset;
  label: string;
}

/**
 * What a member's diet history says.
 *
 * Every figure here is derived on demand from the log, so it can never drift out of step with the
 * data it describes. Nothing on this page is a target, an allowance or a verdict — and no figure
 * combines exercise with intake (FR-023).
 */
@Component({
  selector: 'app-diet-analytics',
  standalone: false,
  templateUrl: './diet-analytics.component.html',
  styleUrls: ['./diet-analytics.component.css']
})
export class DietAnalyticsComponent implements OnInit {
  readonly periods: PeriodChoice[] = [
    { value: 'Week', label: 'Last 7 days' },
    { value: 'Month', label: 'Last 30 days' },
    { value: 'Quarter', label: 'Last 90 days' },
    { value: 'Plan', label: 'Whole plan' }
  ];

  period: PeriodPreset = 'Month';

  intake: IntakeAnalysis | null = null;
  loadingIntake = false;
  intakeError: string | null = null;

  macros: MacroAnalysis | null = null;
  loadingMacros = false;

  patterns: EatingPatterns | null = null;
  loadingPatterns = false;

  noticed: Observations | null = null;
  loadingObservations = false;

  trend: IntakeTrend | null = null;
  loadingTrend = false;

  habits: HabitInsights | null = null;
  loadingHabits = false;

  readonly seriesChoices: SeriesChoice[] = [
    { value: 'calories', label: 'Calories', unit: 'kcal' },
    { value: 'protein', label: 'Protein', unit: 'g' },
    { value: 'carbs', label: 'Carbohydrate', unit: 'g' },
    { value: 'fat', label: 'Fat', unit: 'g' }
  ];

  series: TrendSeries = 'calories';

  /** Logical chart size. CSS scales it; the viewBox keeps the stroke even. */
  readonly chart = { width: 720, height: 200, left: 46, right: 10, top: 12, bottom: 24 };

  constructor(private dietService: DietService, private router: Router) {}

  ngOnInit(): void {
    this.load();
  }

  choosePeriod(period: PeriodPreset): void {
    if (period === this.period) {
      return;
    }
    this.period = period;
    this.load();
  }

  load(): void {
    this.loadIntake();
    this.loadMacros();
    this.loadPatterns();
    this.loadObservations();
    this.loadTrend();
    this.loadHabits();
  }

  chooseSeries(series: TrendSeries): void {
    this.series = series;
  }

  /** True once the member has logged something in the period worth breaking down. */
  get hasIntake(): boolean {
    return (this.intake?.summary.averagedOverDays ?? 0) > 0;
  }

  /**
   * How the average should be described. The denominator is never dropped: an average over three
   * days shown beside the word "month" is the easiest lie this page could tell (FR-003).
   */
  get averageCaption(): string {
    if (!this.intake) {
      return '';
    }

    const days = this.intake.summary.averagedOverDays;
    return `averaged over ${days} logged ${days === 1 ? 'day' : 'days'}`;
  }

  get intakeChange(): number | null {
    const previous = this.intake?.summary.previousAverageDailyKilocalories;
    if (previous === null || previous === undefined || !this.intake) {
      return null;
    }
    return this.intake.summary.averageDailyKilocalories - previous;
  }

  /** Bar width as a percentage of the largest value in the same chart. */
  barWidth(value: number, largest: number): string {
    if (largest <= 0) {
      return '0%';
    }
    return `${Math.max(2, Math.round((value / largest) * 100))}%`;
  }

  get largestMeal(): number {
    return Math.max(0, ...(this.intake?.meals ?? []).map(m => m.kilocalories));
  }

  get largestCategory(): number {
    return Math.max(0, ...(this.intake?.categories ?? []).map(c => c.kilocalories));
  }

  get largestFood(): number {
    return Math.max(0, ...(this.intake?.topFoods ?? []).map(f => f.kilocalories));
  }

  /** Categories with nothing logged are dropped from the chart but kept in the totals. */
  get shownCategories() {
    return (this.intake?.categories ?? []).filter(c => c.kilocalories > 0);
  }

  /** True once there is a logged day to average macronutrients over. */
  get hasMacros(): boolean {
    return (this.macros?.averagedOverDays ?? 0) > 0;
  }

  /** Bar width for a macronutrient against the larger of actual and target. */
  macroWidth(actual: number, target: number | null): string {
    const largest = Math.max(actual, target ?? 0);
    return largest <= 0 ? '0%' : `${Math.max(2, Math.round((actual / largest) * 100))}%`;
  }

  macroTargetWidth(target: number | null, actual: number): string {
    if (target === null) {
      return '0%';
    }
    const largest = Math.max(actual, target);
    return largest <= 0 ? '0%' : `${Math.max(2, Math.round((target / largest) * 100))}%`;
  }

  /** True once any day in the period was logged. */
  get hasPatterns(): boolean {
    return (this.patterns?.byWeekday ?? []).some(d => d.loggedDays > 0);
  }

  get largestWeekday(): number {
    return Math.max(0, ...(this.patterns?.byWeekday ?? []).map(d => d.averageKilocalories));
  }

  get largestHour(): number {
    return Math.max(0, ...(this.patterns?.byHour ?? []).map(h => h.kilocalories));
  }

  /** Column height in the 24-hour histogram, as a percentage of the busiest hour. */
  hourHeight(kilocalories: number): string {
    if (this.largestHour <= 0) {
      return '0%';
    }
    return `${Math.max(kilocalories > 0 ? 4 : 0, Math.round((kilocalories / this.largestHour) * 100))}%`;
  }

  /** Only every third hour is labelled; twenty-four labels do not fit on a phone. */
  showsHourLabel(hour: number): boolean {
    return hour % 3 === 0;
  }

  shortWeekday(day: string): string {
    return day.substring(0, 3);
  }

  /**
   * Whether to explain the silence rather than just showing none. A member below the minimum is
   * told what it would take; one above it is told nothing stood out (FR-018, FR-021).
   */
  get needsMoreDaysForObservations(): boolean {
    if (!this.noticed) {
      return false;
    }
    return this.noticed.period.loggedDays < this.noticed.minimumDaysForAnyObservation;
  }

  get moreDaysNeeded(): number {
    if (!this.noticed) {
      return 0;
    }
    return Math.max(0, this.noticed.minimumDaysForAnyObservation - this.noticed.period.loggedDays);
  }

  // --- Trend chart ------------------------------------------------------

  get hasTrend(): boolean {
    return (this.trend?.loggedDays ?? 0) > 0;
  }

  get seriesChoice(): SeriesChoice {
    return this.seriesChoices.find(c => c.value === this.series) ?? this.seriesChoices[0];
  }

  /** True when the selected macronutrient has no target to draw a reference line against. */
  get seriesHasTarget(): boolean {
    const points = this.trend?.points ?? [];
    return points.some(p => this.targetOf(p) !== null);
  }

  /** The largest value the axis has to fit: the series, and its target if there is one. */
  get axisMax(): number {
    const points = this.trend?.points ?? [];
    const values = points.filter(p => p.logged).map(p => this.valueOf(p));
    const targets = points.map(p => this.targetOf(p)).filter((t): t is number => t !== null);
    const peak = Math.max(0, ...values, ...targets);

    // A little headroom, rounded to something a label can read.
    return peak <= 0 ? 1 : Math.ceil((peak * 1.08) / 50) * 50;
  }

  /**
   * One polyline per unbroken run of logged days.
   *
   * This is the whole point of the chart. A single polyline across every point would draw a
   * straight line through days the member never logged, showing intake that did not happen.
   */
  get trendSegments(): string[] {
    const points = this.trend?.points ?? [];
    const segments: string[] = [];
    let run: string[] = [];

    points.forEach((point, index) => {
      if (point.logged) {
        run.push(`${this.xAt(index)},${this.yAt(this.valueOf(point))}`);
        return;
      }

      if (run.length > 0) {
        segments.push(run.join(' '));
        run = [];
      }
    });

    if (run.length > 0) {
      segments.push(run.join(' '));
    }

    // A lone logged day between two gaps has no line to draw; the dot carries it instead.
    return segments.filter(s => s.includes(' '));
  }

  /** The target reference, continuous because a target applies whether or not a day was logged. */
  get targetLine(): string {
    const points = this.trend?.points ?? [];

    return points
      .map((point, index) => {
        const target = this.targetOf(point);
        return target === null ? null : `${this.xAt(index)},${this.yAt(target)}`;
      })
      .filter((p): p is string => p !== null)
      .join(' ');
  }

  /** Dots on the logged days, dropped once there are too many to tell apart. */
  get trendDots(): { x: number; y: number; point: DailyIntakePoint }[] {
    const points = this.trend?.points ?? [];
    if (points.length > 45) {
      return [];
    }

    return points
      .map((point, index) => ({ x: this.xAt(index), y: this.yAt(this.valueOf(point)), point }))
      .filter(d => d.point.logged);
  }

  /** Three gridlines and their labels: nothing, half, and the top of the axis. */
  get gridLines(): { y: number; label: string }[] {
    const max = this.axisMax;
    return [0, max / 2, max].map(value => ({
      y: this.yAt(value),
      label: Math.round(value).toString()
    }));
  }

  get firstDate(): string {
    return this.trend?.points[0]?.date ?? '';
  }

  get lastDate(): string {
    const points = this.trend?.points ?? [];
    return points[points.length - 1]?.date ?? '';
  }

  get gapDays(): number {
    return (this.trend?.points ?? []).filter(p => !p.logged).length;
  }

  private valueOf(point: DailyIntakePoint): number {
    switch (this.series) {
      case 'protein': return point.proteinG;
      case 'carbs': return point.carbsG;
      case 'fat': return point.fatG;
      default: return point.kilocalories;
    }
  }

  private targetOf(point: DailyIntakePoint): number | null {
    switch (this.series) {
      case 'protein': return point.targetProteinG;
      case 'carbs': return point.targetCarbsG;
      case 'fat': return point.targetFatG;
      default: return point.targetKilocalories;
    }
  }

  private xAt(index: number): number {
    const count = this.trend?.points.length ?? 0;
    const span = this.chart.width - this.chart.left - this.chart.right;

    if (count <= 1) {
      return this.chart.left + span / 2;
    }
    return this.chart.left + (index / (count - 1)) * span;
  }

  private yAt(value: number): number {
    const span = this.chart.height - this.chart.top - this.chart.bottom;
    const ratio = Math.min(1, Math.max(0, value / this.axisMax));

    return this.chart.top + span - ratio * span;
  }

  // --- Habits: beer and exercise --------------------------------------

  /** Whether there is anything to compare - beer days on one side, other in-plan days on the other. */
  get hasBeerComparison(): boolean {
    return !!this.habits && this.habits.beerDays > 0 && this.habits.onNonBeerDays.days > 0;
  }

  /** A share (0..1) as a whole percentage. */
  asPercent(share: number): number {
    return Math.round(share * 100);
  }

  /** A count per week, shown to one decimal unless it is whole. */
  perWeek(value: number): string {
    return Number.isInteger(value) ? `${value}` : value.toFixed(1);
  }

  /**
   * The plain-language read of the comparison: are beer days more often over target than other
   * days, less often, or about the same?
   */
  get beerComparisonSentence(): string {
    if (!this.habits || !this.hasBeerComparison) {
      return '';
    }

    const beer = this.asPercent(this.habits.onBeerDays.overTargetShare);
    const other = this.asPercent(this.habits.onNonBeerDays.overTargetShare);
    const gap = beer - other;

    if (Math.abs(gap) < 10) {
      return `Your eating on beer days looks about the same as on other days (${beer}% over target vs ${other}%).`;
    }
    if (gap > 0) {
      return `On beer days you go over target ${beer}% of the time, against ${other}% on other days.`;
    }
    return `On beer days you go over target ${beer}% of the time — less often than the ${other}% on other days.`;
  }

  private loadHabits(): void {
    this.loadingHabits = true;

    this.dietService.getHabitInsights(this.period).subscribe({
      next: habits => {
        this.habits = habits;
        this.loadingHabits = false;
      },
      error: () => {
        // One section, not the page.
        this.habits = null;
        this.loadingHabits = false;
      }
    });
  }

  private loadTrend(): void {
    this.loadingTrend = true;

    this.dietService.getIntakeTrend(this.period).subscribe({
      next: trend => {
        this.trend = trend;
        this.loadingTrend = false;
      },
      error: () => {
        this.trend = null;
        this.loadingTrend = false;
      }
    });
  }

  private loadObservations(): void {
    this.loadingObservations = true;

    this.dietService.getObservations(this.period).subscribe({
      next: noticed => {
        this.noticed = noticed;
        this.loadingObservations = false;
      },
      error: () => {
        this.noticed = null;
        this.loadingObservations = false;
      }
    });
  }

  private loadPatterns(): void {
    this.loadingPatterns = true;

    this.dietService.getEatingPatterns(this.period).subscribe({
      next: patterns => {
        this.patterns = patterns;
        this.loadingPatterns = false;
      },
      error: () => {
        this.patterns = null;
        this.loadingPatterns = false;
      }
    });
  }

  private loadMacros(): void {
    this.loadingMacros = true;

    this.dietService.getMacroAnalysis(this.period).subscribe({
      next: analysis => {
        this.macros = analysis;
        this.loadingMacros = false;
      },
      error: () => {
        // The intake load already handles a missing plan; a failure here costs one section, not
        // the page.
        this.macros = null;
        this.loadingMacros = false;
      }
    });
  }

  private loadIntake(): void {
    this.loadingIntake = true;
    this.intakeError = null;

    this.dietService.getIntakeAnalysis(this.period).subscribe({
      next: analysis => {
        this.intake = analysis;
        this.loadingIntake = false;
      },
      error: err => {
        this.loadingIntake = false;
        if (err?.status === 404) {
          this.router.navigate(['/diet/setup']);
          return;
        }
        this.intakeError = err?.error?.message ?? 'Could not load your intake analysis.';
      }
    });
  }
}
