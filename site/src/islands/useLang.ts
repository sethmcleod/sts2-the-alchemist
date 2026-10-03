import type { ComponentChildren } from 'preact';
import { useMemo } from 'preact/hooks';
import { Lang, type LangInit } from '../lib/lang';

export const useLang = (init: LangInit) => useMemo(() => new Lang(init, init.strings, init.game, 'stats'), [init]);

export const fillSlots = (text: string, parts: Record<string, ComponentChildren>) =>
  text.split(/\{(\w+)\}/).map((part, i) => (i % 2 ? (parts[part] ?? `{${part}}`) : part));
