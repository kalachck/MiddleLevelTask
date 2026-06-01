import { jest } from '@jest/globals';
import { act, renderHook } from '@testing-library/react';
import { useRefetchOnNotification } from '../../src/hooks/useRefetchOnNotification';

const DEBOUNCE_MS = 2_000;

interface Event {
    name: string;
}

describe('useRefetchOnNotification', () => {
    beforeEach(() => {
        jest.useFakeTimers();
    });

    afterEach(() => {
        jest.useRealTimers();
    });

    it('does not call refetch on initial mount even when events already exist', () => {
        const refetch = jest.fn();
        const events: Event[] = [{ name: 'Office' }, { name: 'Office' }];

        renderHook(() => useRefetchOnNotification(events, 'Office', refetch));
        act(() => {
            jest.advanceTimersByTime(DEBOUNCE_MS + 100);
        });

        expect(refetch).not.toHaveBeenCalled();
    });

    it('debounces and calls refetch when the location count increases', () => {
        const refetch = jest.fn();
        let events: Event[] = [{ name: 'Office' }];

        const { rerender } = renderHook(
            ({ list }) => useRefetchOnNotification(list, 'Office', refetch),
            { initialProps: { list: events } },
        );

        events = [...events, { name: 'Office' }];
        rerender({ list: events });

        act(() => {
            jest.advanceTimersByTime(DEBOUNCE_MS - 1);
        });
        expect(refetch).not.toHaveBeenCalled();

        act(() => {
            jest.advanceTimersByTime(1);
        });
        expect(refetch).toHaveBeenCalledTimes(1);
    });

    it('ignores new events for a different location', () => {
        const refetch = jest.fn();
        let events: Event[] = [{ name: 'Office' }];

        const { rerender } = renderHook(
            ({ list }) => useRefetchOnNotification(list, 'Office', refetch),
            { initialProps: { list: events } },
        );

        events = [...events, { name: 'Kitchen' }, { name: 'Garage' }];
        rerender({ list: events });

        act(() => {
            jest.advanceTimersByTime(DEBOUNCE_MS + 100);
        });
        expect(refetch).not.toHaveBeenCalled();
    });

    it('collapses bursts into a single refetch via debounce', () => {
        const refetch = jest.fn();
        let events: Event[] = [{ name: 'Office' }];

        const { rerender } = renderHook(
            ({ list }) => useRefetchOnNotification(list, 'Office', refetch),
            { initialProps: { list: events } },
        );

        events = [...events, { name: 'Office' }];
        rerender({ list: events });

        act(() => {
            jest.advanceTimersByTime(500);
        });

        events = [...events, { name: 'Office' }];
        rerender({ list: events });

        act(() => {
            jest.advanceTimersByTime(DEBOUNCE_MS + 100);
        });
        expect(refetch).toHaveBeenCalledTimes(1);
    });

    it('cancels the pending refetch on unmount', () => {
        const refetch = jest.fn();
        let events: Event[] = [{ name: 'Office' }];

        const { rerender, unmount } = renderHook(
            ({ list }) => useRefetchOnNotification(list, 'Office', refetch),
            { initialProps: { list: events } },
        );

        events = [...events, { name: 'Office' }];
        rerender({ list: events });

        unmount();

        act(() => {
            jest.advanceTimersByTime(DEBOUNCE_MS + 100);
        });
        expect(refetch).not.toHaveBeenCalled();
    });
});
