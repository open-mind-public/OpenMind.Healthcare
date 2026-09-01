export interface UserProgress {
  id: number;
  quitDate: string;
  cigarettesPerDay: number;
  pricePerPack: number;
  cigarettesPerPack: number;
  currency: string;
  createdAt: string;
  updatedAt: string;
}

export interface MoneySaved {
  amount: number;
  currency: string;
  symbol: string;
}

export interface ProgressStats {
  daysSmokeFree: number;
  hoursSmokeFree: number;
  minutesSmokeFree: number;
  cigarettesNotSmoked: number;
  moneySaved: MoneySaved | number;
  lifeRegainedMinutes: number;
  lifeRegainedFormatted: string;
  progressPercentage: number;
  currentMilestone: string;
  nextMilestone: string;
  daysToNextMilestone: number;
  totalDaysInJourney: number;
  smokedDays: number;
  cigarettesSmoked: number;
  moneySpentOnRelapses: number;
  currentStreak: number;
  longestStreak: number;
  smokeFreeRate: number;
}

// Smoked ("failed") days - days the user marked as a relapse
export type RelapseTrigger =
  | 'Unspecified'
  | 'Stress'
  | 'Social'
  | 'Alcohol'
  | 'Boredom'
  | 'AfterMeal'
  | 'Coffee'
  | 'Emotional'
  | 'WorkPressure'
  | 'Habit'
  | 'Other';

export interface TriggerOption {
  value: RelapseTrigger;
  label: string;
  icon: string;
}

export const RELAPSE_TRIGGERS: TriggerOption[] = [
  { value: 'Unspecified', label: 'Not sure', icon: '❓' },
  { value: 'Stress', label: 'Stress', icon: '😰' },
  { value: 'Social', label: 'Social', icon: '👥' },
  { value: 'Alcohol', label: 'Alcohol', icon: '🍻' },
  { value: 'Boredom', label: 'Boredom', icon: '🥱' },
  { value: 'AfterMeal', label: 'After a meal', icon: '🍽️' },
  { value: 'Coffee', label: 'Coffee', icon: '☕' },
  { value: 'Emotional', label: 'Emotional', icon: '😢' },
  { value: 'WorkPressure', label: 'Work pressure', icon: '💼' },
  { value: 'Habit', label: 'Habit', icon: '🔄' },
  { value: 'Other', label: 'Other', icon: '📌' }
];

export interface SmokedDay {
  id: string;
  date: string;
  cigarettesSmoked: number;
  trigger: RelapseTrigger;
  note?: string | null;
  moneySpent: number;
  currency: string;
  recordedAt: string;
}

export interface MarkSmokedDayRequest {
  date: string;
  cigarettesSmoked: number;
  trigger: RelapseTrigger;
  note?: string | null;
}

export interface TriggerStat {
  trigger: RelapseTrigger;
  days: number;
  cigarettes: number;
  sharePercentage: number;
}

export interface WeekdayStat {
  weekday: string;
  smokedDays: number;
  totalDays: number;
  relapseRate: number;
}

export interface MonthlyStat {
  year: number;
  month: number;
  label: string;
  smokedDays: number;
  smokeFreeDays: number;
  totalDays: number;
  cigarettes: number;
  smokeFreeRate: number;
}

export type RelapseTrend = 'NotEnoughData' | 'Improving' | 'Stable' | 'Worsening';

export interface RelapseAnalytics {
  totalDaysInJourney: number;
  smokeFreeDays: number;
  smokedDays: number;
  smokeFreeRate: number;
  relapseRate: number;
  totalCigarettesSmoked: number;
  moneySpentOnRelapses: number;
  moneySaved: number;
  currency: string;
  lifeLostMinutes: number;
  lifeLostFormatted: string;
  currentStreak: number;
  longestStreak: number;
  lastRelapseDate?: string | null;
  firstRelapseDate?: string | null;
  daysSinceLastRelapse: number;
  averageCigarettesPerRelapseDay: number;
  averageDaysBetweenRelapses: number;
  relapsesLast30Days: number;
  relapsesPrevious30Days: number;
  trend: RelapseTrend;
  mostCommonTrigger?: RelapseTrigger | null;
  riskiestWeekday?: string | null;
  triggerBreakdown: TriggerStat[];
  weekdayBreakdown: WeekdayStat[];
  monthlyBreakdown: MonthlyStat[];
}

export interface Achievement {
  id: number;
  name: string;
  description: string;
  icon: string;
  requiredDays: number;
  category: string;
  isUnlocked: boolean;
  unlockedAt?: string;
}

export interface HealthMilestone {
  id: number;
  title: string;
  description: string;
  timeInMinutes: number;
  timeDisplay: string;
  icon: string;
  category: string;
  isAchieved: boolean;
  progressPercentage: number;
}

export interface MotivationalQuote {
  id: number;
  quote: string;
  author: string;
  category: string;
}

export interface CravingTip {
  id: number;
  title: string;
  description: string;
  icon: string;
  category: string;
}

export interface DailyEncouragement {
  message: string;
  quote: MotivationalQuote;
  tips: CravingTip[];
  specialMessage: string;
}

// Authentication Models
export interface LoginRequest {
  email: string;
  password: string;
}

export interface RegisterRequest {
  email: string;
  password: string;
  firstName: string;
  lastName: string;
}

export interface AuthResponse {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  accessToken: string;
  refreshToken: string;
}

export interface RefreshTokenRequest {
  refreshToken: string;
}

export interface RevokeTokenRequest {
  refreshToken: string;
}

export interface User {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  createdAt: string;
  lastLoginAt: string;
}
