import { useState } from "react";

import {
  type ModelCheckResult,
  useModelAvailabilityMutation,
  useModelStatusQuery,
} from "../api/models";

function capabilityLabel(capability: string): string {
  if (capability === "structured_review") {
    return "结构化规划与评审";
  }
  if (capability === "image_generation") {
    return "图像生成";
  }
  return capability;
}

function ModelCheck({ check }: { check: ModelCheckResult }) {
  return (
    <li
      className={
        check.available
          ? "model-check model-check--ok"
          : "model-check model-check--error"
      }
    >
      <div>
        <strong>{capabilityLabel(check.capability)}</strong>
        <span>{check.model}</span>
      </div>
      <div className="model-check__result">
        <strong>{check.available ? "可用" : "不可用"}</strong>
        {check.error_code ? <span>错误码：{check.error_code}</span> : null}
        {!check.available ? (
          <span>{check.retryable ? "可重试" : "不可盲目重试"}</span>
        ) : null}
      </div>
      {check.detail ? <p>{check.detail}</p> : null}
    </li>
  );
}

export function ModelStatusCard() {
  const status = useModelStatusQuery();
  const availability = useModelAvailabilityMutation();
  const [includeImage, setIncludeImage] = useState(false);
  const isConfigured = status.data?.api_key_configured === true;

  return (
    <section className="paper-card paper-card--models">
      <p className="paper-card__label">模型连接</p>
      <h2>OpenAI 适配层</h2>

      {status.isPending ? <p>正在读取服务端模型配置…</p> : null}
      {status.isError ? (
        <div className="model-status-banner model-status-banner--error">
          <strong>无法读取模型配置</strong>
          <button type="button" onClick={() => void status.refetch()}>
            重新读取
          </button>
        </div>
      ) : null}

      {status.data ? (
        <>
          <div
            className={
              isConfigured
                ? "model-status-banner model-status-banner--ok"
                : "model-status-banner model-status-banner--warning"
            }
          >
            <strong>
              {isConfigured
                ? "OpenAI API Key 已配置"
                : "OpenAI API Key 未配置"}
            </strong>
            <span>
              {isConfigured
                ? "密钥仅保存在本地服务端，不会返回浏览器。"
                : "请在本地 .env 配置密钥；离线项目与任务功能仍可使用。"}
            </span>
          </div>

          <dl className="model-facts">
            <div>
              <dt>规划与评审</dt>
              <dd>{status.data.review_model}</dd>
            </div>
            <div>
              <dt>图片生成</dt>
              <dd>{status.data.image_model}</dd>
            </div>
            <div>
              <dt>请求策略</dt>
              <dd>
                {status.data.timeout_seconds} 秒 / {status.data.max_retries} 次瞬时重试
              </dd>
            </div>
          </dl>

          <label className="model-test-option">
            <input
              type="checkbox"
              checked={includeImage}
              disabled={!isConfigured || availability.isPending}
              onChange={(event) => setIncludeImage(event.target.checked)}
            />
            <span>
              <strong>同时测试图像模型</strong>
              <small>默认只测试规划模型；图像模型测试会产生 API 费用。</small>
            </span>
          </label>

          <button
            className="model-test-button"
            type="button"
            disabled={!isConfigured || availability.isPending}
            onClick={() => availability.mutate(includeImage)}
          >
            {availability.isPending ? "正在测试…" : "测试模型连接"}
          </button>
        </>
      ) : null}

      {availability.isError ? (
        <p className="model-test-error">
          模型测试请求失败，请检查本地服务日志后重试。
        </p>
      ) : null}

      {availability.data ? (
        <ul className="model-check-list">
          {availability.data.checks.map((check) => (
            <ModelCheck
              key={`${check.capability}-${check.model}`}
              check={check}
            />
          ))}
        </ul>
      ) : null}
    </section>
  );
}
