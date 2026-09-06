export interface Schedule {
  id: string;
  hour: number;
  minute: number;
  durationSeconds: number;
  deviceId: string;
  deviceName?: string;
}
