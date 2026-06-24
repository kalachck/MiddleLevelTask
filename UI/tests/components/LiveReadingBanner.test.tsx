import { jest } from '@jest/globals';
import { render, screen } from '@testing-library/react';
import LiveReadingBanner from '../../src/components/dashboard/LiveReadingBanner';

describe('<LiveReadingBanner />', () => {
    const NOW = new Date('2026-06-01T12:00:00Z');

    beforeEach(() => {
        jest.useFakeTimers().setSystemTime(NOW);
    });

    afterEach(() => {
        jest.useRealTimers();
    });

    it('renders the children content', () => {
        render(
            <LiveReadingBanner timestamp={new Date(NOW.getTime() - 5_000).toISOString()}>
                <span>CO₂ 600 ppm</span>
            </LiveReadingBanner>,
        );

        expect(screen.getByText('CO₂ 600 ppm')).toBeInTheDocument();
    });

    it('shows the LIVE badge for a recent timestamp', () => {
        render(
            <LiveReadingBanner timestamp={new Date(NOW.getTime() - 10_000).toISOString()}>
                <span>fresh</span>
            </LiveReadingBanner>,
        );

        expect(screen.getByText('LIVE')).toBeInTheDocument();
    });

    it('hides the LIVE badge for an old timestamp', () => {
        render(
            <LiveReadingBanner timestamp={new Date(NOW.getTime() - 5 * 60_000).toISOString()}>
                <span>stale</span>
            </LiveReadingBanner>,
        );

        expect(screen.queryByText('LIVE')).not.toBeInTheDocument();
    });

    it('hides the LIVE badge when no timestamp is provided and renders the em-dash', () => {
        render(
            <LiveReadingBanner>
                <span>no timestamp</span>
            </LiveReadingBanner>,
        );

        expect(screen.queryByText('LIVE')).not.toBeInTheDocument();
        expect(screen.getByText('—')).toBeInTheDocument();
    });

    it('renders a non-empty time string for a valid timestamp', () => {
        const stamp = new Date(NOW.getTime() - 1_000).toISOString();
        const { container } = render(
            <LiveReadingBanner timestamp={stamp}>
                <span>x</span>
            </LiveReadingBanner>,
        );

        const timeSpan = container.querySelector('span.text-xs.text-slate-400');
        expect(timeSpan).not.toBeNull();
        expect(timeSpan?.textContent).not.toBe('—');
        expect((timeSpan?.textContent?.length ?? 0)).toBeGreaterThan(0);
    });
});
