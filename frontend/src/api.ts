export type ProblemDetails = {
  title?: string
  detail?: string
  errors?: Record<string, string[]>
}

export class ApiError extends Error {
  readonly status: number
  readonly problem?: ProblemDetails

  constructor(
    status: number,
    problem?: ProblemDetails,
  ) {
    super(problem?.detail ?? problem?.title ?? 'Не удалось выполнить запрос.')
    this.status = status
    this.problem = problem
  }
}

export async function apiFetch<T>(
  path: string,
  init: RequestInit = {},
): Promise<T> {
  const headers = new Headers(init.headers)

  if (init.body && !(init.body instanceof FormData)) {
    headers.set('Content-Type', 'application/json')
  }

  const response = await fetch(`/api${path}`, {
    ...init,
    headers,
    credentials: 'include',
  })

  const hasJsonBody = response.headers
    .get('content-type')
    ?.includes('json')
  const body = hasJsonBody
    ? ((await response.json()) as ProblemDetails | T)
    : undefined

  if (!response.ok) {
    throw new ApiError(response.status, body as ProblemDetails | undefined)
  }

  return body as T
}

export function getApiErrorMessage(error: unknown): string {
  if (error instanceof ApiError) {
    const validationMessage = Object.values(error.problem?.errors ?? {})
      .flat()
      .at(0)

    return validationMessage ?? error.message
  }

  return 'Не удалось связаться с сервером. Попробуйте ещё раз.'
}
