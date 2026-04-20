import { useError } from '../context/ErrorContext';

export default function ErrorBanner() {
  const { globalError, clearGlobalError } = useError();

  if (!globalError) return null;

  return (
    <div
      role="alert"
      style={{
        background: 'var(--pico-del-color)',
        color: '#fff',
        padding: '0.75rem 1rem',
        display: 'flex',
        justifyContent: 'space-between',
        alignItems: 'center',
        gap: '1rem',
      }}
    >
      <span>{globalError}</span>
      <button
        type="button"
        onClick={clearGlobalError}
        aria-label="Dismiss error"
        style={{
          background: 'none',
          border: 'none',
          color: '#fff',
          cursor: 'pointer',
          fontSize: '1.25rem',
          padding: '0',
          lineHeight: 1,
        }}
      >
        ✕
      </button>
    </div>
  );
}
