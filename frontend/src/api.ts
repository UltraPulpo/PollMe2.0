import type {
  CreatePollRequest,
  CreatePollResponse,
  PollResponse,
  PollResultsResponse,
  VoteRequest,
  CreatorPollSummary,
} from './types';

export class ApiError extends Error {
  status: number;
  constructor(status: number, message: string) {
    super(message);
    this.status = status;
  }
}

async function request<T>(url: string, options?: RequestInit): Promise<T> {
  const res = await fetch(url, {
    ...options,
    headers: { 'Content-Type': 'application/json', ...options?.headers },
  });
  if (!res.ok) {
    const text = await res.text();
    throw new ApiError(res.status, text);
  }
  return res.json();
}

async function requestNoContent(url: string, options?: RequestInit): Promise<void> {
  const res = await fetch(url, {
    ...options,
    headers: { 'Content-Type': 'application/json', ...options?.headers },
  });
  if (!res.ok) {
    const text = await res.text();
    throw new ApiError(res.status, text);
  }
}

export const createPoll = (data: CreatePollRequest) =>
  request<CreatePollResponse>('/api/polls', {
    method: 'POST',
    body: JSON.stringify(data),
  });

export const getPoll = (pollId: string) =>
  request<PollResponse>(`/api/polls/${encodeURIComponent(pollId)}`);

export const submitVote = (pollId: string, data: VoteRequest) =>
  requestNoContent(`/api/polls/${encodeURIComponent(pollId)}/vote`, {
    method: 'POST',
    body: JSON.stringify(data),
  });

export const getResults = (pollId: string) =>
  request<PollResultsResponse>(`/api/polls/${encodeURIComponent(pollId)}/results`);

export const getCreatorPolls = (secretToken: string) =>
  request<CreatorPollSummary[]>(`/api/creator/${encodeURIComponent(secretToken)}/polls`);

export const togglePollActive = (pollId: string) =>
  request<PollResponse>(`/api/polls/${encodeURIComponent(pollId)}`, {
    method: 'PATCH',
  });

export const deletePoll = (pollId: string) =>
  requestNoContent(`/api/polls/${encodeURIComponent(pollId)}`, {
    method: 'DELETE',
  });
