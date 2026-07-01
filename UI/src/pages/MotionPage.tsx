import MotionChart from '../components/dashboard/MotionChart';
import { SENSOR_LOCATIONS } from '../constants/locations';
import { usePageDateRange } from '../hooks/usePageDateRange';

const MotionPage = () => {
  const { from, to } = usePageDateRange('motion');

  return (
    <div className="space-y-8">
      <header className="flex justify-between items-end">
        <div>
          <h1 className="text-3xl font-extrabold text-slate-900">Motion sensors</h1>
          <p className="text-slate-500 mt-2">
            Activity analysis · live status via SignalR
          </p>
        </div>
        <div className="text-right">
          <div className="bg-purple-100 text-purple-700 px-4 py-2 rounded-lg text-sm font-bold">
            Total sensors: {SENSOR_LOCATIONS.length}
          </div>
        </div>
      </header>

      <div className="grid grid-cols-1 lg:grid-cols-2 gap-8">
        {SENSOR_LOCATIONS.map((loc) => (
          <div key={loc} className="hover:shadow-md transition-shadow duration-300 rounded-2xl">
            <MotionChart location={loc} from={from} to={to} />
          </div>
        ))}
      </div>
    </div>
  );
};

export default MotionPage;
