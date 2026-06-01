import { jest } from '@jest/globals';
import { renderHook } from '@testing-library/react';
import type { ReactNode } from 'react';
import * as signalR from '@microsoft/signalr';
import { useSensorNotificationsContext } from '../../src/signalr/useSensorNotificationsContext';
import { SensorNotificationsContext } from '../../src/signalr/sensorNotificationsContextValue';
import type { UseSensorNotificationsResult } from '../../src/signalr/useSensorNotifications';

describe('useSensorNotificationsContext', () => {
    it('throws a clear error when used outside the provider', () => {
        const originalError = console.error;
        console.error = jest.fn();
        try {
            expect(() => renderHook(() => useSensorNotificationsContext())).toThrow(
                /must be used within SensorNotificationsProvider/,
            );
        } finally {
            console.error = originalError;
        }
    });

    it('returns the context value when used inside the provider', () => {
        const value: UseSensorNotificationsResult = {
            connectionState: signalR.HubConnectionState.Connected,
            connectionError: null,
            energyEvents: [{ name: 'Office', energy: 1, timestamp: 't' }],
            motionEvents: [],
            airQualityEvents: [],
            allEvents: [],
        };

        const wrapper = ({ children }: { children: ReactNode }) => (
            <SensorNotificationsContext.Provider value={value}>
                {children}
            </SensorNotificationsContext.Provider>
        );

        const { result } = renderHook(() => useSensorNotificationsContext(), { wrapper });

        expect(result.current).toBe(value);
        expect(result.current.connectionState).toBe(signalR.HubConnectionState.Connected);
        expect(result.current.energyEvents).toHaveLength(1);
    });
});
