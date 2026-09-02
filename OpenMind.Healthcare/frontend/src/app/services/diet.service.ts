import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import {
  AddFoodEntryRequest,
  CreateDietPlanRequest,
  DailyEncouragement,
  DayRange,
  DietAchievementList,
  DietPlan,
  DietPlanResponse,
  DietStatistics,
  EatingTip,
  FoodSearchResult,
  LoggedDay,
  NewlyUnlocked,
  SetTargetsRequest,
  SuggestTargetsRequest,
  TargetSuggestion,
  UpdateDietPlanRequest,
  UpdateDietPlanResponse,
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
