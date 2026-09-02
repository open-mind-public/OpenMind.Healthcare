import { Component, OnInit } from '@angular/core';
import { NavigationEnd, Router } from '@angular/router';
import { filter } from 'rxjs/operators';
import { HealthProgram, ProgramNavItem, absolutePath, programForUrl } from '../../programs/programs';
import { LayoutService } from '../../services/layout.service';

/**
 * The left navigation rail: the pages of whichever programme you are currently in.
 *
 * A vertical rail rather than a horizontal strip because programmes keep growing pages, and a
 * top bar runs out of room long before a sidebar does. It renders nothing outside a programme,
 * so the hub and account pages get the full width.
 *
 * Like the top bar, it reads the programme registry - it has no knowledge of any specific
 * programme.
 */
@Component({
  selector: 'app-sidebar',
  standalone: false,
  template: `
    <aside class="sidebar" id="program-rail" *ngIf="program" [style.--accent]="program.accent">
      <div class="rail-head">
        <span class="rail-mark"><app-icon [name]="program.icon" [size]="15"></app-icon></span>
        <span class="rail-name">{{ program.name }}</span>
        <button
          class="rail-close"
          type="button"
          title="Hide navigation"
          aria-label="Hide navigation"
          (click)="hide()">
          <app-icon name="chevron-left" [size]="15"></app-icon>
        </button>
      </div>

      <nav class="rail-nav">
        <button
          type="button"
          class="rail-item"
          *ngFor="let item of program.nav"
          [class.active]="isActive(pathFor(item))"
          [title]="item.label"
          (click)="navigate(pathFor(item))">
          <app-icon [name]="item.icon" [size]="17"></app-icon>
          <span class="rail-label">{{ item.label }}</span>
        </button>
      </nav>

      <div class="rail-foot" *ngIf="program.settings?.length">
        <button
          type="button"
          class="rail-item subtle"
          *ngFor="let item of program.settings"
          [class.active]="isActive(pathFor(item))"
          [title]="item.label"
          (click)="navigate(pathFor(item))">
          <app-icon [name]="item.icon" [size]="17"></app-icon>
          <span class="rail-label">{{ item.label }}</span>
        </button>
      </div>
    </aside>
  `,
  styles: [`
    :host {
      position: sticky;
      top: var(--header-h);
      align-self: flex-start;
      flex-shrink: 0;
      z-index: 40;
      height: calc(100vh - var(--header-h));
    }

    .sidebar {
      width: 226px;
      height: 100%;
      display: flex;
      flex-direction: column;
      gap: 4px;
      padding: 14px 10px;
      background: var(--surface);
      border-right: 1px solid var(--border);
      overflow-y: auto;
    }

    .rail-head {
      display: flex;
      align-items: center;
      gap: 9px;
      padding: 4px 10px 12px;
      margin-bottom: 4px;
      border-bottom: 1px solid var(--border);
    }

    .rail-mark {
      display: inline-flex;
      align-items: center;
      justify-content: center;
      width: 26px;
      height: 26px;
      border-radius: var(--r-sm);
      background: var(--surface-sunken);
      color: var(--accent);
      flex-shrink: 0;
    }

    .rail-close {
      margin-left: auto;
      display: inline-flex;
      align-items: center;
      justify-content: center;
      width: 24px;
      height: 24px;
      border: none;
      border-radius: var(--r-sm);
      background: transparent;
      color: var(--text-muted);
      cursor: pointer;
      flex-shrink: 0;
    }

    .rail-close:hover {
      background: var(--surface-sunken);
      color: var(--text);
    }

    .rail-name {
      font-size: 0.82rem;
      font-weight: 600;
      text-transform: uppercase;
      letter-spacing: 0.05em;
      color: var(--text-secondary);
      white-space: nowrap;
      overflow: hidden;
      text-overflow: ellipsis;
    }

    .rail-nav {
      display: flex;
      flex-direction: column;
      gap: 1px;
    }

    .rail-foot {
      margin-top: auto;
      padding-top: 10px;
      border-top: 1px solid var(--border);
      display: flex;
      flex-direction: column;
      gap: 1px;
    }

    .rail-item {
      display: flex;
      align-items: center;
      gap: 10px;
      width: 100%;
      padding: 8px 10px;
      border: none;
      border-radius: var(--r-md);
      background: transparent;
      color: var(--text-secondary);
      font-family: var(--font);
      font-size: 0.885rem;
      font-weight: 500;
      cursor: pointer;
      text-align: left;
      transition: background-color 0.15s ease, color 0.15s ease;
    }

    .rail-item:hover {
      background: var(--surface-sunken);
      color: var(--text);
    }

    .rail-item.active {
      background: var(--surface-sunken);
      color: var(--accent);
      font-weight: 600;
    }

    .rail-item.subtle { color: var(--text-muted); }
    .rail-item.subtle:hover { color: var(--text-secondary); }

    .rail-label { white-space: nowrap; overflow: hidden; text-overflow: ellipsis; }

    /* Below this the rail collapses to icons; the title attribute carries the label. */
    @media (max-width: 1000px) {
      .sidebar { width: 56px; padding: 14px 8px; }
      .rail-label, .rail-name, .rail-close { display: none; }
      .rail-head { justify-content: center; padding: 4px 0 12px; }
      .rail-item { justify-content: center; padding: 9px 0; }
    }

    @media (max-width: 640px) {
      :host {
        position: static;
        height: auto;
        align-self: stretch;
      }

      .sidebar {
        width: 100%;
        height: auto;
        flex-direction: row;
        align-items: center;
        gap: 2px;
        overflow-x: auto;
        border-right: none;
        border-bottom: 1px solid var(--border);
        padding: 6px 10px;
      }
      .rail-head { display: none; }
      .rail-nav, .rail-foot {
        flex-direction: row;
        margin-top: 0;
        padding-top: 0;
        border-top: none;
      }
      .rail-item { padding: 9px 12px; }
    }
  `]
})
export class SidebarComponent implements OnInit {
  program: HealthProgram | null = null;

  constructor(private router: Router, private layout: LayoutService) {}

  hide(): void {
    this.layout.setSidebarHidden(true);
  }

  ngOnInit(): void {
    this.program = programForUrl(this.router.url);

    this.router.events
      .pipe(filter((e): e is NavigationEnd => e instanceof NavigationEnd))
      .subscribe(e => (this.program = programForUrl(e.urlAfterRedirects)));
  }

  pathFor(item: ProgramNavItem): string {
    return absolutePath(this.program!, item);
  }

  isActive(path: string): boolean {
    return this.router.url.split('?')[0] === path;
  }

  navigate(path: string): void {
    this.router.navigate([path]);
  }
}
