import { ActuatorDTO } from "./ActuatorDTO";

export interface ControlRuleResponse {
  id: string;
  deviceId: string;
  deviceName: string;
  min: number;
  max: number;
  actuators: ActuatorDTO[];
}