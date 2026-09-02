import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { ApiService } from '../../services/api.service';
import { User } from '../../models/models';

@Component({
  selector: 'app-account',
  standalone: false,
  template: `
    <div class="account-page fade-in">
      <div class="page-header">
        <h1>👤 Account</h1>
        <p class="subtitle">Your details and sign-in security</p>
      </div>

      <!-- Identity -->
      <div class="card">
        <div class="identity" *ngIf="user">
          <span class="avatar">{{ initials }}</span>
          <div class="identity-text">
            <span class="identity-name">{{ displayName }}</span>
            <span class="identity-email">{{ user.email }}</span>
          </div>
        </div>
        <div class="identity-meta" *ngIf="user?.createdAt">
          <span>Member since {{ user!.createdAt | date:'d MMMM yyyy' }}</span>
          <span *ngIf="user?.lastLoginAt">Last signed in {{ user!.lastLoginAt | date:'d MMM yyyy, HH:mm' }}</span>
        </div>
      </div>

      <!-- Profile -->
      <div class="card">
        <h2>Your name</h2>
        <p class="section-hint">This is how the app greets you.</p>

        <form [formGroup]="profileForm" (ngSubmit)="saveProfile()">
          <div class="form-row">
            <div class="form-group">
              <label for="firstName">First name</label>
              <input id="firstName" type="text" formControlName="firstName" maxlength="100">
            </div>
            <div class="form-group">
              <label for="lastName">Last name</label>
              <input id="lastName" type="text" formControlName="lastName" maxlength="100">
            </div>
          </div>

          <p class="feedback error" *ngIf="profileError">{{ profileError }}</p>
          <p class="feedback success" *ngIf="profileSaved">✅ Name updated</p>

          <button
            type="submit"
            class="btn btn-primary"
            [disabled]="profileForm.invalid || savingProfile || profileForm.pristine">
            {{ savingProfile ? 'Saving...' : 'Save name' }}
          </button>
        </form>
      </div>

      <!-- Password -->
      <div class="card">
        <h2>Password</h2>
        <p class="section-hint">Choose something at least 6 characters long.</p>

        <form [formGroup]="passwordForm" (ngSubmit)="savePassword()">
          <div class="form-group">
            <label for="currentPassword">Current password</label>
            <input id="currentPassword" type="password" formControlName="currentPassword" autocomplete="current-password">
          </div>

          <div class="form-row">
            <div class="form-group">
              <label for="newPassword">New password</label>
              <input id="newPassword" type="password" formControlName="newPassword" autocomplete="new-password">
              <span class="error-text" *ngIf="newPassword?.touched && newPassword?.hasError('minlength')">
                At least 6 characters
              </span>
            </div>
            <div class="form-group">
              <label for="confirmPassword">Confirm new password</label>
              <input id="confirmPassword" type="password" formControlName="confirmPassword" autocomplete="new-password">
              <span class="error-text" *ngIf="confirmPassword?.touched && !passwordsMatch">
                Passwords do not match
              </span>
            </div>
          </div>

          <p class="feedback error" *ngIf="passwordError">{{ passwordError }}</p>
          <p class="feedback success" *ngIf="passwordSaved">✅ Password changed</p>

          <button
            type="submit"
            class="btn btn-primary"
            [disabled]="passwordForm.invalid || !passwordsMatch || savingPassword">
            {{ savingPassword ? 'Changing...' : 'Change password' }}
          </button>
        </form>
      </div>

      <!-- Session -->
      <div class="card danger-card">
        <h2>Sign out</h2>
        <p class="section-hint">Ends this session on this device. Your journey and history stay saved.</p>
        <button type="button" class="btn btn-danger" (click)="logout()">Log out</button>
      </div>
    </div>
  `,
  styles: [`
    .account-page {
      max-width: 640px;
      margin: 0 auto;
      padding: 20px;
      display: flex;
      flex-direction: column;
      gap: 20px;
    }

    .page-header {
      text-align: center;

      h1 {
        font-size: 2.2rem;
        margin-bottom: 8px;
        color: var(--accent);
      }

      .subtitle {
        color: var(--text-secondary);
      }
    }

    .card {
      background: var(--surface-sunken);
      border: 1px solid var(--border);
      border-radius: 18px;
      padding: 25px;

      h2 {
        font-size: 1.15rem;
        color: var(--text);
        margin-bottom: 4px;
      }

      .section-hint {
        font-size: 13px;
        color: var(--text-muted);
        margin-bottom: 20px;
      }
    }

    .danger-card {
      border-color: var(--danger-border);
      background: var(--danger-subtle);
    }

    .identity {
      display: flex;
      align-items: center;
      gap: 16px;

      .avatar {
        display: flex;
        align-items: center;
        justify-content: center;
        width: 56px;
        height: 56px;
        border-radius: 50%;
        background: var(--accent);
        color: var(--text-on-accent);
        font-size: 20px;
        font-weight: 700;
      }

      .identity-text {
        display: flex;
        flex-direction: column;
        gap: 3px;
      }

      .identity-name {
        font-size: 18px;
        font-weight: 600;
        color: var(--text);
      }

      .identity-email {
        font-size: 13px;
        color: var(--text-muted);
      }
    }

    .identity-meta {
      display: flex;
      flex-wrap: wrap;
      gap: 18px;
      margin-top: 18px;
      padding-top: 16px;
      border-top: 1px solid var(--border);
      font-size: 12px;
      color: var(--text-muted);
    }

    .form-group {
      margin-bottom: 18px;

      label {
        display: block;
        margin-bottom: 8px;
        font-weight: 500;
        font-size: 14px;
        color: var(--text);
      }

      input {
        width: 100%;
        padding: 13px 15px;
        font-size: 15px;
      }

      .error-text {
        display: block;
        margin-top: 6px;
        font-size: 12px;
        color: var(--danger);
      }
    }

    .form-row {
      display: grid;
      grid-template-columns: 1fr 1fr;
      gap: 16px;
    }

    .feedback {
      margin-bottom: 16px;
      padding: 11px 14px;
      border-radius: 10px;
      font-size: 13px;

      &.error {
        background: var(--danger-subtle);
        border: 1px solid var(--danger-border);
        color: var(--danger);
      }

      &.success {
        background: var(--accent-subtle);
        border: 1px solid var(--accent-border);
        color: var(--accent);
      }
    }

    .btn {
      padding: 13px 26px;
      border: none;
      border-radius: 25px;
      font-family: var(--font);
      font-size: 15px;
      font-weight: 600;
      cursor: pointer;
      transition: all 0.3s ease;

      &:disabled {
        opacity: 0.5;
        cursor: not-allowed;
      }

      &.btn-primary {
        background: var(--accent);
        color: var(--text-on-accent);

        &:hover:not(:disabled) {
          transform: translateY(-1px);
          box-shadow: var(--shadow-md);
        }
      }

      &.btn-danger {
        background: var(--danger);
        color: var(--text-on-accent);

        &:hover:not(:disabled) {
          transform: translateY(-1px);
          box-shadow: var(--shadow-md);
        }
      }
    }

    .fade-in {
      animation: fadeIn 0.5s ease-out;
    }

    @keyframes fadeIn {
      from { opacity: 0; transform: translateY(20px); }
      to   { opacity: 1; transform: translateY(0); }
    }

    @media (max-width: 560px) {
      .form-row {
        grid-template-columns: 1fr;
      }
    }
  `]
})
export class AccountComponent implements OnInit {
  user: User | null = null;

  profileForm: FormGroup;
  passwordForm: FormGroup;

  savingProfile = false;
  savingPassword = false;
  profileError = '';
  passwordError = '';
  profileSaved = false;
  passwordSaved = false;

  constructor(
    private fb: FormBuilder,
    private apiService: ApiService,
    private router: Router
  ) {
    this.profileForm = this.fb.group({
      firstName: ['', [Validators.required, Validators.maxLength(100)]],
      lastName: ['', [Validators.required, Validators.maxLength(100)]]
    });

    this.passwordForm = this.fb.group({
      currentPassword: ['', Validators.required],
      newPassword: ['', [Validators.required, Validators.minLength(6)]],
      confirmPassword: ['', Validators.required]
    });
  }

  ngOnInit(): void {
    this.user = this.apiService.currentUser;
    this.patchProfile();

    // The cached user carries only what login returned; fetch the full record
    this.apiService.loadCurrentUser().subscribe({
      next: (user) => {
        this.user = user;
        this.patchProfile();
      },
      error: () => { /* keep showing the cached user */ }
    });
  }

  get displayName(): string {
    const name = `${this.user?.firstName ?? ''} ${this.user?.lastName ?? ''}`.trim();
    return name || this.user?.email || 'Your account';
  }

  get initials(): string {
    const first = this.user?.firstName?.trim()?.[0] ?? '';
    const last = this.user?.lastName?.trim()?.[0] ?? '';
    const initials = `${first}${last}`.toUpperCase();
    return initials || (this.user?.email?.[0]?.toUpperCase() ?? '?');
  }

  get newPassword() { return this.passwordForm.get('newPassword'); }
  get confirmPassword() { return this.passwordForm.get('confirmPassword'); }

  get passwordsMatch(): boolean {
    return this.newPassword?.value === this.confirmPassword?.value;
  }

  saveProfile(): void {
    if (this.profileForm.invalid) return;

    this.savingProfile = true;
    this.profileError = '';
    this.profileSaved = false;

    this.apiService.updateProfile({
      firstName: this.profileForm.value.firstName.trim(),
      lastName: this.profileForm.value.lastName.trim()
    }).subscribe({
      next: (user) => {
        this.user = user;
        this.savingProfile = false;
        this.profileSaved = true;
        this.profileForm.markAsPristine();
      },
      error: (err) => {
        this.savingProfile = false;
        this.profileError = err?.error?.message || 'Could not save your name. Please try again.';
      }
    });
  }

  savePassword(): void {
    if (this.passwordForm.invalid || !this.passwordsMatch) return;

    this.savingPassword = true;
    this.passwordError = '';
    this.passwordSaved = false;

    this.apiService.changePassword({
      currentPassword: this.passwordForm.value.currentPassword,
      newPassword: this.passwordForm.value.newPassword
    }).subscribe({
      next: () => {
        this.savingPassword = false;
        this.passwordSaved = true;
        this.passwordForm.reset();
      },
      error: (err) => {
        this.savingPassword = false;
        this.passwordError = err?.error?.message || 'Could not change your password. Please try again.';
      }
    });
  }

  logout(): void {
    this.apiService.logout();
    this.router.navigate(['/login']);
  }

  private patchProfile(): void {
    if (!this.user) return;
    this.profileForm.patchValue({
      firstName: this.user.firstName ?? '',
      lastName: this.user.lastName ?? ''
    }, { emitEvent: false });
    this.profileForm.markAsPristine();
  }
}
