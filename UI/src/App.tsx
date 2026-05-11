import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import EnergyPage from './pages/EnergyPage';
import AirQualityPage from './pages/AirQualityPage';
import MotionPage from './pages/MotionPage';
import Layout from './components/ui/Layout';

function App() {
  return (
    <BrowserRouter>
      <Routes>
        <Route path="/" element={<Layout />}>
          <Route index element={<Navigate to="/energy" replace />} />
          
          <Route path="energy" element={<EnergyPage />} />
          <Route path="air-quality" element={<AirQualityPage />} />
          <Route path="motion" element={<MotionPage />} />
        </Route>
      </Routes>
    </BrowserRouter>
  );
}

export default App;