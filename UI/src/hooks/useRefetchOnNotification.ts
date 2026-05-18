import { useEffect, useRef } from 'react';
import { countForLocation } from '../signalr/selectors';

const REFETCH_DEBOUNCE_MS = 2_000;

export function useRefetchOnNotification<T extends { name: string }>(
  events: T[],
  location: string,
  refetch: () => void | Promise<unknown>,
): void {
  const countRef = useRef(countForLocation(events, location));

  useEffect(() => {
    const count = countForLocation(events, location);
    if (count <= countRef.current) return;

    countRef.current = count;
    const timer = window.setTimeout(() => {
      void refetch();
    }, REFETCH_DEBOUNCE_MS);

    return () => window.clearTimeout(timer);
  }, [events, location, refetch]);
}
