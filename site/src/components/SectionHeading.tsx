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
