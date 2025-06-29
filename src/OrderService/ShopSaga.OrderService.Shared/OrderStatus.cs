namespace ShopSaga.OrderService.Shared
{
    public static class OrderStatus
    {
        public const string Created = "Created";
        public const string StockPending = "StockPending";
        public const string StockReserved = "StockReserved";
        public const string StockConfirmed = "StockConfirmed";
        public const string PaymentPending = "PaymentPending";
        public const string PaymentCompleted = "PaymentCompleted";
        public const string Completed = "Completed";
        public const string StockCancelled = "StockCancelled";
        public const string StockExpired = "StockExpired";
        public const string Cancelled = "Cancelled";
        public const string PaymentFailed = "PaymentFailed";
        public const string Refunded = "Refunded";
        public const string ManualIntervention = "ManualIntervention";
        
        public static bool IsPrePivot(string status)
        {
            return status == Created || 
                   status == StockPending || 
                   status == StockReserved;
        }
        
        public static bool IsPostPivot(string status)
        {
            return status == StockConfirmed || 
                   status == PaymentPending || 
                   status == PaymentCompleted || 
                   status == Completed ||
                   status == PaymentFailed ||
                   status == Refunded ||
                   status == ManualIntervention;
        }
        
        public static bool IsFinalState(string status)
        {
            return status == Completed || 
                   status == Cancelled || 
                   status == StockCancelled ||
                   status == StockExpired ||
                   status == Refunded ||
                   status == ManualIntervention;
        }
        
        public static bool CanProcessPayment(string status)
        {
            return status == StockConfirmed;
        }
    }
}
