import { createContext, useContext, type ReactNode } from 'react';
import {
  useSensorNotifications,
  type UseSensorNotificationsResult,
} from './useSensorNotifications';

const SensorNotificationsContext = createContext<UseSensorNotificationsResult | null>(null);

export function SensorNotificationsProvider({ children }: { children: ReactNode }) {
  const value = useSensorNotifications();
  return (
    <SensorNotificationsContext.Provider value={value}>{children}</SensorNotificationsContext.Provider>
  );
}

export function useSensorNotificationsContext(): UseSensorNotificationsResult {
  const ctx = useContext(SensorNotificationsContext);
  if (!ctx) {
    throw new Error('useSensorNotificationsContext must be used within SensorNotificationsProvider');
  }
  return ctx;
}
