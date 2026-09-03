// Wire types for the diet subdomain. Enums cross the wire as names, not ordinals.
// Weights are kilograms and heights centimetres - display conversion is the client's job.

export type GoalType = 'LoseWeight' | 'Maintain' | 'GainWeight' | 'EatConsistently';

export type ActivityLevel =
  | 'Sedentary'
  | 'LightlyActive'
  | 'ModeratelyActive'
  | 'VeryActive'
  | 'ExtraActive';

export type BiologicalSex = 'Female' | 'Male';

export type TargetSource = 'Suggested' | 'MemberSet';

export type MealType = 'Breakfast' | 'Lunch' | 'Dinner' | 'Snack';

/** Exactly three. A day outside the plan is flagged with `withinPlan`, not a fourth state. */
export type DayState = 'NotLogged' | 'OnTarget' | 'OverTarget';

export type FoodCategory =
  | 'Staple'
  | 'Protein'
  | 'Dairy'
  | 'Fruit'
  | 'Vegetable'
  | 'PreparedMeal'
  | 'Snack'
  | 'Drink';

export type TipCategory = 'Craving' | 'Planning' | 'PortionControl' | 'EatingOut' | 'Mindset';

export type AchievementCriterion = 'ConsecutiveOnTargetDays' | 'TotalDaysLogged' | 'DaysOnPlan';

export interface NutritionValues {
  calories: number;
  proteinG: number;
  carbsG: number;
  fatG: number;
}

export interface NutritionTargets {
  calories: number;
  proteinG: number | null;
  carbsG: number | null;
  fatG: number | null;
}

export interface BodyMetrics {
  heightCm: number;
  age: number;
  sex: BiologicalSex;
}

export interface DietPlan {
  id: string;
  goal: GoalType;
  startDate: string;
  bodyMetrics: BodyMetrics;
  activityLevel: ActivityLevel;
  targets: NutritionTargets;
  targetSource: TargetSource;
  targetWeightKg: number | null;
  currentWeightKg: number;
  createdAt: string;
  updatedAt: string;
}

/** Every intermediate is returned so the UI can explain the number rather than assert it. */
export interface TargetSuggestion {
  suggestedTargets: NutritionTargets;
  restingEnergyKcal: number;
  activityAdjustedKcal: number;
  goalAdjustmentKcal: number;
  wasClampedToFloor: boolean;
  floorKcal: number;
  disclaimer: string;
}

export interface SuggestTargetsRequest {
  goal: GoalType;
  bodyMetrics: BodyMetrics;
  currentWeightKg: number;
  activityLevel: ActivityLevel;
}

export interface CreateDietPlanRequest extends SuggestTargetsRequest {
  startDate: string;
  targetWeightKg?: number | null;
  targets: NutritionTargets;
  targetSource: TargetSource;
}

export interface UpdateDietPlanRequest {
  goal: GoalType;
  startDate: string;
  bodyMetrics: BodyMetrics;
  activityLevel: ActivityLevel;
  targetWeightKg?: number | null;
}

export interface SetTargetsRequest {
  targets: NutritionTargets;
  targetSource: TargetSource;
}

/** A successful save that still carries a warning - the override is allowed, not blocked. */
export interface DietPlanResponse {
  plan: DietPlan;
  belowSafeFloorWarning: string | null;
}

export interface UpdateDietPlanResponse {
  plan: DietPlan;
  refreshedSuggestion: TargetSuggestion;
  targetsUnchanged: boolean;
}

export interface FoodEntry {
  id: string;
  mealType: MealType;
  foodName: string;
  servingLabel: string;
  quantity: number;
  nutrition: NutritionValues;
  foodLibraryItemId: string;
  servingSizeId: string;
  loggedAt: string;
}

export interface LoggedDay {
  date: string;
  state: DayState;
  /** Concurrency token. Echo it back on every write or the server answers 409. */
  version: string;
  targets: NutritionTargets;
  totals: NutritionValues;
  remainingCalories: number;
  overageCalories: number;
  entries: FoodEntry[];
}

export interface DaySummary {
  date: string;
  withinPlan: boolean;
  state?: DayState;
  consumedCalories?: number;
  targetCalories?: number;
}

export interface DayRange {
  from: string;
  to: string;
  planStartDate: string;
  days: DaySummary[];
}

export interface AddFoodEntryRequest {
  foodLibraryItemId: string;
  servingSizeId: string;
  quantity: number;
  mealType: MealType;
  version?: string | null;
}

export interface UpdateFoodEntryRequest {
  servingSizeId: string;
  quantity: number;
  mealType: MealType;
  version: string;
}

export interface ServingSize {
  id: string;
  label: string;
  gramWeight: number;
  nutrition: NutritionValues;
}

export interface FoodLibraryItem {
  id: string;
  name: string;
  category: FoodCategory;
  servingSizes: ServingSize[];
}

export interface FoodSearchResult {
  query: string;
  matches: FoodLibraryItem[];
}

export interface WeightReading {
  date: string;
  weightKg: number;
}

export interface WeightTrend {
  readings: WeightReading[];
  startWeightKg: number | null;
  currentWeightKg: number | null;
  changeKg: number | null;
  targetWeightKg: number | null;
  remainingToTargetKg: number | null;
  goalReached: boolean;
}

export interface DietStatistics {
  currentStreakDays: number;
  longestStreakDays: number;
  totalDaysLogged: number;
  averageDailyCalories: number;
  averageWindowDays: number;
  planStartDate: string;
  daysOnPlan: number;
}

export interface DietAchievement {
  id: string;
  name: string;
  description: string;
  icon: string;
  criterion: AchievementCriterion;
  threshold: number;
  unlocked: boolean;
  earnedOn: string | null;
  remaining: number;
}

export interface DietAchievementList {
  achievements: DietAchievement[];
}

export interface NewlyUnlocked {
  newlyUnlocked: DietAchievement[];
}

export interface EatingTip {
  id: string;
  title: string;
  description: string;
  icon: string;
  category: TipCategory;
}

export interface DailyEncouragement {
  message: string;
  currentStreakDays: number;
  tone: string;
}

// --- Exercise logging -------------------------------------------------------
// Deliberately separate from the eating types above. No shape here combines an estimate with a
// calorie target, and no food-log type gained an exercise field: a day's eating verdict cannot
// move because activity was recorded (FR-013, FR-016).

export type ActivityCategory =
  | 'Walking'
  | 'Running'
  | 'Cycling'
  | 'Swimming'
  | 'Gym'
  | 'Sport'
  | 'HomeAndGarden'
  | 'Everyday';

export interface ActivityTypeSummary {
  id: string;
  name: string;
  category: ActivityCategory;
  met: number;
}

/** An empty `matches` is how a member learns an activity is not in the catalogue. */
export interface ActivitySearchResponse {
  query: string;
  matches: ActivityTypeSummary[];
}

export interface ExerciseEntry {
  id: string;
  activityTypeId: string;
  activityName: string;
  met: number;
  durationMinutes: number;
  /** An estimate, and labelled as one wherever it is shown. Never a spendable allowance. */
  estimatedKcal: number;
  recordedAt: string;
}

export interface ExerciseDay {
  date: string;
  /** Null when no exercise day exists yet; echoed back on every write to an existing day. */
  version: string | null;
  totalMinutes: number;
  totalKilocalories: number;
  entries: ExerciseEntry[];
}

/** One row per day that has activity. Absence means no exercise, not an unknown state. */
export interface ExerciseDaySummary {
  date: string;
  totalMinutes: number;
  totalKilocalories: number;
  entryCount: number;
}

export interface ExerciseRangeResponse {
  from: string;
  to: string;
  days: ExerciseDaySummary[];
}

export interface ActivitySummary {
  windowDays: number;
  activeDays: number;
  totalMinutes: number;
  totalKilocalories: number;
  previousWindowActiveDays: number;
  previousWindowMinutes: number;
}

export interface AddExerciseEntryRequest {
  activityTypeId: string;
  durationMinutes: number;
  version: string | null;
}

export interface UpdateExerciseEntryRequest {
  activityTypeId: string;
  durationMinutes: number;
  version: string;
}
