import { Component, Input } from '@angular/core';
import { DomSanitizer, SafeHtml } from '@angular/platform-browser';

/**
 * A small inline line-icon set.
 *
 * Emoji were doing this job before. They render differently on every platform, cannot inherit
 * colour or stroke weight, and read as decoration rather than interface - which is most of what
 * made the product look unprofessional. These are 24x24 stroke paths using `currentColor`, so an
 * icon takes the colour and weight of whatever it sits in.
 *
 * Deliberately no icon-font or SVG-sprite dependency: the set is small enough to inline.
 */
export type IconName =
  | 'grid' | 'chart' | 'calendar' | 'trending' | 'heart' | 'award' | 'zap' | 'lifebuoy'
  | 'utensils' | 'scale' | 'lightbulb' | 'target' | 'settings' | 'logout' | 'user'
  | 'chevron-down' | 'chevron-left' | 'arrow-right' | 'plus' | 'close' | 'check' | 'clock'
  | 'leaf' | 'layers' | 'panel-left' | 'sun' | 'moon' | 'monitor';

const PATHS: Record<IconName, string> = {
  grid: '<rect x="3" y="3" width="7" height="7" rx="1.5"/><rect x="14" y="3" width="7" height="7" rx="1.5"/><rect x="3" y="14" width="7" height="7" rx="1.5"/><rect x="14" y="14" width="7" height="7" rx="1.5"/>',
  chart: '<path d="M3 3v18h18"/><path d="M7 15l3.5-4 3 2.5L20 7"/>',
  calendar: '<rect x="3" y="5" width="18" height="16" rx="2"/><path d="M3 10h18M8 3v4M16 3v4"/>',
  trending: '<path d="M3 17l6-6 4 4 8-8"/><path d="M15 7h6v6"/>',
  heart: '<path d="M20.8 5.6a5 5 0 0 0-7.1 0L12 7.3l-1.7-1.7a5 5 0 0 0-7.1 7.1l8.8 8.8 8.8-8.8a5 5 0 0 0 0-7.1z"/>',
  award: '<circle cx="12" cy="9" r="6"/><path d="M8.2 14.3L7 22l5-3 5 3-1.2-7.7"/>',
  zap: '<path d="M13 2L4 14h7l-1 8 9-12h-7l1-8z"/>',
  lifebuoy: '<circle cx="12" cy="12" r="9"/><circle cx="12" cy="12" r="3.5"/><path d="M5.6 5.6l3.9 3.9M14.5 14.5l3.9 3.9M18.4 5.6l-3.9 3.9M9.5 14.5l-3.9 3.9"/>',
  utensils: '<path d="M4 3v7a3 3 0 0 0 6 0V3"/><path d="M7 10v11"/><path d="M17 3c-1.7 1-2.5 3-2.5 5.5S15.3 13 17 14v7"/>',
  scale: '<path d="M12 3v18"/><path d="M5 7h14"/><path d="M8 7l-4 7a4 4 0 0 0 8 0z"/><path d="M16 7l-4 7a4 4 0 0 0 8 0z" transform="translate(4 0)"/>',
  lightbulb: '<path d="M9 18h6"/><path d="M10 22h4"/><path d="M12 2a6 6 0 0 0-3.5 10.9c.6.5 1 1.2 1 2h5c0-.8.4-1.5 1-2A6 6 0 0 0 12 2z"/>',
  target: '<circle cx="12" cy="12" r="9"/><circle cx="12" cy="12" r="5"/><circle cx="12" cy="12" r="1.5"/>',
  settings: '<circle cx="12" cy="12" r="3"/><path d="M19.4 15a1.6 1.6 0 0 0 .3 1.8l.1.1a2 2 0 1 1-2.8 2.8l-.1-.1a1.6 1.6 0 0 0-1.8-.3 1.6 1.6 0 0 0-1 1.5V21a2 2 0 1 1-4 0v-.1A1.6 1.6 0 0 0 9 19.4a1.6 1.6 0 0 0-1.8.3l-.1.1a2 2 0 1 1-2.8-2.8l.1-.1a1.6 1.6 0 0 0 .3-1.8 1.6 1.6 0 0 0-1.5-1H3a2 2 0 1 1 0-4h.1A1.6 1.6 0 0 0 4.6 9a1.6 1.6 0 0 0-.3-1.8l-.1-.1a2 2 0 1 1 2.8-2.8l.1.1a1.6 1.6 0 0 0 1.8.3H9a1.6 1.6 0 0 0 1-1.5V3a2 2 0 1 1 4 0v.1a1.6 1.6 0 0 0 1 1.5 1.6 1.6 0 0 0 1.8-.3l.1-.1a2 2 0 1 1 2.8 2.8l-.1.1a1.6 1.6 0 0 0-.3 1.8V9a1.6 1.6 0 0 0 1.5 1H21a2 2 0 1 1 0 4h-.1a1.6 1.6 0 0 0-1.5 1z"/>',
  logout: '<path d="M9 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h4"/><path d="M16 17l5-5-5-5"/><path d="M21 12H9"/>',
  user: '<circle cx="12" cy="8" r="4"/><path d="M4 21a8 8 0 0 1 16 0"/>',
  'chevron-down': '<path d="M6 9l6 6 6-6"/>',
  'chevron-left': '<path d="M15 6l-6 6 6 6"/>',
  'panel-left': '<rect x="3" y="4" width="18" height="16" rx="2"/><path d="M9.5 4v16"/>',
  sun: '<circle cx="12" cy="12" r="4"/><path d="M12 2v2M12 20v2M4.9 4.9l1.4 1.4M17.7 17.7l1.4 1.4M2 12h2M20 12h2M4.9 19.1l1.4-1.4M17.7 6.3l1.4-1.4"/>',
  moon: '<path d="M21 12.8A9 9 0 1 1 11.2 3a7 7 0 0 0 9.8 9.8z"/>',
  monitor: '<rect x="2" y="4" width="20" height="13" rx="2"/><path d="M8 21h8M12 17v4"/>',
  'arrow-right': '<path d="M5 12h14"/><path d="M13 6l6 6-6 6"/>',
  plus: '<path d="M12 5v14M5 12h14"/>',
  close: '<path d="M6 6l12 12M18 6L6 18"/>',
  check: '<path d="M20 6L9 17l-5-5"/>',
  clock: '<circle cx="12" cy="12" r="9"/><path d="M12 7v5l3 2"/>',
  leaf: '<path d="M11 20A7 7 0 0 1 4 13c0-5 4-9 16-9 0 10-4 14-9 14z"/><path d="M4 21c2-6 5-9 9-11"/>',
  layers: '<path d="M12 2l9 5-9 5-9-5 9-5z"/><path d="M3 12l9 5 9-5"/><path d="M3 17l9 5 9-5"/>'
};

@Component({
  selector: 'app-icon',
  standalone: false,
  template: `<span class="icon-wrap" [style.width.px]="size" [style.height.px]="size" [innerHTML]="svg"></span>`,
  styles: [`
    .icon-wrap {
      display: inline-flex;
      align-items: center;
      justify-content: center;
      flex-shrink: 0;
    }

    .icon-wrap ::ng-deep svg {
      width: 100%;
      height: 100%;
      display: block;
    }
  `]
})
export class IconComponent {
  @Input() size = 18;
  @Input() strokeWidth = 1.75;

  private _name: IconName = 'grid';
  svg: SafeHtml = '';

  constructor(private sanitizer: DomSanitizer) {}

  @Input()
  set name(value: IconName) {
    this._name = value;
    const paths = PATHS[value] ?? PATHS['grid'];

    this.svg = this.sanitizer.bypassSecurityTrustHtml(
      `<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="${this.strokeWidth}" ` +
      `stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">${paths}</svg>`
    );
  }

  get name(): IconName {
    return this._name;
  }
}
