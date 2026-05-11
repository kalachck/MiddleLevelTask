import AirQualityChart from '../components/dashboard/AirQualityChart';

const AirQualityPage = () => {
  const locations = [
    "Office", 
    "Living Room", 
    "Kitchen", 
    "Corridor", 
    "Bedroom", 
    "Garage"
  ];

  const from = "2026-04-30T00:00:00Z";
  const to = "2026-04-30T23:59:59Z";

  return (
    <div className="space-y-8">
      <header className="flex justify-between items-end">
        <div>
          <h1 className="text-3xl font-extrabold text-slate-900">Air Quality Monitoring</h1>
        </div>
        <div className="bg-blue-100 text-blue-700 px-4 py-2 rounded-lg text-sm font-bold">
          Active zones: {locations.length}
        </div>
      </header>

      <div className="grid grid-cols-1 lg:grid-cols-2 gap-8">
        {locations.map((loc) => (
          <div key={loc}>
            <AirQualityChart 
              location={loc} 
              from={from} 
              to={to} 
            />
          </div>
        ))}
      </div>
    </div>
  );
};

export default AirQualityPage;