import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import type { MemberResponseDto } from "~/api";
import { AppProvider, useApp } from "~/context/AppContext";

function Consumer() {
  const {
    boardGroupId,
    setBoardGroupId,
    candidateBoardGroupId,
    setCandidateBoardGroupId,
    financialYearStartDate,
    setFinancialYearStartDate,
    committeeCreationDate,
    setCommitteeCreationDate,
    member,
    setMember,
  } = useApp();

  return (
    <div>
      <span data-testid="boardGroupId">{String(boardGroupId)}</span>
      <span data-testid="candidateBoardGroupId">
        {String(candidateBoardGroupId)}
      </span>
      <span data-testid="financialYearStartDate">
        {String(financialYearStartDate)}
      </span>
      <span data-testid="committeeCreationDate">
        {String(committeeCreationDate)}
      </span>
      <span data-testid="member">{member ? member.firstName : "null"}</span>
      <button type="button" onClick={() => setBoardGroupId(5)}>
        setBoardGroupId
      </button>
      <button type="button" onClick={() => setCandidateBoardGroupId(6)}>
        setCandidateBoardGroupId
      </button>
      <button
        type="button"
        onClick={() => setFinancialYearStartDate("2024-01-01")}
      >
        setFinancialYearStartDate
      </button>
      <button
        type="button"
        onClick={() => setCommitteeCreationDate("2023-01-01")}
      >
        setCommitteeCreationDate
      </button>
      <button
        type="button"
        onClick={() => setMember({ firstName: "Jane" } as MemberResponseDto)}
      >
        setMember
      </button>
    </div>
  );
}

describe("useApp", () => {
  it("throws when used outside of an AppProvider", () => {
    // Suppress the expected React error boundary console noise for this negative test.
    const consoleError = vi
      .spyOn(console, "error")
      .mockImplementation(() => {});

    expect(() => render(<Consumer />)).toThrow(
      "useApp must be used within AppProvider",
    );

    consoleError.mockRestore();
  });
});

describe("AppProvider", () => {
  it("provides default null state", () => {
    render(
      <AppProvider>
        <Consumer />
      </AppProvider>,
    );

    expect(screen.getByTestId("boardGroupId")).toHaveTextContent("null");
    expect(screen.getByTestId("candidateBoardGroupId")).toHaveTextContent(
      "null",
    );
    expect(screen.getByTestId("financialYearStartDate")).toHaveTextContent(
      "null",
    );
    expect(screen.getByTestId("committeeCreationDate")).toHaveTextContent(
      "null",
    );
    expect(screen.getByTestId("member")).toHaveTextContent("null");
  });

  it("updates state via the exposed setters", async () => {
    const user = userEvent.setup();

    render(
      <AppProvider>
        <Consumer />
      </AppProvider>,
    );

    await user.click(screen.getByText("setBoardGroupId"));
    expect(screen.getByTestId("boardGroupId")).toHaveTextContent("5");

    await user.click(screen.getByText("setCandidateBoardGroupId"));
    expect(screen.getByTestId("candidateBoardGroupId")).toHaveTextContent("6");

    await user.click(screen.getByText("setFinancialYearStartDate"));
    expect(screen.getByTestId("financialYearStartDate")).toHaveTextContent(
      "2024-01-01",
    );

    await user.click(screen.getByText("setCommitteeCreationDate"));
    expect(screen.getByTestId("committeeCreationDate")).toHaveTextContent(
      "2023-01-01",
    );

    await user.click(screen.getByText("setMember"));
    expect(screen.getByTestId("member")).toHaveTextContent("Jane");
  });
});
