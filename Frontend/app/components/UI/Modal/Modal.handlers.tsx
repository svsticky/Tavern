/**
 * Processes keyboard events to handle modal interactions, specifically looking
 * for the "Escape" key to trigger the closing of a modal.
 *
 * @param {KeyboardEvent} event - The keyboard event object from the browser.
 * @param {function} onClose - The callback function to execute when the Escape key is pressed.
 */
export const handleModalKeyDown = (
  event: KeyboardEvent,
  onClose: () => void,
) => {
  if (event.key === "Escape") {
    onClose();
  }
};

/**
 * A factory function that creates a scoped keyboard event handler for a specific modal instance.
 * This is useful for adding and removing event listeners in a clean, reusable way
 * within functional components (e.g., inside a useEffect).
 *
 * @param {function} onClose - The callback function that should run when the modal needs to close.
 * @returns {function} A function that accepts a KeyboardEvent and handles it via handleModalKeyDown.
 */
export const createModalKeyDownHandler = (onClose: () => void) => {
  return (event: KeyboardEvent) => handleModalKeyDown(event, onClose);
};
