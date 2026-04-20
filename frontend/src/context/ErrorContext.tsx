import { createContext, useContext, useState } from 'react';
import type { ReactNode } from 'react';

interface ErrorContextValue {
  globalError: string;
  setGlobalError: (message: string) => void;
  clearGlobalError: () => void;
}

const ErrorContext = createContext<ErrorContextValue>({
  globalError: '',
  setGlobalError: () => {},
  clearGlobalError: () => {},
});

export function ErrorProvider({ children }: { children: ReactNode }) {
  const [globalError, setGlobalErrorState] = useState('');

  const setGlobalError = (message: string) => setGlobalErrorState(message);
  const clearGlobalError = () => setGlobalErrorState('');

  return (
    <ErrorContext.Provider value={{ globalError, setGlobalError, clearGlobalError }}>
      {children}
    </ErrorContext.Provider>
  );
}

export function useError() {
  return useContext(ErrorContext);
}
