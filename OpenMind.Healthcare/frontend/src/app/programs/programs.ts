/**
 * The registry of health programmes this platform offers.
 *
 * OpenMind Health is a wide healthcare system: quitting smoking is one programme, diet is
 * another, and more will follow. Each is a separate bounded context on the backend with its own
 * service and database, and the front end mirrors that — a programme owns its own navigation and
 * knows nothing about its siblings.
 *
 * Adding a programme is an entry in this file plus its routes. It must never mean editing another
 * programme's menu.
 */

import { IconName } from '../components/icon/icon.component';

export interface ProgramNavItem {
  /** Path relative to the programme's basePath, or '' for its landing page. */
  path: string;
  label: string;
  icon: IconName;
}

export interface HealthProgram {
  id: string;
  /** The programme's own name. The platform is branded separately, above it. */
  name: string;
  tagline: string;
  icon: IconName;
  /** Accent colour for this programme's cards and active states. */
  accent: string;
  /** Every route in the programme lives under this prefix. */
  basePath: string;
  /** Where entering the programme lands. */
  home: string;
  nav: ProgramNavItem[];
  /** Shown in the programme's settings menu, under its own base path. */
  settings?: ProgramNavItem[];
}

export const PROGRAMS: HealthProgram[] = [
  {
    id: 'quit-smoking',
    name: 'Quit Smoking',
    tagline: 'Track smoke-free days, savings and health milestones',
    icon: 'heart',
    accent: '#2563eb',
    basePath: '/quit-smoking',
    home: '/quit-smoking/dashboard',
    nav: [
      { path: 'dashboard', label: 'Dashboard', icon: 'chart' },
      { path: 'calendar', label: 'Calendar', icon: 'calendar' },
      { path: 'analytics', label: 'Analytics', icon: 'trending' },
      { path: 'health', label: 'Health', icon: 'heart' },
      { path: 'achievements', label: 'Achievements', icon: 'award' },
      { path: 'motivation', label: 'Motivation', icon: 'zap' },
      { path: 'craving-help', label: 'Craving Help', icon: 'lifebuoy' }
    ],
    settings: [{ path: 'setup', label: 'Quit date & habits', icon: 'target' }]
  },
  {
    id: 'diet',
    name: 'Diet',
    tagline: 'Log what you eat against a daily target that fits you',
    icon: 'utensils',
    accent: '#0d9488',
    basePath: '/diet',
    home: '/diet/today',
    nav: [
      { path: 'today', label: 'Today', icon: 'utensils' },
      { path: 'calendar', label: 'History', icon: 'calendar' },
      { path: 'weight', label: 'Weight', icon: 'scale' },
      { path: 'activity', label: 'Activity', icon: 'zap' },
      { path: 'achievements', label: 'Achievements', icon: 'award' },
      { path: 'guidance', label: 'Guidance', icon: 'lightbulb' }
    ],
    settings: [{ path: 'setup', label: 'Diet plan & targets', icon: 'target' }]
  }
];

/** The programme a URL belongs to, or null for platform-level pages (hub, account, auth). */
export function programForUrl(url: string): HealthProgram | null {
  const path = url.split('?')[0].split('#')[0];
  return PROGRAMS.find(p => path === p.basePath || path.startsWith(`${p.basePath}/`)) ?? null;
}

export function absolutePath(program: HealthProgram, item: ProgramNavItem): string {
  return item.path ? `${program.basePath}/${item.path}` : program.basePath;
}
