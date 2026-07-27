import { client } from "./client.gen";
import type {
  PostRegistrationDocumentDto,
  RegistrationDocumentResponseDto,
  RegistrationDocumentUpdateDto,
} from "./registrationDocuments.types";

export const getRegistrationdocuments = async () => {
  return client.get<RegistrationDocumentResponseDto[]>({
    responseType: "json",
    security: [{ scheme: "bearer", type: "http" }],
    url: "/registrationdocuments",
  });
};

export const postRegistrationdocuments = async (options: {
  body: PostRegistrationDocumentDto;
}) => {
  return client.post<RegistrationDocumentResponseDto>({
    responseType: "json",
    security: [{ scheme: "bearer", type: "http" }],
    url: "/registrationdocuments",
    body: options.body,
    headers: {
      "Content-Type": "application/json",
    },
  });
};

export const putRegistrationdocumentsById = async (options: {
  path: { id: number };
  body: RegistrationDocumentUpdateDto;
}) => {
  return client.put<void>({
    security: [{ scheme: "bearer", type: "http" }],
    url: `/registrationdocuments/${options.path.id}`,
    body: options.body,
    headers: {
      "Content-Type": "application/json",
    },
  });
};

export const deleteRegistrationdocumentsById = async (options: {
  path: { id: number };
}) => {
  return client.delete<void>({
    security: [{ scheme: "bearer", type: "http" }],
    url: `/registrationdocuments/${options.path.id}`,
  });
};
