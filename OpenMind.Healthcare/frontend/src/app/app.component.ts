import { Component, OnInit } from '@angular/core';
import { NavigationEnd, Router } from '@angular/router';
import { filter } from 'rxjs/operators';
import { ApiService } from './services/api.service';
import { programForUrl } from './programs/programs';
import { LayoutService } from './services/layout.service';
import { ThemeService } from './services/theme.service';

/**
 * The application shell: a full-width top bar, then a left rail and the content beside it.
 *
 * The rail appears only inside a programme. The hub, account and auth pages get the full width,
 * because there is no programme navigation to show there.
 */
@Component({
  selector: 'app-root',
  standalone: false,
  template: `
    <app-navbar *ngIf="showChrome"></app-navbar>

    <div class="shell-body" [class.chromeless]="!showChrome">
      <app-sidebar *ngIf="showChrome && inProgram && !sidebarHidden"></app-sidebar>

      <main class="main-content" [class.full-height]="!showChrome">
        <router-outlet></router-outlet>
      </main>
    </div>
  `,
  styles: [`
    .shell-body {
      display: flex;
      align-items: flex-start;
      min-height: calc(100vh - var(--header-h));
    }

    .shell-body.chromeless { display: block; }

    .main-content {
      flex: 1;
      min-width: 0;
      padding: 24px;
    }

    .main-content.full-height {
      min-height: 100vh;
      padding: 0;
    }

    @media (max-width: 640px) {
      .shell-body { display: block; }
      .main-content { padding: 16px; }
    }
  `]
})
export class AppComponent implements OnInit {
  title = 'OpenMind Health';
  showChrome = false;
  inProgram = false;
  sidebarHidden = false;

  constructor(
    private router: Router,
    private apiService: ApiService,
    private layout: LayoutService,
    private theme: ThemeService
  ) {}

  ngOnInit(): void {
    this.theme.init();

    this.router.events
      .pipe(filter((e): e is NavigationEnd => e instanceof NavigationEnd))
      .subscribe(() => this.updateShell());

    this.apiService.currentUser$.subscribe(() => this.updateShell());

    this.layout.sidebarHidden$.subscribe(hidden => (this.sidebarHidden = hidden));

    this.updateShell();
  }

  private updateShell(): void {
    const url = this.router.url;
    const isAuthRoute = url === '/login' || url === '/register';

    this.showChrome = this.apiService.isLoggedIn && !isAuthRoute;
    this.inProgram = programForUrl(url) !== null;
  }
}
