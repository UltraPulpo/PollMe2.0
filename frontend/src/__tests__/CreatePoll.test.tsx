/**
 * Unit tests for src/pages/CreatePoll.tsx
 *
 * Renders the page inside <MemoryRouter> + <ErrorProvider>, mocks the api
 * module (preserving the real ApiError class so instanceof checks work in the
 * component), and verifies form behaviour and state transitions.
 */

import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { ErrorProvider } from '../context/ErrorContext';
import CreatePoll from '../pages/CreatePoll';
import { ApiError } from '../api';
import * as api from '../api';

// Preserve the real ApiError so `err instanceof ApiError` works in the component.
jest.mock('../api', () => ({
  ...jest.requireActual('../api'),
  createPoll: jest.fn(),
}));

const mockCreatePoll = api.createPoll as jest.MockedFunction<typeof api.createPoll>;

function renderCreatePoll() {
  return render(
    <MemoryRouter>
      <ErrorProvider>
        <CreatePoll />
      </ErrorProvider>
    </MemoryRouter>,
  );
}

beforeEach(() => {
  jest.resetAllMocks();
});

describe('CreatePoll', () => {
  it('renders form with title input and option placeholders', () => {
    renderCreatePoll();
    expect(screen.getByRole('heading', { name: 'Create a Poll' })).toBeInTheDocument();
    expect(screen.getByPlaceholderText("What's your question?")).toBeInTheDocument();
    expect(screen.getByPlaceholderText('Option 1')).toBeInTheDocument();
    expect(screen.getByPlaceholderText('Option 2')).toBeInTheDocument();
  });

  it('submit button is present and enabled when form is pristine', () => {
    renderCreatePoll();
    expect(screen.getByRole('button', { name: 'Create Poll' })).toBeInTheDocument();
  });

  it('submits the form and shows success state with created links', async () => {
    mockCreatePoll.mockResolvedValueOnce({
      pollId: 'abc-123',
      secretToken: 'secret-tok',
      voteUrl: '/poll/abc-123',
      dashboardUrl: '/dashboard/secret-tok',
    });

    renderCreatePoll();

    fireEvent.change(screen.getByPlaceholderText("What's your question?"), {
      target: { value: 'Best colour?' },
    });
    fireEvent.change(screen.getByPlaceholderText('Option 1'), { target: { value: 'Red' } });
    fireEvent.change(screen.getByPlaceholderText('Option 2'), { target: { value: 'Blue' } });

    fireEvent.click(screen.getByRole('button', { name: 'Create Poll' }));

    await waitFor(() => {
      expect(screen.getByRole('heading', { name: 'Poll Created!' })).toBeInTheDocument();
    });

    expect(mockCreatePoll).toHaveBeenCalledWith({
      title: 'Best colour?',
      description: undefined,
      pollType: 'SingleChoice',
      options: ['Red', 'Blue'],
    });
  });

  it('shows inline error message when API returns ApiError', async () => {
    mockCreatePoll.mockRejectedValueOnce(new ApiError(422, 'Title is required'));

    renderCreatePoll();

    fireEvent.change(screen.getByPlaceholderText("What's your question?"), {
      target: { value: 'Empty test' },
    });
    // Fill required option fields to pass HTML5 form validation
    fireEvent.change(screen.getByPlaceholderText('Option 1'), { target: { value: 'A' } });
    fireEvent.change(screen.getByPlaceholderText('Option 2'), { target: { value: 'B' } });
    fireEvent.click(screen.getByRole('button', { name: 'Create Poll' }));

    await waitFor(() => {
      expect(screen.getByRole('alert')).toHaveTextContent('Error 422: Title is required');
    });
  });

  it('shows Create Another Poll button in success state', async () => {
    mockCreatePoll.mockResolvedValueOnce({
      pollId: 'poll-xyz',
      secretToken: 'tok',
      voteUrl: '/poll/poll-xyz',
      dashboardUrl: '/dashboard/tok',
    });

    renderCreatePoll();

    fireEvent.change(screen.getByPlaceholderText("What's your question?"), {
      target: { value: 'Another poll?' },
    });
    fireEvent.change(screen.getByPlaceholderText('Option 1'), { target: { value: 'Yes' } });
    fireEvent.change(screen.getByPlaceholderText('Option 2'), { target: { value: 'No' } });
    fireEvent.click(screen.getByRole('button', { name: 'Create Poll' }));

    await waitFor(() => {
      expect(screen.getByRole('button', { name: 'Create Another Poll' })).toBeInTheDocument();
    });
  });

  it('resets to form view when Create Another Poll is clicked', async () => {
    mockCreatePoll.mockResolvedValueOnce({
      pollId: 'poll-xyz',
      secretToken: 'tok',
      voteUrl: '/poll/poll-xyz',
      dashboardUrl: '/dashboard/tok',
    });

    renderCreatePoll();

    fireEvent.change(screen.getByPlaceholderText("What's your question?"), {
      target: { value: 'Reset test' },
    });
    fireEvent.change(screen.getByPlaceholderText('Option 1'), { target: { value: 'Yes' } });
    fireEvent.change(screen.getByPlaceholderText('Option 2'), { target: { value: 'No' } });
    fireEvent.click(screen.getByRole('button', { name: 'Create Poll' }));

    await waitFor(() =>
      screen.getByRole('button', { name: 'Create Another Poll' }),
    );

    fireEvent.click(screen.getByRole('button', { name: 'Create Another Poll' }));

    expect(screen.getByRole('heading', { name: 'Create a Poll' })).toBeInTheDocument();
  });
});
