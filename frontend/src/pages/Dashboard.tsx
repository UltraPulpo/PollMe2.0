import { useEffect, useState } from 'react';
import { useParams, Link } from 'react-router-dom';
import { getCreatorPolls, togglePollActive, deletePoll, ApiError } from '../api';
import type { CreatorPollSummary } from '../types';
import CopyLinkButton from '../components/CopyLinkButton';
import { useError } from '../context/ErrorContext';

export default function Dashboard() {
  const { secretToken } = useParams<{ secretToken: string }>();
  const [polls, setPolls] = useState<CreatorPollSummary[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const { setGlobalError } = useError();

  useEffect(() => {
    if (!secretToken) return;
    setLoading(true);
    getCreatorPolls(secretToken)
      .then(setPolls)
      .catch((err) => {
        if (err instanceof ApiError && err.status === 404) {
          setError('Dashboard not found. Check your link.');
        } else {
          setGlobalError('Failed to load dashboard. Please try again.');
        }
      })
      .finally(() => setLoading(false));
  }, [secretToken]);

  const handleToggle = async (pollId: string) => {
    try {
      const updated = await togglePollActive(pollId);
      setPolls((prev) =>
        prev.map((p) => (p.id === pollId ? { ...p, isActive: updated.isActive } : p)),
      );
    } catch {
      setGlobalError('Failed to toggle poll status. Please try again.');
    }
  };

  const handleDelete = async (pollId: string, title: string) => {
    if (!confirm(`Are you sure you want to delete "${title}"? This cannot be undone.`)) return;
    try {
      await deletePoll(pollId);
      setPolls((prev) => prev.filter((p) => p.id !== pollId));
    } catch {
      setGlobalError('Failed to delete poll. Please try again.');
    }
  };

  if (loading) {
    return (
      <main className="container">
        <article aria-busy="true">Loading dashboard...</article>
      </main>
    );
  }

  if (error) {
    return (
      <main className="container">
        <article>
          <h1>Error</h1>
          <p>{error}</p>
        </article>
      </main>
    );
  }

  const baseUrl = window.location.origin;

  return (
    <main className="container">
      <h1>Your Polls</h1>
      <p>
        <Link to="/" role="button">
          + Create New Poll
        </Link>
      </p>

      {polls.length === 0 && <p>You haven't created any polls yet.</p>}

      {polls.map((poll) => (
        <article key={poll.id}>
          <header>
            <hgroup>
              <h2>{poll.title}</h2>
              <p>
                <span
                  style={{
                    display: 'inline-block',
                    padding: '0.1rem 0.5rem',
                    borderRadius: '4px',
                    fontSize: '0.8rem',
                    background: 'var(--pico-secondary-background)',
                    marginRight: '0.5rem',
                  }}
                >
                  {poll.pollType === 'SingleChoice' ? 'Single' : 'Multiple'}
                </span>
                <span>
                  {poll.totalVotes} vote{poll.totalVotes !== 1 ? 's' : ''}
                </span>
                {' · '}
                <span style={{ color: poll.isActive ? 'var(--pico-ins-color)' : 'var(--pico-del-color)' }}>
                  {poll.isActive ? 'Active' : 'Closed'}
                </span>
              </p>
            </hgroup>
          </header>

          <div style={{ display: 'flex', gap: '0.5rem', flexWrap: 'wrap' }}>
            <Link to={`/poll/${poll.id}/results`} role="button" className="outline">
              View Results
            </Link>
            <CopyLinkButton url={`${baseUrl}/poll/${poll.id}`} label="Copy Vote Link" />
            <button
              type="button"
              className={poll.isActive ? 'secondary' : 'contrast'}
              onClick={() => handleToggle(poll.id)}
            >
              {poll.isActive ? 'Close Poll' : 'Reopen Poll'}
            </button>
            <button
              type="button"
              className="secondary outline"
              style={{ color: 'var(--pico-del-color)', borderColor: 'var(--pico-del-color)' }}
              onClick={() => handleDelete(poll.id, poll.title)}
            >
              Delete
            </button>
          </div>
        </article>
      ))}
    </main>
  );
}
