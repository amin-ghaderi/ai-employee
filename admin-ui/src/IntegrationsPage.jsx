import { useCallback, useEffect, useState } from 'react';
import { BotsApi } from './api/services/bots.js';
import { IntegrationsApi } from './api/services/integrations.js';

function botNameForId(bots, botId) {
  const id = String(botId);
  const b = bots.find((x) => String(x.id) === id);
  return b?.name ?? id;
}

export default function IntegrationsPage() {
  const [integrations, setIntegrations] = useState([]);
  const [bots, setBots] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [botId, setBotId] = useState('');
  const [channel, setChannel] = useState('');
  const [externalId, setExternalId] = useState('');
  const [validationError, setValidationError] = useState('');
  const [createError, setCreateError] = useState('');
  const [createBusy, setCreateBusy] = useState(false);
  const [loadingIds, setLoadingIds] = useState(() => new Set());
  const [actionErrors, setActionErrors] = useState({});

  const loadData = useCallback(async () => {
    setError('');
    setLoading(true);
    try {
      const [i, b] = await Promise.all([
        IntegrationsApi.list(),
        BotsApi.list(),
      ]);
      setIntegrations(Array.isArray(i) ? i : []);
      setBots(Array.isArray(b) ? b : []);
    } catch (e) {
      setIntegrations([]);
      setBots([]);
      setError(
        e.response?.data?.title ||
          e.response?.data?.message ||
          e.message ||
          'Failed to load integrations or bots'
      );
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    loadData();
  }, [loadData]);

  function addLoadingId(id) {
    setLoadingIds((prev) => {
      const next = new Set(prev);
      next.add(id);
      return next;
    });
  }

  function removeLoadingId(id) {
    setLoadingIds((prev) => {
      const next = new Set(prev);
      next.delete(id);
      return next;
    });
  }

  function clearActionError(id) {
    setActionErrors((prev) => {
      const key = String(id);
      if (!(key in prev)) return prev;
      const next = { ...prev };
      delete next[key];
      return next;
    });
  }

  async function handleCreate(e) {
    e.preventDefault();
    const bid = botId.trim();
    const ch = channel.trim();
    const ext = externalId.trim();
    if (!bid) {
      setValidationError('botId is required.');
      return;
    }
    if (!ch) {
      setValidationError('channel is required.');
      return;
    }
    if (!ext) {
      setValidationError('externalId is required.');
      return;
    }
    setValidationError('');
    setCreateError('');
    setCreateBusy(true);
    try {
      await IntegrationsApi.create({
        botId: bid,
        channel: ch,
        externalId: ext,
      });
      setBotId('');
      setChannel('');
      setExternalId('');
      await loadData();
    } catch (e) {
      const msg =
        e.response?.data?.title ||
          e.response?.data?.message ||
          (Array.isArray(e.response?.data?.errors) &&
            e.response.data.errors.join('; ')) ||
          e.message ||
          'Create failed';
      setCreateError(msg);
    } finally {
      setCreateBusy(false);
    }
  }

  async function handleEnable(id) {
    clearActionError(id);
    addLoadingId(id);
    try {
      await IntegrationsApi.enable(id);
      await loadData();
    } catch (e) {
      const msg =
        e.response?.data?.title ||
          e.response?.data?.message ||
          e.message ||
          'Enable failed';
      setActionErrors((prev) => ({ ...prev, [String(id)]: msg }));
    } finally {
      removeLoadingId(id);
    }
  }

  async function handleDisable(id) {
    clearActionError(id);
    addLoadingId(id);
    try {
      await IntegrationsApi.disable(id);
      await loadData();
    } catch (e) {
      const msg =
        e.response?.data?.title ||
          e.response?.data?.message ||
          e.message ||
          'Disable failed';
      setActionErrors((prev) => ({ ...prev, [String(id)]: msg }));
    } finally {
      removeLoadingId(id);
    }
  }

  return (
    <div>
      <h1>Integrations</h1>
      <form onSubmit={handleCreate}>
        <div>
          <label htmlFor="integration-bot">Bot</label>{' '}
          <select
            id="integration-bot"
            value={botId}
            onChange={(e) => setBotId(e.target.value)}
            disabled={createBusy}
          >
            <option value="">—</option>
            {bots.map((b) => (
              <option key={b.id} value={b.id}>
                {b.name ?? b.id}
              </option>
            ))}
          </select>
        </div>
        <div>
          <label htmlFor="integration-channel">Channel</label>{' '}
          <input
            id="integration-channel"
            type="text"
            value={channel}
            onChange={(e) => setChannel(e.target.value)}
            onBlur={(e) => {
              setChannel(e.target.value.trim().toLowerCase());
            }}
            disabled={createBusy}
          />
        </div>
        <div>
          <label htmlFor="integration-external-id">External ID</label>{' '}
          <input
            id="integration-external-id"
            type="text"
            value={externalId}
            onChange={(e) => setExternalId(e.target.value)}
            disabled={createBusy}
          />
        </div>
        <button type="submit" disabled={createBusy}>
          {createBusy ? 'Creating…' : 'Create'}
        </button>
        {validationError && <p>{validationError}</p>}
        {createError && <p>{createError}</p>}
      </form>
      {loading && <p>Loading…</p>}
      {error && <p>{error}</p>}
      {!loading && !error && Object.keys(actionErrors).length > 0 && (
        <div>
          {Object.entries(actionErrors).map(([id, msg]) => (
            <p key={id}>
              Integration {id}: {msg}
            </p>
          ))}
        </div>
      )}
      {!loading && !error && (
        <table>
          <thead>
            <tr>
              <th>Bot name</th>
              <th>Channel</th>
              <th>ExternalId</th>
              <th>Enabled</th>
              <th>Actions</th>
            </tr>
          </thead>
          <tbody>
            {integrations.map((row) => {
              const id = row.id;
              const busy = loadingIds.has(id);
              return (
                <tr key={id}>
                  <td>{botNameForId(bots, row.botId)}</td>
                  <td>{row.channel ?? ''}</td>
                  <td>{row.externalId ?? ''}</td>
                  <td>{row.isEnabled ? 'Yes' : 'No'}</td>
                  <td>
                    <button
                      type="button"
                      disabled={busy}
                      onClick={() => handleEnable(id)}
                    >
                      {busy ? 'Processing...' : 'Enable'}
                    </button>{' '}
                    <button
                      type="button"
                      disabled={busy}
                      onClick={() => handleDisable(id)}
                    >
                      {busy ? 'Processing...' : 'Disable'}
                    </button>
                  </td>
                </tr>
              );
            })}
          </tbody>
        </table>
      )}
    </div>
  );
}
