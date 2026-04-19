import { jsPDF } from "jspdf";

/**
 * Generates a PDF document in A3 format from an array of image URLs. Each image is added to a new page in the PDF,
 * and the resulting document is saved as "document-a3.pdf". The images are added with a "FAST" compression method for better performance.
 * @param images An array of image URLs to be included in the PDF document.
 * @returns A promise that resolves when the PDF has been generated and saved.
 */
export const generateA3Pdf = async (imageUrls: string[], token: string): Promise<void> => {
  const pdf = new jsPDF({
    orientation: 'p',
    unit: 'mm',
    format: 'a3'
  });

  const width = pdf.internal.pageSize.getWidth();
  const height = pdf.internal.pageSize.getHeight();

  for (let i = 0; i < imageUrls.length; i++) {
    try {
      const response = await fetch(imageUrls[i], {
        headers: {
          'Authorization': `Bearer ${token}`
        }
      });

      if (!response.ok) continue;

      const blob = await response.blob();
      const base64Data = await new Promise<string>((resolve, reject) => {
        const reader = new FileReader();
        reader.onloadend = () => resolve(reader.result as string);
        reader.onerror = reject;
        reader.readAsDataURL(blob);
      });

      if (i > 0) {
        pdf.addPage('a3', 'p');
      }

      pdf.addImage(base64Data, 'JPEG', 0, 0, width, height, undefined, 'FAST');
    } catch (error) {
      console.error(error);
    }
  }

  pdf.save("a3.pdf");
};