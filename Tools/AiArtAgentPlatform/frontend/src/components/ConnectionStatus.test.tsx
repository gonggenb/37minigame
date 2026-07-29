import { fireEvent, render, screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";

import { ConnectionStatus } from "./ConnectionStatus";

describe("ConnectionStatus", () => {
  it("shows a connecting message while health is loading", () => {
    render(<ConnectionStatus state="loading" />);

    expect(screen.getByText("正在连接本地服务")).toBeInTheDocument();
  });

  it("shows the connected state", () => {
    render(<ConnectionStatus state="connected" />);

    expect(screen.getByText("本地服务已连接")).toBeInTheDocument();
  });

  it("shows a retry action when the service is unavailable", () => {
    const onRetry = vi.fn();
    render(<ConnectionStatus state="error" onRetry={onRetry} />);

    expect(screen.getByText("本地服务不可用")).toBeInTheDocument();
    fireEvent.click(screen.getByRole("button", { name: "重新连接" }));
    expect(onRetry).toHaveBeenCalledOnce();
  });
});
