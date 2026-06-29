// Mock saved delivery addresses. In production these belong to the User Service
// (Izzuwan module) and are fetched per user — see services/userAddressApi.js.
//
// address: { id, label, isDefault, recipientName, phone,
//            addressLine1, addressLine2, postcode, city, state, notes }
export const MOCK_SAVED_ADDRESSES = [
  {
    id: 'addr-home',
    label: 'Home',
    isDefault: true,
    recipientName: 'Ahmad Faizal',
    phone: '012-345 6789',
    addressLine1: 'No. 8, Jalan ABC',
    addressLine2: '',
    postcode: '43000',
    city: 'Kajang',
    state: 'Selangor',
    notes: '',
  },
  {
    id: 'addr-office',
    label: 'Office',
    isDefault: false,
    recipientName: 'Ahmad Faizal',
    phone: '012-345 6789',
    addressLine1: 'Level 8, Wisma XYZ',
    addressLine2: '',
    postcode: '63000',
    city: 'Cyberjaya',
    state: 'Selangor',
    notes: '',
  },
]
