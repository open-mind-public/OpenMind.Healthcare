import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { DietService } from '../../services/diet.service';
import {
  ActivityLevel,
  BiologicalSex,
  CreateDietPlanRequest,
  DietPlan,
  GoalType,
  TargetSuggestion
} from '../../models/diet.models';

@Component({
  selector: 'app-diet-setup',
  standalone: false,
  templateUrl: './diet-setup.component.html',
  styleUrls: ['./diet-setup.component.css']
})
export class DietSetupComponent implements OnInit {
  form!: FormGroup;

  suggestion: TargetSuggestion | null = null;
  existingPlan: DietPlan | null = null;

  /** Set when the member replaces the suggestion with a number of their own. */
  overriding = false;

  loading = false;
  saving = false;
  error: string | null = null;
  floorWarning: string | null = null;

  readonly goals: { value: GoalType; label: string }[] = [
    { value: 'LoseWeight', label: 'Lose weight' },
    { value: 'Maintain', label: 'Maintain my weight' },
    { value: 'GainWeight', label: 'Gain weight' },
    { value: 'EatConsistently', label: 'Just eat more consistently' }
  ];

  readonly activityLevels: { value: ActivityLevel; label: string }[] = [
    { value: 'Sedentary', label: 'Sedentary - little or no exercise' },
    { value: 'LightlyActive', label: 'Lightly active - 1 to 3 days a week' },
    { value: 'ModeratelyActive', label: 'Moderately active - 3 to 5 days a week' },
    { value: 'VeryActive', label: 'Very active - 6 to 7 days a week' },
    { value: 'ExtraActive', label: 'Extra active - physical job or twice daily training' }
  ];

  readonly sexes: { value: BiologicalSex; label: string }[] = [
    { value: 'Female', label: 'Female' },
    { value: 'Male', label: 'Male' }
  ];

  constructor(
    private fb: FormBuilder,
    private dietService: DietService,
    private router: Router
  ) {}

  ngOnInit(): void {
    const today = new Date().toISOString().substring(0, 10);

    this.form = this.fb.group({
      goal: ['LoseWeight', Validators.required],
      startDate: [today, Validators.required],
      heightCm: [null, [Validators.required, Validators.min(50), Validators.max(250)]],
      age: [null, [Validators.required, Validators.min(13), Validators.max(120)]],
      sex: ['Female', Validators.required],
      currentWeightKg: [null, [Validators.required, Validators.min(20), Validators.max(500)]],
      activityLevel: ['ModeratelyActive', Validators.required],
      targetWeightKg: [null, [Validators.min(20), Validators.max(500)]],
      calories: [null, [Validators.min(1)]]
    });

    // A member who already has a plan sees it rather than the setup flow.
    this.loading = true;
    this.dietService.getPlan().subscribe({
      next: plan => {
        this.existingPlan = plan;
        this.loading = false;
        this.router.navigate(['/diet/today']);
      },
      error: () => {
        this.loading = false;
      }
    });
  }

  /** The body details the suggestion is calculated from. */
  get canSuggest(): boolean {
    const f = this.form?.value;
    return !!(f?.heightCm && f?.age && f?.currentWeightKg && f?.sex && f?.activityLevel && f?.goal);
  }

  requestSuggestion(): void {
    if (!this.canSuggest) {
      return;
    }

    const f = this.form.value;
    this.error = null;

    this.dietService
      .suggestTargets({
        goal: f.goal,
        bodyMetrics: { heightCm: f.heightCm, age: f.age, sex: f.sex },
        currentWeightKg: f.currentWeightKg,
        activityLevel: f.activityLevel
      })
      .subscribe({
        next: suggestion => {
          this.suggestion = suggestion;
          this.overriding = false;
          this.form.patchValue({ calories: suggestion.suggestedTargets.calories });
        },
        error: err => (this.error = err?.error?.message ?? 'Could not calculate a suggestion.')
      });
  }

  startOverriding(): void {
    this.overriding = true;
  }

  acceptSuggestion(): void {
    if (this.suggestion) {
      this.overriding = false;
      this.form.patchValue({ calories: this.suggestion.suggestedTargets.calories });
    }
  }

  save(): void {
    if (this.form.invalid || !this.suggestion) {
      this.form.markAllAsTouched();
      return;
    }

    const f = this.form.value;
    const usingSuggestion = !this.overriding
      && f.calories === this.suggestion.suggestedTargets.calories;

    const request: CreateDietPlanRequest = {
      goal: f.goal,
      startDate: f.startDate,
      bodyMetrics: { heightCm: f.heightCm, age: f.age, sex: f.sex },
      activityLevel: f.activityLevel,
      currentWeightKg: f.currentWeightKg,
      targetWeightKg: f.targetWeightKg ?? null,
      targets: usingSuggestion
        ? this.suggestion.suggestedTargets
        : { calories: f.calories, proteinG: null, carbsG: null, fatG: null },
      targetSource: usingSuggestion ? 'Suggested' : 'MemberSet'
    };

    this.saving = true;
    this.error = null;

    this.dietService.createPlan(request).subscribe({
      next: response => {
        this.saving = false;
        this.floorWarning = response.belowSafeFloorWarning;

        // A warning accompanies a successful save - it is not a refusal. Give the member a
        // moment to read it before moving on.
        if (this.floorWarning) {
          setTimeout(() => this.router.navigate(['/diet/today']), 4000);
        } else {
          this.router.navigate(['/diet/today']);
        }
      },
      error: err => {
        this.saving = false;
        this.error = err?.error?.message ?? 'Could not save your plan.';
      }
    });
  }
}
