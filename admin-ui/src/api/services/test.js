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

export async function runJudgeWithDebug(text, channel, externalId) {
  const body = { text };
  if (channel) body.channel = channel;
  if (externalId) body.externalId = externalId;
  const { data } = await api.post('/test/judge-with-debug', body);
  return data;
}

export async function runRealFlowTest({ text, channel, externalUserId, externalChatId, integrationExternalId }) {
  const body = {
    text,
    channel,
    externalUserId,
    externalChatId,
    resetConversation: true,
    disableAutomation: true,
  };
  if (integrationExternalId) body.integrationExternalId = integrationExternalId;
  const { data } = await api.post('/test/real-flow', body);
  return data;
}

export async function runLeadWithDebug(payload) {
  const { data } = await api.post('/test/lead-with-debug', payload);
  return data;
}
