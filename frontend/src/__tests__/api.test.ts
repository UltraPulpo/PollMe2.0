/**
 * Unit tests for src/api.ts
 *
 * Strategy: mock global.fetch — every exported function is tested for:
 *   • happy-path: returns correctly typed data on ok:true
 *   • error-path: throws ApiError with the right status on ok:false
 */

import {
  createPoll,
  getPoll,
  submitVote,
  getResults,
  getCreatorPolls,
  togglePollActive,
  deletePoll,
  ApiError,
} from '../api';

const mockFetch = jest.fn();
// In jsdom (the test environment), window.fetch is the global fetch
window.fetch = mockFetch as unknown as typeof fetch;

beforeEach(() => {
  jest.resetAllMocks();
});

// ── Helpers ───────────────────────────────────────────────────────────────────

function okJson(body: unknown) {
  return { ok: true, json: () => Promise.resolve(body) };
}

function okNoContent() {
  return { ok: true };
}

function errorResponse(status: number, text = 'Error') {
  return { ok: false, status, text: () => Promise.resolve(text) };
}

// ── ApiError ──────────────────────────────────────────────────────────────────

describe('ApiError', () => {
  it('stores status and message, extends Error', () => {
    const err = new ApiError(404, 'Not Found');
    expect(err.status).toBe(404);
    expect(err.message).toBe('Not Found');
    expect(err).toBeInstanceOf(Error);
  });
});

// ── createPoll ────────────────────────────────────────────────────────────────

describe('createPoll', () => {
  it('POSTs to /api/polls and returns CreatePollResponse', async () => {
    const payload = {
      pollId: 'abc-123',
      secretToken: 'tok',
      voteUrl: '/poll/abc-123',
      dashboardUrl: '/dashboard/tok',
    };
    mockFetch.mockResolvedValueOnce(okJson(payload));

    const result = await createPoll({ title: 'Test', pollType: 'SingleChoice', options: ['A', 'B'] });

    expect(result).toEqual(payload);
    expect(mockFetch).toHaveBeenCalledWith('/api/polls', expect.objectContaining({ method: 'POST' }));
  });

  it('throws ApiError on 400', async () => {
    mockFetch.mockResolvedValueOnce(errorResponse(400, 'Validation failed'));
    await expect(
      createPoll({ title: '', pollType: 'SingleChoice', options: [] }),
    ).rejects.toMatchObject({ status: 400, message: 'Validation failed' });
  });
});

// ── getPoll ───────────────────────────────────────────────────────────────────

describe('getPoll', () => {
  it('GETs /api/polls/:id and returns PollResponse', async () => {
    const payload = {
      id: 'poll-1',
      title: 'My Poll',
      pollType: 'SingleChoice',
      isActive: true,
      options: [{ id: 'opt-1', text: 'Yes' }],
      createdAtUtc: '2024-01-01T00:00:00Z',
    };
    mockFetch.mockResolvedValueOnce(okJson(payload));

    const result = await getPoll('poll-1');
    expect(result).toEqual(payload);
    expect(mockFetch).toHaveBeenCalledWith('/api/polls/poll-1', expect.any(Object));
  });

  it('throws ApiError on 404', async () => {
    mockFetch.mockResolvedValueOnce(errorResponse(404, 'Not found'));
    await expect(getPoll('nope')).rejects.toBeInstanceOf(ApiError);
  });
});

// ── submitVote ────────────────────────────────────────────────────────────────

describe('submitVote', () => {
  it('POSTs to /api/polls/:id/vote and resolves to undefined', async () => {
    mockFetch.mockResolvedValueOnce(okNoContent());
    await expect(submitVote('poll-1', { optionIds: ['opt-1'] })).resolves.toBeUndefined();
    expect(mockFetch).toHaveBeenCalledWith(
      '/api/polls/poll-1/vote',
      expect.objectContaining({ method: 'POST' }),
    );
  });

  it('throws ApiError(409) on duplicate vote', async () => {
    mockFetch.mockResolvedValueOnce(errorResponse(409, 'Already voted'));
    await expect(submitVote('poll-1', { optionIds: ['opt-1'] })).rejects.toMatchObject({ status: 409 });
  });
});

// ── getResults ────────────────────────────────────────────────────────────────

describe('getResults', () => {
  it('GETs /api/polls/:id/results and returns PollResultsResponse', async () => {
    const payload = {
      pollId: 'poll-1',
      title: 'My Poll',
      totalVotes: 5,
      options: [{ id: 'opt-1', text: 'Yes', voteCount: 5, percentage: 100 }],
    };
    mockFetch.mockResolvedValueOnce(okJson(payload));
    const result = await getResults('poll-1');
    expect(result).toEqual(payload);
  });
});

// ── getCreatorPolls ───────────────────────────────────────────────────────────

describe('getCreatorPolls', () => {
  it('GETs /api/creator/:token/polls and returns array', async () => {
    const payload = [
      {
        id: 'poll-1',
        title: 'My Poll',
        pollType: 'SingleChoice',
        totalVotes: 0,
        isActive: true,
        createdAtUtc: '2024-01-01T00:00:00Z',
      },
    ];
    mockFetch.mockResolvedValueOnce(okJson(payload));

    const result = await getCreatorPolls('my-secret');
    expect(result).toEqual(payload);
    expect(mockFetch).toHaveBeenCalledWith('/api/creator/my-secret/polls', expect.any(Object));
  });
});

// ── togglePollActive ──────────────────────────────────────────────────────────

describe('togglePollActive', () => {
  it('PATCHes /api/polls/:id and returns updated PollResponse', async () => {
    const payload = {
      id: 'poll-1',
      title: 'My Poll',
      pollType: 'SingleChoice',
      isActive: false,
      options: [],
      createdAtUtc: '2024-01-01T00:00:00Z',
    };
    mockFetch.mockResolvedValueOnce(okJson(payload));

    const result = await togglePollActive('poll-1');
    expect(result.isActive).toBe(false);
    expect(mockFetch).toHaveBeenCalledWith(
      '/api/polls/poll-1',
      expect.objectContaining({ method: 'PATCH' }),
    );
  });
});

// ── deletePoll ────────────────────────────────────────────────────────────────

describe('deletePoll', () => {
  it('DELETEs /api/polls/:id and resolves to undefined', async () => {
    mockFetch.mockResolvedValueOnce(okNoContent());
    await expect(deletePoll('poll-1')).resolves.toBeUndefined();
    expect(mockFetch).toHaveBeenCalledWith(
      '/api/polls/poll-1',
      expect.objectContaining({ method: 'DELETE' }),
    );
  });

  it('throws ApiError(403) on forbidden', async () => {
    mockFetch.mockResolvedValueOnce(errorResponse(403, 'Forbidden'));
    await expect(deletePoll('poll-1')).rejects.toMatchObject({ status: 403 });
  });
});
