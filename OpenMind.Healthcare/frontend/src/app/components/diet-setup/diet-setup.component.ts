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

/**
 * The diet plan screen, in two modes.
 *
 * Without a plan it is the setup flow. With one it is the plan's settings - the same fields,
 * pre-filled, saving through the update endpoints instead of the create one. It deliberately does
 * not redirect away when a plan exists: this is where "Diet plan & targets" in the settings menu
 * lands, and a settings screen that bounces you elsewhere is useless.
 */
@Component({
  selector: 'app-diet-setup',
  standalone: false,
  templateUrl: './diet-setup.component.html',
  styleUrls: ['./diet-setup.component.css']
})
export class DietSetupComponent implements OnInit {
  form!: FormGroup;

  mode: 'create' | 'edit' = 'create';
  suggestion: TargetSuggestion | null = null;
  existingPlan: DietPlan | null = null;

  /** Set when the member replaces the suggestion with a number of their own. */
  overriding = false;

  loading = false;
  saving = false;
  saved = false;
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

    this.loading = true;
    this.dietService.getPlan().subscribe({
      next: plan => {
        this.mode = 'edit';
        this.existingPlan = plan;
        this.fillFrom(plan);
        this.loading = false;
        this.requestSuggestion();
      },
      // 404 simply means there is no plan yet - that is the setup flow, not an error.
      error: () => {
        this.mode = 'create';
        this.loading = false;
      }
    });
  }

  private fillFrom(plan: DietPlan): void {
    this.form.patchValue({
      goal: plan.goal,
      startDate: plan.startDate,
      heightCm: plan.bodyMetrics.heightCm,
      age: plan.bodyMetrics.age,
      sex: plan.bodyMetrics.sex,
      currentWeightKg: plan.currentWeightKg,
      activityLevel: plan.activityLevel,
      targetWeightKg: plan.targetWeightKg,
      calories: plan.targets.calories
    });

    // A target the member chose themselves stays theirs until they say otherwise.
    this.overriding = plan.targetSource === 'MemberSet';
  }

  /** The body details the suggestion is calculated from. */
  get canSuggest(): boolean {
    const f = this.form?.value;
    return !!(f?.heightCm && f?.age && f?.currentWeightKg && f?.sex && f?.activityLevel && f?.goal);
  }

  /** True when the form's target differs from what the system would now suggest. */
  get targetDiffersFromSuggestion(): boolean {
    return !!this.suggestion
      && this.form?.value.calories !== this.suggestion.suggestedTargets.calories;
  }

  requestSuggestion(): void {
    if (!this.canSuggest) {
      return;
    }

    const f = this.form.value;
    this.error = null;
    this.saved = false;

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

          // On first setup the suggestion is the proposal. On an existing plan it is only an
          // offer - a member's own target is never overwritten without them saying so.
          if (this.mode === 'create' && !this.overriding) {
            this.form.patchValue({ calories: suggestion.suggestedTargets.calories });
          }
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

    this.saving = true;
    this.error = null;
    this.saved = false;

    if (this.mode === 'edit') {
      this.update();
    } else {
      this.create();
    }
  }

  private create(): void {
    const f = this.form.value;
    const usingSuggestion = !this.overriding && f.calories === this.suggestion!.suggestedTargets.calories;

    const request: CreateDietPlanRequest = {
      goal: f.goal,
      startDate: f.startDate,
      bodyMetrics: { heightCm: f.heightCm, age: f.age, sex: f.sex },
      activityLevel: f.activityLevel,
      currentWeightKg: f.currentWeightKg,
      targetWeightKg: f.targetWeightKg ?? null,
      targets: usingSuggestion
        ? this.suggestion!.suggestedTargets
        : { calories: f.calories, proteinG: null, carbsG: null, fatG: null },
      targetSource: usingSuggestion ? 'Suggested' : 'MemberSet'
    };

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

  /**
   * Two calls, because the API keeps them separate on purpose: updating details never moves the
   * target in force, so changing the target is its own explicit step.
   */
  private update(): void {
    const f = this.form.value;

    this.dietService
      .updatePlan({
        goal: f.goal,
        startDate: f.startDate,
        bodyMetrics: { heightCm: f.heightCm, age: f.age, sex: f.sex },
        activityLevel: f.activityLevel,
        targetWeightKg: f.targetWeightKg ?? null
      })
      .subscribe({
        next: response => {
          this.existingPlan = response.plan;
          this.suggestion = response.refreshedSuggestion;

          const targetChanged = f.calories !== response.plan.targets.calories;
          if (!targetChanged) {
            this.finishUpdate(null);
            return;
          }

          const matchesSuggestion = f.calories === response.refreshedSuggestion.suggestedTargets.calories;

          this.dietService
            .setTargets({
              targets: matchesSuggestion
                ? response.refreshedSuggestion.suggestedTargets
                : { calories: f.calories, proteinG: null, carbsG: null, fatG: null },
              targetSource: matchesSuggestion ? 'Suggested' : 'MemberSet'
            })
            .subscribe({
              next: targetResponse => {
                this.existingPlan = targetResponse.plan;
                this.finishUpdate(targetResponse.belowSafeFloorWarning);
              },
              error: err => this.failUpdate(err)
            });
        },
        error: err => this.failUpdate(err)
      });
  }

  private finishUpdate(warning: string | null): void {
    this.saving = false;
    this.saved = true;
    this.floorWarning = warning;

    if (this.existingPlan) {
      this.overriding = this.existingPlan.targetSource === 'MemberSet';
    }
  }

  private failUpdate(err: any): void {
    this.saving = false;
    this.error = err?.error?.message ?? 'Could not save your changes.';
  }

  backToToday(): void {
    this.router.navigate(['/diet/today']);
  }
}
