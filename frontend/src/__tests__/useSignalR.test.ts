/**
 * Unit tests for src/hooks/useSignalR.ts
 *
 * @microsoft/signalr is mocked so no real WebSocket is opened.
 * The HubConnectionBuilder mock is configured in beforeEach so that
 * mockOn / mockStart / mockInvoke / mockStop are accessible in assertions.
 */

import { renderHook, act } from '@testing-library/react';
import { HubConnectionBuilder } from '@microsoft/signalr';
import { useSignalR } from '../hooks/useSignalR';
import type { PollResultsResponse } from '../types';

// Provide a simple stub — HubConnectionBuilder.mockImplementation fills in the
// per-test behaviour in beforeEach.
jest.mock('@microsoft/signalr', () => ({
  HubConnectionBuilder: jest.fn(),
}));

// Per-test mock handles — reset in beforeEach
const mockOn = jest.fn();
const mockStart = jest.fn();
const mockInvoke = jest.fn();
const mockStop = jest.fn();

beforeEach(() => {
  jest.clearAllMocks();
  mockStart.mockResolvedValue(undefined);
  mockInvoke.mockResolvedValue(undefined);
  mockStop.mockResolvedValue(undefined);

  (HubConnectionBuilder as jest.Mock).mockImplementation(() => ({
    withUrl: jest.fn().mockReturnThis(),
    withAutomaticReconnect: jest.fn().mockReturnThis(),
    build: jest.fn().mockReturnValue({
      on: mockOn,
      start: mockStart,
      invoke: mockInvoke,
      stop: mockStop,
    }),
  }));
});

describe('useSignalR', () => {
  it('does nothing when pollId is undefined', () => {
    renderHook(() => useSignalR(undefined, jest.fn()));
    expect(HubConnectionBuilder).not.toHaveBeenCalled();
    expect(mockStart).not.toHaveBeenCalled();
  });

  it('builds and starts a connection when pollId is provided', async () => {
    await act(async () => {
      renderHook(() => useSignalR('poll-1', jest.fn()));
    });
    expect(HubConnectionBuilder).toHaveBeenCalled();
    expect(mockStart).toHaveBeenCalled();
  });

  it('invokes JoinPoll after connection starts', async () => {
    await act(async () => {
      renderHook(() => useSignalR('poll-1', jest.fn()));
    });
    expect(mockInvoke).toHaveBeenCalledWith('JoinPoll', 'poll-1');
  });

  it('registers a ResultsUpdated listener on the connection', async () => {
    await act(async () => {
      renderHook(() => useSignalR('poll-1', jest.fn()));
    });
    expect(mockOn).toHaveBeenCalledWith('ResultsUpdated', expect.any(Function));
  });

  it('calls the onResultsUpdated callback when ResultsUpdated fires', async () => {
    const onUpdate = jest.fn();

    await act(async () => {
      renderHook(() => useSignalR('poll-1', onUpdate));
    });

    // Retrieve the listener registered with connection.on('ResultsUpdated', ...)
    const listenerCall = (mockOn.mock.calls as [string, (r: PollResultsResponse) => void][]).find(
      ([event]) => event === 'ResultsUpdated',
    );
    expect(listenerCall).toBeDefined();
    const listener = listenerCall![1];

    const results: PollResultsResponse = {
      pollId: 'poll-1',
      title: 'Test Poll',
      totalVotes: 2,
      options: [],
    };

    act(() => {
      listener(results);
    });

    expect(onUpdate).toHaveBeenCalledWith(results);
  });

  it('invokes LeavePoll and stops connection on unmount', async () => {
    let unmount!: () => void;

    await act(async () => {
      ({ unmount } = renderHook(() => useSignalR('poll-1', jest.fn())));
    });

    await act(async () => {
      unmount();
      // Flush the .finally(() => connection.stop()) microtask chain
      await Promise.resolve();
      await Promise.resolve();
    });

    expect(mockInvoke).toHaveBeenCalledWith('LeavePoll', 'poll-1');
    expect(mockStop).toHaveBeenCalled();
  });
});
