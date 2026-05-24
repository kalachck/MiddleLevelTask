import { useContext } from 'react';
import type { UseSensorNotificationsResult } from './useSensorNotifications';
import { SensorNotificationsContext } from './sensorNotificationsContextValue';

export function useSensorNotificationsContext(): UseSensorNotificationsResult {
  const ctx = useContext(SensorNotificationsContext);
  if (!ctx) {
    throw new Error('useSensorNotificationsContext must be used within SensorNotificationsProvider');
  }
  return ctx;
}
