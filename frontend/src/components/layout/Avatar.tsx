import { cn } from '@/lib/cn'

function initials(name: string) {
  return name
    .split(' ')
    .filter(Boolean)
    .map((part) => part[0])
    .slice(0, 2)
    .join('')
    .toUpperCase()
}

export function Avatar({ name, variant = 'dark' }: { name: string; variant?: 'dark' | 'light' }) {
  return (
    <div
      className={cn(
        'flex size-9 shrink-0 items-center justify-center rounded-full text-xs font-semibold',
        variant === 'dark'
          ? 'bg-brand-600 text-white'
          : // Darkens when hovered inside a clickable parent marked "group" (the user menu button).
            'bg-brand-50 text-brand-700 transition-colors group-hover:bg-brand-600 group-hover:text-white',
      )}
      aria-hidden
    >
      {initials(name)}
    </div>
  )
}
