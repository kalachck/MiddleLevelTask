import React, { useMemo } from 'react';
import { useQuery } from '@apollo/client/react';
import {
  BarChart, Bar, XAxis, YAxis, CartesianGrid, Tooltip, ResponsiveContainer,
} from 'recharts';
import { AGGREGATE_MOTION } from '../../graphql/queries';
import { useSensorNotificationsContext } from '../../signalr/useSensorNotificationsContext';
import { latestForLocation } from '../../signalr/selectors';
import { useRefetchOnNotification } from '../../hooks/useRefetchOnNotification';
import LiveReadingBanner from './LiveReadingBanner';

interface Props {
  location: string;
  from: string;
  to: string;
}

const MotionChart: React.FC<Props> = ({ location, from, to }) => {
  const { motionEvents } = useSensorNotificationsContext();
  const latest = useMemo(
    () => latestForLocation(motionEvents, location),
    [motionEvents, location],
  );

  const { data, loading, error, refetch } = useQuery(AGGREGATE_MOTION, {
    variables: { location, from, to, interval: '1 minute' },
    fetchPolicy: 'network-only',
    nextFetchPolicy: 'cache-first',
  });

  useRefetchOnNotification(motionEvents, location, refetch);

  const chartData = useMemo(() => {
    if (!data?.aggregateMotion) return []

    const formattedDbData = data.aggregateMotion.map((item) => ({
      ...item,
      label: new Date(item.timeBucket).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })
    }));

    if (latest) {
      const latestTime = new Date(latest.timestamp).getTime();
      const lastDbTime = formattedDbData.length > 0
      ? new Date(formattedDbData[formattedDbData.length - 1].timeBucket).getTime()
      : 0;

      if (latestTime > lastDbTime) {
        formattedDbData.push({
          timeBucket: latest.timestamp,
          label: new Date(latest.timestamp).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' }),
          eventCount: latest.motionDetected ? 1 : 0,
          isConstant: true
        });
      }
    }

    return formattedDbData;
  }, [data, latest])

  if (loading && !data) {
    return (
      <div className="h-72 flex items-center justify-center bg-slate-50 rounded-xl border border-slate-100 animate-pulse">
        <span className="text-slate-400">Loading...</span>
      </div>
    );
  }

  if (error) {
    return (
      <div className="h-72 flex items-center justify-center text-red-500 border border-red-200 rounded-lg bg-red-50">
        Error: {error.message}
      </div>
    );
  }

  return (
    <div className="p-6 bg-white rounded-xl shadow-sm border border-slate-200 w-full">
      <div className="flex justify-between items-center mb-2">
        <h3 className="text-lg font-semibold text-slate-800">Motion activity — {location}</h3>
        {loading && <span className="text-xs text-slate-400">Updating…</span>}
      </div>

      {latest ? (
        <LiveReadingBanner timestamp={latest.timestamp}>
          <span className="text-slate-500">Status</span>
          <span
            className={`font-bold ${latest.motionDetected ? 'text-purple-600' : 'text-slate-600'}`}
          >
            {latest.motionDetected ? 'Motion detected' : 'No motion'}
          </span>
        </LiveReadingBanner>
      ) : (
        <p className="mb-4 text-xs text-slate-400">Waiting for live reading…</p>
      )}

      <div className="h-80 w-full">
        <ResponsiveContainer width="100%" height={300}>
          <BarChart data={chartData} margin={{ top: 10, right: 10, left: -20, bottom: 0 }}>
            <CartesianGrid strokeDasharray="3 3" vertical={false} stroke="#e2e8f0" />
            <XAxis
              dataKey="label"
              axisLine={false}
              tickLine={false}
              tick={{ fill: '#64748b', fontSize: 12 }}
            />
            <YAxis
              axisLine={false}
              tickLine={false}
              tick={{ fill: '#64748b', fontSize: 12 }}
            />
            <Tooltip
              cursor={{ fill: '#f8fafc' }}
              contentStyle={{ borderRadius: '8px', border: 'none', boxShadow: '0 4px 6px -1px rgb(0 0 0 / 0.1)' }}
            />
            <Bar
              dataKey="eventCount"
              fill="#8b5cf6"
              radius={[4, 4, 0, 0]}
              name="Events in minute"
            />
          </BarChart>
        </ResponsiveContainer>
      </div>
    </div>
  );
};

export default MotionChart;
