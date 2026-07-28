export class ApiError extends Error {
  constructor(
    message: string,
    public readonly status: number,
  ) {
    super(message);
    this.name = "ApiError";
  }
}

export interface JsonRequestOptions {
  timeoutMs?: number;
}

export interface JsonBodyRequestOptions extends JsonRequestOptions {
  body?: unknown;
}

async function responseErrorMessage(response: Response): Promise<string> {
  try {
    const payload = (await response.json()) as {
      detail?: string | { message?: string };
    };
    if (typeof payload.detail === "string") {
      return payload.detail;
    }
    if (payload.detail?.message) {
      return payload.detail.message;
    }
  } catch {
    // 非 JSON 错误回退到状态码消息。
  }
  return `Local API request failed with status ${response.status}`;
}

export async function getJson<T>(
  path: string,
  { timeoutMs = 10_000 }: JsonRequestOptions = {},
): Promise<T> {
  const controller = new AbortController();
  const timeout = window.setTimeout(() => controller.abort(), timeoutMs);

  try {
    const response = await fetch(path, {
      headers: { Accept: "application/json" },
      signal: controller.signal,
    });

    if (!response.ok) {
      throw new ApiError(
        await responseErrorMessage(response),
        response.status,
      );
    }

    return (await response.json()) as T;
  } catch (error) {
    if (controller.signal.aborted) {
      throw new Error("Local API request timed out");
    }
    throw error;
  } finally {
    window.clearTimeout(timeout);
  }
}

export async function postJson<TResponse>(
  path: string,
  body: unknown,
  { timeoutMs = 10_000 }: JsonRequestOptions = {},
): Promise<TResponse> {
  return sendJson<TResponse>("POST", path, body, timeoutMs);
}

export async function putJson<TResponse>(
  path: string,
  body: unknown,
  { timeoutMs = 10_000 }: JsonRequestOptions = {},
): Promise<TResponse> {
  return sendJson<TResponse>("PUT", path, body, timeoutMs);
}

export async function deleteRequest(
  path: string,
  { timeoutMs = 10_000 }: JsonRequestOptions = {},
): Promise<void> {
  const controller = new AbortController();
  const timeout = window.setTimeout(() => controller.abort(), timeoutMs);

  try {
    const response = await fetch(path, {
      method: "DELETE",
      headers: { Accept: "application/json" },
      signal: controller.signal,
    });
    if (!response.ok) {
      throw new ApiError(
        await responseErrorMessage(response),
        response.status,
      );
    }
  } catch (error) {
    if (controller.signal.aborted) {
      throw new Error("Local API request timed out");
    }
    throw error;
  } finally {
    window.clearTimeout(timeout);
  }
}

async function sendJson<TResponse>(
  method: "POST" | "PUT",
  path: string,
  body: unknown,
  timeoutMs: number,
): Promise<TResponse> {
  const controller = new AbortController();
  const timeout = window.setTimeout(() => controller.abort(), timeoutMs);

  try {
    const response = await fetch(path, {
      method,
      headers: {
        Accept: "application/json",
        "Content-Type": "application/json",
      },
      body: JSON.stringify(body),
      signal: controller.signal,
    });

    if (!response.ok) {
      throw new ApiError(
        await responseErrorMessage(response),
        response.status,
      );
    }

    return (await response.json()) as TResponse;
  } catch (error) {
    if (controller.signal.aborted) {
      throw new Error("Local API request timed out");
    }
    throw error;
  } finally {
    window.clearTimeout(timeout);
  }
}
