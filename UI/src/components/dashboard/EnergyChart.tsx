import React, { useMemo } from 'react';
import { useQuery } from '@apollo/client/react';
import {
  AreaChart, Area, XAxis, YAxis, CartesianGrid, Tooltip, ResponsiveContainer,
} from 'recharts';
import { AGGREGATE_ENERGY } from '../../graphql/queries';
import { useSensorNotificationsContext } from '../../signalr/useSensorNotificationsContext';
import { latestForLocation } from '../../signalr/selectors';
import { useRefetchOnNotification } from '../../hooks/useRefetchOnNotification';
import LiveReadingBanner from './LiveReadingBanner';

interface Props {
  location: string;
  from: string;
  to: string;
}

const EnergyChart: React.FC<Props> = ({ location, from, to }) => {
  const { energyEvents } = useSensorNotificationsContext();

  const latest = useMemo(
    () => latestForLocation(energyEvents, location),
    [energyEvents, location],
  );

  const { data, loading, error, refetch } = useQuery(AGGREGATE_ENERGY, {
    variables: { location, from, to, interval: '1 minute' },
    fetchPolicy: 'network-only',
    nextFetchPolicy: 'cache-first',
  });

  useRefetchOnNotification(energyEvents, location, refetch);

  const chartData = useMemo(() => {
    if (!data?.aggregateEnergy) return []

    const formattedDbData = data.aggregateEnergy.map((item) => ({
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
          totalEnergy: latest.energy,
          label: new Date(latest.timestamp).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' }),
          avgPower: 0,
          peakPower: 0
        })
      }
    }

    return formattedDbData;
  }, [data, latest]);

  if (loading && !data) {
    return (
      <div className="h-[350px] flex items-center justify-center bg-slate-50 rounded-xl border border-dashed border-slate-300 animate-pulse">
        <span className="text-slate-400 font-medium">Loading energy consumption data...</span>
      </div>
    );
  }

  if (error) {
    return (
      <div className="h-[350px] flex items-center justify-center text-red-500 border border-red-100 bg-red-50 rounded-xl">
        <div className="text-center">
          <p className="font-bold">Unexpected error</p>
          <p className="text-sm opacity-80">{error.message}</p>
        </div>
      </div>
    );
  }

  return (
    <div className="p-6 bg-white rounded-2xl shadow-sm border border-slate-200 mb-10">
      <div className="flex justify-between items-start mb-2">
        <p className="text-sm font-semibold text-slate-700">Location: {location}</p>
        {loading && <span className="text-xs text-slate-400">Updating…</span>}
      </div>

      {latest ? (
        <LiveReadingBanner timestamp={latest.timestamp}>
          <span className="text-slate-500">Current</span>
          <span className="font-bold text-blue-600">{latest.energy.toLocaleString()} kWh</span>
        </LiveReadingBanner>
      ) : (
        <p className="mb-4 text-xs text-slate-400">Waiting for live reading…</p>
      )}

      <div className="h-80 w-full">
        <ResponsiveContainer width="100%" height={300}>
          <AreaChart data={chartData}>
            <defs>
              <linearGradient id={`colorEnergy-${location.replace(/\s/g, '')}`} x1="0" y1="0" x2="0" y2="1">
                <stop offset="5%" stopColor="#3b82f6" stopOpacity={0.2} />
                <stop offset="95%" stopColor="#3b82f6" stopOpacity={0} />
              </linearGradient>
            </defs>
            <CartesianGrid strokeDasharray="3 3" vertical={false} stroke="#f1f5f9" />
            <XAxis
              dataKey="label"
              axisLine={false}
              tickLine={false}
              tick={{ fill: '#94a3b8', fontSize: 12 }}
              minTickGap={40}
            />
            <YAxis axisLine={false} tickLine={false} tick={{ fill: '#94a3b8', fontSize: 12 }} />
            <Tooltip
              contentStyle={{
                borderRadius: '12px',
                border: 'none',
                boxShadow: '0 10px 15px -3px rgba(0, 0, 0, 0.1)',
              }}
            />
            <Area
              type="monotone"
              dataKey="totalEnergy"
              stroke="#3b82f6"
              strokeWidth={3}
              fillOpacity={1}
              fill={`url(#colorEnergy-${location.replace(/\s/g, '')})`}
              animationDuration={1500}
            />
          </AreaChart>
        </ResponsiveContainer>
      </div>
    </div>
  );
};

export default EnergyChart;
