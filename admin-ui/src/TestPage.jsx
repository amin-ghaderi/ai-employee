import { useEffect, useState } from 'react';
import { IntegrationsApi } from './api/services/integrations.js';
import { runIntegrationJudgeTest, runJudgeTest } from './api/services/test.js';

export default function TestPage() {
  const [text, setText] = useState('');
  const [channel, setChannel] = useState('');
  const [externalId, setExternalId] = useState('');
  const [integrations, setIntegrations] = useState([]);
  const [integrationsError, setIntegrationsError] = useState(null);
  const [selectedIntegrationId, setSelectedIntegrationId] = useState('');
  const [winner, setWinner] = useState('');
  const [reason, setReason] = useState('');
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);

  useEffect(() => {
    async function load() {
      try {
        const data = await IntegrationsApi.list();
        setIntegrations(Array.isArray(data) ? data : []);
        setIntegrationsError(null);
      } catch (e) {
        setIntegrationsError('Failed to load integrations');
      }
    }
    load();
  }, []);

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
      const data = await runJudgeTest(text.trim());
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

  async function runIntegration() {
    setError('');
    setWinner('');
    setReason('');

    if (!text.trim()) {
      setError('Text is required');
      return;
    }
    if (!channel.trim()) {
      setError('Channel is required');
      return;
    }
    if (!externalId.trim()) {
      setError('External ID is required');
      return;
    }

    setLoading(true);
    try {
      const data = await runIntegrationJudgeTest(
        text.trim(),
        channel.trim(),
        externalId.trim()
      );
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
        <label htmlFor="test-integration-select">Integration</label>{' '}
        <select
          id="test-integration-select"
          value={selectedIntegrationId}
          onChange={(e) => {
            const id = e.target.value;
            setSelectedIntegrationId(id);
            const selected = integrations.find((i) => String(i.id) === id);
            if (selected) {
              setChannel(selected.channel);
              setExternalId(selected.externalId);
            } else {
              setChannel('');
              setExternalId('');
            }
          }}
          disabled={loading}
        >
          <option value="">-- Select integration --</option>
          {integrations
            .filter((i) => i.isEnabled)
            .map((i) => (
              <option key={i.id} value={i.id}>
                {i.channel} | {i.externalId}
              </option>
            ))}
        </select>
      </div>
      {integrationsError && <p>{integrationsError}</p>}
      <div>
        <label htmlFor="test-channel">Channel</label>{' '}
        <input
          id="test-channel"
          type="text"
          value={channel}
          onChange={(e) => setChannel(e.target.value)}
          disabled={loading}
        />
      </div>
      <div>
        <label htmlFor="test-external-id">External ID</label>{' '}
        <input
          id="test-external-id"
          type="text"
          value={externalId}
          onChange={(e) => setExternalId(e.target.value)}
          disabled={loading}
        />
      </div>
      <div>
        <button type="button" onClick={runJudge} disabled={loading}>
          {loading ? 'Running…' : 'Run Test'}
        </button>
        <button type="button" onClick={runJudge} disabled={loading}>
          Run Again
        </button>
        <button type="button" onClick={runIntegration} disabled={loading}>
          {loading ? 'Running…' : 'Run Integration Test'}
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
