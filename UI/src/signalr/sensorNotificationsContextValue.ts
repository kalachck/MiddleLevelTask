import { createContext } from 'react';
import type { UseSensorNotificationsResult } from './useSensorNotifications';

export const SensorNotificationsContext = createContext<UseSensorNotificationsResult | null>(null);
