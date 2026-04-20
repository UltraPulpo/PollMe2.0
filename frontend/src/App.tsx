import { Routes, Route } from 'react-router-dom';
import { ErrorProvider } from './context/ErrorContext';
import ErrorBanner from './components/ErrorBanner';
import CreatePoll from './pages/CreatePoll';
import VotePage from './pages/VotePage';
import ResultsPage from './pages/ResultsPage';
import Dashboard from './pages/Dashboard';

function App() {
  return (
    <ErrorProvider>
      <ErrorBanner />
      <Routes>
        <Route path="/" element={<CreatePoll />} />
        <Route path="/poll/:pollId" element={<VotePage />} />
        <Route path="/poll/:pollId/results" element={<ResultsPage />} />
        <Route path="/dashboard/:secretToken" element={<Dashboard />} />
      </Routes>
    </ErrorProvider>
  );
}

export default App
