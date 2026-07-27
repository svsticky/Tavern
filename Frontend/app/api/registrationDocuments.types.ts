export type RegistrationDocumentResponseDto = {
  id: number;
  nameDutch: string;
  nameEnglish: string;
  url: string;
  sortOrder: number;
};

export type PostRegistrationDocumentDto = {
  nameDutch: string;
  nameEnglish: string;
  url: string;
  sortOrder?: number;
};

export type RegistrationDocumentUpdateDto = {
  nameDutch: string;
  nameEnglish: string;
  url: string;
  sortOrder?: number;
};
