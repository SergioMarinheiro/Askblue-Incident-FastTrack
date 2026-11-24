import { useState } from 'react'
import { cn } from '@/lib/cn'

/** How many characters long text shows before it is cut with "…" and "+ Expand". */
export const TEXT_PREVIEW_CHARS = 300

/**
 * Long text cut at `max` characters with "…" and an Expand / Collapse toggle. Short text is shown as is.
 * Keeps line breaks, and long words without spaces wrap instead of overflowing.
 */
export function ExpandableText({ text, max = TEXT_PREVIEW_CHARS, className }: { text: string; max?: number; className?: string }) {
  const [expanded, setExpanded] = useState(false)
  const isLong = text.length > max
  return (
    // The button is inside the paragraph, so it flows right after the last word like more text.
    <p className={cn('whitespace-pre-line wrap-anywhere', className)}>
      {isLong && !expanded ? `${text.slice(0, max).trimEnd()}…` : text}
      {isLong && (
        <button
          type="button"
          onClick={() => setExpanded((e) => !e)}
          aria-expanded={expanded}
          className="ml-2 font-semibold whitespace-nowrap text-brand-600 hover:underline"
        >
          {expanded ? '− Collapse' : '+ Expand'}
        </button>
      )}
    </p>
  )
}
