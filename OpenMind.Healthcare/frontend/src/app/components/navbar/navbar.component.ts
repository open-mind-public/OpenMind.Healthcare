import { Component, HostListener, OnInit } from '@angular/core';
import { NavigationEnd, Router } from '@angular/router';
import { filter } from 'rxjs/operators';
import { ApiService } from '../../services/api.service';
import { LayoutService } from '../../services/layout.service';
import { ThemePreference, ThemeService } from '../../services/theme.service';
import { User } from '../../models/models';
import { IconName } from '../icon/icon.component';
import { HealthProgram, PROGRAMS, absolutePath, programForUrl } from '../../programs/programs';

/** One row in the settings menu: either navigates to a route or runs an action. */
interface SettingsItem {
  icon: IconName;
  label: string;
  kind?: 'theme';
  route?: string;
  action?: () => void;
  danger?: boolean;
  separated?: boolean;
}

/**
 * The platform shell.
 *
 * Two bars, deliberately. The upper one is OpenMind Health: the brand, which programme you are
 * in, and your account. The lower one belongs to whichever programme you are inside, and shows
 * only that programme's pages.
 *
 * The separation is the whole point. Quitting smoking and diet are peer programmes, not features
 * of one another, so neither appears in the other's menu - and a third programme is a registry
 * entry rather than an edit to either of theirs.
 */
@Component({
  selector: 'app-navbar',
  standalone: false,
  template: `
    <header class="shell">
      <div class="platform-bar">
        <button
          class="icon-btn rail-toggle"
          type="button"
          *ngIf="inProgram"
          [attr.aria-expanded]="!sidebarHidden"
          aria-controls="program-rail"
          [title]="sidebarHidden ? 'Show navigation' : 'Hide navigation'"
          [attr.aria-label]="sidebarHidden ? 'Show navigation' : 'Hide navigation'"
          (click)="toggleSidebar($event)">
          <app-icon name="panel-left" [size]="17"></app-icon>
        </button>

        <div class="brand" (click)="goHome()" title="All programmes">
          <span class="brand-mark"><app-icon name="layers" [size]="16"></app-icon></span>
          <span class="brand-text">OpenMind&nbsp;Health</span>
        </div>

        <div class="program-switcher">
          <button
            class="switcher-btn"
            type="button"
            aria-haspopup="menu"
            [attr.aria-expanded]="switcherOpen"
            (click)="toggleSwitcher($event)">
            <ng-container *ngIf="currentProgram; else allProgrammes">
              <app-icon [name]="currentProgram.icon" [size]="15"></app-icon>
              <span class="switcher-name">{{ currentProgram.name }}</span>
            </ng-container>
            <ng-template #allProgrammes>
              <app-icon name="grid" [size]="15"></app-icon>
              <span class="switcher-name">Choose a programme</span>
            </ng-template>
            <app-icon class="caret" [class.open]="switcherOpen" name="chevron-down" [size]="14"></app-icon>
          </button>

          <div class="menu switcher-dropdown" *ngIf="switcherOpen" role="menu">
            <span class="menu-title">Programmes</span>
            <button
              class="program-option"
              type="button"
              role="menuitem"
              *ngFor="let program of programs"
              [class.current]="program.id === currentProgram?.id"
              [style.--accent]="program.accent"
              (click)="enter(program)">
              <span class="option-mark"><app-icon [name]="program.icon" [size]="16"></app-icon></span>
              <span class="option-text">
                <strong>{{ program.name }}</strong>
                <small>{{ program.tagline }}</small>
              </span>
              <app-icon class="option-check" *ngIf="program.id === currentProgram?.id" name="check" [size]="15"></app-icon>
            </button>
            <button class="menu-item separated" type="button" role="menuitem" (click)="goHome()">
              <app-icon name="grid" [size]="16"></app-icon>
              <span>All programmes</span>
            </button>
          </div>
        </div>

        <div class="user-menu">
          <span class="username">{{ getDisplayName() }}</span>

          <div class="settings-menu">
            <button
              class="icon-btn"
              [class.open]="settingsOpen"
              type="button"
              aria-haspopup="menu"
              [attr.aria-expanded]="settingsOpen"
              aria-label="Settings"
              title="Settings"
              (click)="toggleSettings($event)">
              <app-icon name="settings" [size]="17"></app-icon>
            </button>

            <div class="menu settings-dropdown" *ngIf="settingsOpen" role="menu">
              <span class="menu-title">Settings</span>

              <ng-container *ngFor="let item of settingsItems">
                <!-- Theme is a settings row like any other; it just expands in place. -->
                <ng-container *ngIf="item.kind === 'theme'; else plainItem">
                  <button
                    class="menu-item"
                    type="button"
                    role="menuitem"
                    [class.separated]="item.separated"
                    [attr.aria-expanded]="themeOpen"
                    (click)="toggleThemeOptions($event)">
                    <app-icon [name]="currentThemeOption.icon" [size]="16"></app-icon>
                    <span>Theme</span>
                    <span class="menu-value">{{ currentThemeOption.label }}</span>
                    <app-icon class="menu-caret" [class.open]="themeOpen" name="chevron-down" [size]="13"></app-icon>
                  </button>

                  <div class="theme-switch" *ngIf="themeOpen" role="group" aria-label="Theme">
                    <button
                      type="button"
                      *ngFor="let option of themeOptions"
                      [class.active]="theme === option.value"
                      [attr.aria-pressed]="theme === option.value"
                      [title]="option.label"
                      (click)="setTheme(option.value, $event)">
                      <app-icon [name]="option.icon" [size]="14"></app-icon>
                      <span>{{ option.label }}</span>
                    </button>
                  </div>
                </ng-container>

                <ng-template #plainItem>
                  <button
                    class="menu-item"
                    type="button"
                    role="menuitem"
                    [class.danger]="item.danger"
                    [class.separated]="item.separated"
                    (click)="openSetting(item)">
                    <app-icon [name]="item.icon" [size]="16"></app-icon>
                    <span>{{ item.label }}</span>
                  </button>
                </ng-template>
              </ng-container>
            </div>
          </div>
        </div>
      </div>

    </header>
  `,
  styles: [`
    /* The sticky element must be the host. On the inner .shell it never moves: its
       containing block is the host, which is exactly as tall as it is. */
    :host {
      position: sticky;
      top: 0;
      z-index: 50;
      display: block;
    }

    .shell {
      background: var(--surface);
    }

    .platform-bar {
      display: flex;
      align-items: center;
      gap: 12px;
      height: var(--header-h);
      padding: 0 20px;
      border-bottom: 1px solid var(--border);
    }

    /* --- brand --- */

    .rail-toggle { margin-right: -2px; }

    .brand {
      display: flex;
      align-items: center;
      gap: 9px;
      cursor: pointer;
      flex-shrink: 0;
    }

    .brand-mark {
      display: inline-flex;
      align-items: center;
      justify-content: center;
      width: 26px;
      height: 26px;
      border-radius: var(--r-sm);
      background: var(--accent);
      color: var(--text-on-accent);
    }

    .brand-text {
      font-size: 0.94rem;
      font-weight: 600;
      letter-spacing: -0.01em;
      color: var(--text);
      white-space: nowrap;
    }

    /* --- programme switcher --- */

    .program-switcher {
      position: relative;
      margin-right: auto;
      padding-left: 16px;
      margin-left: 4px;
      border-left: 1px solid var(--border);
    }

    .switcher-btn {
      display: flex;
      align-items: center;
      gap: 8px;
      padding: 6px 10px;
      border-radius: var(--r-md);
      border: 1px solid transparent;
      background: transparent;
      color: var(--text);
      cursor: pointer;
      font-family: var(--font);
      font-size: 0.88rem;
      font-weight: 500;
      transition: background-color 0.15s ease;
    }

    .switcher-btn:hover { background: var(--surface-sunken); }

    .switcher-name { white-space: nowrap; }

    .caret {
      color: var(--text-muted);
      transition: transform 0.15s ease;
    }

    .caret.open { transform: rotate(180deg); }

    /* --- menus --- */

    .menu {
      position: absolute;
      top: calc(100% + 6px);
      background: var(--surface);
      border: 1px solid var(--border);
      border-radius: var(--r-lg);
      box-shadow: var(--shadow-lg);
      padding: 5px;
      display: flex;
      flex-direction: column;
      gap: 1px;
      min-width: 220px;
    }

    .switcher-dropdown { left: 12px; min-width: 300px; }
    .settings-dropdown { right: 0; min-width: 248px; }

    .menu-value {
      margin-left: auto;
      color: var(--text-muted);
      font-size: 0.8rem;
    }

    .menu-caret {
      color: var(--text-muted);
      transition: transform 0.15s ease;
    }

    .menu-caret.open { transform: rotate(180deg); }

    .theme-switch {
      display: flex;
      gap: 2px;
      padding: 2px;
      margin: 2px 8px 6px 34px;
      border-radius: var(--r-md);
      background: var(--surface-sunken);
    }

    .theme-switch button {
      flex: 1;
      display: inline-flex;
      align-items: center;
      justify-content: center;
      gap: 5px;
      padding: 6px 4px;
      border: 1px solid transparent;
      border-radius: var(--r-sm);
      background: transparent;
      color: var(--text-secondary);
      font-family: var(--font);
      font-size: 0.76rem;
      font-weight: 500;
      cursor: pointer;
      transition: background-color 0.15s ease, color 0.15s ease;
    }

    .theme-switch button:hover { color: var(--text); }

    .theme-switch button.active {
      background: var(--surface);
      border-color: var(--border);
      color: var(--text);
      box-shadow: var(--shadow-xs);
    }

    .menu-title {
      font-size: 0.68rem;
      text-transform: uppercase;
      letter-spacing: 0.06em;
      color: var(--text-muted);
      padding: 8px 10px 5px;
      font-weight: 600;
    }

    .menu-item {
      display: flex;
      align-items: center;
      gap: 10px;
      padding: 8px 10px;
      border: none;
      border-radius: var(--r-sm);
      background: transparent;
      color: var(--text);
      cursor: pointer;
      text-align: left;
      font-family: var(--font);
      font-size: 0.875rem;
    }

    .menu-item:hover { background: var(--surface-sunken); }
    .menu-item.danger { color: var(--danger); }
    .menu-item.danger:hover { background: var(--danger-subtle); }

    .menu-item.separated {
      margin-top: 5px;
      padding-top: 11px;
      border-top: 1px solid var(--border);
      border-radius: 0 0 var(--r-sm) var(--r-sm);
    }

    .program-option {
      display: flex;
      align-items: center;
      gap: 11px;
      padding: 9px 10px;
      border: none;
      border-radius: var(--r-sm);
      background: transparent;
      color: var(--text);
      cursor: pointer;
      text-align: left;
      font-family: var(--font);
    }

    .program-option:hover { background: var(--surface-sunken); }
    .program-option.current { background: var(--surface-sunken); }

    .option-mark {
      display: inline-flex;
      align-items: center;
      justify-content: center;
      width: 30px;
      height: 30px;
      border-radius: var(--r-sm);
      background: var(--surface-sunken);
      color: var(--accent);
      flex-shrink: 0;
    }

    .program-option.current .option-mark {
      background: var(--accent);
      color: var(--text-on-accent);
    }

    .option-text { display: flex; flex-direction: column; gap: 1px; min-width: 0; flex: 1; }
    .option-text strong { font-size: 0.875rem; font-weight: 600; }
    .option-text small { font-size: 0.75rem; color: var(--text-muted); line-height: 1.35; }

    .option-check { color: var(--accent); flex-shrink: 0; }

    /* --- user --- */

    .user-menu {
      display: flex;
      align-items: center;
      gap: 10px;
      flex-shrink: 0;
    }

    .username {
      color: var(--text-secondary);
      font-size: 0.85rem;
      font-weight: 500;
    }

    .settings-menu { position: relative; }

    .icon-btn {
      display: inline-flex;
      align-items: center;
      justify-content: center;
      width: 32px;
      height: 32px;
      border-radius: var(--r-md);
      border: 1px solid transparent;
      background: transparent;
      color: var(--text-secondary);
      cursor: pointer;
      transition: background-color 0.15s ease, color 0.15s ease;
    }

    .icon-btn:hover, .icon-btn.open {
      background: var(--surface-sunken);
      color: var(--text);
    }

    @media (max-width: 820px) {
      .platform-bar { padding: 0 14px; gap: 10px; }
      .username { display: none; }
      .program-switcher { padding-left: 10px; }
      .switcher-name { max-width: 110px; overflow: hidden; text-overflow: ellipsis; }
      .switcher-dropdown { min-width: 260px; left: 4px; }
    }
  `]
})
export class NavbarComponent implements OnInit {
  currentUser: User | null = null;
  settingsOpen = false;
  switcherOpen = false;

  readonly programs = PROGRAMS;
  currentProgram: HealthProgram | null = null;

  sidebarHidden = false;
  theme: ThemePreference = 'system';

  readonly themeOptions: { value: ThemePreference; label: string; icon: IconName }[] = [
    { value: 'light', label: 'Light', icon: 'sun' },
    { value: 'dark', label: 'Dark', icon: 'moon' },
    { value: 'system', label: 'System', icon: 'monitor' }
  ];

  get inProgram(): boolean {
    return this.currentProgram !== null;
  }

  constructor(
    private router: Router,
    private apiService: ApiService,
    private layout: LayoutService,
    private themeService: ThemeService
  ) {}

  themeOpen = false;

  get currentThemeOption(): { value: ThemePreference; label: string; icon: IconName } {
    return this.themeOptions.find(o => o.value === this.theme) ?? this.themeOptions[2];
  }

  toggleThemeOptions(event: MouseEvent): void {
    // Keep the settings menu open; only the theme row expands.
    event.stopPropagation();
    this.themeOpen = !this.themeOpen;
  }

  /** Deliberately leaves the menu open: choosing a theme is something you compare. */
  setTheme(value: ThemePreference, event: MouseEvent): void {
    event.stopPropagation();
    this.themeService.setTheme(value);
  }

  toggleSidebar(event: MouseEvent): void {
    event.stopPropagation();
    this.layout.toggleSidebar();
  }

  ngOnInit(): void {
    this.apiService.currentUser$.subscribe(user => {
      this.currentUser = user;
    });

    this.layout.sidebarHidden$.subscribe(hidden => (this.sidebarHidden = hidden));

    this.themeService.theme$.subscribe(theme => (this.theme = theme));

    this.currentProgram = programForUrl(this.router.url);

    this.router.events
      .pipe(filter((e): e is NavigationEnd => e instanceof NavigationEnd))
      .subscribe(e => {
        this.currentProgram = programForUrl(e.urlAfterRedirects);
        this.settingsOpen = false;
        this.switcherOpen = false;
      });
  }

  /**
   * The current programme's own settings come first, then the platform-level rows. A programme's
   * settings never appear while you are somewhere else.
   */
  get settingsItems(): SettingsItem[] {
    const program = this.currentProgram;

    const programSettings: SettingsItem[] = (program?.settings ?? []).map(item => ({
      icon: item.icon,
      label: item.label,
      route: absolutePath(program!, item)
    }));

    return [
      ...programSettings,
      { icon: 'user', label: 'Account', route: '/account', separated: programSettings.length > 0 },
      { icon: this.currentThemeOption.icon, label: 'Theme', kind: 'theme' },
      { icon: 'logout', label: 'Log out', action: () => this.logout(), danger: true, separated: true }
    ];
  }

  navigate(path: string): void {
    this.settingsOpen = false;
    this.switcherOpen = false;
    this.router.navigate([path]);
  }

  goHome(): void {
    this.navigate('/home');
  }

  enter(program: HealthProgram): void {
    this.navigate(program.home);
  }

  toggleSettings(event: MouseEvent): void {
    // stop the document listener below from closing it again straight away
    event.stopPropagation();
    this.switcherOpen = false;
    this.settingsOpen = !this.settingsOpen;
    if (!this.settingsOpen) { this.themeOpen = false; }
  }

  toggleSwitcher(event: MouseEvent): void {
    event.stopPropagation();
    this.settingsOpen = false;
    this.switcherOpen = !this.switcherOpen;
  }

  openSetting(item: SettingsItem): void {
    this.settingsOpen = false;

    if (item.action) {
      item.action();
      return;
    }

    if (item.route) {
      this.router.navigate([item.route]);
    }
  }

  @HostListener('document:click')
  onDocumentClick(): void {
    this.settingsOpen = false;
    this.switcherOpen = false;
    this.themeOpen = false;
  }

  @HostListener('document:keydown.escape')
  onEscape(): void {
    this.settingsOpen = false;
    this.switcherOpen = false;
    this.themeOpen = false;
  }

  logout(): void {
    this.apiService.logout();
    this.router.navigate(['/login']);
  }

  getDisplayName(): string {
    if (!this.currentUser) return 'Guest';

    const firstName = this.currentUser.firstName || '';
    const lastName = this.currentUser.lastName || '';

    if (firstName && lastName) {
      return `${firstName} ${lastName}`;
    } else if (firstName) {
      return firstName;
    } else if (lastName) {
      return lastName;
    } else {
      return this.currentUser.email || 'Guest';
    }
  }
}
