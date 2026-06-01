import { jest } from '@jest/globals';
import { renderHook } from '@testing-library/react';
import { usePageDateRange } from '../../src/hooks/usePageDateRange';

describe('usePageDateRange', () => {
    const NOW = new Date('2026-06-01T15:30:00Z');

    beforeEach(() => {
        jest.useFakeTimers().setSystemTime(NOW);
    });

    afterEach(() => {
        jest.useRealTimers();
    });

    it('returns ISO strings for energy preset (3 days back)', () => {
        const { result } = renderHook(() => usePageDateRange('energy'));

        expect(result.current.to).toBe(NOW.toISOString());

        const from = new Date(result.current.from);
        const expectedFrom = new Date(NOW);
        expectedFrom.setDate(expectedFrom.getDate() - 3);
        expect(from.toISOString()).toBe(expectedFrom.toISOString());
    });

    it('returns ISO strings for airQuality preset (today midnight local)', () => {
        const { result } = renderHook(() => usePageDateRange('airQuality'));

        expect(result.current.to).toBe(NOW.toISOString());

        const from = new Date(result.current.from);
        expect(from.getHours()).toBe(0);
        expect(from.getMinutes()).toBe(0);
        expect(from.getSeconds()).toBe(0);
        expect(from.getMilliseconds()).toBe(0);

        expect(from.getFullYear()).toBe(NOW.getFullYear());
        expect(from.getMonth()).toBe(NOW.getMonth());
        expect(from.getDate()).toBe(NOW.getDate());
    });

    it('returns ISO strings for motion preset (48h back)', () => {
        const { result } = renderHook(() => usePageDateRange('motion'));

        expect(result.current.to).toBe(NOW.toISOString());

        const fromMs = new Date(result.current.from).getTime();
        expect(NOW.getTime() - fromMs).toBe(48 * 60 * 60 * 1000);
    });

    it('memoises the result for a stable preset across renders', () => {
        const { result, rerender } = renderHook(({ preset }) => usePageDateRange(preset), {
            initialProps: { preset: 'energy' as const },
        });

        const first = result.current;
        rerender({ preset: 'energy' as const });
        expect(result.current).toBe(first);
    });

    it('recomputes when the preset changes', () => {
        const { result, rerender } = renderHook(({ preset }) => usePageDateRange(preset), {
            initialProps: { preset: 'energy' as 'energy' | 'motion' },
        });

        const first = result.current;
        rerender({ preset: 'motion' });
        expect(result.current).not.toBe(first);
        expect(result.current.from).not.toBe(first.from);
    });
});
