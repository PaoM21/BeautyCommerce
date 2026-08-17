import { api } from "./api";

export interface DriveSyncMatch {
  productId: string;
  productName: string;
  sourceFile: string;
  imageUrl: string;
  isPrimary: boolean;
}

export interface DriveSyncResult {
  updatedProducts: DriveSyncMatch[];
  unmatchedFiles: string[];
  errors: string[];
  filesProcessed: number;
}

export async function syncImagesFromDrive(): Promise<DriveSyncResult> {
  const response = await api.post<DriveSyncResult>(
    "/media/sync-drive-images"
  );

  return response.data;
}
