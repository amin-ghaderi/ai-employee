import { useEffect, useState } from 'react';
import { getConfig, updateConfig } from './api/services/config.js';

const BOT_ID = 'f7c3b5a0-1111-4111-8111-111111111111';

export default function ConfigPage() {
  const [judgePrompt, setJudgePrompt] = useState('');
  const [leadPrompt, setLeadPrompt] = useState('');
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [success, setSuccess] = useState('');

  useEffect(() => {
    let cancelled = false;
    (async () => {
      setError('');
      setSuccess('');
      setLoading(true);
      try {
        const { data } = await api.get(`/config/${BOT_ID}`);
        if (cancelled) return;
        setJudgePrompt(data.persona?.prompts?.judge ?? '');
        setLeadPrompt(data.persona?.prompts?.lead ?? '');
      } catch (e) {
        if (!cancelled) {
          setError(
            e.response?.data?.title ||
              e.response?.data?.message ||
              e.message ||
              'Failed to load config'
          );
        }
      } finally {
        if (!cancelled) setLoading(false);
      }
    })();
    return () => {
      cancelled = true;
    };
  }, []);

  async function save() {
    setError('');
    setSuccess('');
    setLoading(true);
    try {
      await updateConfig(BOT_ID, {
        judgePrompt,
        leadPrompt,
      });
      setSuccess('Saved successfully.');
    } catch (e) {
      setError(
        e.response?.data?.title ||
          e.response?.data?.message ||
          e.message ||
          'Save failed'
      );
    } finally {
      setLoading(false);
    }
  }

  return (
    <div>
      <h1>Bot configuration</h1>
      {loading && <p>Loading…</p>}
      <div>
        <label>
          JudgePrompt
          <textarea
            value={judgePrompt}
            onChange={(e) => setJudgePrompt(e.target.value)}
            rows={12}
            cols={80}
          />
        </label>
      </div>
      <div>
        <label>
          LeadPrompt
          <textarea
            value={leadPrompt}
            onChange={(e) => setLeadPrompt(e.target.value)}
            rows={12}
            cols={80}
          />
        </label>
      </div>
      <button type="button" onClick={save} disabled={loading}>
        Save
      </button>
      {success && <p>{success}</p>}
      {error && <p>{error}</p>}
    </div>
  );
}
