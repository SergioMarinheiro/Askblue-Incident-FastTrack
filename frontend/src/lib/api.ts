/**
 * Small wrapper around fetch for calling the backend.
 * - Adds the JWT (if logged in) as a Bearer token.
 * - Sends/reads JSON.
 * - Turns error responses (ProblemDetails) into an ApiError with a readable message.
 * - On 401 for an authenticated request, calls the registered handler (logs the user out).
 */

type ProblemDetails = {
  title?: string
  detail?: string
  status?: number
  errors?: Record<string, string[]>
}

export class ApiError extends Error {
  readonly status: number
  /** Field validation errors from the API, keyed by field name. */
  readonly fieldErrors: Record<string, string[]>
  /** The parsed response body, for endpoints whose error body is not ProblemDetails (e.g. submit's 409). */
  readonly data: unknown

  constructor(status: number, message: string, fieldErrors: Record<string, string[]> = {}, data: unknown = undefined) {
    super(message)
    this.name = 'ApiError'
    this.status = status
    this.fieldErrors = fieldErrors
    this.data = data
  }
}

let getToken: () => string | null = () => null
let onUnauthorized: () => void = () => {}

/** Called once by the AuthProvider so the API client knows the token and how to log out. */
export function configureApi(options: { getToken: () => string | null; onUnauthorized: () => void }) {
  getToken = options.getToken
  onUnauthorized = options.onUnauthorized
}

type RequestOptions = Omit<RequestInit, 'body'> & {
  /** Plain objects are sent as JSON; FormData is sent as-is (file uploads). */
  body?: unknown
  /** Set to false for endpoints that must not send the token (login). */
  auth?: boolean
}

export async function apiFetch<T>(path: string, options: RequestOptions = {}): Promise<T> {
  const { body, auth = true, headers, ...rest } = options
  const finalHeaders = new Headers(headers)
  finalHeaders.set('Accept', 'application/json')

  let finalBody: BodyInit | undefined
  if (body instanceof FormData) {
    finalBody = body
  } else if (body !== undefined) {
    finalHeaders.set('Content-Type', 'application/json')
    finalBody = JSON.stringify(body)
  }

  const token = auth ? getToken() : null
  if (token) finalHeaders.set('Authorization', `Bearer ${token}`)

  const response = await send(path, { ...rest, headers: finalHeaders, body: finalBody }, token)

  if (response.status === 204) return undefined as T
  const contentType = response.headers.get('Content-Type') ?? ''
  if (contentType.includes('json')) return (await response.json()) as T
  return undefined as T
}

/**
 * Downloads a file that needs the login token (so a plain <a href> won't work)
 * and hands it to the browser as a normal file download.
 */
export async function apiDownload(path: string, fileName: string) {
  const token = getToken()
  const headers = new Headers()
  if (token) headers.set('Authorization', `Bearer ${token}`)

  const response = await send(path, { headers }, token)
  const blob = await response.blob()

  const url = URL.createObjectURL(blob)
  const link = document.createElement('a')
  link.href = url
  link.download = fileName
  document.body.appendChild(link)
  link.click()
  link.remove()
  setTimeout(() => URL.revokeObjectURL(url), 1000)
}

async function send(path: string, init: RequestInit, token: string | null): Promise<Response> {
  let response: Response
  try {
    response = await fetch(path, init)
  } catch (err) {
    // Cancelled on purpose (e.g. the page was left): let the caller see the abort as-is.
    if (err instanceof DOMException && err.name === 'AbortError') throw err
    throw new ApiError(0, 'Could not reach the server. Check your connection and try again.')
  }

  if (!response.ok) {
    if (response.status === 401 && token) onUnauthorized()
    throw await toApiError(response)
  }
  return response
}

async function toApiError(response: Response): Promise<ApiError> {
  let data: unknown = undefined
  try {
    data = await response.json()
  } catch {
    // No JSON body.
  }
  const problem: ProblemDetails =
    data !== null && typeof data === 'object' && !Array.isArray(data) ? (data as ProblemDetails) : {}

  const fieldErrors = problem.errors ?? {}
  const firstFieldError = Object.values(fieldErrors)[0]?.[0]
  const message =
    problem.detail ||
    firstFieldError ||
    problem.title ||
    (response.status >= 500 ? 'Something went wrong on the server. Please try again.' : `Request failed (${response.status}).`)

  return new ApiError(response.status, message, fieldErrors, data)
}
