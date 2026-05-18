import { useMemo } from 'react';

type PageRangePreset = 'energy' | 'airQuality' | 'motion';

/** Stable date ranges per page — GraphQL loads on mount; charts refetch on notifications. */
export function usePageDateRange(preset: PageRangePreset) {
  return useMemo(() => {
    const to = new Date();
    const from = new Date();

    switch (preset) {
      case 'energy':
        from.setDate(from.getDate() - 3);
        break;
      case 'airQuality':
        from.setHours(0, 0, 0, 0);
        break;
      case 'motion':
        from.setTime(from.getTime() - 48 * 60 * 60 * 1000);
        break;
    }

    return { from: from.toISOString(), to: to.toISOString() };
  }, [preset]);
}
