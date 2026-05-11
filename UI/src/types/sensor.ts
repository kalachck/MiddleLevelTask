// Общий тип для параметров запроса
export interface QueryVars {
  location: string;
  from: string;
  to: string;
  interval: string;
}

// Air Quality
export interface AirQualityStat {
  timeBucket: string;
  avgCo2: number;
  avgPm25: number;
  avgHumidity: number;
  maxCo2: number;
}

export interface AirQualityData {
  aggregateAirQuality: AirQualityStat[];
}

// Motion
export interface MotionStat {
  timeBucket: string;
  eventCount: number;
  isConstant: boolean;
}

export interface MotionData {
  aggregateMotion: MotionStat[];
}

// Переиспользуем твои существующие Energy типы
export interface EnergyStat {
  timeBucket: string;
  totalEnergy: number;
  avgPower: number;
  peakPower: number;
}

export interface EnergyStatsData {
  aggregateEnergy: EnergyStat[];
}