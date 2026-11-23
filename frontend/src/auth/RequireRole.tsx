import type { ReactNode } from 'react'
import { Link } from 'react-router'
import { Card } from '@/components/ui/Card'
import { EmptyState } from '@/components/ui/States'
import { hasRole, type Role } from './roles'
import { useAuth } from './useAuth'

/** Shows its children only to the given roles; others see a "no access" message. The API enforces it too. */
export function RequireRole({ roles, children }: { roles: readonly Role[]; children: ReactNode }) {
  const { user } = useAuth()
  if (hasRole(user?.role, roles)) return children
  return (
    <Card>
      <EmptyState
        title="You don't have access to this page"
        description="Ask an administrator if you think you should."
        action={
          <Link to="/overview" className="text-sm font-semibold text-brand-600 hover:underline">
            Back to overview
          </Link>
        }
      />
    </Card>
  )
}
