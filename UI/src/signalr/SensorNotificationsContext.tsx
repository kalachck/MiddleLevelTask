import { type ReactNode } from 'react';
import {
  useSensorNotifications,
} from './useSensorNotifications';
import { SensorNotificationsContext } from './sensorNotificationsContextValue';

export function SensorNotificationsProvider({ children }: { children: ReactNode }) {
  const value = useSensorNotifications();
  return (
    <SensorNotificationsContext.Provider value={value}>{children}</SensorNotificationsContext.Provider>
  );
}
