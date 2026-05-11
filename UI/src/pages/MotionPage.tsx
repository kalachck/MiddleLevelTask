import MotionChart from '../components/dashboard/MotionChart';

const MotionPage = () => {
  const locations = [
    "Office", 
    "Living Room", 
    "Kitchen", 
    "Corridor", 
    "Bedroom", 
    "Garage"
  ];

  const to = new Date().toISOString();
// eslint-disable-next-line react-hooks/purity
const from = new Date(Date.now() - 48 * 60 * 60 * 1000).toISOString();

  return (
    <div className="space-y-8">
      <header className="flex justify-between items-end">
        <div>
          <h1 className="text-3xl font-extrabold text-slate-900">Датчики движения</h1>
          <p className="text-slate-500 mt-2">Анализ частоты активности и присутствия в зонах</p>
        </div>
        <div className="bg-purple-100 text-purple-700 px-4 py-2 rounded-lg text-sm font-bold">
          Всего датчиков: {locations.length}
        </div>
      </header>

      <div className="grid grid-cols-1 lg:grid-cols-2 gap-8">
        {locations.map((loc) => (
          <div key={loc} className="hover:shadow-md transition-shadow duration-300 rounded-2xl">
            <MotionChart 
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

export default MotionPage;