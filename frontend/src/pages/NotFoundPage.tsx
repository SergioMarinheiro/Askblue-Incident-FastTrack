import { Link } from 'react-router'

export function NotFoundPage() {
  return (
    <div className="py-20 text-center">
      <h1 className="text-3xl font-bold">Page not found</h1>
      <p className="mt-2 text-muted">The page you are looking for does not exist.</p>
      <Link to="/overview" className="mt-6 inline-block font-semibold text-brand-600 hover:underline">
        Back to overview
      </Link>
    </div>
  )
}
