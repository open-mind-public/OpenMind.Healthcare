import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import {
  ActivitySearchResponse,
  AddEntryFromShortcutRequest,
  ActivitySummary,
  AddExerciseEntryRequest,
  AddFoodEntryRequest,
  BeerDayRange,
  CreateDietPlanRequest,
  DailyEncouragement,
  DayRange,
  DietAchievementList,
  DietPlan,
  DietPlanResponse,
  DietStatistics,
  EatingPatterns,
  EatingTip,
  HabitInsights,
  CreateShortcutRequest,
  ExerciseDay,
  ExerciseRangeResponse,
  ExerciseShortcutList,
  FoodSearchResult,
  IntakeAnalysis,
  IntakeTrend,
  MacroAnalysis,
  LoggedDay,
  NewlyUnlocked,
  Observations,
  PeriodPreset,
  SetTargetsRequest,
  SuggestTargetsRequest,
  TargetSuggestion,
  UpdateDietPlanRequest,
  UpdateDietPlanResponse,
  UpdateExerciseEntryRequest,
  UpdateFoodEntryRequest,
  WeightTrend
} from '../models/diet.models';

/**
 * The diet subdomain's API. Requests go to `/diet-api`, which the dev proxy and nginx both
 * rewrite to `/api` on DietApi - adding one without the other works in dev and breaks in Docker.
 * The auth interceptor attaches the bearer token, so nothing here handles credentials.
 */
@Injectable({
  providedIn: 'root'
})
export class DietService {
  private baseUrl = '/diet-api';

  constructor(private http: HttpClient) {}

  // --- Plan -------------------------------------------------------------

  getPlan(): Observable<DietPlan> {
    return this.http.get<DietPlan>(`${this.baseUrl}/diet-plan`);
  }

  suggestTargets(request: SuggestTargetsRequest): Observable<TargetSuggestion> {
    return this.http.post<TargetSuggestion>(`${this.baseUrl}/diet-plan/target-suggestion`, request);
  }

  createPlan(request: CreateDietPlanRequest): Observable<DietPlanResponse> {
    return this.http.post<DietPlanResponse>(`${this.baseUrl}/diet-plan`, request);
  }

  updatePlan(request: UpdateDietPlanRequest): Observable<UpdateDietPlanResponse> {
    return this.http.put<UpdateDietPlanResponse>(`${this.baseUrl}/diet-plan`, request);
  }

  setTargets(request: SetTargetsRequest): Observable<DietPlanResponse> {
    return this.http.put<DietPlanResponse>(`${this.baseUrl}/diet-plan/targets`, request);
  }

  // --- Food log ---------------------------------------------------------

  getDay(date: string): Observable<LoggedDay> {
    return this.http.get<LoggedDay>(`${this.baseUrl}/food-log/${date}`);
  }

  getDayRange(from: string, to: string): Observable<DayRange> {
    const params = new HttpParams().set('from', from).set('to', to);
    return this.http.get<DayRange>(`${this.baseUrl}/food-log`, { params });
  }

  addEntry(date: string, request: AddFoodEntryRequest): Observable<LoggedDay> {
    return this.http.post<LoggedDay>(`${this.baseUrl}/food-log/${date}/entries`, request);
  }

  updateEntry(entryId: string, request: UpdateFoodEntryRequest): Observable<LoggedDay> {
    return this.http.put<LoggedDay>(`${this.baseUrl}/food-log/entries/${entryId}`, request);
  }

  deleteEntry(entryId: string, version: string): Observable<LoggedDay | null> {
    const params = new HttpParams().set('version', version);
    return this.http.delete<LoggedDay | null>(`${this.baseUrl}/food-log/entries/${entryId}`, { params });
  }

  // --- Food library -----------------------------------------------------

  searchFoods(query: string, limit = 20): Observable<FoodSearchResult> {
    const params = new HttpParams().set('q', query).set('limit', limit);
    return this.http.get<FoodSearchResult>(`${this.baseUrl}/food-library/search`, { params });
  }

  // --- Analytics --------------------------------------------------------
  // Read-only. Four calls rather than one composite response, so each section renders as it
  // arrives and each user story is independently testable.

  getIntakeAnalysis(period: PeriodPreset): Observable<IntakeAnalysis> {
    const params = new HttpParams().set('period', period);
    return this.http.get<IntakeAnalysis>(`${this.baseUrl}/diet-analytics/intake`, { params });
  }

  getIntakeTrend(period: PeriodPreset): Observable<IntakeTrend> {
    const params = new HttpParams().set('period', period);
    return this.http.get<IntakeTrend>(`${this.baseUrl}/diet-analytics/trend`, { params });
  }

  getMacroAnalysis(period: PeriodPreset): Observable<MacroAnalysis> {
    const params = new HttpParams().set('period', period);
    return this.http.get<MacroAnalysis>(`${this.baseUrl}/diet-analytics/macros`, { params });
  }

  getEatingPatterns(period: PeriodPreset): Observable<EatingPatterns> {
    // The browser reports the offset with the opposite sign to the one the contract expects:
    // getTimezoneOffset() is minutes to ADD to local time to reach UTC.
    const params = new HttpParams()
      .set('period', period)
      .set('utcOffsetMinutes', -new Date().getTimezoneOffset());
    return this.http.get<EatingPatterns>(`${this.baseUrl}/diet-analytics/patterns`, { params });
  }

  getObservations(period: PeriodPreset): Observable<Observations> {
    const params = new HttpParams()
      .set('period', period)
      .set('utcOffsetMinutes', -new Date().getTimezoneOffset());
    return this.http.get<Observations>(`${this.baseUrl}/diet-analytics/observations`, { params });
  }

  /** Beer and exercise frequency for the period, and eating outcomes on beer days vs other days. */
  getHabitInsights(period: PeriodPreset): Observable<HabitInsights> {
    const params = new HttpParams().set('period', period);
    return this.http.get<HabitInsights>(`${this.baseUrl}/diet-analytics/habits`, { params });
  }

  // --- Exercise ---------------------------------------------------------
  // Deliberately separate calls from the food log. The eating endpoints know nothing about
  // exercise; screens that show both fetch both and merge (research.md R-005).

  getExerciseDay(date: string): Observable<ExerciseDay> {
    return this.http.get<ExerciseDay>(`${this.baseUrl}/exercise/${date}`);
  }

  getExerciseRange(from: string, to: string): Observable<ExerciseRangeResponse> {
    const params = new HttpParams().set('from', from).set('to', to);
    return this.http.get<ExerciseRangeResponse>(`${this.baseUrl}/exercise`, { params });
  }

  getActivitySummary(): Observable<ActivitySummary> {
    return this.http.get<ActivitySummary>(`${this.baseUrl}/exercise/summary`);
  }

  addExerciseEntry(date: string, request: AddExerciseEntryRequest): Observable<ExerciseDay> {
    return this.http.post<ExerciseDay>(`${this.baseUrl}/exercise/${date}/entries`, request);
  }

  updateExerciseEntry(entryId: string, request: UpdateExerciseEntryRequest): Observable<ExerciseDay> {
    return this.http.put<ExerciseDay>(`${this.baseUrl}/exercise/entries/${entryId}`, request);
  }

  /** Null once the day's last session goes - the date reverts to no exercise recorded. */
  deleteExerciseEntry(entryId: string, version: string): Observable<ExerciseDay | null> {
    const params = new HttpParams().set('version', version);
    return this.http.delete<ExerciseDay | null>(`${this.baseUrl}/exercise/entries/${entryId}`, { params });
  }

  // --- Beer days ------------------------------------------------------------
  // A third independent calendar range, merged client-side alongside eating and exercise. The
  // eating endpoints know nothing about beer (005 research.md R-003).

  getBeerRange(from: string, to: string): Observable<BeerDayRange> {
    const params = new HttpParams().set('from', from).set('to', to);
    return this.http.get<BeerDayRange>(`${this.baseUrl}/beer-days`, { params });
  }

  markBeerDay(date: string): Observable<void> {
    return this.http.put<void>(`${this.baseUrl}/beer-days/${date}`, {});
  }

  unmarkBeerDay(date: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/beer-days/${date}`);
  }

  // --- Exercise shortcuts -----------------------------------------------
  // Every one of these answers with the whole list, so the client updates in one round trip
  // rather than reconciling a patch.

  getExerciseShortcuts(): Observable<ExerciseShortcutList> {
    return this.http.get<ExerciseShortcutList>(`${this.baseUrl}/exercise-shortcuts`);
  }

  createExerciseShortcut(request: CreateShortcutRequest): Observable<ExerciseShortcutList> {
    return this.http.post<ExerciseShortcutList>(`${this.baseUrl}/exercise-shortcuts`, request);
  }

  renameExerciseShortcut(id: string, name: string): Observable<ExerciseShortcutList> {
    return this.http.put<ExerciseShortcutList>(`${this.baseUrl}/exercise-shortcuts/${id}`, { name });
  }

  /** Sends the complete ordered list, not a move — idempotent and race-free. */
  reorderExerciseShortcuts(orderedIds: string[]): Observable<ExerciseShortcutList> {
    return this.http.put<ExerciseShortcutList>(`${this.baseUrl}/exercise-shortcuts/order`, { orderedIds });
  }

  deleteExerciseShortcut(id: string): Observable<ExerciseShortcutList> {
    return this.http.delete<ExerciseShortcutList>(`${this.baseUrl}/exercise-shortcuts/${id}`);
  }

  addExerciseEntryFromShortcut(
    date: string, request: AddEntryFromShortcutRequest): Observable<ExerciseDay> {
    return this.http.post<ExerciseDay>(`${this.baseUrl}/exercise/${date}/entries/from-shortcut`, request);
  }

  searchActivities(query: string, limit = 20): Observable<ActivitySearchResponse> {
    const params = new HttpParams().set('q', query).set('limit', limit);
    return this.http.get<ActivitySearchResponse>(`${this.baseUrl}/activity-catalogue/search`, { params });
  }

  // --- Weight -----------------------------------------------------------

  getWeightTrend(from?: string, to?: string): Observable<WeightTrend> {
    let params = new HttpParams();
    if (from) { params = params.set('from', from); }
    if (to) { params = params.set('to', to); }
    return this.http.get<WeightTrend>(`${this.baseUrl}/weight`, { params });
  }

  recordWeight(date: string, weightKg: number): Observable<WeightTrend> {
    return this.http.put<WeightTrend>(`${this.baseUrl}/weight/${date}`, { weightKg });
  }

  deleteWeightReading(date: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/weight/${date}`);
  }

  // --- Statistics, achievements, guidance -------------------------------

  getStats(): Observable<DietStatistics> {
    return this.http.get<DietStatistics>(`${this.baseUrl}/diet-stats`);
  }

  getAchievements(): Observable<DietAchievementList> {
    return this.http.get<DietAchievementList>(`${this.baseUrl}/diet-achievements`);
  }

  checkAchievements(): Observable<NewlyUnlocked> {
    return this.http.post<NewlyUnlocked>(`${this.baseUrl}/diet-achievements/check`, {});
  }

  getTips(category?: string): Observable<EatingTip[]> {
    let params = new HttpParams();
    if (category) { params = params.set('category', category); }
    return this.http.get<EatingTip[]>(`${this.baseUrl}/diet-guidance/tips`, { params });
  }

  getEncouragement(): Observable<DailyEncouragement> {
    return this.http.get<DailyEncouragement>(`${this.baseUrl}/diet-guidance/encouragement`);
  }
}
