export interface InventoryItem {
  productId: string;
  productName: string;
  availableStock: number;
  reservedStock: number;
  totalStock: number;
}

export type HoldStatus = 'Active' | 'Released' | 'Expired';

export interface Hold {
  holdId: string;
  productId: string;
  customerId?: string;
  quantity: number;
  status: HoldStatus;
  createdAt: string;
  expiresAt: string;
  releasedAt?: string;
  minutesRemaining: number;
}

export interface CreateHoldRequest {
  productId: string;
  quantity: number;
  customerId?: string;
}

export interface ApiError {
  code: string;
  message: string;
}
