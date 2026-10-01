// A stats section heading that a link can point at. Its id is the same in every language, so a
// shared link works in all of them. Pointing at the heading shows a # that links to it; the # is only
// for the pointer, since the address itself works for everyone

import type { ComponentChildren } from 'preact';

interface Props {
  id: string;
  class?: string;
  children: ComponentChildren;
}

export default function SectionHeading({ id, class: className, children }: Props) {
  return (
    <h2 id={id} class={['anchored', className].filter(Boolean).join(' ')}>
      {children}
      <a class="anchor" href={`#${id}`} aria-hidden="true" tabIndex={-1}>
        #
      </a>
    </h2>
  );
}
