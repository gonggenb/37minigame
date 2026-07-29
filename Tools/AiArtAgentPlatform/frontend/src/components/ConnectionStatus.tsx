export type ConnectionState = "loading" | "connected" | "error";

interface ConnectionStatusProps {
  state: ConnectionState;
  onRetry?: () => void;
}

const stateContent: Record<ConnectionState, { label: string; detail: string }> = {
  loading: {
    label: "正在连接本地服务",
    detail: "正在检查 127.0.0.1 上的 FastAPI 服务。",
  },
  connected: {
    label: "本地服务已连接",
    detail: "工作台只通过本机回环地址通信。",
  },
  error: {
    label: "本地服务不可用",
    detail: "请确认后端已经启动，然后重新连接。",
  },
};

export function ConnectionStatus({ state, onRetry }: ConnectionStatusProps) {
  const content = stateContent[state];

  return (
    <section className={`connection-status connection-status--${state}`} aria-live="polite">
      <span className="connection-status__mark" aria-hidden="true" />
      <div>
        <strong>{content.label}</strong>
        <p>{content.detail}</p>
      </div>
      {state === "error" ? (
        <button type="button" onClick={onRetry}>
          重新连接
        </button>
      ) : null}
    </section>
  );
}
