import { Component, HostListener, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { ApiService } from '../../services/api.service';
import { User } from '../../models/models';

/** One row in the settings menu: either navigates to a route or runs an action. */
interface SettingsItem {
  icon: string;
  label: string;
  route?: string;
  action?: () => void;
  danger?: boolean;
  separated?: boolean;
}

@Component({
  selector: 'app-navbar',
  standalone: false,
  template: `
    <nav class="navbar">
      <div class="navbar-brand" (click)="navigate('/dashboard')">
        <span class="logo">🚭</span>
        <span class="brand-text">Smoke Free Journey</span>
      </div>
      <ul class="navbar-menu">
        <li (click)="navigate('/dashboard')" [class.active]="isActive('/dashboard')">
          <span class="icon">📊</span>
          Dashboard
        </li>
        <li (click)="navigate('/calendar')" [class.active]="isActive('/calendar')">
          <span class="icon">📅</span>
          Calendar
        </li>
        <li (click)="navigate('/analytics')" [class.active]="isActive('/analytics')">
          <span class="icon">📈</span>
          Analytics
        </li>
        <li (click)="navigate('/health')" [class.active]="isActive('/health')">
          <span class="icon">❤️</span>
          Health
        </li>
        <li (click)="navigate('/achievements')" [class.active]="isActive('/achievements')">
          <span class="icon">🏆</span>
          Achievements
        </li>
        <li (click)="navigate('/motivation')" [class.active]="isActive('/motivation')">
          <span class="icon">💪</span>
          Motivation
        </li>
        <li (click)="navigate('/craving-help')" [class.active]="isActive('/craving-help')" class="craving-btn">
          <span class="icon">🆘</span>
          Craving Help
        </li>
      </ul>
      <div class="user-menu">
        <span class="username">{{ getDisplayName() }}</span>

        <div class="settings-menu">
          <button
            class="settings-btn"
            [class.open]="settingsOpen"
            type="button"
            aria-haspopup="menu"
            [attr.aria-expanded]="settingsOpen"
            aria-label="Settings"
            title="Settings"
            (click)="toggleSettings($event)">
            <span class="gear">⚙️</span>
          </button>

          <div class="settings-dropdown" *ngIf="settingsOpen" role="menu">
            <span class="dropdown-title">Settings</span>
            <button
              class="dropdown-item"
              type="button"
              role="menuitem"
              *ngFor="let item of settingsItems"
              [class.danger]="item.danger"
              [class.separated]="item.separated"
              (click)="openSetting(item)">
              <span class="item-icon">{{ item.icon }}</span>
              <span class="item-label">{{ item.label }}</span>
            </button>
          </div>
        </div>

      </div>
    </nav>
  `,
  styles: [`
    .navbar {
      display: flex;
      justify-content: space-between;
      align-items: center;
      padding: 15px 30px;
      background: rgba(255, 255, 255, 0.05);
      backdrop-filter: blur(10px);
      border-bottom: 1px solid rgba(255, 255, 255, 0.1);
    }
    
    .navbar-brand {
      display: flex;
      align-items: center;
      gap: 12px;
      cursor: pointer;
      transition: transform 0.3s ease;
      
      &:hover {
        transform: scale(1.05);
      }
    }
    
    .logo {
      font-size: 32px;
    }
    
    .brand-text {
      font-size: 20px;
      font-weight: 700;
      background: linear-gradient(135deg, #10b981, #34d399);
      -webkit-background-clip: text;
      -webkit-text-fill-color: transparent;
    }
    
    .navbar-menu {
      display: flex;
      list-style: none;
      gap: 10px;
      
      li {
        display: flex;
        align-items: center;
        gap: 8px;
        padding: 10px 16px;
        border-radius: 10px;
        cursor: pointer;
        transition: all 0.3s ease;
        font-weight: 500;
        
        &:hover {
          background: rgba(255, 255, 255, 0.1);
        }
        
        &.active {
          background: rgba(16, 185, 129, 0.2);
          color: #10b981;
        }
        
        &.craving-btn {
          background: linear-gradient(135deg, #ef4444, #dc2626);
          color: white;
          
          &:hover {
            transform: scale(1.05);
            box-shadow: 0 5px 20px rgba(239, 68, 68, 0.4);
          }
        }
      }
    }
    
    .icon {
      font-size: 18px;
    }
    
    .user-menu {
      display: flex;
      align-items: center;
      gap: 15px;
    }
    
    .username {
      font-weight: 500;
      color: #10b981;
    }
    
    .settings-menu {
      position: relative;
    }

    .settings-btn {
      display: flex;
      align-items: center;
      justify-content: center;
      width: 38px;
      height: 38px;
      border-radius: 50%;
      border: 1px solid rgba(255, 255, 255, 0.15);
      background: rgba(255, 255, 255, 0.06);
      cursor: pointer;
      transition: all 0.3s ease;

      .gear {
        font-size: 17px;
        line-height: 1;
        display: block;
        transition: transform 0.4s ease;
      }

      &:hover {
        background: rgba(255, 255, 255, 0.16);

        .gear {
          transform: rotate(45deg);
        }
      }

      &.open {
        background: rgba(16, 185, 129, 0.2);
        border-color: rgba(16, 185, 129, 0.45);

        .gear {
          transform: rotate(90deg);
        }
      }
    }

    .settings-dropdown {
      position: absolute;
      top: calc(100% + 10px);
      right: 0;
      z-index: 50;
      min-width: 210px;
      padding: 8px;
      background: #111827;
      border: 1px solid rgba(255, 255, 255, 0.12);
      border-radius: 14px;
      box-shadow: 0 18px 45px rgba(0, 0, 0, 0.45);
      animation: dropdownIn 0.18s ease-out;

      .dropdown-title {
        display: block;
        padding: 8px 12px 10px;
        font-size: 11px;
        font-weight: 600;
        text-transform: uppercase;
        letter-spacing: 0.07em;
        color: rgba(255, 255, 255, 0.4);
      }
    }

    .dropdown-item {
      display: flex;
      align-items: center;
      gap: 12px;
      width: 100%;
      padding: 10px 12px;
      border: none;
      border-radius: 10px;
      background: transparent;
      color: white;
      font-family: 'Poppins', sans-serif;
      text-align: left;
      cursor: pointer;
      transition: background 0.2s ease;

      &:hover {
        background: rgba(255, 255, 255, 0.08);
      }

      .item-icon {
        font-size: 17px;
        line-height: 1;
      }

      .item-label {
        font-size: 14px;
        font-weight: 500;
        white-space: nowrap;
      }

      &.separated {
        margin-top: 6px;
        padding-top: 14px;
        border-top: 1px solid rgba(255, 255, 255, 0.1);
        border-radius: 0 0 10px 10px;
      }

      &.danger {
        .item-label {
          color: #f87171;
        }

        &:hover {
          background: rgba(239, 68, 68, 0.14);
        }
      }
    }

    @keyframes dropdownIn {
      from { opacity: 0; transform: translateY(-6px); }
      to   { opacity: 1; transform: translateY(0); }
    }


    @media (max-width: 768px) {
      .navbar {
        flex-direction: column;
        gap: 15px;
      }
      
      .navbar-menu {
        flex-wrap: wrap;
        justify-content: center;
      }
      
      .user-menu {
        order: -1;
      }

      .settings-dropdown {
        right: auto;
        left: 50%;
        transform: translateX(-50%);
      }
    }
  `]
})
export class NavbarComponent implements OnInit {
  currentUser: User | null = null;
  settingsOpen = false;

  /** Everything in the settings menu. Add a row here to add a setting. */
  settingsItems: SettingsItem[] = [
    { icon: '📆', label: 'Quit date & habits', route: '/setup' },
    { icon: '👤', label: 'Account', route: '/account' },
    { icon: '🚪', label: 'Log out', action: () => this.logout(), danger: true, separated: true }
  ];

  constructor(
    private router: Router,
    private apiService: ApiService
  ) {}

  ngOnInit(): void {
    this.apiService.currentUser$.subscribe(user => {
      this.currentUser = user;
    });
  }

  navigate(path: string): void {
    this.settingsOpen = false;
    this.router.navigate([path]);
  }

  toggleSettings(event: MouseEvent): void {
    // stop the document listener below from closing it again straight away
    event.stopPropagation();
    this.settingsOpen = !this.settingsOpen;
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
  }

  @HostListener('document:keydown.escape')
  onEscape(): void {
    this.settingsOpen = false;
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

  isActive(path: string): boolean {
    return this.router.url === path;
  }
}
