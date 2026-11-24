import { useState } from 'react'
import { Eye, EyeOff } from 'lucide-react'
import { controlClass } from '@/components/ui/FormField'
import { inputClass } from '@/components/ui/form'

type Props = { id: string; value: string; onChange: (value: string) => void; disabled?: boolean; error?: string }

/** Password field for admins setting someone else's password, with a show/hide toggle. */
export function PasswordInput({ id, value, onChange, disabled, error }: Props) {
  const [visible, setVisible] = useState(false)
  return (
    <div className="relative">
      <input
        id={id}
        type={visible ? 'text' : 'password'}
        autoComplete="new-password"
        value={value}
        onChange={(e) => onChange(e.target.value)}
        disabled={disabled}
        className={controlClass(`${inputClass} pr-10 font-mono`, error)}
      />
      <button
        type="button"
        onClick={() => setVisible((v) => !v)}
        aria-label={visible ? 'Hide password' : 'Show password'}
        className="absolute top-1/2 right-2 -translate-y-1/2 rounded p-1 text-muted hover:text-ink"
      >
        {visible ? <EyeOff className="size-4" aria-hidden /> : <Eye className="size-4" aria-hidden />}
      </button>
    </div>
  )
}
