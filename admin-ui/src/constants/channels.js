/** Canonical integration channel values (aligned with backend / admin presets). */
export const CHANNEL_OPTIONS = [
  { value: 'telegram', label: 'Telegram' },
  { value: 'whatsapp', label: 'WhatsApp Cloud API' },
  { value: 'whatsapp-cloud', label: 'WhatsApp Cloud API (Alias)' },
  { value: 'meta-whatsapp', label: 'Meta WhatsApp' },
  { value: 'slack', label: 'Slack' },
  { value: 'slack-events', label: 'Slack Events API' },
  { value: 'slack-api', label: 'Slack API' },
  { value: 'web', label: 'Web' },
  { value: 'generic-webhook', label: 'Generic Webhook' },
  { value: 'webhook', label: 'Webhook (Alias)' },
  { value: 'generic', label: 'Generic (Alias)' },
  { value: 'custom', label: 'Custom (Alias)' },
];

const knownValues = new Set(CHANNEL_OPTIONS.map((o) => o.value));

/**
 * Options for a &lt;select&gt; when the current value may be a legacy/custom channel not in {@link CHANNEL_OPTIONS}.
 * @param {string} [currentValue]
 * @returns {typeof CHANNEL_OPTIONS}
 */
export function channelOptionsWithCurrent(currentValue) {
  const v = currentValue != null ? String(currentValue).trim() : '';
  if (!v || knownValues.has(v)) {
    return CHANNEL_OPTIONS;
  }
  return [
    { value: v, label: `${v} (saved)` },
    ...CHANNEL_OPTIONS,
  ];
}
