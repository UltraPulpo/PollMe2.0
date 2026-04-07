import { useState } from 'react';
import type { FormEvent } from 'react';
import { createPoll, ApiError } from '../api';
import type { PollType, CreatePollResponse } from '../types';
import OptionsList from '../components/OptionsList';
import CopyLinkButton from '../components/CopyLinkButton';

export default function CreatePoll() {
  const [title, setTitle] = useState('');
  const [description, setDescription] = useState('');
  const [pollType, setPollType] = useState<PollType>('SingleChoice');
  const [options, setOptions] = useState<string[]>(['', '']);
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState('');
  const [result, setResult] = useState<CreatePollResponse | null>(null);

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault();
    setError('');
    setSubmitting(true);

    try {
      const response = await createPoll({
        title: title.trim(),
        description: description.trim() || undefined,
        pollType,
        options: options.map((o) => o.trim()),
      });
      setResult(response);
    } catch (err) {
      if (err instanceof ApiError) {
        setError(`Error ${err.status}: ${err.message}`);
      } else {
        setError('An unexpected error occurred.');
      }
    } finally {
      setSubmitting(false);
    }
  };

  const baseUrl = window.location.origin;

  if (result) {
    const voteUrl = `${baseUrl}${result.voteUrl}`;
    const dashboardUrl = `${baseUrl}${result.dashboardUrl}`;
    const resultsUrl = `${baseUrl}/poll/${result.pollId}/results`;

    return (
      <main className="container">
        <article>
          <header>
            <h1>Poll Created!</h1>
          </header>
          <p>Your poll has been created successfully. Share the links below:</p>

          <h3>Vote Link</h3>
          <p>
            <code>{voteUrl}</code>
          </p>
          <CopyLinkButton url={voteUrl} label="Copy Vote Link" />

          <h3>Results Link</h3>
          <p>
            <code>{resultsUrl}</code>
          </p>
          <CopyLinkButton url={resultsUrl} label="Copy Results Link" />

          <h3>Dashboard Link</h3>
          <p>
            <small>Keep this link private — it controls your polls.</small>
          </p>
          <p>
            <code>{dashboardUrl}</code>
          </p>
          <CopyLinkButton url={dashboardUrl} label="Copy Dashboard Link" />

          <footer>
            <button type="button" onClick={() => setResult(null)}>
              Create Another Poll
            </button>
          </footer>
        </article>
      </main>
    );
  }

  return (
    <main className="container">
      <article>
        <header>
          <h1>Create a Poll</h1>
        </header>
        <form onSubmit={handleSubmit}>
          <label>
            Title
            <input
              type="text"
              value={title}
              onChange={(e) => setTitle(e.target.value)}
              placeholder="What's your question?"
              required
              maxLength={200}
            />
          </label>

          <label>
            Description <small>(optional)</small>
            <textarea
              value={description}
              onChange={(e) => setDescription(e.target.value)}
              placeholder="Add more context..."
              maxLength={1000}
            />
          </label>

          <fieldset>
            <legend>Poll Type</legend>
            <label>
              <input
                type="radio"
                name="pollType"
                value="SingleChoice"
                checked={pollType === 'SingleChoice'}
                onChange={() => setPollType('SingleChoice')}
              />
              Single Choice
            </label>
            <label>
              <input
                type="radio"
                name="pollType"
                value="MultipleChoice"
                checked={pollType === 'MultipleChoice'}
                onChange={() => setPollType('MultipleChoice')}
              />
              Multiple Choice
            </label>
          </fieldset>

          <label>Options</label>
          <OptionsList options={options} onChange={setOptions} />

          {error && (
            <p role="alert" style={{ color: 'var(--pico-del-color)' }}>
              {error}
            </p>
          )}

          <button type="submit" disabled={submitting} aria-busy={submitting}>
            {submitting ? 'Creating...' : 'Create Poll'}
          </button>
        </form>
      </article>
    </main>
  );
}
