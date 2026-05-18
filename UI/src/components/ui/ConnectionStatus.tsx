import * as signalR from '@microsoft/signalr';
import { useSensorNotificationsContext } from '../../signalr/SensorNotificationsContext';

const labels: Record<signalR.HubConnectionState, string> = {
  [signalR.HubConnectionState.Connected]: 'Live',
  [signalR.HubConnectionState.Connecting]: 'Connecting',
  [signalR.HubConnectionState.Reconnecting]: 'Reconnecting',
  [signalR.HubConnectionState.Disconnected]: 'Offline',
  [signalR.HubConnectionState.Disconnecting]: 'Disconnecting',
};

const dotClass: Record<signalR.HubConnectionState, string> = {
  [signalR.HubConnectionState.Connected]: 'bg-emerald-500',
  [signalR.HubConnectionState.Connecting]: 'bg-amber-400 animate-pulse',
  [signalR.HubConnectionState.Reconnecting]: 'bg-amber-400 animate-pulse',
  [signalR.HubConnectionState.Disconnected]: 'bg-slate-300',
  [signalR.HubConnectionState.Disconnecting]: 'bg-slate-300',
};

export default function ConnectionStatus() {
  const { connectionState, connectionError } = useSensorNotificationsContext();
  const label = labels[connectionState] ?? 'Unknown';

  return (
    <div
      className="mt-auto rounded-xl border border-slate-100 bg-slate-50 px-3 py-2.5"
      title={connectionError ?? undefined}
    >
      <div className="flex items-center gap-2">
        <span className={`h-2 w-2 shrink-0 rounded-full ${dotClass[connectionState]}`} />
        <span className="text-xs font-medium text-slate-600">{label}</span>
      </div>
      {connectionError && (
        <p className="mt-1 truncate text-[10px] text-red-500">{connectionError}</p>
      )}
    </div>
  );
}
