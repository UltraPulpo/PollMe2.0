/**
 * Unit tests for src/pages/ResultsPage.tsx
 *
 * useSignalR is mocked (no-op) so we test the static render path.
 * getResults is mocked via jest.requireActual so ApiError is real.
 */

import { render, screen, waitFor } from '@testing-library/react';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { ErrorProvider } from '../context/ErrorContext';
import ResultsPage from '../pages/ResultsPage';
import { ApiError } from '../api';
import * as api from '../api';

jest.mock('../api', () => ({
  ...jest.requireActual('../api'),
  getResults: jest.fn(),
}));

// Silence the real SignalR connection in tests
jest.mock('../hooks/useSignalR', () => ({
  useSignalR: jest.fn(),
}));

const mockGetResults = api.getResults as jest.MockedFunction<typeof api.getResults>;

const POLL_ID = 'poll-42';

const mockResults = {
  pollId: POLL_ID,
  title: 'Best Colour?',
  totalVotes: 3,
  options: [
    { id: 'opt-1', text: 'Red', voteCount: 2, percentage: 67 },
    { id: 'opt-2', text: 'Blue', voteCount: 1, percentage: 33 },
  ],
};

function renderResultsPage(pollId = POLL_ID) {
  return render(
    <MemoryRouter initialEntries={[`/poll/${pollId}/results`]}>
      <ErrorProvider>
        <Routes>
          <Route path="/poll/:pollId/results" element={<ResultsPage />} />
        </Routes>
      </ErrorProvider>
    </MemoryRouter>,
  );
}

beforeEach(() => {
  jest.resetAllMocks();
});

describe('ResultsPage', () => {
  it('shows loading indicator while fetching', () => {
    mockGetResults.mockReturnValueOnce(new Promise(() => {}));
    renderResultsPage();
    expect(screen.getByText('Loading results...')).toBeInTheDocument();
  });

  it('renders poll title and option results after fetch', async () => {
    mockGetResults.mockResolvedValueOnce(mockResults);
    renderResultsPage();

    await waitFor(() =>
      expect(screen.getByRole('heading', { name: 'Best Colour?' })).toBeInTheDocument(),
    );

    expect(screen.getByText('Red')).toBeInTheDocument();
    expect(screen.getByText('Blue')).toBeInTheDocument();
    // total votes count appears in bold
    expect(screen.getByText('3')).toBeInTheDocument();
  });

  it('renders a result bar for each option', async () => {
    mockGetResults.mockResolvedValueOnce(mockResults);
    renderResultsPage();

    await waitFor(() => screen.getByText('Red'));

    // Each ResultsBar renders vote count + percentage text
    expect(screen.getByText('2 votes (67%)')).toBeInTheDocument();
    expect(screen.getByText('1 vote (33%)')).toBeInTheDocument();
  });

  it('shows error message when API returns 404', async () => {
    mockGetResults.mockRejectedValueOnce(new ApiError(404, 'Not found'));
    renderResultsPage();

    await waitFor(() =>
      expect(screen.getByText('Poll not found.')).toBeInTheDocument(),
    );
  });

  it('calls getResults with the pollId from the route', async () => {
    mockGetResults.mockResolvedValueOnce(mockResults);
    renderResultsPage(POLL_ID);

    await waitFor(() => screen.getByRole('heading', { name: 'Best Colour?' }));

    expect(mockGetResults).toHaveBeenCalledWith(POLL_ID);
  });
});
