import EnergyChart from '../components/dashboard/EnergyChart';

const EnergyPage = () => {
  const locations = [
    "Office", 
    "Living Room", 
    "Kitchen", 
    "Corridor", 
    "Bedroom", 
    "Garage"
  ];

  const from = "2026-04-27T00:00:00Z";
  const to = "2026-04-30T23:59:59Z";

  return (
    <div className="space-y-8">
      <header className="flex justify-between items-end">
        <div>
          <h1 className="text-3xl font-extrabold text-slate-900">Мониторинг электроэнергии</h1>
          <p className="text-slate-500 mt-2">Обзор потребления по всем активным точкам (Апрель 2026)</p>
        </div>
        <div className="text-right text-sm text-slate-400">
          Найдено локаций: {locations.length}
        </div>
      </header>

      {/* Сетка графиков: 2 колонки на больших экранах, 1 на мобильных */}
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-8">
        {locations.map((loc) => (
          <div key={loc} className="transition-transform hover:scale-[1.01]">
             <EnergyChart 
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

export default EnergyPage;