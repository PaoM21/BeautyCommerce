export interface DashboardOrder {
  id: string;
  orderNumber: string;
  customer: string;
  total: number;
  createdAt: string;
}

export interface MonthlySales {
  month: string;
  total: number;
}

export interface Dashboard {
  totalProducts: number;
  totalOrders: number;
  totalCustomers: number;
  totalSales: number;
  salesThisMonth: number;
  pendingOrders: number;
  lowStockProducts: number;
  outOfStockProducts: number;
  lastOrders: DashboardOrder[];
  salesByMonth: MonthlySales[];
}
