import { api } from '../client.js';

export async function runJudgeTest(text) {
  const { data } = await api.post('/test/judge', { text });
  return data;
}

export async function runIntegrationJudgeTest(text, channel, externalId) {
  const { data } = await api.post('/test/integration', {
    text,
    channel,
    externalId,
  });
  return data;
}

export async function runLeadWithDebug(payload) {
  const res = await fetch('/admin/test/lead-with-debug', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(payload),
  });

  if (!res.ok) {
    throw new Error('Lead test failed');
  }

  return res.json();
}
