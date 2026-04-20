import { useEffect, useState } from 'react';
import type { FormEvent } from 'react';
import { useParams, useNavigate, Link } from 'react-router-dom';
import { getPoll, submitVote, ApiError } from '../api';
import type { PollResponse } from '../types';
import { useError } from '../context/ErrorContext';

export default function VotePage() {
  const { pollId } = useParams<{ pollId: string }>();
  const navigate = useNavigate();
  const [poll, setPoll] = useState<PollResponse | null>(null);
  const [loading, setLoading] = useState(true);
  const [selected, setSelected] = useState<string[]>([]);
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState('');
  const [alreadyVoted, setAlreadyVoted] = useState(false);
  const [notFound, setNotFound] = useState(false);
  const { setGlobalError } = useError();

  useEffect(() => {
    if (!pollId) return;
    setLoading(true);
    getPoll(pollId)
      .then(setPoll)
      .catch((err) => {
        if (err instanceof ApiError && err.status === 404) {
          setNotFound(true);
        } else {
          setGlobalError('Failed to load poll. Please try again.');
        }
      })
      .finally(() => setLoading(false));
  }, [pollId]);

  const handleToggle = (optionId: string) => {
    if (!poll) return;
    if (poll.pollType === 'SingleChoice') {
      setSelected([optionId]);
    } else {
      setSelected((prev) =>
        prev.includes(optionId) ? prev.filter((id) => id !== optionId) : [...prev, optionId],
      );
    }
  };

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault();
    if (!pollId || selected.length === 0) return;
    setError('');
    setSubmitting(true);

    try {
      await submitVote(pollId, { optionIds: selected });
      navigate(`/poll/${pollId}/results`);
    } catch (err) {
      if (err instanceof ApiError && err.status === 409) {
        setAlreadyVoted(true);
      } else if (err instanceof ApiError) {
        setError(`Error ${err.status}: ${err.message}`);
      } else {
        setGlobalError('An unexpected error occurred. Please try again.');
      }
    } finally {
      setSubmitting(false);
    }
  };

  if (loading) {
    return (
      <main className="container">
        <article aria-busy="true">Loading poll...</article>
      </main>
    );
  }

  if (notFound) {
    return (
      <main className="container">
        <article>
          <h1>Poll Not Found</h1>
          <p>This poll doesn't exist or has been deleted.</p>
          <Link to="/" role="button">
            Create a Poll
          </Link>
        </article>
      </main>
    );
  }

  if (alreadyVoted) {
    return (
      <main className="container">
        <article>
          <h1>Already Voted</h1>
          <p>You have already voted on this poll.</p>
          <Link to={`/poll/${pollId}/results`} role="button">
            View Results
          </Link>
        </article>
      </main>
    );
  }

  if (!poll) return null;

  if (!poll.isActive) {
    return (
      <main className="container">
        <article>
          <h1>{poll.title}</h1>
          <p>This poll is no longer accepting votes.</p>
          <Link to={`/poll/${pollId}/results`} role="button">
            View Results
          </Link>
        </article>
      </main>
    );
  }

  return (
    <main className="container">
      <article>
        <header>
          <h1>{poll.title}</h1>
          {poll.description && <p>{poll.description}</p>}
          <small>
            {poll.pollType === 'SingleChoice' ? 'Choose one option' : 'Choose one or more options'}
          </small>
        </header>

        <form onSubmit={handleSubmit}>
          {poll.options.map((option) => (
            <label key={option.id}>
              <input
                type={poll.pollType === 'SingleChoice' ? 'radio' : 'checkbox'}
                name="vote"
                value={option.id}
                checked={selected.includes(option.id)}
                onChange={() => handleToggle(option.id)}
              />
              {option.text}
            </label>
          ))}

          {error && (
            <p role="alert" style={{ color: 'var(--pico-del-color)' }}>
              {error}
            </p>
          )}

          <button type="submit" disabled={submitting || selected.length === 0} aria-busy={submitting}>
            {submitting ? 'Submitting...' : 'Vote'}
          </button>
        </form>

        <footer>
          <small>
            <Link to={`/poll/${pollId}/results`}>View results without voting</Link>
          </small>
        </footer>
      </article>
    </main>
  );
}
