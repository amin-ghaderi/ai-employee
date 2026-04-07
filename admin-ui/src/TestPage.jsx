import { useEffect, useRef, useState } from 'react';
import { api } from './api/client.js';
import { BehaviorsApi } from './api/services/behaviors.js';
import { IntegrationsApi } from './api/services/integrations.js';
import { PersonasApi } from './api/services/personas.js';
import { runIntegrationJudgeTest, runJudgeTest, runLeadWithDebug } from './api/services/test.js';

export default function TestPage() {
  const [mode, setMode] = useState(() => {
    return localStorage.getItem('test_mode') || 'judge';
  });
  const [text, setText] = useState('');
  const [channel, setChannel] = useState('');
  const [externalId, setExternalId] = useState('');
  const [leadPersonaId, setLeadPersonaId] = useState('');
  const [leadBehaviorId, setLeadBehaviorId] = useState('');
  const [leadAnswersText, setLeadAnswersText] = useState('');
  const [leadAnswerKeysText, setLeadAnswerKeysText] = useState('');
  const [personas, setPersonas] = useState([]);
  const [behaviors, setBehaviors] = useState([]);
  const [metaLoading, setMetaLoading] = useState(false);
  const [integrations, setIntegrations] = useState([]);
  const [integrationsError, setIntegrationsError] = useState(null);
  const [selectedIntegrationId, setSelectedIntegrationId] = useState('');
  const [winner, setWinner] = useState('');
  const [reason, setReason] = useState('');
  const [leadResult, setLeadResult] = useState(null);
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);
  const [debugData, setDebugData] = useState(null);
  const [debugLoading, setDebugLoading] = useState(false);
  const [debugError, setDebugError] = useState('');
  const [copied, setCopied] = useState(false);
  const latestDebugRequest = useRef(0);

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

  useEffect(() => {
    async function loadMeta() {
      try {
        setMetaLoading(true);
        const [p, b] = await Promise.all([PersonasApi.list(), BehaviorsApi.list()]);
        setPersonas(Array.isArray(p) ? p : []);
        setBehaviors(Array.isArray(b) ? b : []);
      } catch (e) {
        console.warn('Failed to load personas/behaviors', e);
      } finally {
        setMetaLoading(false);
      }
    }

    loadMeta();
  }, []);

  useEffect(() => {
    setDebugData(null);
    setWinner('');
    setReason('');
    setLeadResult(null);
    setError('');
  }, [mode]);

  useEffect(() => {
    localStorage.setItem('test_mode', mode);
  }, [mode]);

  async function fetchDebug(channelValue, externalIdValue, transcriptText) {
    const normalizedChannel = String(channelValue ?? '').trim();
    const normalizedExternalId = String(externalIdValue ?? '').trim();
    if (!normalizedChannel && !normalizedExternalId) {
      setDebugData(null);
      setDebugError('Missing channel or integration');
      return;
    }

    setDebugLoading(true);
    setDebugError('');
    try {
      const params = {
        channel: normalizedChannel,
        externalId: normalizedExternalId,
      };
      const t = String(transcriptText ?? '').trim();
      if (t) params.text = t;

      const response = await api.get('/debug/judge', {
        params,
      });
      setDebugData(response.data || null);
    } catch (e) {
      console.error('Debug fetch failed', e);
      setDebugError('Failed to load debug preview');
      setDebugData(null);
    } finally {
      setDebugLoading(false);
    }
  }

  async function handleCopyPrompt() {
    if (!debugData?.prompt) return;

    try {
      await navigator.clipboard.writeText(debugData.prompt);
      setCopied(true);

      setTimeout(() => {
        setCopied(false);
      }, 1500);
    } catch (err) {
      console.error('Copy failed', err);
    }
  }

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
      await fetchDebug(channel.trim(), externalId.trim(), text);
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
      await fetchDebug(channel.trim(), externalId.trim(), text);
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

  async function runLead() {
    if (!leadPersonaId.trim() || !leadBehaviorId.trim()) {
      setError('PersonaId and BehaviorId are required');
      return;
    }

    setLoading(true);
    setError('');
    setDebugData(null);
    setWinner('');
    setReason('');
    setLeadResult(null);

    try {
      const answers = leadAnswersText
        .split('\n')
        .map((s) => s.trim())
        .filter(Boolean);

      const answerKeys = leadAnswerKeysText
        ? leadAnswerKeysText
            .split('\n')
            .map((s) => s.trim())
            .filter(Boolean)
        : null;

      const result = await runLeadWithDebug({
        personaId: leadPersonaId.trim(),
        behaviorId: leadBehaviorId.trim(),
        answers,
        answerKeys,
      });

      setDebugData(result.debug ?? null);
      setLeadResult({
        userType: result.userType,
        intent: result.intent,
        potential: result.potential,
      });
    } catch (e) {
      setError(e.message || 'Lead test failed');
    } finally {
      setLoading(false);
    }
  }

  return (
    <div>
      <div style={{ marginBottom: 12 }}>
        <button type="button" onClick={() => setMode('judge')} disabled={loading || mode === 'judge'}>
          Judge
        </button>{' '}
        <button type="button" onClick={() => setMode('lead')} disabled={loading || mode === 'lead'}>
          Lead
        </button>
      </div>
      {mode === 'judge' && (
        <>
          <textarea
            value={text}
            onChange={(e) => {
              setText(e.target.value);
              setDebugData(null);
            }}
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
                setDebugData(null);
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
              onChange={(e) => {
                setChannel(e.target.value);
                setDebugData(null);
              }}
              disabled={loading}
            />
          </div>
          <div>
            <label htmlFor="test-external-id">External ID</label>{' '}
            <input
              id="test-external-id"
              type="text"
              value={externalId}
              onChange={(e) => {
                setExternalId(e.target.value);
                setDebugData(null);
              }}
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
        </>
      )}
      {mode === 'lead' && (
        <div style={{ marginBottom: 16 }}>
          <div>
            <label>Persona</label>{' '}
            <select
              value={leadPersonaId}
              onChange={(e) => {
                setLeadPersonaId(e.target.value);
                setDebugData(null);
              }}
              disabled={loading || metaLoading}
            >
              <option value="">Select Persona (required)</option>
              {personas.length === 0 && (
                <option value="" disabled>
                  No personas available
                </option>
              )}
              {personas.map((p) => (
                <option key={p.id} value={p.id}>
                  {p.displayName || p.id}
                </option>
              ))}
            </select>
          </div>
          <div>
            <label>Behavior</label>{' '}
            <select
              value={leadBehaviorId}
              onChange={(e) => {
                setLeadBehaviorId(e.target.value);
                setDebugData(null);
              }}
              disabled={loading || metaLoading}
            >
              <option value="">Select Behavior (required)</option>
              {behaviors.length === 0 && (
                <option value="" disabled>
                  No behaviors available
                </option>
              )}
              {behaviors.map((b) => (
                <option key={b.id} value={b.id}>
                  {b.id}
                </option>
              ))}
            </select>
          </div>
          <div>
            <textarea
              placeholder="Answers (one per line)"
              value={leadAnswersText}
              onChange={(e) => {
                setLeadAnswersText(e.target.value);
                setDebugData(null);
              }}
              rows={6}
              cols={70}
              disabled={loading}
            />
          </div>
          <div>
            <textarea
              placeholder="Answer Keys (optional, one per line)"
              value={leadAnswerKeysText}
              onChange={(e) => {
                setLeadAnswerKeysText(e.target.value);
                setDebugData(null);
              }}
              rows={6}
              cols={70}
              disabled={loading}
            />
          </div>
          <button type="button" onClick={runLead} disabled={loading}>
            {loading ? 'Running…' : 'Run Lead Test'}
          </button>
        </div>
      )}
      {debugLoading && <p>Loading preview...</p>}
      {debugError && <p style={{ color: 'red' }}>{debugError}</p>}
      {!debugData && !debugLoading && (
        <p style={{ color: '#888' }}>Run a test to see debug information</p>
      )}
      {debugData && (
        <div
          style={{
            border: '1px solid #ddd',
            borderRadius: '6px',
            padding: '12px',
            marginTop: '12px',
            background: '#fafafa',
          }}
        >
          <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
            <h3>Prompt Preview (Effective AI Input)</h3>
            <button
              type="button"
              onClick={handleCopyPrompt}
              disabled={!debugData?.prompt}
            >
              {copied ? 'Copied! ✅' : 'Copy Prompt'}
            </button>
          </div>
          <div>
            <strong>Source:</strong> {debugData.promptSource}
          </div>
          {mode === 'judge' && (
            <>
              <div>
                <strong>Mode:</strong> {debugData.pathType}
              </div>
              {debugData.pathType === 'SIMPLE' && (
                <div style={{ color: 'orange' }}>Simple Mode</div>
              )}
              <hr />
              <h4>Tokens</h4>
              <div>
                <ul>
                  <li>{'{{input}}'}: {debugData.hasInputToken ? '✅' : '❌'}</li>
                  <li>{'{{goal}}'}: {debugData.hasGoalToken ? '✅' : '❌'}</li>
                  <li>{'{{experience}}'}: {debugData.hasExperienceToken ? '✅' : '❌'}</li>
                </ul>
              </div>
            </>
          )}
          {debugData.schema && (
            <>
              <hr />
              <h4>Schema</h4>
              <div>
                <pre
                  style={{
                    maxHeight: '200px',
                    overflow: 'auto',
                    background: '#f7f7f7',
                    padding: '8px',
                  }}
                >
                  {JSON.stringify(debugData.schema, null, 2)}
                </pre>
              </div>
            </>
          )}
          <hr />
          <h4>Context</h4>
          <div>
            <ul>
              <li>BotId: {debugData.botId}</li>
              <li>PersonaId: {debugData.personaId}</li>
              <li>BehaviorId: {debugData.behaviorId}</li>
              <li>Channel: {debugData.channel}</li>
            </ul>
          </div>
          <hr />
          <h4>Prompt</h4>
          <pre style={{ whiteSpace: 'pre-wrap', maxHeight: '300px', overflow: 'auto' }}>
            {debugData.prompt}
          </pre>
          <hr />
          <h4>Response</h4>
          <div>
            <strong>Latency:</strong> {debugData.latencyMs} ms
          </div>
          <div>
            <strong>Prompt Hash:</strong> {debugData.promptHash}
          </div>
          {debugData.parsedResult && (
            <div>
              <strong>Parsed Result:</strong>
              <pre
                style={{
                  maxHeight: '200px',
                  overflow: 'auto',
                  background: '#f7f7f7',
                  padding: '8px',
                }}
              >
                {JSON.stringify(debugData.parsedResult, null, 2)}
              </pre>
            </div>
          )}
          {debugData.rawResponse && (
            <div>
              <strong>Raw Response:</strong>
              <pre
                style={{
                  maxHeight: '200px',
                  overflow: 'auto',
                  background: '#f7f7f7',
                  padding: '8px',
                }}
              >
                {debugData.rawResponse}
              </pre>
            </div>
          )}
        </div>
      )}
      {mode === 'lead' && leadResult && (
        <div style={{ marginTop: 16 }}>
          <h4>Lead Result</h4>
          <p>
            <strong>User Type:</strong> {leadResult.userType}
          </p>
          <p>
            <strong>Intent:</strong> {leadResult.intent}
          </p>
          <p>
            <strong>Potential:</strong> {leadResult.potential}
          </p>
        </div>
      )}
      {mode === 'judge' && (winner || reason) && (
        <div>
          <p>Winner: {winner}</p>
          <p>Reason: {reason}</p>
        </div>
      )}
      {error && <p>{error}</p>}
    </div>
  );
}
