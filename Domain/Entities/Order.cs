using Domain.Exceptions;

namespace Domain.Entities
{
    public class Order : SoftDeletableEntity
    {
        public int OrderId { get; set; }
        public string OrderNumber { get; set; } = null!;
        
        public string UserId { get; set; } = null!;
        public User? User { get; set; }
        
        public ICollection<OrderLine> Lines { get; set; } = new List<OrderLine>();
        
        public OrderStatus OrderStatus { get; set; } = OrderStatus.Pending;
        public DateTime OrderedAt { get; set; } = DateTime.UtcNow;
        public DateTime? CancelledAt { get; set; }
        
        public string FirstName { get; set; } = null!;
        public string LastName { get; set; } = null!;
        public string Phone { get; set; } = null!;
        public string City { get; set; } = null!;
        public string District { get; set; } = null!;
        public string AddressLine { get; set; } = null!;
        public string? PostalCode { get; set; }
        
        public decimal SubTotal { get; set; } // İndirim, kargo ve vergi öncesi toplam
        public decimal TaxAmount { get; set; } // Vergi miktarı
        public decimal ShippingCost { get; set; } // Kargo ücreti
        public decimal TotalAmount { get; set; } // İndirim, kargo ve vergi sonrası toplam
        public string Currency { get; set; } = "TRY";
        
        public decimal? TotalDiscountAmount { get; set; } // Toplam indirim miktarı
        public decimal? CouponDiscountAmount { get; set; } // Kupon indirimi
        public decimal? CampaignDiscountTotal { get; set; } // Kampanya indirimleri toplamı
        public string? CouponCode { get; set; } // Kullanılan kupon kodu
        
        public ICollection<OrderCampaign> AppliedCampaigns { get; set; } = new List<OrderCampaign>();
        
        public ShippingMethod ShippingMethod { get; set; } = ShippingMethod.Standard;
        public DateTime? ShippedAt { get; set; }
        public DateTime? DeliveredAt { get; set; }
        public string? TrackingNumber { get; set; }
        public string? ShippingCompanyName { get; set; }
        public string? ShippingServiceName { get; set; }
        
        public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.CreditCard;
        public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Pending;
        public string? PaymentProvider { get; set; }
        public string? PaymentTransactionId { get; set; }
        public DateTime? PaidAt { get; set; }
        
        // Payment Details from Iyzico
        public string? CardType { get; set; } // CREDIT_CARD, DEBIT_CARD
        public string? CardAssociation { get; set; } // MASTER_CARD, VISA, TROY, AMEX
        public string? CardFamily { get; set; } // Bonus, Axess, World, Maximum, etc.
        public string? BankName { get; set; } // İş Bankası, Garanti, etc.
        public int? InstallmentCount { get; set; } // 1 = Peşin, 2+ = Taksit
        public string? LastFourDigits { get; set; } // Son 4 hane
        
        public bool GiftWrap { get; set; }
        
        public string? CustomerNotes { get; set; }
        public string? AdminNotes { get; set; }
        
        public ICollection<OrderHistory> History { get; set; } = new List<OrderHistory>();

        #region Validation Methods

        public void ValidateForCreation()
        {
            ValidateOrderNumber();
            ValidateCustomerInfo();
            ValidateAddress();
            ValidatePricing();
            ValidateLines();
        }

        private void ValidateOrderNumber()
        {
            if (string.IsNullOrWhiteSpace(OrderNumber))
            {
                throw new OrderValidationException("Sipariş numarası boş olamaz.");
            }

            if (OrderNumber.Length < 5 || OrderNumber.Length > 50)
            {
                throw new OrderValidationException("Sipariş numarası 5-50 karakter arasında olmalıdır.");
            }
        }

        private void ValidateCustomerInfo()
        {
            if (string.IsNullOrWhiteSpace(FirstName))
            {
                throw new OrderValidationException("Ad boş olamaz.");
            }

            if (FirstName.Length < 2 || FirstName.Length > 100)
            {
                throw new OrderValidationException("Ad 2-100 karakter arasında olmalıdır.");
            }

            if (string.IsNullOrWhiteSpace(LastName))
            {
                throw new OrderValidationException("Soyad boş olamaz.");
            }

            if (LastName.Length < 2 || LastName.Length > 100)
            {
                throw new OrderValidationException("Soyad 2-100 karakter arasında olmalıdır.");
            }

            if (string.IsNullOrWhiteSpace(Phone))
            {
                throw new OrderValidationException("Telefon numarası boş olamaz.");
            }

            if (string.IsNullOrWhiteSpace(UserId))
            {
                throw new OrderValidationException("Kullanıcı ID boş olamaz.");
            }
        }

        public void ValidateAddress()
        {
            if (string.IsNullOrWhiteSpace(City))
            {
                throw new OrderValidationException("Şehir boş olamaz.");
            }

            if (City.Length < 2 || City.Length > 100)
            {
                throw new OrderValidationException("Şehir 2-100 karakter arasında olmalıdır.");
            }

            if (string.IsNullOrWhiteSpace(District))
            {
                throw new OrderValidationException("İlçe boş olamaz.");
            }

            if (District.Length < 2 || District.Length > 100)
            {
                throw new OrderValidationException("İlçe 2-100 karakter arasında olmalıdır.");
            }

            if (string.IsNullOrWhiteSpace(AddressLine))
            {
                throw new OrderValidationException("Adres detayı boş olamaz.");
            }

            if (AddressLine.Length < 10 || AddressLine.Length > 500)
            {
                throw new OrderValidationException("Adres detayı 10-500 karakter arasında olmalıdır.");
            }
        }

        public void ValidatePricing()
        {
            if (SubTotal < 0)
            {
                throw new OrderValidationException("Alt toplam 0'dan küçük olamaz.");
            }

            if (TaxAmount < 0)
            {
                throw new OrderValidationException("Vergi miktarı 0'dan küçük olamaz.");
            }

            if (ShippingCost < 0)
            {
                throw new OrderValidationException("Kargo ücreti 0'dan küçük olamaz.");
            }

            if (TotalAmount < 0)
            {
                throw new OrderValidationException("Toplam tutar 0'dan küçük olamaz.");
            }

            if (TotalDiscountAmount.HasValue && TotalDiscountAmount.Value < 0)
            {
                throw new OrderValidationException("İndirim miktarı 0'dan küçük olamaz.");
            }

            if (CouponDiscountAmount.HasValue && CouponDiscountAmount.Value < 0)
            {
                throw new OrderValidationException("Kupon indirimi 0'dan küçük olamaz.");
            }

            if (CampaignDiscountTotal.HasValue && CampaignDiscountTotal.Value < 0)
            {
                throw new OrderValidationException("Kampanya indirimi 0'dan küçük olamaz.");
            }
        }

        private void ValidateLines()
        {
            if (Lines == null || !Lines.Any())
            {
                throw new OrderValidationException("Sipariş en az bir ürün içermelidir.");
            }

            foreach (var line in Lines)
            {
                line.ValidateForCreation();
            }
        }

        #endregion

        #region Business Logic Methods

        public void CalculateTotals()
        {
            SubTotal = Lines.Sum(l => l.LineTotal);

            decimal totalDiscount = 0;
            
            if (CouponDiscountAmount.HasValue)
            {
                totalDiscount += CouponDiscountAmount.Value;
            }

            if (CampaignDiscountTotal.HasValue)
            {
                totalDiscount += CampaignDiscountTotal.Value;
            }

            TotalDiscountAmount = totalDiscount > 0 ? totalDiscount : null;

            TotalAmount = SubTotal - (TotalDiscountAmount ?? 0) + TaxAmount + ShippingCost;

            if (TotalAmount < 0)
            {
                TotalAmount = 0;
            }
        }

        public void MarkAsPaid(string transactionId, string provider, string? cardType = null, 
            string? cardAssociation = null, string? cardFamily = null, string? bankName = null, 
            int? installmentCount = null, string? lastFourDigits = null)
        {
            if (PaymentStatus == PaymentStatus.Completed)
            {
                throw new OrderValidationException("Sipariş zaten ödenmiş.");
            }

            if (string.IsNullOrWhiteSpace(transactionId))
            {
                throw new OrderValidationException("İşlem ID boş olamaz.");
            }

            PaymentStatus = PaymentStatus.Completed;
            PaymentTransactionId = transactionId;
            PaymentProvider = provider;
            PaidAt = DateTime.UtcNow;
            
            CardType = cardType;
            CardAssociation = cardAssociation;
            CardFamily = cardFamily;
            BankName = bankName;
            InstallmentCount = installmentCount;
            LastFourDigits = lastFourDigits;
            
            if (OrderStatus == OrderStatus.Pending)
            {
                OrderStatus = OrderStatus.Processing;
            }
        }

        public void MarkAsShipped(string trackingNumber, string? companyName = null, string? serviceName = null)
        {
            if (OrderStatus == OrderStatus.Cancelled)
            {
                throw new OrderValidationException("İptal edilmiş sipariş gönderilemez.");
            }

            if (PaymentStatus != PaymentStatus.Completed && PaymentMethod != PaymentMethod.CashOnDelivery)
            {
                throw new OrderValidationException("Ödenmemiş sipariş gönderilemez.");
            }

            if (string.IsNullOrWhiteSpace(trackingNumber))
            {
                throw new OrderValidationException("Takip numarası boş olamaz.");
            }

            OrderStatus = OrderStatus.Shipped;
            ShippedAt = DateTime.UtcNow;
            TrackingNumber = trackingNumber;
            ShippingCompanyName = companyName;
            ShippingServiceName = serviceName;
        }

        public void MarkAsDelivered()
        {
            if (OrderStatus != OrderStatus.Shipped)
            {
                throw new OrderValidationException("Sadece gönderilmiş siparişler teslim edilebilir.");
            }

            OrderStatus = OrderStatus.Delivered;
            DeliveredAt = DateTime.UtcNow;
        }

        public bool CanBeCancelled()
        {
            return OrderStatus == OrderStatus.Pending || OrderStatus == OrderStatus.Processing;
        }

        public void Cancel(string reason)
        {
            if (!CanBeCancelled())
            {
                throw new OrderValidationException($"Bu durumda sipariş iptal edilemez: {OrderStatus}");
            }

            OrderStatus = OrderStatus.Cancelled;
            CancelledAt = DateTime.UtcNow;
            
            if (!string.IsNullOrWhiteSpace(AdminNotes))
            {
                AdminNotes += $"\n[İptal - {DateTime.UtcNow:yyyy-MM-dd HH:mm}] {reason}";
            }
            else
            {
                AdminNotes = $"[İptal - {DateTime.UtcNow:yyyy-MM-dd HH:mm}] {reason}";
            }
        }

        public bool CanBeRefunded()
        {
            if (OrderStatus != OrderStatus.Delivered || PaymentStatus != PaymentStatus.Completed)
            {
                return false;
            }

            if (DeliveredAt.HasValue)
            {
                var daysSinceDelivery = (DateTime.UtcNow - DeliveredAt.Value).TotalDays;
                return daysSinceDelivery <= 7;
            }

            return false;
        }

        public void MarkAsRefunded()
        {
            if (!CanBeRefunded())
            {
                throw new OrderValidationException("Bu sipariş için iade yapılamaz.");
            }

            PaymentStatus = PaymentStatus.Refunded;
        }

        public void MarkAsFailed(string failureReason)
        {
            OrderStatus = OrderStatus.Failed;
            PaymentStatus = PaymentStatus.Failed;
            
            if (!string.IsNullOrWhiteSpace(AdminNotes))
            {
                AdminNotes += $"\n[Ödeme Başarısız - {DateTime.UtcNow:yyyy-MM-dd HH:mm}] {failureReason}";
            }
            else
            {
                AdminNotes = $"[Ödeme Başarısız - {DateTime.UtcNow:yyyy-MM-dd HH:mm}] {failureReason}";
            }
        }


        public void SoftDelete(string deletedByUserId)
        {
            IsDeleted = true;
            DeletedAt = DateTime.UtcNow;
            DeletedByUserId = deletedByUserId;
        }

        #endregion
    }

    public enum OrderStatus
    {
        Pending,        // Beklemede
        Processing,     // İşleniyor
        Shipped,        // Kargoya verildi
        Delivered,      // Teslim edildi
        Cancelled,      // İptal edildi
        Returned,       // İade edildi
        Failed         // Başarısız
    }

    public enum ShippingMethod
    {
        Standard,       // Standart kargo
        Express,        // Hızlı kargo
        HandlingOnly    // Sadece hazırlama (mağazadan teslim vb.)
    }

    public enum PaymentMethod
    {
        CreditCard,         // Kredi/Banka Kartı
        BankTransfer,       // Havale/EFT
        CashOnDelivery      // Kapıda Ödeme
    }

    public enum PaymentStatus
    {
        Pending,    // Ödeme bekleniyor
        Completed,  // Ödeme tamamlandı
        Failed,     // Ödeme başarısız
        Refunded    // İade edildi
    }
}