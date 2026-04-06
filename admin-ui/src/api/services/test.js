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
