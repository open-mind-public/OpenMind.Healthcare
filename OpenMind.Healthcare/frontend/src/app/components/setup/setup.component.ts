import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { ApiService } from '../../services/api.service';
import { SmokedDay, UserProgress } from '../../models/models';

@Component({
  selector: 'app-setup',
  standalone: false,
  template: `
    <div class="setup-page fade-in">
      <div class="card setup-card">
        <div class="setup-header">
          <h1>{{ isEditing ? '⚙️ Adjust Your Journey' : '🌟 Let\\'s Begin Your Journey' }}</h1>
          <p>
            {{ isEditing
                ? 'Change your quit date or smoking details - every total is recalculated from these.'
                : 'Please provide some information to personalize your experience' }}
          </p>
        </div>

        <div class="current-journey" *ngIf="isEditing && currentProgress">
          <span class="current-label">Currently starting from</span>
          <span class="current-value">{{ currentQuitDate | date:'EEEE, d MMMM yyyy, HH:mm' }}</span>
        </div>

        <form [formGroup]="setupForm" (ngSubmit)="onSubmit()">
          <div class="form-group">
            <label for="quitDate">When did you quit smoking?</label>
            <input
              type="datetime-local"
              id="quitDate"
              formControlName="quitDate"
              [max]="maxDate">
            <span class="hint">Select the date and time you had your last cigarette</span>
            <span class="error-text" *ngIf="setupForm.get('quitDate')?.hasError('required') && setupForm.get('quitDate')?.touched">
              A quit date is required
            </span>
          </div>

          <div class="warning-banner" *ngIf="daysDroppedByNewDate > 0">
            <span class="warning-icon">⚠️</span>
            <div>
              <strong>
                {{ daysDroppedByNewDate }} marked smoked
                {{ daysDroppedByNewDate === 1 ? 'day' : 'days' }} will be deleted
              </strong>
              <p>
                {{ daysDroppedByNewDate === 1 ? 'It falls' : 'They fall' }} before the new quit date,
                so {{ daysDroppedByNewDate === 1 ? 'it is' : 'they are' }} no longer part of this journey.
                Earliest affected: {{ earliestDroppedDate | date:'d MMM yyyy' }}.
              </p>
            </div>
          </div>

          <div class="form-group">
            <label for="cigarettesPerDay">How many cigarettes did you smoke per day?</label>
            <input
              type="number"
              id="cigarettesPerDay"
              formControlName="cigarettesPerDay"
              min="1"
              max="100"
              placeholder="e.g., 20">
          </div>

          <div class="form-row">
            <div class="form-group">
              <label for="currency">Currency</label>
              <select
                id="currency"
                formControlName="currency">
                <option value="USD">USD ($)</option>
                <option value="VND">VND (₫)</option>
              </select>
            </div>

            <div class="form-group">
              <label for="pricePerPack">Price per pack ({{ getCurrencySymbol() }})</label>
              <input
                type="text"
                id="pricePerPack"
                inputmode="decimal"
                autocomplete="off"
                [value]="priceDisplay"
                [placeholder]="getCurrencyPlaceholder()"
                (focus)="onPriceFocus()"
                (input)="onPriceInput($event)"
                (blur)="onPriceBlur()">
              <span class="error-text" *ngIf="setupForm.get('pricePerPack')?.invalid && setupForm.get('pricePerPack')?.touched">
                Enter a price greater than zero
              </span>
            </div>
          </div>

          <div class="form-group">
            <label for="cigarettesPerPack">Cigarettes per pack</label>
            <input
              type="number"
              id="cigarettesPerPack"
              formControlName="cigarettesPerPack"
              min="1"
              max="50"
              placeholder="e.g., 20">
          </div>

          <div class="motivation-section" *ngIf="!isEditing">
            <h3>💪 Remember Why You're Doing This</h3>
            <ul class="reasons-list">
              <li>🫀 Better heart and lung health</li>
              <li>💰 More money in your pocket</li>
              <li>👨‍👩‍👧‍👦 More time with loved ones</li>
              <li>🏃 Improved energy and fitness</li>
              <li>😤 Freedom from addiction</li>
              <li>🌟 A longer, healthier life</li>
            </ul>
          </div>

          <p class="submit-error" *ngIf="submitError">{{ submitError }}</p>

          <div class="form-actions">
            <button
              type="button"
              class="btn btn-ghost"
              *ngIf="isEditing"
              (click)="cancel()"
              [disabled]="isSubmitting">
              Cancel
            </button>
            <button
              type="submit"
              class="btn btn-primary submit-btn"
              [disabled]="setupForm.invalid || isSubmitting">
              {{ submitLabel }}
            </button>
          </div>
        </form>
      </div>
    </div>
  `,
  styles: [`
    .setup-page {
      max-width: 600px;
      margin: 40px auto;
      padding: 20px;
    }

    .setup-card {
      padding: 40px;
    }

    .setup-header {
      text-align: center;
      margin-bottom: 30px;

      h1 {
        font-size: 28px;
        margin-bottom: 10px;
        background: linear-gradient(135deg, #10b981, #3b82f6);
        -webkit-background-clip: text;
        -webkit-text-fill-color: transparent;
      }

      p {
        color: rgba(255, 255, 255, 0.7);
      }
    }

    .current-journey {
      display: flex;
      flex-direction: column;
      align-items: center;
      gap: 4px;
      padding: 14px;
      margin-bottom: 30px;
      background: rgba(16, 185, 129, 0.1);
      border: 1px solid rgba(16, 185, 129, 0.25);
      border-radius: 12px;

      .current-label {
        font-size: 11px;
        text-transform: uppercase;
        letter-spacing: 0.06em;
        color: rgba(255, 255, 255, 0.5);
      }

      .current-value {
        font-size: 15px;
        font-weight: 600;
        color: #34d399;
      }
    }

    .warning-banner {
      display: flex;
      gap: 12px;
      padding: 16px;
      margin-bottom: 25px;
      background: rgba(239, 68, 68, 0.12);
      border: 1px solid rgba(239, 68, 68, 0.3);
      border-radius: 12px;

      .warning-icon {
        font-size: 20px;
        line-height: 1.2;
      }

      strong {
        display: block;
        color: #fca5a5;
        font-size: 14px;
        margin-bottom: 4px;
      }

      p {
        font-size: 13px;
        color: rgba(255, 255, 255, 0.7);
        line-height: 1.5;
      }
    }

    .form-group {
      margin-bottom: 25px;

      label {
        display: block;
        margin-bottom: 8px;
        font-weight: 500;
        color: rgba(255, 255, 255, 0.9);
      }

      input, select {
        width: 100%;
        padding: 14px 16px;
        font-size: 16px;
      }

      select {
        background-color: #1a1a2e;
        color: white;
        border: 1px solid rgba(255, 255, 255, 0.1);
        border-radius: 8px;
        cursor: pointer;
      }

      .hint {
        display: block;
        margin-top: 5px;
        font-size: 12px;
        color: rgba(255, 255, 255, 0.5);
      }

      .error-text {
        display: block;
        margin-top: 5px;
        font-size: 12px;
        color: #f87171;
      }
    }

    .form-row {
      display: grid;
      grid-template-columns: 1fr 1fr;
      gap: 20px;
    }

    .motivation-section {
      margin: 30px 0;
      padding: 25px;
      background: linear-gradient(135deg, rgba(16, 185, 129, 0.1), rgba(59, 130, 246, 0.1));
      border-radius: 16px;
      border: 1px solid rgba(16, 185, 129, 0.2);

      h3 {
        margin-bottom: 15px;
        color: #10b981;
      }
    }

    .reasons-list {
      list-style: none;
      display: grid;
      grid-template-columns: 1fr 1fr;
      gap: 10px;

      li {
        padding: 8px 12px;
        background: rgba(255, 255, 255, 0.05);
        border-radius: 8px;
        font-size: 14px;
      }
    }

    .submit-error {
      margin-top: 20px;
      padding: 12px 16px;
      border-radius: 10px;
      background: rgba(239, 68, 68, 0.15);
      border: 1px solid rgba(239, 68, 68, 0.3);
      color: #fca5a5;
      font-size: 14px;
    }

    .form-actions {
      display: flex;
      gap: 12px;
      margin-top: 20px;
    }

    .submit-btn {
      flex: 1;
      padding: 18px;
      font-size: 18px;

      &:disabled {
        opacity: 0.6;
        cursor: not-allowed;
      }
    }

    .btn-ghost {
      padding: 18px 28px;
      font-size: 16px;
      font-weight: 600;
      border-radius: 25px;
      border: 1px solid rgba(255, 255, 255, 0.2);
      background: rgba(255, 255, 255, 0.08);
      color: white;
      cursor: pointer;
      transition: all 0.3s ease;

      &:hover:not(:disabled) {
        background: rgba(255, 255, 255, 0.16);
      }

      &:disabled {
        opacity: 0.6;
        cursor: not-allowed;
      }
    }

    @media (max-width: 480px) {
      .form-row,
      .reasons-list,
      .form-actions {
        grid-template-columns: 1fr;
        flex-direction: column;
      }
    }
  `]
})
export class SetupComponent implements OnInit {
  setupForm: FormGroup;
  isSubmitting = false;
  maxDate: string;

  /** Existing journey, when the user is adjusting rather than starting one. */
  currentProgress: UserProgress | null = null;
  currentQuitDate: Date | null = null;
  submitError = '';

  /** What the price field shows: grouped when idle, plain digits while being typed into. */
  priceDisplay = '';
  private priceFocused = false;

  private smokedDays: SmokedDay[] = [];

  constructor(
    private fb: FormBuilder,
    private apiService: ApiService,
    private router: Router
  ) {
    // The input is local time, so the ceiling has to be local too - a UTC value
    // would put the limit hours in the past for anyone east of Greenwich.
    this.maxDate = SetupComponent.toInputValue(new Date());

    const twoWeeksAgo = new Date();
    twoWeeksAgo.setDate(twoWeeksAgo.getDate() - 14);

    this.setupForm = this.fb.group({
      quitDate: [SetupComponent.toInputValue(twoWeeksAgo), Validators.required],
      cigarettesPerDay: [20, [Validators.required, Validators.min(1), Validators.max(100)]],
      currency: ['USD', Validators.required],
      pricePerPack: [10.00, [Validators.required, Validators.min(0.01)]],
      cigarettesPerPack: [20, [Validators.required, Validators.min(1), Validators.max(50)]]
    });
  }

  ngOnInit(): void {
    this.refreshPriceDisplay();

    // Switching currency changes both the precision and the grouping
    this.setupForm.get('currency')!.valueChanges.subscribe(() => this.normalisePriceForCurrency());

    this.apiService.getProgress().subscribe({
      next: (progress) => {
        this.currentProgress = progress;
        this.currentQuitDate = SetupComponent.parseApiDate(progress.quitDate);

        this.setupForm.patchValue({
          quitDate: SetupComponent.toInputValue(this.currentQuitDate),
          cigarettesPerDay: progress.cigarettesPerDay,
          currency: progress.currency,
          pricePerPack: progress.pricePerPack,
          cigarettesPerPack: progress.cigarettesPerPack
        });
        this.normalisePriceForCurrency();

        this.loadSmokedDays();
      },
      error: () => {
        this.currentProgress = null; // no journey yet - stay in "start" mode
      }
    });
  }

  get isEditing(): boolean {
    return this.currentProgress !== null;
  }

  get submitLabel(): string {
    if (this.isSubmitting) return this.isEditing ? 'Saving...' : 'Starting...';
    return this.isEditing ? 'Save changes' : 'Start My Smoke-Free Life! 🚀';
  }

  /** Marked smoked days that the chosen quit date would push out of the journey. */
  get droppedDays(): SmokedDay[] {
    const quitDay = this.selectedQuitDay();
    if (!quitDay) return [];
    return this.smokedDays.filter(d => d.date < quitDay);
  }

  get daysDroppedByNewDate(): number {
    return this.droppedDays.length;
  }

  get earliestDroppedDate(): Date | null {
    const dropped = this.droppedDays;
    return dropped.length > 0 ? SetupComponent.parseApiDate(dropped[0].date) : null;
  }

  /** VND has no sub-unit, so a price in dong is always a whole number. */
  private get currencyHasDecimals(): boolean {
    return this.setupForm.get('currency')?.value !== 'VND';
  }

  private get priceValue(): number {
    const value = Number(this.setupForm.get('pricePerPack')?.value);
    return Number.isFinite(value) ? value : 0;
  }

  onPriceFocus(): void {
    // Show the bare number while editing - grouping separators fight with the caret
    this.priceFocused = true;
    this.priceDisplay = this.priceValue ? `${this.priceValue}` : '';
  }

  onPriceInput(event: Event): void {
    const raw = (event.target as HTMLInputElement).value;
    const allowed = this.currencyHasDecimals ? /[^0-9.]/g : /[^0-9]/g;
    const cleaned = raw.replace(allowed, '');

    this.priceDisplay = cleaned;
    const parsed = Number.parseFloat(cleaned);
    this.setupForm.get('pricePerPack')!.setValue(Number.isFinite(parsed) ? parsed : null);
  }

  onPriceBlur(): void {
    this.priceFocused = false;
    this.setupForm.get('pricePerPack')!.markAsTouched();
    this.normalisePriceForCurrency();
  }

  /** Rounds a dong price to a whole number and re-renders the grouped display. */
  private normalisePriceForCurrency(): void {
    if (!this.currencyHasDecimals) {
      const rounded = Math.round(this.priceValue);
      if (rounded !== this.priceValue) {
        this.setupForm.get('pricePerPack')!.setValue(rounded, { emitEvent: false });
      }
    }

    this.refreshPriceDisplay();
  }

  private refreshPriceDisplay(): void {
    if (this.priceFocused) return;

    const value = this.priceValue;
    this.priceDisplay = value > 0
      ? value.toLocaleString('en-US', {
          minimumFractionDigits: this.currencyHasDecimals ? 2 : 0,
          maximumFractionDigits: this.currencyHasDecimals ? 2 : 0
        })
      : '';
  }

  getCurrencySymbol(): string {
    return this.setupForm.get('currency')?.value === 'VND' ? '₫' : '$';
  }

  getCurrencyPlaceholder(): string {
    return this.currencyHasDecimals ? 'e.g., 10.00' : 'e.g., 30,000';
  }

  cancel(): void {
    this.router.navigate(['/dashboard']);
  }

  onSubmit(): void {
    if (this.setupForm.invalid) return;

    this.isSubmitting = true;
    this.submitError = '';

    const formValue = this.setupForm.value;
    const progress = {
      quitDate: new Date(formValue.quitDate).toISOString(),
      cigarettesPerDay: formValue.cigarettesPerDay,
      pricePerPack: this.currencyHasDecimals ? formValue.pricePerPack : Math.round(formValue.pricePerPack),
      cigarettesPerPack: formValue.cigarettesPerPack,
      currency: formValue.currency
    };

    this.apiService.saveProgress(progress).subscribe({
      next: () => {
        this.apiService.refreshStats();
        this.router.navigate(['/dashboard']);
      },
      error: (err) => {
        this.isSubmitting = false;
        this.submitError = err?.error?.message || 'Could not save your journey. Please try again.';
      }
    });
  }

  private loadSmokedDays(): void {
    this.apiService.getSmokedDays().subscribe({
      next: (days) => {
        this.smokedDays = days;
      }
    });
  }

  /** yyyy-MM-dd of the quit date currently chosen in the form. */
  private selectedQuitDay(): string | null {
    const value = this.setupForm.get('quitDate')?.value;
    return typeof value === 'string' && value.length >= 10 ? value.slice(0, 10) : null;
  }

  /**
   * The API sends UTC without a zone marker, which JavaScript would otherwise read as
   * local time and shift by the offset. Pin it to UTC before converting.
   */
  private static parseApiDate(value: string): Date {
    const hasZone = /[zZ]$|[+-]\d{2}:\d{2}$/.test(value);
    return new Date(hasZone || value.length <= 10 ? value : `${value}Z`);
  }

  /** Local yyyy-MM-ddTHH:mm, the format a datetime-local input expects. */
  private static toInputValue(date: Date): string {
    const pad = (n: number) => `${n}`.padStart(2, '0');
    return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}`
      + `T${pad(date.getHours())}:${pad(date.getMinutes())}`;
  }
}
