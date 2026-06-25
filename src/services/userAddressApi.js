// =========================================================
// Forkly — User address API (saved delivery addresses)
//
// Saved addresses are owned by the USER SERVICE (Izzuwan module). This file is
// the single integration point — currently backed by mock data. The Order
// Service (Hanif) only stores a *snapshot* of the chosen address with the order;
// it does not own the address book.
// =========================================================

import { config } from '../config.js'
import { MOCK_SAVED_ADDRESSES } from '../data/addresses.js'

/**
 * Get the signed-in user's saved addresses.
 * @param {string} [userId]
 * @returns {Promise<Array>}
 */
export async function getSavedAddresses(userId) {
  // TODO: Fetch user saved addresses from User Service
  // Future REST: GET /users/{userId}/addresses
  // Future gRPC: UserService.GetSavedAddresses(userId)
  void config // marks where the gateway base URL will be used
  void userId
  return Promise.resolve(MOCK_SAVED_ADDRESSES)
}

/**
 * Persist a new address to the user's profile.
 * @param {string} userId
 * @param {object} address
 * @returns {Promise<object>} the saved address (with id)
 */
export async function saveAddress(userId, address) {
  // TODO: Save address to User Service
  // REST: POST /users/{userId}/addresses
  // gRPC: UserService.SaveAddress(address)
  void config
  void userId
  // Echo back a shape resembling what the service would return.
  return Promise.resolve({ ...address, id: 'addr-' + Math.random().toString(36).slice(2, 8) })
}
