import astro from 'eslint-plugin-astro';
import perfectionist from 'eslint-plugin-perfectionist';
import { defineConfig } from 'eslint/config';
import tseslint from 'typescript-eslint';

const DIRECTIVE = /^\s*(eslint|global|@ts-|\/ <reference)/;

const local = {
  rules: {
    'no-comments': {
      create(context) {
        const source = context.sourceCode;
        const text = source.text;
        const removal = ([start, end]) => {
          const lineStart = text.lastIndexOf('\n', start - 1) + 1;
          const lineEnd = text.indexOf('\n', end) === -1 ? text.length : text.indexOf('\n', end);
          const before = text.slice(lineStart, start);
          if (!before.trim() && !text.slice(end, lineEnd).trim())
            return [lineStart, Math.min(lineEnd + 1, text.length)];
          return [start - (before.length - before.trimEnd().length), end];
        };
        const report = (loc, range) =>
          context.report({ fix: (fixer) => fixer.removeRange(removal(range)), loc, messageId: 'comment' });
        return {
          AstroHTMLComment: (node) => report(node.loc, node.range),
          Program() {
            for (const comment of source.getAllComments()) {
              if (DIRECTIVE.test(comment.value)) continue;
              const node = source.getNodeByRangeIndex(comment.range[0]);
              report(comment.loc, node?.type === 'JSXEmptyExpression' ? node.parent.range : comment.range);
            }
          },
        };
      },
      meta: { fixable: 'code', messages: { comment: 'No comments' }, type: 'suggestion' },
    },
  },
};

export default defineConfig(
  { ignores: ['dist/', '.astro/', '.vercel/', 'data/'] },
  ...astro.configs['flat/base'],
  { files: ['**/*.{ts,tsx}'], languageOptions: { parser: tseslint.parser } },
  {
    files: ['**/*.astro'],
    languageOptions: { parserOptions: { extraFileExtensions: ['.astro'], parser: tseslint.parser } },
  },
  {
    files: ['**/*.{js,mjs,ts,tsx,astro}'],
    plugins: { local, perfectionist },
    rules: {
      'local/no-comments': 'error',
      'perfectionist/sort-array-includes': 'error',
      'perfectionist/sort-enums': 'error',
      'perfectionist/sort-exports': 'error',
      'perfectionist/sort-imports': [
        'error',
        { groups: ['builtin', 'external', 'internal', ['parent', 'sibling', 'index'], 'unknown'], newlinesBetween: 0 },
      ],
      'perfectionist/sort-interfaces': 'error',
      'perfectionist/sort-intersection-types': 'error',
      'perfectionist/sort-jsx-props': 'error',
      'perfectionist/sort-named-exports': 'error',
      'perfectionist/sort-named-imports': 'error',
      'perfectionist/sort-object-types': 'error',
      'perfectionist/sort-objects': 'error',
      'perfectionist/sort-union-types': 'error',
    },
    settings: { perfectionist: { ignoreCase: true, type: 'natural' } },
  },
);
