import { useCallback, useEffect, useState } from 'react';
import { BotsApi } from '../api/services/bots.js';
import { IntegrationsApi } from '../api/services/integrations.js';
import {
  CHANNEL_OPTIONS,
  channelOptionsWithCurrent,
} from '../constants/channels.js';

function botNameForId(bots, botId) {
  const id = String(botId);
  const b = bots.find((x) => String(x.id) === id);
  return b?.name ?? id;
}

/** @param {Record<string, unknown>} integration */
function integrationSupportsWebhook(integration) {
  if (integration.supportsWebhook === true) return true;
  if (integration.supportsWebhook === false) return false;
  return Boolean(
    integration.externalId &&
      String(integration.externalId).trim().length > 0
  );
}

/** @param {unknown} e */
function messageFromAxiosError(e) {
  const d = e?.response?.data;
  if (d && typeof d === 'object') {
    if (typeof d.lastError === 'string' && d.lastError) return d.lastError;
    if (typeof d.title === 'string' && d.title) return d.title;
    if (typeof d.message === 'string' && d.message) return d.message;
  }
  if (e && typeof e.message === 'string') return e.message;
  return 'Request failed';
}

/** @param {Record<string, unknown> | null | undefined} raw */
function normalizeWebhookSummary(raw) {
  if (!raw || typeof raw !== 'object') {
    return {
      webhookUrl: null,
      status: 'error',
      lastError: 'Invalid response',
      lastSyncedAt: null,
    };
  }
  return {
    webhookUrl: typeof raw.webhookUrl === 'string' ? raw.webhookUrl : null,
    status: typeof raw.status === 'string' ? raw.status : 'error',
    lastError: typeof raw.lastError === 'string' ? raw.lastError : null,
    lastSyncedAt: typeof raw.lastSyncedAt === 'string' ? raw.lastSyncedAt : null,
  };
}

const BADGE_STYLES = {
  synced: { background: '#0f766e', color: '#fff' },
  active: { background: '#15803d', color: '#fff' },
  not_registered: { background: '#64748b', color: '#fff' },
  deleted: { background: '#475569', color: '#fff' },
  error: { background: '#b91c1c', color: '#fff' },
  mismatch: { background: '#ca8a04', color: '#111' },
  not_found: { background: '#7f1d1d', color: '#fff' },
};

function WebhookStatusBadge({ status }) {
  const key = String(status ?? 'error').toLowerCase();
  const style = BADGE_STYLES[key] ?? {
    background: '#334155',
    color: '#fff',
  };
  return (
    <span
      style={{
        display: 'inline-block',
        fontSize: '0.75rem',
        fontWeight: 600,
        letterSpacing: '0.02em',
        padding: '2px 8px',
        borderRadius: '999px',
        textTransform: 'lowercase',
        ...style,
      }}
    >
      {key.replace(/_/g, ' ')}
    </span>
  );
}

export default function IntegrationsPage() {
  const [integrations, setIntegrations] = useState([]);
  const [bots, setBots] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [botId, setBotId] = useState('');
  const [channel, setChannel] = useState('');
  const [externalId, setExternalId] = useState('');
  const [createGatewayChannel, setCreateGatewayChannel] = useState('');
  const [createGatewayExternalId, setCreateGatewayExternalId] = useState('');
  const [validationError, setValidationError] = useState('');
  const [createError, setCreateError] = useState('');
  const [createBusy, setCreateBusy] = useState(false);
  const [loadingIds, setLoadingIds] = useState(() => new Set());
  const [actionErrors, setActionErrors] = useState({});

  /** @type {Record<string, { webhookUrl: string | null, status: string, lastError: string | null, lastSyncedAt: string | null }>} */
  const [webhookSummaries, setWebhookSummaries] = useState({});
  const [webhookStatusLoadingIds, setWebhookStatusLoadingIds] = useState(
    () => new Set()
  );
  const [webhookActionIds, setWebhookActionIds] = useState(() => new Set());
  const [webhookActionErrors, setWebhookActionErrors] = useState({});

  /** @type {Record<string, { channel: string, externalId: string }>} */
  const [gatewayDrafts, setGatewayDrafts] = useState({});
  const [gatewaySaveBusyIds, setGatewaySaveBusyIds] = useState(() => new Set());
  const [gatewaySaveErrors, setGatewaySaveErrors] = useState({});

  useEffect(() => {
    if (!Array.isArray(integrations)) return;
    const next = {};
    for (const row of integrations) {
      next[String(row.id)] = {
        channel:
          row.gatewayChannel != null ? String(row.gatewayChannel) : '',
        externalId:
          row.gatewayExternalId != null ? String(row.gatewayExternalId) : '',
      };
    }
    setGatewayDrafts(next);
  }, [integrations]);

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

  useEffect(() => {
    if (loading || error) return undefined;
    const ids = integrations
      .filter((row) => integrationSupportsWebhook(row))
      .map((r) => r.id);
    if (ids.length === 0) {
      setWebhookSummaries({});
      setWebhookStatusLoadingIds(new Set());
      return undefined;
    }

    let cancelled = false;

    setWebhookStatusLoadingIds(new Set(ids));

    (async () => {
      // Sequential calls: integrations that share the same bot token hit the same Telegram getWebhookInfo;
      // parallel bursts can cause throttling or flaky responses so one row looks "fine" and others do not.
      const next = {};
      for (const id of ids) {
        if (cancelled) return;
        try {
          const data = await IntegrationsApi.getWebhookStatus(id);
          next[String(id)] = normalizeWebhookSummary(data);
        } catch (e) {
          const d = e.response?.data;
          if (d && typeof d === 'object' && typeof d.status === 'string') {
            next[String(id)] = normalizeWebhookSummary(d);
          } else {
            next[String(id)] = {
              webhookUrl: null,
              status: 'error',
              lastError: messageFromAxiosError(e),
              lastSyncedAt: null,
            };
          }
        }
      }

      if (cancelled) return;

      setWebhookSummaries(next);
      setWebhookStatusLoadingIds(new Set());
    })();

    return () => {
      cancelled = true;
    };
  }, [integrations, loading, error]);

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

  function addWebhookActionId(id) {
    setWebhookActionIds((prev) => {
      const next = new Set(prev);
      next.add(id);
      return next;
    });
  }

  function removeWebhookActionId(id) {
    setWebhookActionIds((prev) => {
      const next = new Set(prev);
      next.delete(id);
      return next;
    });
  }

  function clearWebhookActionError(id) {
    setWebhookActionErrors((prev) => {
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
    const gwCh = createGatewayChannel.trim().toLowerCase();
    const gwExt = createGatewayExternalId.trim();
    if ((gwCh && !gwExt) || (!gwCh && gwExt)) {
      setValidationError(
        'Gateway Channel and Gateway External ID must be provided together.'
      );
      return;
    }
    setValidationError('');
    setCreateError('');
    setCreateBusy(true);
    try {
      const body = {
        botId: bid,
        channel: ch.toLowerCase(),
        externalId: ext,
      };
      if (gwCh || gwExt) {
        body.gatewayChannel = gwCh ? gwCh.toLowerCase() : null;
        body.gatewayExternalId = gwExt;
      }
      await IntegrationsApi.create(body);
      setBotId('');
      setChannel('');
      setExternalId('');
      setCreateGatewayChannel('');
      setCreateGatewayExternalId('');
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

  async function handleSyncWebhook(id) {
    clearWebhookActionError(id);
    addWebhookActionId(id);
    try {
      const data = await IntegrationsApi.syncWebhook(id);
      setWebhookSummaries((prev) => ({
        ...prev,
        [String(id)]: normalizeWebhookSummary(data),
      }));
    } catch (e) {
      const d = e.response?.data;
      if (d && typeof d === 'object' && typeof d.status === 'string') {
        setWebhookSummaries((prev) => ({
          ...prev,
          [String(id)]: normalizeWebhookSummary(d),
        }));
      }
      setWebhookActionErrors((prev) => ({
        ...prev,
        [String(id)]: messageFromAxiosError(e),
      }));
    } finally {
      removeWebhookActionId(id);
    }
  }

  async function handleRefreshWebhookStatus(id) {
    clearWebhookActionError(id);
    addWebhookActionId(id);
    try {
      const data = await IntegrationsApi.getWebhookStatus(id);
      setWebhookSummaries((prev) => ({
        ...prev,
        [String(id)]: normalizeWebhookSummary(data),
      }));
    } catch (e) {
      const d = e.response?.data;
      if (d && typeof d === 'object' && typeof d.status === 'string') {
        setWebhookSummaries((prev) => ({
          ...prev,
          [String(id)]: normalizeWebhookSummary(d),
        }));
      }
      setWebhookActionErrors((prev) => ({
        ...prev,
        [String(id)]: messageFromAxiosError(e),
      }));
    } finally {
      removeWebhookActionId(id);
    }
  }

  function clearGatewaySaveError(id) {
    setGatewaySaveErrors((prev) => {
      const key = String(id);
      if (!(key in prev)) return prev;
      const next = { ...prev };
      delete next[key];
      return next;
    });
  }

  function addGatewaySaveBusyId(id) {
    setGatewaySaveBusyIds((prev) => {
      const next = new Set(prev);
      next.add(id);
      return next;
    });
  }

  function removeGatewaySaveBusyId(id) {
    setGatewaySaveBusyIds((prev) => {
      const next = new Set(prev);
      next.delete(id);
      return next;
    });
  }

  function updateGatewayDraft(id, field, value) {
    const key = String(id);
    setGatewayDrafts((prev) => ({
      ...prev,
      [key]: {
        channel: field === 'channel' ? value : (prev[key]?.channel ?? ''),
        externalId:
          field === 'externalId' ? value : (prev[key]?.externalId ?? ''),
      },
    }));
  }

  async function handleSaveGateway(id) {
    const key = String(id);
    clearGatewaySaveError(id);
    const draft = gatewayDrafts[key] ?? { channel: '', externalId: '' };
    const gwCh = draft.channel.trim().toLowerCase();
    const gwExt = draft.externalId.trim();
    if ((gwCh && !gwExt) || (!gwCh && gwExt)) {
      setGatewaySaveErrors((prev) => ({
        ...prev,
        [key]:
          'Gateway Channel and Gateway External ID must be provided together.',
      }));
      return;
    }
    addGatewaySaveBusyId(id);
    try {
      await IntegrationsApi.update(id, {
        gatewayChannel: gwCh || '',
        gatewayExternalId: gwExt || '',
      });
      await loadData();
    } catch (e) {
      const msg =
        e.response?.data?.title ||
          e.response?.data?.message ||
          (Array.isArray(e.response?.data?.errors) &&
            e.response.data.errors.join('; ')) ||
          e.message ||
          'Save gateway failed';
      setGatewaySaveErrors((prev) => ({ ...prev, [key]: msg }));
    } finally {
      removeGatewaySaveBusyId(id);
    }
  }

  async function handleDeleteWebhook(id) {
    if (
      !window.confirm(
        'Remove the webhook for this integration? Bots may stop receiving updates until you sync again.'
      )
    ) {
      return;
    }
    clearWebhookActionError(id);
    addWebhookActionId(id);
    try {
      const data = await IntegrationsApi.deleteWebhook(id);
      setWebhookSummaries((prev) => ({
        ...prev,
        [String(id)]: normalizeWebhookSummary(data),
      }));
    } catch (e) {
      const d = e.response?.data;
      if (d && typeof d === 'object' && typeof d.status === 'string') {
        setWebhookSummaries((prev) => ({
          ...prev,
          [String(id)]: normalizeWebhookSummary(d),
        }));
      }
      setWebhookActionErrors((prev) => ({
        ...prev,
        [String(id)]: messageFromAxiosError(e),
      }));
    } finally {
      removeWebhookActionId(id);
    }
  }

  const cellStyle = { verticalAlign: 'top' };
  const gatewayInputStyle = {
    width: '100%',
    minWidth: '140px',
    maxWidth: '220px',
    fontSize: '0.8rem',
    padding: '4px 6px',
    boxSizing: 'border-box',
  };
  const readOnlyInputStyle = {
    width: '100%',
    minWidth: '260px',
    maxWidth: '420px',
    fontFamily: 'ui-monospace, monospace',
    fontSize: '0.8rem',
    padding: '4px 6px',
    boxSizing: 'border-box',
  };

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
          <select
            id="integration-channel"
            value={channel}
            onChange={(e) => setChannel(e.target.value)}
            disabled={createBusy}
            style={{ minWidth: '220px', fontSize: '0.95rem' }}
          >
            <option value="">-- Select channel --</option>
            {CHANNEL_OPTIONS.map((chOpt) => (
              <option key={chOpt.value} value={chOpt.value}>
                {chOpt.label}
              </option>
            ))}
          </select>
        </div>
        <div>
          <label htmlFor="integration-external-id">
            External ID (Telegram: bot token; Generic / WhatsApp / Slack: HTTPS URL)
          </label>{' '}
          <input
            id="integration-external-id"
            type="text"
            value={externalId}
            onChange={(e) => setExternalId(e.target.value)}
            disabled={createBusy}
          />
        </div>
        <div>
          <label htmlFor="integration-gateway-channel">
            Gateway channel (optional — must match an outbound sender)
          </label>{' '}
          <select
            id="integration-gateway-channel"
            value={createGatewayChannel}
            onChange={(e) => setCreateGatewayChannel(e.target.value)}
            disabled={createBusy}
            style={{ minWidth: '220px', fontSize: '0.95rem' }}
          >
            <option value="">-- Select Gateway Channel (Optional) --</option>
            {CHANNEL_OPTIONS.map((chOpt) => (
              <option key={chOpt.value} value={chOpt.value}>
                {chOpt.label}
              </option>
            ))}
          </select>
        </div>
        <div>
          <label htmlFor="integration-gateway-external-id">
            Gateway external ID (optional — destination chat id for that channel)
          </label>{' '}
          <input
            id="integration-gateway-external-id"
            type="text"
            value={createGatewayExternalId}
            onChange={(e) => setCreateGatewayExternalId(e.target.value)}
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
        <table style={{ borderCollapse: 'collapse', marginTop: '1rem' }}>
          <thead>
            <tr>
              <th style={{ textAlign: 'left', padding: '4px 8px' }}>Bot name</th>
              <th style={{ textAlign: 'left', padding: '4px 8px' }}>Channel</th>
              <th style={{ textAlign: 'left', padding: '4px 8px' }}>Provider</th>
              <th style={{ textAlign: 'left', padding: '4px 8px' }}>ExternalId</th>
              <th style={{ textAlign: 'left', padding: '4px 8px' }}>
                Gateway channel
              </th>
              <th style={{ textAlign: 'left', padding: '4px 8px' }}>
                Gateway external ID
              </th>
              <th style={{ textAlign: 'left', padding: '4px 8px' }}>
                Save gateway
              </th>
              <th style={{ textAlign: 'left', padding: '4px 8px' }}>Enabled</th>
              <th style={{ textAlign: 'left', padding: '4px 8px' }}>
                Webhook URL
              </th>
              <th style={{ textAlign: 'left', padding: '4px 8px' }}>Webhook</th>
              <th style={{ textAlign: 'left', padding: '4px 8px' }}>Actions</th>
            </tr>
          </thead>
          <tbody>
            {integrations.map((row) => {
              const id = row.id;
              const busy = loadingIds.has(id);
              const gatewayBusy = gatewaySaveBusyIds.has(id);
              const supportsWebhook = integrationSupportsWebhook(row);
              const key = String(id);
              const gwDraft = gatewayDrafts[key] ?? {
                channel: '',
                externalId: '',
              };
              const summary = webhookSummaries[key];
              const statusLoading = webhookStatusLoadingIds.has(id);
              const webhookBusy = webhookActionIds.has(id);
              const urlDisplay = supportsWebhook
                ? summary?.webhookUrl ?? ''
                : '';

              return (
                <tr key={id}>
                  <td style={cellStyle}>{botNameForId(bots, row.botId)}</td>
                  <td style={cellStyle}>{row.channel ?? ''}</td>
                  <td style={cellStyle}>{row.provider ?? 'unknown'}</td>
                  <td style={cellStyle}>{row.externalId ?? ''}</td>
                  <td style={cellStyle}>
                    <select
                      aria-label="Gateway channel"
                      value={gwDraft.channel}
                      onChange={(e) =>
                        updateGatewayDraft(
                          id,
                          'channel',
                          (e.target.value || '').trim().toLowerCase()
                        )
                      }
                      style={gatewayInputStyle}
                    >
                      <option value="">-- None --</option>
                      {channelOptionsWithCurrent(gwDraft.channel).map(
                        (chOpt) => (
                          <option key={chOpt.value} value={chOpt.value}>
                            {chOpt.label}
                          </option>
                        )
                      )}
                    </select>
                  </td>
                  <td style={cellStyle}>
                    <input
                      type="text"
                      aria-label="Gateway external ID"
                      value={gwDraft.externalId}
                      onChange={(e) =>
                        updateGatewayDraft(id, 'externalId', e.target.value)
                      }
                      style={gatewayInputStyle}
                    />
                  </td>
                  <td style={cellStyle}>
                    <button
                      type="button"
                      disabled={busy || gatewayBusy}
                      onClick={() => handleSaveGateway(id)}
                    >
                      {gatewayBusy ? 'Saving…' : 'Save'}
                    </button>
                    {gatewaySaveErrors[key] && (
                      <div
                        style={{
                          marginTop: '6px',
                          fontSize: '0.8rem',
                          color: '#b91c1c',
                          maxWidth: '200px',
                        }}
                      >
                        {gatewaySaveErrors[key]}
                      </div>
                    )}
                  </td>
                  <td style={cellStyle}>{row.isEnabled ? 'Yes' : 'No'}</td>
                  <td style={cellStyle}>
                    {supportsWebhook ? (
                      <input
                        type="text"
                        readOnly
                        aria-label="Webhook URL"
                        value={
                          statusLoading && !summary
                            ? 'Loading…'
                            : urlDisplay
                        }
                        style={readOnlyInputStyle}
                      />
                    ) : (
                      '—'
                    )}
                  </td>
                  <td style={cellStyle}>
                    {supportsWebhook ? (
                      <div>
                        {statusLoading && !webhookBusy ? (
                          <span style={{ color: '#64748b', fontSize: '0.85rem' }}>
                            Loading status…
                          </span>
                        ) : summary ? (
                          <div>
                            <WebhookStatusBadge status={summary.status} />
                            {summary.lastError && (
                              <div
                                style={{
                                  marginTop: '6px',
                                  fontSize: '0.8rem',
                                  color: '#b91c1c',
                                  maxWidth: '320px',
                                }}
                              >
                                {summary.lastError}
                              </div>
                            )}
                            {summary.lastSyncedAt && (
                              <div
                                style={{
                                  marginTop: '4px',
                                  fontSize: '0.75rem',
                                  color: '#64748b',
                                }}
                              >
                                Last synced: {summary.lastSyncedAt}
                              </div>
                            )}
                          </div>
                        ) : (
                          <span style={{ color: '#64748b' }}>—</span>
                        )}
                      </div>
                    ) : (
                      '—'
                    )}
                  </td>
                  <td style={cellStyle}>
                    {supportsWebhook ? (
                      <>
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
                        <div style={{ marginTop: '8px' }}>
                          <button
                            type="button"
                            disabled={webhookBusy}
                            onClick={() => handleSyncWebhook(id)}
                          >
                            {webhookBusy ? 'Working…' : 'Sync Webhook'}
                          </button>{' '}
                          <button
                            type="button"
                            disabled={webhookBusy || statusLoading}
                            onClick={() => handleRefreshWebhookStatus(id)}
                          >
                            {webhookBusy ? 'Working…' : 'Refresh status'}
                          </button>{' '}
                          <button
                            type="button"
                            disabled={webhookBusy}
                            onClick={() => handleDeleteWebhook(id)}
                          >
                            {webhookBusy ? 'Working…' : 'Delete Webhook'}
                          </button>
                        </div>
                        {webhookActionErrors[key] && (
                          <p
                            style={{
                              margin: '6px 0 0',
                              fontSize: '0.85rem',
                              color: '#b91c1c',
                            }}
                          >
                            {webhookActionErrors[key]}
                          </p>
                        )}
                      </>
                    ) : (
                      '—'
                    )}
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
