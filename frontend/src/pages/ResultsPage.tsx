import { useEffect, useState } from 'react';
import { useParams, Link } from 'react-router-dom';
import { getResults, ApiError } from '../api';
import type { PollResultsResponse } from '../types';
import { useSignalR } from '../hooks/useSignalR';
import ResultsBar from '../components/ResultsBar';
import CopyLinkButton from '../components/CopyLinkButton';

export default function ResultsPage() {
  const { pollId } = useParams<{ pollId: string }>();
  const [results, setResults] = useState<PollResultsResponse | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');

  useEffect(() => {
    if (!pollId) return;
    setLoading(true);
    getResults(pollId)
      .then(setResults)
      .catch((err) => {
        if (err instanceof ApiError && err.status === 404) {
          setError('Poll not found.');
        } else {
          setError('Failed to load results.');
        }
      })
      .finally(() => setLoading(false));
  }, [pollId]);

  // Live updates via SignalR
  useSignalR(pollId, setResults);

  if (loading) {
    return (
      <main className="container">
        <article aria-busy="true">Loading results...</article>
      </main>
    );
  }

  if (error) {
    return (
      <main className="container">
        <article>
          <h1>Error</h1>
          <p>{error}</p>
          <Link to="/" role="button">
            Create a Poll
          </Link>
        </article>
      </main>
    );
  }

  if (!results) return null;

  const voteUrl = `${window.location.origin}/poll/${pollId}`;

  return (
    <main className="container">
      <article>
        <header>
          <h1>{results.title}</h1>
          <p>
            <strong>{results.totalVotes}</strong> total vote
            {results.totalVotes !== 1 ? 's' : ''}
          </p>
        </header>

        {results.options.map((option) => (
          <ResultsBar key={option.id} option={option} />
        ))}

        <footer>
          <div
            style={{ display: 'flex', gap: '0.5rem', flexWrap: 'wrap', alignItems: 'center' }}
          >
            <Link to={`/poll/${pollId}`} role="button" className="outline">
              Vote on this poll
            </Link>
            <CopyLinkButton url={voteUrl} label="Copy Vote Link" />
          </div>
          <small style={{ display: 'block', marginTop: '0.5rem' }}>
            Results update in real-time when new votes are cast.
          </small>
        </footer>
      </article>
    </main>
  );
}
