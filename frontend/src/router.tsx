import { createBrowserRouter, Navigate } from 'react-router'
import { RequireAuth } from '@/auth/RequireAuth'
import { RequireRole } from '@/auth/RequireRole'
import { USER_LIST_ROLES } from '@/features/users/permissions'
import { UsersPage } from '@/pages/UsersPage'
import { StoresPage } from '@/pages/StoresPage'
import { ResolutionTypesPage } from '@/pages/ResolutionTypesPage'
import { ADMIN_OR_ABOVE } from '@/auth/roles'
import { AppLayout } from '@/components/layout/AppLayout'
import { LoginPage } from '@/pages/LoginPage'
import { OverviewPage } from '@/pages/OverviewPage'
import { IncidentsPage } from '@/pages/IncidentsPage'
import { NewIncidentPage } from '@/pages/NewIncidentPage'
import { IncidentDetailPage } from '@/pages/IncidentDetailPage'
import { IncidentSubmittedPage } from '@/pages/IncidentSubmittedPage'
import { EditIncidentPage } from '@/pages/EditIncidentPage'
import { incidentRef } from '@/features/incidents/labels'
import { NotFoundPage } from '@/pages/NotFoundPage'

// `handle.crumb` is the label shown in the top-bar breadcrumbs for that route
// (a string, or a function of the URL params for dynamic pages).
export const router = createBrowserRouter([
  { path: '/login', element: <LoginPage /> },
  {
    path: '/',
    element: (
      <RequireAuth>
        <AppLayout />
      </RequireAuth>
    ),
    handle: { crumb: 'Workspace' },
    children: [
      { index: true, element: <Navigate to="/overview" replace /> },
      { path: 'overview', element: <OverviewPage />, handle: { crumb: 'Overview' } },
      {
        path: 'incidents',
        handle: { crumb: 'Incidents' },
        children: [
          { index: true, element: <IncidentsPage /> },
          { path: 'new', element: <NewIncidentPage />, handle: { crumb: 'New incident' } },
          {
            path: ':id',
            handle: { crumb: (params: { id?: string }) => incidentRef(Number(params.id)) },
            children: [
              { index: true, element: <IncidentDetailPage /> },
              { path: 'submitted', element: <IncidentSubmittedPage />, handle: { crumb: 'Confirmation' } },
              { path: 'edit', element: <EditIncidentPage />, handle: { crumb: 'Edit' } },
            ],
          },
        ],
      },
      {
        path: 'users',
        element: (
          <RequireRole roles={USER_LIST_ROLES}>
            <UsersPage />
          </RequireRole>
        ),
        handle: { crumb: 'Users' },
      },
      {
        path: 'stores',
        element: (
          <RequireRole roles={ADMIN_OR_ABOVE}>
            <StoresPage />
          </RequireRole>
        ),
        handle: { crumb: 'Stores' },
      },
      {
        path: 'resolution-types',
        element: (
          <RequireRole roles={ADMIN_OR_ABOVE}>
            <ResolutionTypesPage />
          </RequireRole>
        ),
        handle: { crumb: 'Resolution types' },
      },
      { path: '*', element: <NotFoundPage />, handle: { crumb: 'Not found' } },
    ],
  },
])
