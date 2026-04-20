import type { PollOptionResultResponse } from '../types';

interface ResultsBarProps {
  option: PollOptionResultResponse;
}

export default function ResultsBar({ option }: ResultsBarProps) {
  return (
    <div style={{ marginBottom: '1rem' }}>
      <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: '0.25rem' }}>
        <span>{option.text}</span>
        <span>
          {option.voteCount} vote{option.voteCount !== 1 ? 's' : ''} ({option.percentage}%)
        </span>
      </div>
      <div
        style={{
          background: 'var(--pico-secondary-background)',
          borderRadius: '4px',
          overflow: 'hidden',
          height: '1.5rem',
        }}
      >
        <div
          style={{
            width: `${option.percentage}%`,
            background: 'var(--pico-primary-background)',
            height: '100%',
            borderRadius: '4px',
            transition: 'width 0.3s ease',
            minWidth: option.voteCount > 0 ? '2px' : '0',
          }}
        />
      </div>
    </div>
  );
}
