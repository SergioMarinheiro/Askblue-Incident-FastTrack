import { useCallback, useEffect, useState } from 'react'
import { Outlet, useLocation } from 'react-router'
import { Sidebar } from './Sidebar'
import { Topbar } from './Topbar'

export function AppLayout() {
  const [menuOpen, setMenuOpen] = useState(false)
  const location = useLocation()
  const closeMenu = useCallback(() => setMenuOpen(false), [])

  // Close the mobile menu after navigating somewhere.
  useEffect(() => {
    setMenuOpen(false)
  }, [location.pathname])

  return (
    <div className="flex min-h-screen">
      <Sidebar mobileOpen={menuOpen} onClose={closeMenu} />
      <div className="flex min-w-0 flex-1 flex-col">
        <Topbar onOpenMenu={() => setMenuOpen(true)} menuOpen={menuOpen} />
        <main className="flex-1 px-4 py-6 sm:px-6 lg:px-8 lg:py-8">
          <div className="mx-auto max-w-6xl">
            <Outlet />
          </div>
        </main>
      </div>
    </div>
  )
}
