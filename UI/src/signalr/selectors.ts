const LIVE_THRESHOLD_MS = 60_000;

export function latestForLocation<T extends { name: string }>(
  events: T[],
  location: string,
): T | undefined {
  for (let i = events.length - 1; i >= 0; i--) {
    if (events[i].name === location) return events[i];
  }
  return undefined;
}

export function countForLocation<T extends { name: string }>(
  events: T[],
  location: string,
): number {
  return events.filter((e) => e.name === location).length;
}

export function isRecentTimestamp(timestamp: string | undefined, thresholdMs = LIVE_THRESHOLD_MS): boolean {
  if (!timestamp) return false;
  const t = new Date(timestamp).getTime();
  if (Number.isNaN(t)) return false;
  return Date.now() - t < thresholdMs;
}

export function formatReadingTime(timestamp: string | undefined): string {
  if (!timestamp) return '—';
  const d = new Date(timestamp);
  if (Number.isNaN(d.getTime())) return '—';
  return d.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit', second: '2-digit' });
}
