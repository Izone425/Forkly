// Mock order history. Each order matches the agreed data structure so it can be
// swapped for real API data later (see services/orderHistoryApi.js).
//
// order:     { orderId, userId, orderDate, status, totalAmount, orderItems }
// orderItem: { menuId, menuName, quantity, price }
//
// `totalAmount` is the grand total (RM). Item count is derived from orderItems.
export const MOCK_ORDER_HISTORY = [
  {
    orderId: 'ORD-000104',
    userId: 'u-001',
    orderDate: '2026-06-20T19:12:00',
    status: 'Preparing',
    totalAmount: 30.0,
    orderItems: [
      { menuId: 'burger', menuName: 'Classic Burger', quantity: 1, price: 10 },
      { menuId: 'wings', menuName: 'Chicken Wings', quantity: 1, price: 12 },
      { menuId: 'soda', menuName: 'Soft Drink', quantity: 2, price: 4 },
    ],
  },
  {
    orderId: 'ORD-000103',
    userId: 'u-001',
    orderDate: '2026-06-15T12:05:00',
    status: 'Completed',
    totalAmount: 16.0,
    orderItems: [
      { menuId: 'fries', menuName: 'Fries', quantity: 2, price: 5 },
      { menuId: 'coffee', menuName: 'Coffee', quantity: 1, price: 6 },
    ],
  },
  {
    orderId: 'ORD-000102',
    userId: 'u-001',
    orderDate: '2026-06-12T13:35:00',
    status: 'Completed',
    totalAmount: 25.0,
    orderItems: [
      { menuId: 'burger', menuName: 'Classic Burger', quantity: 2, price: 10 },
      { menuId: 'fries', menuName: 'Fries', quantity: 1, price: 5 },
    ],
  },
]
