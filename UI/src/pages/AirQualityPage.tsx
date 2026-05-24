import AirQualityChart from '../components/dashboard/AirQualityChart';
import { SENSOR_LOCATIONS } from '../constants/locations';
import { usePageDateRange } from '../hooks/usePageDateRange';
import { useSensorNotificationsContext } from '../signalr/useSensorNotificationsContext';

const AirQualityPage = () => {
  const { from, to } = usePageDateRange('airQuality');
  const { airQualityEvents } = useSensorNotificationsContext();
  const liveCount = airQualityEvents.length;

  return (
    <div className="space-y-8">
      <header className="flex justify-between items-end">
        <div>
          <h1 className="text-3xl font-extrabold text-slate-900">Air Quality Monitoring</h1>
        </div>
        <div className="text-right">
          <div className="bg-blue-100 text-blue-700 px-4 py-2 rounded-lg text-sm font-bold">
            Active zones: {SENSOR_LOCATIONS.length}
          </div>
          {liveCount > 0 && (
            <p className="mt-1 text-xs text-emerald-600 font-medium">{liveCount} live events</p>
          )}
        </div>
      </header>

      <div className="grid grid-cols-1 lg:grid-cols-2 gap-8">
        {SENSOR_LOCATIONS.map((loc) => (
          <div key={loc}>
            <AirQualityChart location={loc} from={from} to={to} />
          </div>
        ))}
      </div>
    </div>
  );
};

export default AirQualityPage;
