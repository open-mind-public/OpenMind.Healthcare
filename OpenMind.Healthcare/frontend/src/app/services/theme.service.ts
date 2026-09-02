import { Injectable } from '@angular/core';
import { BehaviorSubject } from 'rxjs';

export type ThemePreference = 'light' | 'dark' | 'system';

export const THEME_STORAGE_KEY = 'openmind.theme';

/**
 * The member's theme preference, and the `data-theme` attribute that expresses it.
 *
 * Three states, not two. "System" is the default and a real choice: it follows the operating
 * system and keeps following it, so someone whose machine switches at dusk gets that for free.
 * A two-way toggle would silently pin them to whatever the OS happened to be on first load.
 *
 * How it reaches CSS: an explicit choice sets `data-theme` on the document root and wins over
 * everything. "System" removes the attribute entirely, letting the `prefers-color-scheme` media
 * query in styles.scss decide - so there is no listener to keep in sync, and no state to go
 * stale when the OS changes while the tab is open.
 */
@Injectable({ providedIn: 'root' })
export class ThemeService {
  private readonly preference = new BehaviorSubject<ThemePreference>(this.read());

  readonly theme$ = this.preference.asObservable();

  get theme(): ThemePreference {
    return this.preference.value;
  }

  /** Called once at startup; the same attribute is also set by an inline script in index.html. */
  init(): void {
    this.apply(this.preference.value);
  }

  setTheme(value: ThemePreference): void {
    this.preference.next(value);
    this.apply(value);

    try {
      localStorage.setItem(THEME_STORAGE_KEY, value);
    } catch {
      // Private windows and blocked site data throw. The theme still applies for this
      // session; it just will not be remembered.
    }
  }

  private apply(value: ThemePreference): void {
    const root = document.documentElement;

    if (value === 'system') {
      root.removeAttribute('data-theme');
    } else {
      root.setAttribute('data-theme', value);
    }
  }

  private read(): ThemePreference {
    try {
      const stored = localStorage.getItem(THEME_STORAGE_KEY);
      return stored === 'light' || stored === 'dark' ? stored : 'system';
    } catch {
      return 'system';
    }
  }
}
