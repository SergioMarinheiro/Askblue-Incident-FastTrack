import { useState } from 'react'
import { Eye, EyeOff, Wand2 } from 'lucide-react'
import { controlClass } from '@/components/ui/FormField'
import { inputClass } from '@/components/ui/form'

/** A random 14-character password from an unambiguous alphabet (no 0/O, 1/l/I). */
function generatePassword() {
  const alphabet = 'ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz23456789!@#$%&*?'
  const bytes = crypto.getRandomValues(new Uint32Array(14))
  return Array.from(bytes, (b) => alphabet[b % alphabet.length]).join('')
}

type Props = { id: string; value: string; onChange: (value: string) => void; disabled?: boolean; error?: string }

/** Password field for admins setting someone else's password: show/hide and generate. */
export function PasswordInput({ id, value, onChange, disabled, error }: Props) {
  const [visible, setVisible] = useState(false)
  return (
    <div className="flex gap-2">
      <div className="relative flex-1">
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
      <button
        type="button"
        onClick={() => {
          onChange(generatePassword())
          setVisible(true)
        }}
        disabled={disabled}
        className="inline-flex items-center gap-1.5 rounded-lg border border-line px-3 text-sm font-semibold text-ink hover:bg-surface disabled:opacity-60"
      >
        <Wand2 className="size-4" aria-hidden />
        Generate
      </button>
    </div>
  )
}
