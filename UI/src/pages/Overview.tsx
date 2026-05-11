import { useState } from "react";
import EnergyChart from "../components/dashboard/EnergyChart";
import AirQualityChart from "../components/dashboard/AirQualityChart";
import MotionChart from "../components/dashboard/MotionChart";

const Overview = () => {
  const [location] = useState("Office");
  const to = new Date().toISOString();
  // eslint-disable-next-line react-hooks/purity
  const from = new Date(Date.now() - 24 * 60 * 60 * 1000).toISOString();

  return (
    <div className="grid grid-cols-1 lg:grid-cols-2 gap-6 p-6">
      <div className="lg:col-span-2">
         <EnergyChart location={location} from={from} to={to} />
      </div>
      <AirQualityChart location={location} from={from} to={to} />
      <MotionChart location={location} from={from} to={to} />
    </div>
  );
};

export default Overview;