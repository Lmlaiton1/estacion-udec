export interface SensorMeasurement {
  id: string;
  device_Name: string;
  mark: string;
  sn: string;
  measurements: {
    value: number;
    date: string;
    unit: string;
  }[];
}