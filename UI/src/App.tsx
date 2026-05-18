import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import EnergyPage from './pages/EnergyPage';
import AirQualityPage from './pages/AirQualityPage';
import MotionPage from './pages/MotionPage';
import Layout from './components/ui/Layout';
import { SensorNotificationsProvider } from './signalr/SensorNotificationsContext';

function App() {
  return (
    <BrowserRouter>
      <SensorNotificationsProvider>
      <Routes>
        <Route path="/" element={<Layout />}>
          <Route index element={<Navigate to="/energy" replace />} />
          
          <Route path="energy" element={<EnergyPage />} />
          <Route path="air-quality" element={<AirQualityPage />} />
          <Route path="motion" element={<MotionPage />} />
        </Route>
      </Routes>
      </SensorNotificationsProvider>
    </BrowserRouter>
  );
}

export default App;