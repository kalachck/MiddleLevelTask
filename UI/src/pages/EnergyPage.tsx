import EnergyChart from '../components/dashboard/EnergyChart';
import { SENSOR_LOCATIONS } from '../constants/locations';
import { usePageDateRange } from '../hooks/usePageDateRange';
import { useSensorNotificationsContext } from '../signalr/SensorNotificationsContext';

const EnergyPage = () => {
  const { from, to } = usePageDateRange('energy');
  const { energyEvents } = useSensorNotificationsContext();
  const liveCount = energyEvents.length;

  return (
    <div className="space-y-8">
      <header className="flex justify-between items-end">
        <div>
          <h1 className="text-3xl font-extrabold text-slate-900">Мониторинг электроэнергии</h1>
          <p className="text-slate-500 mt-2">
            История из GraphQL · обновления в реальном времени через SignalR
          </p>
        </div>
        <div className="text-right text-sm text-slate-500">
          <p>Локаций: {SENSOR_LOCATIONS.length}</p>
          {liveCount > 0 && (
            <p className="text-emerald-600 font-medium">{liveCount} live events buffered</p>
          )}
        </div>
      </header>

      <div className="grid grid-cols-1 lg:grid-cols-2 gap-8">
        {SENSOR_LOCATIONS.map((loc) => (
          <div key={loc} className="transition-transform hover:scale-[1.01]">
            <EnergyChart location={loc} from={from} to={to} />
          </div>
        ))}
      </div>
    </div>
  );
};

export default EnergyPage;
