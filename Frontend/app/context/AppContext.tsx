import React, {
  createContext,
  type ReactNode,
  useContext,
  useState,
} from "react";
import type { MemberResponseDto } from "~/api";

/**
 * The shape of the global application context state.
 * @interface AppContextType
 * @property {number | null} boardGroupId - The ID of the currently active board group.
 * @property {(id: number) => void} setBoardGroupId - Updates the current board group ID.
 * @property {number | null} candidateBoardGroupId - The ID of a board group currently under consideration (e.g., during an application process).
 * @property {(id: number) => void} setCandidateBoardGroupId - Updates the candidate board group ID.
 * @property {MemberResponseDto | null} member - The profile data for the currently authenticated member.
 * @property {React.Dispatch<React.SetStateAction<MemberResponseDto | null>>} setMember - Updates the member state.
 */
interface AppContextType {
  boardGroupId: number | null;
  setBoardGroupId: (id: number) => void;
  candidateBoardGroupId: number | null;
  setCandidateBoardGroupId: (id: number) => void;
  member: MemberResponseDto | null;
  setMember: React.Dispatch<React.SetStateAction<MemberResponseDto | null>>;
}

/**
 * Internal context object for the application state.
 * Initialized as undefined to enforce the use of the Provider.
 */
const AppContext = createContext<AppContextType | undefined>(undefined);

/**
 * Global State Provider that wraps the application.
 *
 * This provider manages high-level state that needs to be accessed across various
 * routes and component trees, such as the current member's profile and
 * specific group identifiers used for filtering or context-aware logic.
 *
 * @component
 * @param {Object} props - The component properties.
 * @param {ReactNode} props.children - The component tree that will have access to the context.
 */
export const AppProvider = ({ children }: { children: ReactNode }) => {
  const [boardGroupId, setBoardGroupId] = useState<number | null>(null);
  const [candidateBoardGroupId, setCandidateBoardGroupId] = useState<
    number | null
  >(null);
  const [member, setMember] = useState<MemberResponseDto | null>(null);

  return (
    <AppContext.Provider
      value={{
        boardGroupId,
        setBoardGroupId,
        candidateBoardGroupId,
        setCandidateBoardGroupId,
        member,
        setMember,
      }}
    >
      {children}
    </AppContext.Provider>
  );
};

/**
 * Custom hook to access the application context.
 *
 * Provides a convenient way for functional components to consume global state.
 * It includes a safety check to ensure that the hook is only used within
 * an `AppProvider` hierarchy.
 *
 * @throws {Error} If used outside of an AppProvider.
 * @returns {AppContextType} The current application context value.
 *
 * @example
 * const { member, setMember } = useApp();
 */
export const useApp = () => {
  const context = useContext(AppContext);
  if (!context) throw new Error("useApp must be used within AppProvider");
  return context;
};
