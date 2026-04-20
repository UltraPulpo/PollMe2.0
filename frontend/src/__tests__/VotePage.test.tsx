/**
 * Unit tests for src/pages/VotePage.tsx
 *
 * Renders via MemoryRouter + Route so that useParams gets the right pollId.
 * useNavigate is mocked so we can assert navigation without an actual history.
 * The api module is mocked with jest.requireActual to preserve ApiError.
 */

import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { ErrorProvider } from '../context/ErrorContext';
import VotePage from '../pages/VotePage';
import { ApiError } from '../api';
import * as api from '../api';

// Keep mockNavigate accessible in test assertions.
// The arrow function in the factory is called at runtime (after module init),
// so mockNavigate is already initialised by then.
const mockNavigate = jest.fn();

jest.mock('react-router-dom', () => ({
  ...jest.requireActual('react-router-dom'),
  useNavigate: () => mockNavigate,
}));

jest.mock('../api', () => ({
  ...jest.requireActual('../api'),
  getPoll: jest.fn(),
  submitVote: jest.fn(),
}));

const mockGetPoll = api.getPoll as jest.MockedFunction<typeof api.getPoll>;
const mockSubmitVote = api.submitVote as jest.MockedFunction<typeof api.submitVote>;

const POLL_ID = 'poll-1';

const mockPoll = {
  id: POLL_ID,
  title: 'Favourite colour?',
  pollType: 'SingleChoice' as const,
  isActive: true,
  options: [
    { id: 'opt-1', text: 'Red' },
    { id: 'opt-2', text: 'Blue' },
  ],
  createdAtUtc: '2024-01-01T00:00:00Z',
};

function renderVotePage(pollId = POLL_ID) {
  return render(
    <MemoryRouter initialEntries={[`/poll/${pollId}`]}>
      <ErrorProvider>
        <Routes>
          <Route path="/poll/:pollId" element={<VotePage />} />
        </Routes>
      </ErrorProvider>
    </MemoryRouter>,
  );
}

beforeEach(() => {
  jest.resetAllMocks();
});

describe('VotePage', () => {
  it('shows loading indicator while fetching', () => {
    // Never resolves — stays in loading state
    mockGetPoll.mockReturnValueOnce(new Promise(() => {}));
    renderVotePage();
    expect(screen.getByText('Loading poll...')).toBeInTheDocument();
  });

  it('renders poll title and options after fetch', async () => {
    mockGetPoll.mockResolvedValueOnce(mockPoll);
    renderVotePage();

    await waitFor(() =>
      expect(screen.getByRole('heading', { name: 'Favourite colour?' })).toBeInTheDocument(),
    );
    expect(screen.getByLabelText('Red')).toBeInTheDocument();
    expect(screen.getByLabelText('Blue')).toBeInTheDocument();
  });

  it('shows Not Found state on 404', async () => {
    mockGetPoll.mockRejectedValueOnce(new ApiError(404, 'Poll not found'));
    renderVotePage();

    await waitFor(() =>
      expect(screen.getByRole('heading', { name: 'Poll Not Found' })).toBeInTheDocument(),
    );
  });

  it('Vote button is disabled until an option is selected', async () => {
    mockGetPoll.mockResolvedValueOnce(mockPoll);
    renderVotePage();

    await waitFor(() => screen.getByLabelText('Red'));

    expect(screen.getByRole('button', { name: 'Vote' })).toBeDisabled();
    fireEvent.click(screen.getByLabelText('Red'));
    expect(screen.getByRole('button', { name: 'Vote' })).not.toBeDisabled();
  });

  it('submits vote with correct option and navigates to results', async () => {
    mockGetPoll.mockResolvedValueOnce(mockPoll);
    mockSubmitVote.mockResolvedValueOnce(undefined);
    renderVotePage();

    await waitFor(() => screen.getByLabelText('Blue'));

    fireEvent.click(screen.getByLabelText('Blue'));
    fireEvent.click(screen.getByRole('button', { name: 'Vote' }));

    await waitFor(() => {
      expect(mockSubmitVote).toHaveBeenCalledWith(POLL_ID, { optionIds: ['opt-2'] });
      expect(mockNavigate).toHaveBeenCalledWith(`/poll/${POLL_ID}/results`);
    });
  });

  it('shows Already Voted state when API returns 409', async () => {
    mockGetPoll.mockResolvedValueOnce(mockPoll);
    mockSubmitVote.mockRejectedValueOnce(new ApiError(409, 'Already voted'));
    renderVotePage();

    await waitFor(() => screen.getByLabelText('Red'));

    fireEvent.click(screen.getByLabelText('Red'));
    fireEvent.click(screen.getByRole('button', { name: 'Vote' }));

    await waitFor(() =>
      expect(screen.getByRole('heading', { name: 'Already Voted' })).toBeInTheDocument(),
    );
  });

  it('shows inline error message for non-409 vote errors', async () => {
    mockGetPoll.mockResolvedValueOnce(mockPoll);
    mockSubmitVote.mockRejectedValueOnce(new ApiError(400, 'Poll is inactive'));
    renderVotePage();

    await waitFor(() => screen.getByLabelText('Red'));

    fireEvent.click(screen.getByLabelText('Red'));
    fireEvent.click(screen.getByRole('button', { name: 'Vote' }));

    await waitFor(() =>
      expect(screen.getByRole('alert')).toHaveTextContent('Error 400: Poll is inactive'),
    );
  });

  it('shows inactive poll message when poll is not active', async () => {
    mockGetPoll.mockResolvedValueOnce({ ...mockPoll, isActive: false });
    renderVotePage();

    await waitFor(() =>
      expect(screen.getByText('This poll is no longer accepting votes.')).toBeInTheDocument(),
    );
  });
});
