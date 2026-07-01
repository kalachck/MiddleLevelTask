import * as signalR from '@microsoft/signalr';
import { useEffect, useRef, useState } from 'react';
import type {
  AirQualityProcessedPayload,
  EnergyProcessedPayload,
  MotionProcessedPayload,
  SensorRealtimeEvent,
} from '../types/sensorRealtime';
import { env } from '../config/env';

const MAX_BUFFER = 200;

export interface UseSensorNotificationsResult {
  connectionState: signalR.HubConnectionState;
  connectionError: string | null;
  /** Newest-last batches ( capped ) for inspection / future UI. */
  energyEvents: EnergyProcessedPayload[];
  motionEvents: MotionProcessedPayload[];
  airQualityEvents: AirQualityProcessedPayload[];
  /** All channels, interleaved in receive order (capped). */
  allEvents: SensorRealtimeEvent[];
}

function appendCapped<T>(prev: T[], item: T): T[] {
  const next = [...prev, item];
  return next.length > MAX_BUFFER ? next.slice(-MAX_BUFFER) : next;
}

export function useSensorNotifications(
  hubUrl: string = env.signalrHubUrl,
  hubKey: string = env.signalrHubKey,
): UseSensorNotificationsResult {
  const [connectionState, setConnectionState] = useState(signalR.HubConnectionState.Disconnected);
  const [connectionError, setConnectionError] = useState<string | null>(null);
  const [energyEvents, setEnergyEvents] = useState<EnergyProcessedPayload[]>([]);
  const [motionEvents, setMotionEvents] = useState<MotionProcessedPayload[]>([]);
  const [airQualityEvents, setAirQualityEvents] = useState<AirQualityProcessedPayload[]>([]);
  const [allEvents, setAllEvents] = useState<SensorRealtimeEvent[]>([]);
  const hubKeyRef = useRef(hubKey);

  useEffect(() => {
    hubKeyRef.current = hubKey;
  }, [hubKey]);

  useEffect(() => {
    const connection = new signalR.HubConnectionBuilder()
      .withUrl(hubUrl, {
        accessTokenFactory: () => hubKeyRef.current,
      })
      .withAutomaticReconnect()
      .build();

    setConnectionState(connection.state);

    connection.onreconnecting(() => {
      setConnectionState(connection.state);
      setConnectionError(null);
    });

    connection.onreconnected(() => {
      setConnectionState(connection.state);
      setConnectionError(null);
    });

    connection.onclose((err) => {
      setConnectionState(connection.state);
      if (err) setConnectionError(err.message);
    });

    connection.on('NotifyEnergyProcessed', (payload: EnergyProcessedPayload) => {
      setEnergyEvents((p) => appendCapped(p, payload));
      setAllEvents((p) => appendCapped(p, { kind: 'energy', payload }));
    });

    connection.on('NotifyMotionProcessed', (payload: MotionProcessedPayload) => {
      setMotionEvents((p) => appendCapped(p, payload));
      setAllEvents((p) => appendCapped(p, { kind: 'motion', payload }));
    });

    connection.on('NotifyAirQualityProcessed', (payload: AirQualityProcessedPayload) => {
      setAirQualityEvents((p) => appendCapped(p, payload));
      setAllEvents((p) => appendCapped(p, { kind: 'airQuality', payload }));
    });

    let cancelled = false;

    void (async () => {
      try {
        setConnectionError(null);
        await connection.start();
        if (!cancelled) setConnectionState(connection.state);
      } catch (e) {
        if (!cancelled) {
          setConnectionError(e instanceof Error ? e.message : String(e));
          setConnectionState(connection.state);
        }
      }
    })();

    return () => {
      cancelled = true;
      void connection.stop();
    };
  }, [hubUrl]);

  return {
    connectionState,
    connectionError,
    energyEvents,
    motionEvents,
    airQualityEvents,
    allEvents,
  };
}
