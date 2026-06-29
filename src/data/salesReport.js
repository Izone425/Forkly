// =========================================================
// Forkly — MOCK monthly sales report data (admin report page).
//
// Shape mirrors what the Order Service report endpoint will return later
// (GET /api/orders/reports/monthly). Each month lists per-menu-item units sold;
// sales = unitPrice * qty is computed in the view, so this stays simple.
//
// Replace with the live API by setting VITE_ORDER_API_BASE — see services/reportApi.js.
// =========================================================

export const SALES_REPORT = {
  currency: 'MYR',
  generatedAt: '2026-06-30',
  months: [
    {
      month: '2026-01', label: 'Jan', orderCount: 360,
      items: [
        { name: 'Classic Burger', unitPrice: 10, qty: 280 },
        { name: 'Chicken Wings', unitPrice: 12, qty: 190 },
        { name: 'Fries', unitPrice: 5, qty: 420 },
        { name: 'Ice Cream Sundae', unitPrice: 7, qty: 130 },
        { name: 'Coffee', unitPrice: 6, qty: 350 },
        { name: 'Soft Drink', unitPrice: 4, qty: 260 },
      ],
    },
    {
      month: '2026-02', label: 'Feb', orderCount: 380,
      items: [
        { name: 'Classic Burger', unitPrice: 10, qty: 300 },
        { name: 'Chicken Wings', unitPrice: 12, qty: 205 },
        { name: 'Fries', unitPrice: 5, qty: 440 },
        { name: 'Ice Cream Sundae', unitPrice: 7, qty: 140 },
        { name: 'Coffee', unitPrice: 6, qty: 365 },
        { name: 'Soft Drink', unitPrice: 4, qty: 275 },
      ],
    },
    {
      month: '2026-03', label: 'Mar', orderCount: 405,
      items: [
        { name: 'Classic Burger', unitPrice: 10, qty: 330 },
        { name: 'Chicken Wings', unitPrice: 12, qty: 220 },
        { name: 'Fries', unitPrice: 5, qty: 470 },
        { name: 'Ice Cream Sundae', unitPrice: 7, qty: 150 },
        { name: 'Coffee', unitPrice: 6, qty: 390 },
        { name: 'Soft Drink', unitPrice: 4, qty: 290 },
      ],
    },
    {
      month: '2026-04', label: 'Apr', orderCount: 430,
      items: [
        { name: 'Classic Burger', unitPrice: 10, qty: 360 },
        { name: 'Chicken Wings', unitPrice: 12, qty: 240 },
        { name: 'Fries', unitPrice: 5, qty: 500 },
        { name: 'Ice Cream Sundae', unitPrice: 7, qty: 165 },
        { name: 'Coffee', unitPrice: 6, qty: 410 },
        { name: 'Soft Drink', unitPrice: 4, qty: 305 },
      ],
    },
    {
      month: '2026-05', label: 'May', orderCount: 455,
      items: [
        { name: 'Classic Burger', unitPrice: 10, qty: 390 },
        { name: 'Chicken Wings', unitPrice: 12, qty: 255 },
        { name: 'Fries', unitPrice: 5, qty: 530 },
        { name: 'Ice Cream Sundae', unitPrice: 7, qty: 175 },
        { name: 'Coffee', unitPrice: 6, qty: 435 },
        { name: 'Soft Drink', unitPrice: 4, qty: 320 },
      ],
    },
    {
      month: '2026-06', label: 'Jun', orderCount: 480,
      items: [
        { name: 'Classic Burger', unitPrice: 10, qty: 420 },
        { name: 'Chicken Wings', unitPrice: 12, qty: 275 },
        { name: 'Fries', unitPrice: 5, qty: 560 },
        { name: 'Ice Cream Sundae', unitPrice: 7, qty: 185 },
        { name: 'Coffee', unitPrice: 6, qty: 460 },
        { name: 'Soft Drink', unitPrice: 4, qty: 340 },
      ],
    },
  ],
}
