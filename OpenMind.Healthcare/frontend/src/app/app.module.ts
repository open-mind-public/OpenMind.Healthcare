import { NgModule } from '@angular/core';
import { BrowserModule } from '@angular/platform-browser';
import { BrowserAnimationsModule } from '@angular/platform-browser/animations';
import { HttpClientModule, HTTP_INTERCEPTORS } from '@angular/common/http';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { RouterModule, Routes } from '@angular/router';

import { AppComponent } from './app.component';
import { DashboardComponent } from './components/dashboard/dashboard.component';
import { SetupComponent } from './components/setup/setup.component';
import { AchievementsComponent } from './components/achievements/achievements.component';
import { HealthMilestonesComponent } from './components/health-milestones/health-milestones.component';
import { MotivationComponent } from './components/motivation/motivation.component';
import { CravingHelpComponent } from './components/craving-help/craving-help.component';
import { StatsCardComponent } from './components/stats-card/stats-card.component';
import { NavbarComponent } from './components/navbar/navbar.component';
import { ProgressCalendarComponent } from './components/progress-calendar/progress-calendar.component';
import { RelapseAnalyticsComponent } from './components/relapse-analytics/relapse-analytics.component';
import { AccountComponent } from './components/account/account.component';
import { DietSetupComponent } from './components/diet-setup/diet-setup.component';
import { DietDashboardComponent } from './components/diet-dashboard/diet-dashboard.component';
import { ActivitySummaryComponent } from './components/activity-summary/activity-summary.component';
import { DietAnalyticsComponent } from './components/diet-analytics/diet-analytics.component';
import { ExerciseLogComponent } from './components/exercise-log/exercise-log.component';
import { FoodSearchComponent } from './components/food-search/food-search.component';
import { DietCalendarComponent } from './components/diet-calendar/diet-calendar.component';
import { WeightTrackerComponent } from './components/weight-tracker/weight-tracker.component';
import { DietAchievementsComponent } from './components/diet-achievements/diet-achievements.component';
import { DietGuidanceComponent } from './components/diet-guidance/diet-guidance.component';
import { HomeComponent } from './components/home/home.component';
import { IconComponent } from './components/icon/icon.component';
import { SidebarComponent } from './components/sidebar/sidebar.component';
import { LoginComponent } from './components/auth/login/login.component';
import { RegisterComponent } from './components/auth/register/register.component';
import { AuthGuard } from './guards/auth.guard';
import { AuthInterceptor } from './interceptors/auth.interceptor';

const routes: Routes = [
  { path: '', redirectTo: '/home', pathMatch: 'full' },

  // Platform-level
  { path: 'login', component: LoginComponent },
  { path: 'register', component: RegisterComponent },
  { path: 'home', component: HomeComponent, canActivate: [AuthGuard] },
  { path: 'account', component: AccountComponent, canActivate: [AuthGuard] },

  // Programme: Quit Smoking
  { path: 'quit-smoking', redirectTo: '/quit-smoking/dashboard', pathMatch: 'full' },
  { path: 'quit-smoking/dashboard', component: DashboardComponent, canActivate: [AuthGuard] },
  { path: 'quit-smoking/setup', component: SetupComponent, canActivate: [AuthGuard] },
  { path: 'quit-smoking/calendar', component: ProgressCalendarComponent, canActivate: [AuthGuard] },
  { path: 'quit-smoking/analytics', component: RelapseAnalyticsComponent, canActivate: [AuthGuard] },
  { path: 'quit-smoking/health', component: HealthMilestonesComponent, canActivate: [AuthGuard] },
  { path: 'quit-smoking/achievements', component: AchievementsComponent, canActivate: [AuthGuard] },
  { path: 'quit-smoking/motivation', component: MotivationComponent, canActivate: [AuthGuard] },
  { path: 'quit-smoking/craving-help', component: CravingHelpComponent, canActivate: [AuthGuard] },

  // Programme: Diet
  { path: 'diet', redirectTo: '/diet/today', pathMatch: 'full' },
  { path: 'diet/today', component: DietDashboardComponent, canActivate: [AuthGuard] },
  { path: 'diet/setup', component: DietSetupComponent, canActivate: [AuthGuard] },
  { path: 'diet/log/:date', component: DietDashboardComponent, canActivate: [AuthGuard] },
  { path: 'diet/calendar', component: DietCalendarComponent, canActivate: [AuthGuard] },
  { path: 'diet/weight', component: WeightTrackerComponent, canActivate: [AuthGuard] },
  { path: 'diet/activity', component: ActivitySummaryComponent, canActivate: [AuthGuard] },
  { path: 'diet/analytics', component: DietAnalyticsComponent, canActivate: [AuthGuard] },
  { path: 'diet/achievements', component: DietAchievementsComponent, canActivate: [AuthGuard] },
  { path: 'diet/guidance', component: DietGuidanceComponent, canActivate: [AuthGuard] },

  // Paths from before programmes were namespaced. Kept so existing links and bookmarks survive.
  { path: 'dashboard', redirectTo: '/quit-smoking/dashboard' },
  { path: 'setup', redirectTo: '/quit-smoking/setup' },
  { path: 'calendar', redirectTo: '/quit-smoking/calendar' },
  { path: 'analytics', redirectTo: '/quit-smoking/analytics' },
  { path: 'health', redirectTo: '/quit-smoking/health' },
  { path: 'achievements', redirectTo: '/quit-smoking/achievements' },
  { path: 'motivation', redirectTo: '/quit-smoking/motivation' },
  { path: 'craving-help', redirectTo: '/quit-smoking/craving-help' },

  { path: '**', redirectTo: '/home' }
];

@NgModule({
  declarations: [
    AppComponent,
    DashboardComponent,
    SetupComponent,
    AchievementsComponent,
    HealthMilestonesComponent,
    MotivationComponent,
    CravingHelpComponent,
    StatsCardComponent,
    NavbarComponent,
    ProgressCalendarComponent,
    RelapseAnalyticsComponent,
    AccountComponent,
    LoginComponent,
    RegisterComponent,
    DietSetupComponent,
    DietDashboardComponent,
    ActivitySummaryComponent,
    DietAnalyticsComponent,
    ExerciseLogComponent,
    FoodSearchComponent,
    DietCalendarComponent,
    WeightTrackerComponent,
    DietAchievementsComponent,
    DietGuidanceComponent,
    HomeComponent,
    IconComponent,
    SidebarComponent
  ],
  imports: [
    BrowserModule,
    BrowserAnimationsModule,
    HttpClientModule,
    FormsModule,
    ReactiveFormsModule,
    RouterModule.forRoot(routes)
  ],
  providers: [
    {
      provide: HTTP_INTERCEPTORS,
      useClass: AuthInterceptor,
      multi: true
    }
  ],
  bootstrap: [AppComponent]
})
export class AppModule { }
