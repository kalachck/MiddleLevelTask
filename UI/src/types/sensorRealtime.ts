/** Payloads match anonymous objects sent from `SignalRNotificationService` (camelCase JSON). */

export interface EnergyProcessedPayload {
  name: string;
  energy: number;
  timestamp: string;
}

export interface MotionProcessedPayload {
  name: string;
  motionDetected: boolean;
  timestamp: string;
}

export interface AirQualityProcessedPayload {
  name: string;
  co2: number;
  pm25: number;
  humidity: number;
  timestamp: string;
}

export type SensorRealtimeEvent =
  | { kind: 'energy'; payload: EnergyProcessedPayload }
  | { kind: 'motion'; payload: MotionProcessedPayload }
  | { kind: 'airQuality'; payload: AirQualityProcessedPayload };
