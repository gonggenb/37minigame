import { useProjectCostsQuery } from "../api/costs";

export interface CostSummaryCardProps {
  projectId?: string;
}

export function CostSummaryCard({ projectId }: CostSummaryCardProps) {
  const costs = useProjectCostsQuery(projectId);

  return (
    <section className="paper-card paper-card--costs">
      <p className="paper-card__label">API 用量</p>
      <h2>本地费用台账</h2>
      {!projectId ? (
        <p className="empty-state">创建项目后显示模型调用汇总。</p>
      ) : null}
      {costs.isPending && projectId ? <p>正在汇总本地脱敏记录…</p> : null}
      {costs.isError ? <p className="model-test-error">费用记录读取失败。</p> : null}
      {costs.data ? (
        <>
          <div className="cost-summary-total">
            <strong>${costs.data.known_cost_usd.toFixed(4)}</strong>
            <span>{costs.data.request_count} 次已记录模型调用</span>
          </div>
          {costs.data.unknown_cost_count > 0 ? (
            <div className="cost-unknown-note">
              <strong>{costs.data.unknown_cost_count} 条费用未知记录</strong>
              <span>供应商未返回可换算金额时不会伪造精确金额。</span>
            </div>
          ) : (
            <p>当前没有费用未知记录。</p>
          )}
          <div className="cost-breakdown-grid">
            {costs.data.by_model.map((item) => (
              <div key={item.key}>
                <strong>{item.key}</strong>
                <span>{item.request_count} 次 · ${item.known_cost_usd.toFixed(4)}</span>
              </div>
            ))}
          </div>
        </>
      ) : null}
    </section>
  );
}
