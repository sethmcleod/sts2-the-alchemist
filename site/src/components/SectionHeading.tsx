// A stats section heading that a link can point at. Its id is the same in every language, so a
// shared link works in all of them. Pointing at the heading shows a # that links to it; the # is only
// for the pointer, since the address itself works for everyone

import type { ComponentChildren } from 'preact';

interface Props {
  children: ComponentChildren;
  class?: string;
  id: string;
}

export default function SectionHeading({ children, class: className, id }: Props) {
  return (
    <h2 class={['anchored', className].filter(Boolean).join(' ')} id={id}>
      {children}
      <a aria-hidden="true" class="anchor" href={`#${id}`} tabIndex={-1}>
        #
      </a>
    </h2>
  );
}
