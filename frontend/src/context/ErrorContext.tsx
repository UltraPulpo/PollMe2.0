import { createContext, useCallback, useContext, useMemo, useState } from 'react';
import type { ReactNode } from 'react';

interface ErrorContextValue {
  globalError: string;
  setGlobalError: (message: string) => void;
  clearGlobalError: () => void;
}

const ErrorContext = createContext<ErrorContextValue | null>(null);

export function ErrorProvider({ children }: { children: ReactNode }) {
  const [globalError, setGlobalErrorState] = useState('');

  const setGlobalError = useCallback((message: string) => setGlobalErrorState(message), []);
  const clearGlobalError = useCallback(() => setGlobalErrorState(''), []);

  const value = useMemo(
    () => ({ globalError, setGlobalError, clearGlobalError }),
    [globalError, setGlobalError, clearGlobalError],
  );

  return (
    <ErrorContext.Provider value={value}>
      {children}
    </ErrorContext.Provider>
  );
}

export function useError(): ErrorContextValue {
  const ctx = useContext(ErrorContext);
  if (ctx === null) {
    throw new Error('useError must be used within an ErrorProvider');
  }
  return ctx;
}
