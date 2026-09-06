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

// --- Beer days -------------------------------------------------------------
// A lightweight day-level marker: it records only that beer was drunk on a date - no amount and
// no calories, and it never touches the day's eating verdict (005 FR-004).

/** `days` holds only the dates that are beer days within the plan. Absence means "not a beer day". */
export interface BeerDayRange {
  from: string;
  to: string;
  days: string[];
}

export interface UpdateExerciseEntryRequest {
  activityTypeId: string;
  durationMinutes: number;
  version: string;
}

// --- Diet analytics ---------------------------------------------------------
// Read-only. Every response carries the period it was computed over, and every average carries
// the number of days it divided by — the two travel together so a client cannot show one without
// the other (FR-003).

export type PeriodPreset = 'Week' | 'Month' | 'Quarter' | 'Plan';

/** Which days an average divided by. */
export type AveragedOver = 'LoggedDays' | 'AllDays';

export interface AnalysisPeriod {
  preset: PeriodPreset;
  from: string;
  to: string;
  /** True when the requested window was clipped to the plan start or to today. */
  wasNarrowed: boolean;
  totalDays: number;
  loggedDays: number;
  /** False means there is no preceding window — not that the member did nothing in one. */
  hasComparison: boolean;
  previousFrom: string | null;
  previousTo: string | null;
}

export interface IntakeSummary {
  totalKilocalories: number;
  averageDailyKilocalories: number;
  averagedOverDays: number;
  averagedOver: AveragedOver;
  previousAverageDailyKilocalories: number | null;
  onTargetDays: number;
  overTargetDays: number;
  notLoggedDays: number;
}

export interface MealShare {
  meal: MealType;
  kilocalories: number;
  shareOfTotal: number;
  entryCount: number;
}

export interface CategoryShare {
  category: FoodCategory;
  kilocalories: number;
  shareOfTotal: number;
}

export interface FoodContribution {
  foodLibraryItemId: string;
  foodName: string;
  kilocalories: number;
  shareOfTotal: number;
  timesLogged: number;
}

/** `meals` and `categories` are exhaustive and sum to the total; `topFoods` is a top ten and does not. */
export interface IntakeAnalysis {
  period: AnalysisPeriod;
  summary: IntakeSummary;
  meals: MealShare[];
  topFoods: FoodContribution[];
  categories: CategoryShare[];
}

export interface MacroAmounts {
  proteinG: number;
  carbsG: number;
  fatG: number;
}

export interface MacroShares {
  protein: number;
  carbs: number;
  fat: number;
}

/** `target` is null when the plan carries no macronutrient targets. Do not substitute one. */
export interface MacroAnalysis {
  period: AnalysisPeriod;
  averagedOverDays: number;
  hasTargets: boolean;
  actual: MacroAmounts;
  target: MacroAmounts | null;
  shareOfEnergy: MacroShares;
}

export interface WeekdayShare {
  dayOfWeek: string;
  averageKilocalories: number;
  loggedDays: number;
}

export interface HourShare {
  hour: number;
  kilocalories: number;
  shareOfTotal: number;
}

export interface EatingPatterns {
  period: AnalysisPeriod;
  utcOffsetMinutes: number;
  /** Always true here: the time shown is when an entry was recorded, not when it was eaten. */
  isApproximate: boolean;
  approximationReason: string;
  byWeekday: WeekdayShare[];
  byHour: HourShare[];
}

export type ObservationFamily = 'Timing' | 'Composition' | 'Targets' | 'Consistency';

export interface Observation {
  family: ObservationFamily;
  text: string;
  /** The number the claim rests on, carried separately so it can be emphasised in the sentence. */
  figure: string;
  basedOnDays: number;
  strength: number;
}

export interface Observations {
  period: AnalysisPeriod;
  observations: Observation[];
  /** A stated answer, not something to infer from an empty list. */
  nothingStoodOut: boolean;
  minimumDaysForAnyObservation: number;
}

/**
 * One calendar day on the intake trend.
 *
 * `logged` is the field a chart must read first: on an unlogged day the intake figures are
 * placeholders rather than measurements, and a line must break rather than pass through them.
 * The target is meaningful either way — it was in force whether or not the member logged.
 */
export interface DailyIntakePoint {
  date: string;
  logged: boolean;
  kilocalories: number;
  targetKilocalories: number;
  proteinG: number;
  carbsG: number;
  fatG: number;
  targetProteinG: number | null;
  targetCarbsG: number | null;
  targetFatG: number | null;
}

export interface IntakeTrend {
  period: AnalysisPeriod;
  loggedDays: number;
  peakKilocalories: number;
  points: DailyIntakePoint[];
}

/** The eating-state split for one group of days: the counts, and the same counts as fractions. */
export interface EatingOutcome {
  days: number;
  onTargetDays: number;
  overTargetDays: number;
  notLoggedDays: number;
  onTargetShare: number;
  overTargetShare: number;
  notLoggedShare: number;
}

/**
 * How often the member logs beer and exercise over the period, and how eating on beer days
 * compares with every other day. Carries no amount of beer and no calorie figure — consistent
 * with the rest of analytics (005 FR-011..FR-013).
 */
export interface HabitInsights {
  period: AnalysisPeriod;
  inPlanDays: number;
  beerDays: number;
  beerDaysPerWeek: number;
  exerciseDays: number;
  exerciseDaysPerWeek: number;
  onBeerDays: EatingOutcome;
  onNonBeerDays: EatingOutcome;
}

// --- Exercise shortcuts -----------------------------------------------------
// A saved activity and duration, tapped to record a session in one interaction. Deliberately no
// MET and no estimate: the figure is computed when the session is recorded, from the member's
// current weight, so a saved button cannot freeze the weight they had when they saved it.

export interface ExerciseShortcut {
  id: string;
  name: string;
  activityTypeId: string;
  /** Resolved from the catalogue on read, not stored — a corrected name shows up here. */
  activityName: string;
  durationMinutes: number;
  position: number;
  /** False when the activity has left the catalogue; the button is shown unusable. */
  available: boolean;
}

export interface ExerciseShortcutList {
  shortcuts: ExerciseShortcut[];
  maxShortcuts: number;
  /** How many more may be added, so the limit is known before a save fails. */
  remainingSlots: number;
}

export interface CreateShortcutRequest {
  activityTypeId: string;
  durationMinutes: number;
  name: string | null;
}

export interface AddEntryFromShortcutRequest {
  shortcutId: string;
  version: string | null;
}
