import astro from 'eslint-plugin-astro';
import perfectionist from 'eslint-plugin-perfectionist';
import { defineConfig } from 'eslint/config';
import tseslint from 'typescript-eslint';

const membersOf = (node) => node.properties ?? node.members ?? node.body;

const local = {
  rules: {
    'line-comments': {
      create(context) {
        const source = context.sourceCode;
        return {
          Program() {
            for (const comment of source.getAllComments()) {
              if (comment.type !== 'Block' || /^\s*(eslint|global|@ts-|#__PURE__|@vite-ignore)/.test(comment.value))
                continue;
              if (source.getNodeByRangeIndex(comment.range[0])?.type.startsWith('JSX')) continue;
              context.report({ loc: comment.loc, messageId: 'block' });
            }
          },
        };
      },
      meta: { messages: { block: 'Write comments with //' }, type: 'layout' },
    },
    'no-comments-between-members': {
      create(context) {
        const check = (node) => {
          const members = membersOf(node);
          for (const comment of context.sourceCode.getCommentsInside(node)) {
            const inMember = members.some((m) => m.range[0] <= comment.range[0] && comment.range[1] <= m.range[1]);
            if (!inMember) context.report({ loc: comment.loc, messageId: 'between' });
          }
        };
        return {
          ObjectExpression: check,
          ObjectPattern: check,
          TSEnumBody: check,
          TSInterfaceBody: check,
          TSTypeLiteral: check,
        };
      },
      meta: { messages: { between: 'Sorted members take no comments between them' }, type: 'layout' },
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
      'local/line-comments': 'error',
      'local/no-comments-between-members': 'error',
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
