using System;
using System.Collections.Generic;

// Field names must match the backend JSON exactly. The server rejects unknown request fields with 400 INVALID_JSON.
namespace MaxionAssignment.Backend
{
    [Serializable] public class HealthResponse { public string status; }

    [Serializable] public class GuestLoginRequest { public string deviceId; }
    [Serializable] public class EmailCredentialsRequest { public string email; public string password; }

    [Serializable]
    public class AuthResponse
    {
        public string userId;
        public string token;
        public string expiresAt;
        public string accountType;
    }

    [Serializable]
    public class MeResponse
    {
        public string userId;
        public string accountType;
        public string email;
        public string createdAt;
    }

    [Serializable] public class Product { public string id; public string name; public double price; }
    [Serializable] public class ProductListResponse { public List<Product> products; }

    [Serializable] public class CreateOrderRequest { public string productId; public int quantity; }

    [Serializable]
    public class OrderResponse
    {
        public string id;
        public string productId;
        public int quantity;
        public double total;
    }

    [Serializable]
    public class OrderHistoryItem
    {
        public string id;
        public string productId;
        public int quantity;
        public double unitPrice;
        public double total;
        public string createdAt;
    }

    [Serializable] public class OrderListResponse { public List<OrderHistoryItem> orders; }

    [Serializable] public class ApiErrorDetail { public string code; public string message; }
    [Serializable] public class ApiErrorResponse { public ApiErrorDetail error; }
}
