// TypeScript types matching backend DTOs.
// Using string literals instead of enums due to erasableSyntaxOnly constraint.

export type PollType = 'SingleChoice' | 'MultipleChoice';

export interface CreatePollRequest {
  title: string;
  description?: string;
  pollType: PollType;
  options: string[];
}

export interface CreatePollResponse {
  pollId: string;
  secretToken: string;
  voteUrl: string;
  dashboardUrl: string;
}

export interface PollOptionResponse {
  id: string;
  text: string;
}

export interface PollResponse {
  id: string;
  title: string;
  description?: string;
  pollType: PollType;
  isActive: boolean;
  options: PollOptionResponse[];
  createdAtUtc: string;
}

export interface VoteRequest {
  optionIds: string[];
}

export interface PollOptionResultResponse {
  id: string;
  text: string;
  voteCount: number;
  percentage: number;
}

export interface PollResultsResponse {
  pollId: string;
  title: string;
  totalVotes: number;
  options: PollOptionResultResponse[];
}

export interface CreatorPollSummary {
  id: string;
  title: string;
  pollType: PollType;
  totalVotes: number;
  isActive: boolean;
  createdAtUtc: string;
}
