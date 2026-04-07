import { useEffect, useRef, useCallback } from 'react';
import { HubConnectionBuilder } from '@microsoft/signalr';
import type { HubConnection } from '@microsoft/signalr';
import type { PollResultsResponse } from '../types';

export function useSignalR(
  pollId: string | undefined,
  onResultsUpdated: (results: PollResultsResponse) => void,
) {
  const connectionRef = useRef<HubConnection | null>(null);
  const callbackRef = useRef(onResultsUpdated);
  callbackRef.current = onResultsUpdated;

  const stableCallback = useCallback((results: PollResultsResponse) => {
    callbackRef.current(results);
  }, []);

  useEffect(() => {
    if (!pollId) return;

    const connection = new HubConnectionBuilder()
      .withUrl('/hubs/poll')
      .withAutomaticReconnect()
      .build();

    connectionRef.current = connection;

    connection.on('ResultsUpdated', stableCallback);

    connection
      .start()
      .then(() => connection.invoke('JoinPoll', pollId))
      .catch((err) => console.error('SignalR connection error:', err));

    return () => {
      connection
        .invoke('LeavePoll', pollId)
        .catch(() => {})
        .finally(() => connection.stop());
    };
  }, [pollId, stableCallback]);

  return connectionRef;
}
