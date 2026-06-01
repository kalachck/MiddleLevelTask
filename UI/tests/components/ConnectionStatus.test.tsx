import { render, screen } from '@testing-library/react';
import * as signalR from '@microsoft/signalr';
import ConnectionStatus from '../../src/components/ui/ConnectionStatus';
import { SensorNotificationsContext } from '../../src/signalr/sensorNotificationsContextValue';
import type { UseSensorNotificationsResult } from '../../src/signalr/useSensorNotifications';

function renderWith(value: Partial<UseSensorNotificationsResult>) {
    const full: UseSensorNotificationsResult = {
        connectionState: signalR.HubConnectionState.Disconnected,
        connectionError: null,
        energyEvents: [],
        motionEvents: [],
        airQualityEvents: [],
        allEvents: [],
        ...value,
    };
    return render(
        <SensorNotificationsContext.Provider value={full}>
            <ConnectionStatus />
        </SensorNotificationsContext.Provider>,
    );
}

describe('<ConnectionStatus />', () => {
    it.each([
        [signalR.HubConnectionState.Connected, 'Live'],
        [signalR.HubConnectionState.Connecting, 'Connecting'],
        [signalR.HubConnectionState.Reconnecting, 'Reconnecting'],
        [signalR.HubConnectionState.Disconnected, 'Offline'],
        [signalR.HubConnectionState.Disconnecting, 'Disconnecting'],
    ])('renders %s label for state %s', (state, label) => {
        renderWith({ connectionState: state });
        expect(screen.getByText(label)).toBeInTheDocument();
    });

    it('does not render an error message when connectionError is null', () => {
        const { container } = renderWith({
            connectionState: signalR.HubConnectionState.Connected,
            connectionError: null,
        });
        expect(container.querySelector('p.text-red-500')).toBeNull();
    });

    it('renders the connection error message when provided', () => {
        renderWith({
            connectionState: signalR.HubConnectionState.Disconnected,
            connectionError: 'connection refused',
        });
        expect(screen.getByText('connection refused')).toBeInTheDocument();
    });

    it('sets the error as the wrapper title for hover-tooltips', () => {
        const { container } = renderWith({
            connectionState: signalR.HubConnectionState.Disconnected,
            connectionError: 'down for maintenance',
        });
        const wrapper = container.firstElementChild as HTMLElement;
        expect(wrapper.getAttribute('title')).toBe('down for maintenance');
    });

    it('applies the connected (emerald) indicator class when live', () => {
        const { container } = renderWith({
            connectionState: signalR.HubConnectionState.Connected,
        });
        const dot = container.querySelector('span.rounded-full');
        expect(dot?.className).toContain('bg-emerald-500');
    });

    it('applies a pulsing amber indicator while connecting', () => {
        const { container } = renderWith({
            connectionState: signalR.HubConnectionState.Connecting,
        });
        const dot = container.querySelector('span.rounded-full');
        expect(dot?.className).toContain('bg-amber-400');
        expect(dot?.className).toContain('animate-pulse');
    });
});
