import { useState } from 'react';
import { api } from './api';

export default function TestPage() {
  const [text, setText] = useState('');
  const [winner, setWinner] = useState('');
  const [reason, setReason] = useState('');
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);

  async function runJudge() {
    setError('');
    setWinner('');
    setReason('');

    if (!text.trim()) {
      setError('Text is required');
      return;
    }

    setLoading(true);
    try {
      const { data } = await api.post('/test/judge', { text: text.trim() });
      setWinner(data.winner ?? '');
      setReason(data.reason ?? '');
    } catch (e) {
      setError(
        e.response?.data?.title ||
          e.response?.data?.message ||
          e.message ||
          'Request failed'
      );
    } finally {
      setLoading(false);
    }
  }

  return (
    <div>
      <textarea
        value={text}
        onChange={(e) => setText(e.target.value)}
        rows={10}
        cols={70}
        placeholder="Paste transcript or test text…"
      />
      <div>
        <button type="button" onClick={runJudge} disabled={loading}>
          {loading ? 'Running…' : 'Run Test'}
        </button>
        <button type="button" onClick={runJudge} disabled={loading}>
          Run Again
        </button>
      </div>
      {(winner || reason) && (
        <div>
          <p>Winner: {winner}</p>
          <p>Reason: {reason}</p>
        </div>
      )}
      {error && <p>{error}</p>}
    </div>
  );
}
