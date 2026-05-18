export const SENSOR_LOCATIONS = [
  'Office',
  'Living Room',
  'Kitchen',
  'Corridor',
  'Bedroom',
  'Garage',
] as const;

export type SensorLocation = (typeof SENSOR_LOCATIONS)[number];
