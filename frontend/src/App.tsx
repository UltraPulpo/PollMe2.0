import { Routes, Route } from 'react-router-dom';
import CreatePoll from './pages/CreatePoll';
import VotePage from './pages/VotePage';
import ResultsPage from './pages/ResultsPage';
import Dashboard from './pages/Dashboard';

function App() {
  return (
    <Routes>
      <Route path="/" element={<CreatePoll />} />
      <Route path="/poll/:pollId" element={<VotePage />} />
      <Route path="/poll/:pollId/results" element={<ResultsPage />} />
      <Route path="/dashboard/:secretToken" element={<Dashboard />} />
    </Routes>
  );
}

export default App
