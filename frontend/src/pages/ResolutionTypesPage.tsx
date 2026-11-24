import { useMemo, useState } from 'react'
import { useSearchParams } from 'react-router'
import { useQueryClient } from '@tanstack/react-query'
import { CheckCircle2, Pencil, Plus, X } from 'lucide-react'
import { useAuth } from '@/auth/useAuth'
import { Badge } from '@/components/ui/Badge'
import { Card, CardHeader } from '@/components/ui/Card'
import { PageHeader } from '@/components/ui/PageHeader'
import { EmptyState, ErrorState, LoadingState } from '@/components/ui/States'
import { buttonPrimaryClass, inputClass } from '@/components/ui/form'
import { resolutionTypeKeys, useResolutionTypes, type ResolutionType } from '@/features/reference/api'
import { ResolutionTypeModal } from '@/features/resolutionTypes/ResolutionTypeModal'
import { useCompanies } from '@/features/companies/api'

type Dialog = { kind: 'create' } | { kind: 'edit'; type: ResolutionType }

export function ResolutionTypesPage() {
  const { user } = useAuth()
  const queryClient = useQueryClient()
  const isSuperAdmin = user?.role === 'SuperAdmin'

  const { data, isPending, isError, error, refetch } = useResolutionTypes()
  const companies = useCompanies(isSuperAdmin)
  const companyName = (id: number) => companies.data?.find((c) => c.companyId === id)?.name ?? `Company ${id}`

  const [params, setParams] = useSearchParams()
  const companyFilter = params.get('company') ?? ''

  const [dialog, setDialog] = useState<Dialog | null>(null)
  const [notice, setNotice] = useState<string | null>(null)

  // Sorted by company (SuperAdmin only), then by name.
  const rows = useMemo(() => {
    const nameOf = (id: number) => companies.data?.find((c) => c.companyId === id)?.name ?? ''
    return (data ?? [])
      .filter((r) => !companyFilter || String(r.companyId) === companyFilter)
      .sort((a, b) => (isSuperAdmin ? nameOf(a.companyId).localeCompare(nameOf(b.companyId)) : 0) || a.name.localeCompare(b.name))
  }, [data, companyFilter, isSuperAdmin, companies.data])

  async function afterSave(message: string) {
    setDialog(null)
    setNotice(message)
    await queryClient.invalidateQueries({ queryKey: resolutionTypeKeys.all })
  }

  return (
    <>
      <PageHeader
        title="Resolution types"
        actions={
          <button type="button" onClick={() => setDialog({ kind: 'create' })} className={buttonPrimaryClass}>
            <Plus className="size-4" aria-hidden />
            New resolution type
          </button>
        }
      />

      {notice && (
        <div role="status" className="mb-5 flex items-center gap-2 rounded-xl border border-emerald-200 bg-emerald-50 px-4 py-3 text-sm text-emerald-800">
          <CheckCircle2 className="size-4 shrink-0" aria-hidden />
          <span className="flex-1">{notice}</span>
          <button type="button" onClick={() => setNotice(null)} aria-label="Dismiss" className="rounded p-0.5 hover:bg-emerald-100">
            <X className="size-4" aria-hidden />
          </button>
        </div>
      )}

      {isSuperAdmin && (
        <div className="mb-5 sm:w-72">
          <select
            value={companyFilter}
            onChange={(e) => {
              const v = e.target.value
              setParams(v ? { company: v } : {}, { replace: true })
            }}
            aria-label="Filter by company"
            className={inputClass}
          >
            <option value="">All companies</option>
            {companies.data?.map((c) => (
              <option key={c.companyId} value={c.companyId}>
                {c.name}
              </option>
            ))}
          </select>
        </div>
      )}

      <Card>
        <CardHeader title="All resolution types" actions={data && `${rows.length}`} />
        {isPending ? (
          <LoadingState label="Loading resolution types…" />
        ) : isError ? (
          <ErrorState message={error.message} onRetry={() => refetch()} />
        ) : rows.length === 0 ? (
          <EmptyState
            title="No resolution types yet"
            description="Incidents can't be submitted until at least one exists. Common ones: Compensation, HR, Legal, Insurance."
          />
        ) : (
          <div className="overflow-x-auto px-3 pb-3">
            <table className="w-full min-w-[520px] table-fixed text-left text-sm">
              <colgroup>
                <col className={isSuperAdmin ? 'w-[35%]' : 'w-[50%]'} />
                <col className={isSuperAdmin ? 'w-[30%]' : 'w-[40%]'} />
                {isSuperAdmin && <col className="w-[25%]" />}
                <col className="w-[10%]" />
              </colgroup>
              <thead>
                <tr className="bg-surface text-xs font-semibold tracking-wide text-muted uppercase">
                  <th className="rounded-l-lg px-3 py-3">Name</th>
                  <th className="px-3 py-3">After approval</th>
                  {isSuperAdmin && <th className="px-3 py-3">Company</th>}
                  <th className="rounded-r-lg px-3 py-3">
                    <span className="sr-only">Actions</span>
                  </th>
                </tr>
              </thead>
              <tbody className="divide-y divide-line">
                {rows.map((r) => (
                  <tr key={r.resolutionTypeId} className="hover:bg-surface/60">
                    <td className="truncate px-3 py-3.5 font-medium" title={r.name}>
                      {r.name}
                    </td>
                    <td className="px-3 py-3.5">
                      {r.requiresPayment ? <Badge tone="violet">Awaiting payment</Badge> : <Badge tone="blue">Awaiting resolution</Badge>}
                    </td>
                    {isSuperAdmin && <td className="truncate px-3 py-3.5 text-ink/90">{companyName(r.companyId)}</td>}
                    <td className="px-3 py-3.5 text-right">
                      <button
                        type="button"
                        onClick={() => setDialog({ kind: 'edit', type: r })}
                        aria-label={`Edit ${r.name}`}
                        title="Edit"
                        className="rounded-md p-2 text-muted hover:bg-brand-50 hover:text-brand-600"
                      >
                        <Pencil className="size-4" aria-hidden />
                      </button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </Card>

      {dialog?.kind === 'create' && (
        <ResolutionTypeModal onClose={() => setDialog(null)} onSaved={(saved) => afterSave(`"${saved.name}" created.`)} />
      )}
      {dialog?.kind === 'edit' && (
        <ResolutionTypeModal type={dialog.type} onClose={() => setDialog(null)} onSaved={(saved) => afterSave(`"${saved.name}" saved.`)} />
      )}
    </>
  )
}
