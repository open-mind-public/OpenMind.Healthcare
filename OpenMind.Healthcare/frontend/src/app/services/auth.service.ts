import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { BehaviorSubject, Observable, throwError } from 'rxjs';
import { tap, catchError } from 'rxjs/operators';
import {
  AuthResponse,
  User,
  LoginRequest,
  RegisterRequest,
  UpdateProfileRequest,
  ChangePasswordRequest
} from '../models/models';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private readonly ACCESS_TOKEN_KEY = 'access_token';
  private readonly REFRESH_TOKEN_KEY = 'refresh_token';
  private readonly CURRENT_USER_KEY = 'currentUser';
  private readonly userApiUrl = '/user-api';

  private currentUserSubject = new BehaviorSubject<User | null>(null);
  public currentUser$ = this.currentUserSubject.asObservable();

  private isRefreshing = false;
  private refreshTokenSubject = new BehaviorSubject<string | null>(null);

  constructor(private http: HttpClient) {
    this.loadStoredUser();
  }

  private loadStoredUser(): void {
    const savedUser = localStorage.getItem(this.CURRENT_USER_KEY);
    if (savedUser) {
      this.currentUserSubject.next(JSON.parse(savedUser));
    }
  }

  login(credentials: LoginRequest): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.userApiUrl}/auth/login`, credentials).pipe(
      tap(response => this.handleAuthResponse(response))
    );
  }

  register(userData: RegisterRequest): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.userApiUrl}/auth/register`, userData).pipe(
      tap(response => this.handleAuthResponse(response))
    );
  }

  /** Loads the signed-in user fresh from the API (includes createdAt / lastLoginAt). */
  loadCurrentUser(): Observable<User> {
    return this.http.get<User>(`${this.userApiUrl}/auth/me`).pipe(
      tap(user => this.storeUser(user))
    );
  }

  updateProfile(request: UpdateProfileRequest): Observable<User> {
    return this.http.put<User>(`${this.userApiUrl}/auth/profile`, request).pipe(
      tap(user => this.storeUser(user))
    );
  }

  changePassword(request: ChangePasswordRequest): Observable<{ message: string }> {
    return this.http.post<{ message: string }>(`${this.userApiUrl}/auth/change-password`, request);
  }

  /** Keeps the cached user and every subscriber in step with the API. */
  private storeUser(user: User): void {
    const merged = { ...(this.currentUserSubject.value ?? {}), ...user } as User;
    localStorage.setItem(this.CURRENT_USER_KEY, JSON.stringify(merged));
    this.currentUserSubject.next(merged);
  }

  refreshToken(): Observable<AuthResponse> {
    const refreshToken = this.getRefreshToken();
    
    if (!refreshToken) {
      return throwError(() => new Error('No refresh token available'));
    }

    return this.http.post<AuthResponse>(`${this.userApiUrl}/auth/refresh`, { refreshToken }).pipe(
      tap(response => this.handleAuthResponse(response)),
      catchError(error => {
        this.logout();
        return throwError(() => error);
      })
    );
  }

  logout(): void {
    const refreshToken = this.getRefreshToken();
    
    // Attempt to revoke token on server (fire and forget)
    if (refreshToken) {
      this.http.post(`${this.userApiUrl}/auth/revoke`, { refreshToken }).subscribe({
        error: () => {} // Ignore errors on revoke
      });
    }

    this.clearTokens();
    this.currentUserSubject.next(null);
  }

  private handleAuthResponse(response: AuthResponse): void {
    if (response.accessToken && response.refreshToken) {
      this.setAccessToken(response.accessToken);
      this.setRefreshToken(response.refreshToken);
      
      const user: User = {
        id: response.id,
        email: response.email,
        firstName: response.firstName,
        lastName: response.lastName,
        createdAt: '',
        lastLoginAt: ''
      };
      
      localStorage.setItem(this.CURRENT_USER_KEY, JSON.stringify(user));
      this.currentUserSubject.next(user);
    }
  }

  getAccessToken(): string | null {
    return localStorage.getItem(this.ACCESS_TOKEN_KEY);
  }

  setAccessToken(token: string): void {
    localStorage.setItem(this.ACCESS_TOKEN_KEY, token);
  }

  getRefreshToken(): string | null {
    return localStorage.getItem(this.REFRESH_TOKEN_KEY);
  }

  setRefreshToken(token: string): void {
    localStorage.setItem(this.REFRESH_TOKEN_KEY, token);
  }

  clearTokens(): void {
    localStorage.removeItem(this.ACCESS_TOKEN_KEY);
    localStorage.removeItem(this.REFRESH_TOKEN_KEY);
    localStorage.removeItem(this.CURRENT_USER_KEY);
  }

  get isLoggedIn(): boolean {
    return !!this.getAccessToken();
  }

  get currentUser(): User | null {
    return this.currentUserSubject.value;
  }

  // Token refresh state management for interceptor
  get isRefreshingToken(): boolean {
    return this.isRefreshing;
  }

  set isRefreshingToken(value: boolean) {
    this.isRefreshing = value;
  }

  get refreshTokenSubject$(): BehaviorSubject<string | null> {
    return this.refreshTokenSubject;
  }
}
