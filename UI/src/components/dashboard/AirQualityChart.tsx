import React, { useMemo } from 'react';
import { useQuery } from '@apollo/client/react';
import {
  LineChart, Line, XAxis, YAxis, CartesianGrid, Tooltip, ResponsiveContainer, Legend,
} from 'recharts';
import { AGGREGATE_AIR_QUALITY } from '../../graphql/queries';
import { useSensorNotificationsContext } from '../../signalr/useSensorNotificationsContext';
import { latestForLocation } from '../../signalr/selectors';
import { useRefetchOnNotification } from '../../hooks/useRefetchOnNotification';
import LiveReadingBanner from './LiveReadingBanner';

interface Props {
  location: string;
  from: string;
  to: string;
}

const AirQualityChart: React.FC<Props> = ({ location, from, to }) => {
  const { airQualityEvents } = useSensorNotificationsContext();
  const latest = useMemo(
    () => latestForLocation(airQualityEvents, location),
    [airQualityEvents, location],
  );

  const { data, loading, error, refetch } = useQuery(AGGREGATE_AIR_QUALITY, {
    variables: { location, from, to, interval: '1 minute' },
    fetchPolicy: 'network-only',
    nextFetchPolicy: 'cache-first',
  });

  useRefetchOnNotification(airQualityEvents, location, refetch);

  if (loading && !data) {
    return (
      <div className="h-72 flex items-center justify-center bg-slate-50 rounded-xl border border-slate-100 animate-pulse">
        <span className="text-slate-400">Loading air quality data...</span>
      </div>
    );
  }

  if (error) {
    return (
      <div className="h-72 flex items-center justify-center text-red-500 border border-red-200 rounded-lg bg-red-50 p-4 text-center">
        Error: {error.message}
      </div>
    );
  }

  const chartData = data?.aggregateAirQuality.map((item) => ({
    ...item,
    label: new Date(item.timeBucket).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' }),
  })) ?? [];

  return (
    <div className="bg-white p-6 rounded-2xl border border-slate-200 shadow-sm flex flex-col h-full min-h-[400px]">
      <div className="mb-2 flex items-center justify-between">
        <p className="text-sm font-semibold text-slate-700">Location: {location}</p>
        {loading && <span className="text-xs text-slate-400">Updating…</span>}
      </div>

      {latest ? (
        <LiveReadingBanner timestamp={latest.timestamp}>
          <span>
            <span className="text-slate-500">CO₂ </span>
            <span className="font-bold text-red-500">{latest.co2} ppm</span>
          </span>
          <span>
            <span className="text-slate-500">PM2.5 </span>
            <span className="font-bold text-slate-700">{latest.pm25}</span>
          </span>
          <span>
            <span className="text-slate-500">Humidity </span>
            <span className="font-bold text-blue-600">{latest.humidity}%</span>
          </span>
        </LiveReadingBanner>
      ) : (
        <p className="mb-4 text-xs text-slate-400">Waiting for live reading…</p>
      )}

      <div className="flex-1 h-[300px] w-full relative">
        <ResponsiveContainer width="100%" height={300}>
          <LineChart data={chartData} margin={{ top: 10, right: 10, left: -20, bottom: 0 }}>
            <CartesianGrid strokeDasharray="3 3" vertical={false} stroke="#f1f5f9" />
            <XAxis
              dataKey="label"
              axisLine={false}
              tickLine={false}
              tick={{ fill: '#94a3b8', fontSize: 10 }}
            />
            <YAxis
              yAxisId="co2"
              axisLine={false}
              tickLine={false}
              tick={{ fill: '#94a3b8', fontSize: 10 }}
            />
            <YAxis
              yAxisId="pmHumidity"
              orientation="right"
              axisLine={false}
              tickLine={false}
              tick={{ fill: '#94a3b8', fontSize: 10 }}
            />
            <Tooltip contentStyle={{ borderRadius: '12px', border: 'none', boxShadow: '0 10px 15px -3px rgba(0,0,0,0.1)' }} />
            <Legend verticalAlign="top" align="right" wrapperStyle={{ paddingBottom: '20px' }} />
            <Line
              yAxisId="co2"
              type="monotone"
              dataKey="avgCo2"
              name="CO₂ (avg)"
              stroke="#ef4444"
              strokeWidth={3}
              dot={false}
            />
            <Line
              yAxisId="pmHumidity"
              type="monotone"
              dataKey="avgPm25"
              name="PM2.5 (avg)"
              stroke="#f59e0b"
              strokeWidth={3}
              dot={false}
            />
            <Line
              yAxisId="pmHumidity"
              type="monotone"
              dataKey="avgHumidity"
              name="Humidity (avg)"
              stroke="#3b82f6"
              strokeWidth={3}
              dot={false}
            />
          </LineChart>
        </ResponsiveContainer>
      </div>
    </div>
  );
};

export default AirQualityChart;
