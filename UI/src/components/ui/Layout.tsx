import { Outlet, NavLink } from 'react-router-dom';
import { Zap, Wind, Activity, LayoutDashboard } from 'lucide-react';
import ConnectionStatus from './ConnectionStatus';

const Layout = () => {
  const navItems = [
    { to: '/energy', label: 'Energy', icon: <Zap size={20} /> },
    { to: '/air-quality', label: 'Air Quality', icon: <Wind size={20} /> },
    { to: '/motion', label: 'Motion', icon: <Activity size={20} /> },
  ];

  return (
    <div className="flex min-h-screen bg-slate-50">
      <aside className="w-64 bg-white border-r border-slate-200 p-6 flex flex-col">
        <div className="flex items-center gap-2 mb-10 px-2">
          <LayoutDashboard className="text-blue-600" />
          <span className="font-bold text-xl tracking-tight">SensorView</span>
        </div>

        <nav className="space-y-2 flex-1">
          {navItems.map((item) => (
            <NavLink
              key={item.to}
              to={item.to}
              className={({ isActive }) =>
                `flex items-center gap-6 px-4 py-3 rounded-xl font-medium transition-all ${
                  isActive 
                    ? 'bg-blue-50 text-blue-600 shadow-sm' 
                    : 'text-slate-500 hover:bg-slate-50 hover:text-slate-900'
                }`
              }
            >
              {item.icon}
              {item.label}
            </NavLink>
          ))}
        </nav>

        <ConnectionStatus />
      </aside>

      <main className="flex-1 overflow-y-auto bg-slate-50 p-8 min-h-screen">
        <div className="max-w-6xl mx-auto w-full">
          <Outlet />
        </div>
      </main>
    </div>
  );
};

export default Layout;