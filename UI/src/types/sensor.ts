export interface QueryVars {
  location: string;
  from: string;
  to: string;
  interval: string;
}

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

export interface MotionStat {
  timeBucket: string;
  eventCount: number;
  isConstant: boolean;
}

export interface MotionData {
  aggregateMotion: MotionStat[];
}

export interface EnergyStat {
  timeBucket: string;
  totalEnergy: number;
  avgPower: number;
  peakPower: number;
}

export interface EnergyStatsData {
  aggregateEnergy: EnergyStat[];
}