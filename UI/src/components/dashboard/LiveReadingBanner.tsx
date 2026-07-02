import type { ReactNode } from 'react';
import { formatReadingTime, isRecentTimestamp } from '../../signalr/selectors';

interface LiveReadingBannerProps {
  readonly timestamp?: string;
  readonly children: ReactNode;
}

export default function LiveReadingBanner({ timestamp, children }: LiveReadingBannerProps) {
  const isLive = isRecentTimestamp(timestamp);

  return (
    <div className="mb-4 rounded-xl border border-slate-100 bg-slate-50/80 px-4 py-3">
      <div className="flex flex-wrap items-center justify-between gap-2">
        <div className="flex flex-wrap items-center gap-3 text-sm">{children}</div>
        <div className="flex shrink-0 items-center gap-2">
          {isLive && (
            <span className="inline-flex items-center gap-1.5 rounded-full bg-emerald-100 px-2.5 py-0.5 text-xs font-semibold text-emerald-700">
              <span className="h-1.5 w-1.5 animate-pulse rounded-full bg-emerald-500" />
              LIVE
            </span>
          )}
          <span className="text-xs text-slate-400"> {formatReadingTime(timestamp)}</span>
        </div>
      </div>
    </div>
  );
}
