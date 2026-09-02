import { Injectable } from '@angular/core';
import { BehaviorSubject } from 'rxjs';

const STORAGE_KEY = 'openmind.sidebar.hidden';

/**
 * Shell layout state shared by the top bar (which owns the toggle), the rail (which hides) and
 * the shell (which reclaims the column).
 *
 * The choice is remembered, because a member who hides the rail wants it to stay hidden on the
 * next page and the next visit - re-opening it on every navigation would read as a bug.
 * Storage is guarded: a browser with site data blocked still gets a working toggle, just not a
 * remembered one.
 */
@Injectable({ providedIn: 'root' })
export class LayoutService {
  private readonly hidden = new BehaviorSubject<boolean>(this.read());

  /** True when the member has hidden the navigation rail. */
  readonly sidebarHidden$ = this.hidden.asObservable();

  get sidebarHidden(): boolean {
    return this.hidden.value;
  }

  toggleSidebar(): void {
    this.setSidebarHidden(!this.hidden.value);
  }

  setSidebarHidden(value: boolean): void {
    this.hidden.next(value);

    try {
      localStorage.setItem(STORAGE_KEY, value ? '1' : '0');
    } catch {
      // Private windows and blocked site data throw here. The toggle still works for
      // this session; it just will not be remembered.
    }
  }

  private read(): boolean {
    try {
      return localStorage.getItem(STORAGE_KEY) === '1';
    } catch {
      return false;
    }
  }
}
