import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { ApiService } from '../../services/api.service';
import { DietService } from '../../services/diet.service';
import { HealthProgram, PROGRAMS } from '../../programs/programs';

/**
 * A short line of live status per programme, so a card says something true about the member
 * rather than repeating its own tagline.
 */
interface ProgramStatus {
  line: string;
  enrolled: boolean;
}

/**
 * The hub: every programme this member can use, as peers.
 *
 * This is the page that makes the platform legible. Without it, whichever programme you happened
 * to open would look like the whole application - which is exactly how diet ended up buried in
 * the smoking navigation.
 */
@Component({
  selector: 'app-home',
  standalone: false,
  templateUrl: './home.component.html',
  styleUrls: ['./home.component.css']
})
export class HomeComponent implements OnInit {
  readonly programs = PROGRAMS;

  /** Keyed by programme id. Absent while still loading. */
  status: Record<string, ProgramStatus> = {};

  constructor(
    private router: Router,
    private apiService: ApiService,
    private dietService: DietService
  ) {}

  ngOnInit(): void {
    this.loadQuitSmokingStatus();
    this.loadDietStatus();
  }

  enter(program: HealthProgram): void {
    this.router.navigate([program.home]);
  }

  greeting(): string {
    const hour = new Date().getHours();
    if (hour < 12) return 'Good morning';
    if (hour < 18) return 'Good afternoon';
    return 'Good evening';
  }

  private loadQuitSmokingStatus(): void {
    this.apiService.getStats().subscribe({
      next: stats => {
        const days = stats?.daysSmokeFree ?? 0;
        this.status['quit-smoking'] = {
          enrolled: true,
          line: days === 1 ? '1 day smoke free' : `${days} days smoke free`
        };
      },
      // No journey yet is not an error - it is an invitation.
      error: () => (this.status['quit-smoking'] = { enrolled: false, line: 'Not started yet' })
    });
  }

  private loadDietStatus(): void {
    this.dietService.getStats().subscribe({
      next: stats => {
        const streak = stats?.currentStreakDays ?? 0;
        this.status['diet'] = {
          enrolled: true,
          line: streak > 0
            ? `${streak} ${streak === 1 ? 'day' : 'days'} on target`
            : `${stats?.totalDaysLogged ?? 0} days logged`
        };
      },
      error: () => (this.status['diet'] = { enrolled: false, line: 'Not started yet' })
    });
  }
}
