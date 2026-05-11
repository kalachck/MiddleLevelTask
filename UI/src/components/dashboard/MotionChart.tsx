import React from 'react';
import { useQuery } from '@apollo/client/react';
import { 
  BarChart, Bar, XAxis, YAxis, CartesianGrid, Tooltip, ResponsiveContainer 
} from 'recharts';
import { AGGREGATE_MOTION } from '../../graphql/queries';
import type { MotionData, QueryVars } from '../../types/sensor';

interface Props {
  location: string;
  from: string;
  to: string;
}

const MotionChart: React.FC<Props> = ({ location, from, to }) => {
  const { data, loading, error } = useQuery<MotionData, QueryVars>(
    AGGREGATE_MOTION,
    {
      variables: { location, from, to, interval: '1 hour' },
    }
  );

  if (loading) return (
    <div className="h-72 flex items-center justify-center bg-slate-50 rounded-xl border border-slate-100 animate-pulse">
      <span className="text-slate-400">Загрузка...</span>
    </div>
  );
  
  if (error) return (
    <div className="h-72 flex items-center justify-center text-red-500 border border-red-200 rounded-lg bg-red-50">
      Ошибка: {error.message}
    </div>
  );

  const chartData = data?.aggregateMotion.map(item => ({
    ...item,
    label: new Date(item.timeBucket).toLocaleTimeString([], { hour: '2-digit' })
  })) || [];

  console.log(chartData)

  return (
    <div className="p-6 bg-white rounded-xl shadow-sm border border-slate-200 w-full">
      <h3 className="text-lg font-semibold text-slate-800 mb-4">Активность движения — {location}</h3>
      <div className="h-80 w-full">
        <ResponsiveContainer width="100%" height={300}>
          <BarChart data={chartData} margin={{ top: 10, right: 10, left: -20, bottom: 0 }}>
            <CartesianGrid strokeDasharray="3 3" vertical={false} stroke="#e2e8f0" />
            <XAxis 
              dataKey="label" 
              axisLine={false} 
              tickLine={false} 
              tick={{fill: '#64748b', fontSize: 12}} 
            />
            <YAxis 
              axisLine={false} 
              tickLine={false} 
              tick={{fill: '#64748b', fontSize: 12}} 
            />
            <Tooltip 
              cursor={{fill: '#f8fafc'}} 
              contentStyle={{ borderRadius: '8px', border: 'none', boxShadow: '0 4px 6px -1px rgb(0 0 0 / 0.1)' }} 
            />
            <Bar 
              dataKey="eventCount" 
              fill="#8b5cf6" 
              radius={[4, 4, 0, 0]} 
              name="Событий за час" 
            />
          </BarChart>
        </ResponsiveContainer>
      </div>
    </div>
  );
};

export default MotionChart;