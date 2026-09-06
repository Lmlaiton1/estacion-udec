export interface IAudit {
    id: string;
    date: Date;
    device: string;
    action: string;
    observation: string;
}