import { ActuatorDTO } from "./ActuatorDTO";

export interface UpdateControlRuleRequest {
  min: number;
  max: number;
  actuators: ActuatorDTO[];
}