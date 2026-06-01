import { jest } from '@jest/globals';
import {
    countForLocation,
    formatReadingTime,
    isRecentTimestamp,
    latestForLocation,
} from '../../src/signalr/selectors';

describe('signalr/selectors', () => {
    describe('latestForLocation', () => {
        it('returns the last event with a matching name', () => {
            const events = [
                { name: 'Kitchen', value: 1 },
                { name: 'Office', value: 2 },
                { name: 'Kitchen', value: 3 },
                { name: 'Office', value: 4 },
            ];
            expect(latestForLocation(events, 'Kitchen')).toEqual({ name: 'Kitchen', value: 3 });
            expect(latestForLocation(events, 'Office')).toEqual({ name: 'Office', value: 4 });
        });

        it('returns undefined when no event matches', () => {
            const events = [{ name: 'Kitchen', value: 1 }];
            expect(latestForLocation(events, 'Garage')).toBeUndefined();
        });

        it('returns undefined for an empty array', () => {
            expect(latestForLocation([] as { name: string }[], 'Office')).toBeUndefined();
        });
    });

    describe('countForLocation', () => {
        it('counts only events whose name matches', () => {
            const events = [
                { name: 'Kitchen' },
                { name: 'Office' },
                { name: 'Kitchen' },
                { name: 'Kitchen' },
            ];
            expect(countForLocation(events, 'Kitchen')).toBe(3);
            expect(countForLocation(events, 'Office')).toBe(1);
            expect(countForLocation(events, 'Garage')).toBe(0);
        });

        it('returns 0 for an empty array', () => {
            expect(countForLocation([] as { name: string }[], 'Office')).toBe(0);
        });
    });

    describe('isRecentTimestamp', () => {
        const NOW = new Date('2026-06-01T12:00:00Z').getTime();

        beforeEach(() => {
            jest.useFakeTimers().setSystemTime(NOW);
        });

        afterEach(() => {
            jest.useRealTimers();
        });

        it('returns true when timestamp is within the default 60s threshold', () => {
            const recent = new Date(NOW - 30_000).toISOString();
            expect(isRecentTimestamp(recent)).toBe(true);
        });

        it('returns false when timestamp is older than the default threshold', () => {
            const old = new Date(NOW - 120_000).toISOString();
            expect(isRecentTimestamp(old)).toBe(false);
        });

        it('respects a custom threshold', () => {
            const stamp = new Date(NOW - 5_000).toISOString();
            expect(isRecentTimestamp(stamp, 1_000)).toBe(false);
            expect(isRecentTimestamp(stamp, 10_000)).toBe(true);
        });

        it('returns false when timestamp is undefined', () => {
            expect(isRecentTimestamp(undefined)).toBe(false);
        });

        it('returns false when timestamp cannot be parsed', () => {
            expect(isRecentTimestamp('not-a-date')).toBe(false);
        });
    });

    describe('formatReadingTime', () => {
        it('returns an em-dash for undefined input', () => {
            expect(formatReadingTime(undefined)).toBe('—');
        });

        it('returns an em-dash for an unparsable timestamp', () => {
            expect(formatReadingTime('not-a-date')).toBe('—');
        });

        it('returns a non-empty locale time string for a valid timestamp', () => {
            const formatted = formatReadingTime(new Date('2026-06-01T09:30:00Z').toISOString());
            expect(typeof formatted).toBe('string');
            expect(formatted).not.toBe('—');
            expect(formatted.length).toBeGreaterThan(0);
        });
    });
});
